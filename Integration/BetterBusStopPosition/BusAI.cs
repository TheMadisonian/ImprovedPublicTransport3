using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;
using ImprovedPublicTransport.OptionsFramework;
using ImprovedPublicTransport.Util;

namespace BetterBusStopPosition
{

[HarmonyPatch(typeof(BusAI))]
public static class BusAI_Patch
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(CalculateSegmentPosition))]
    public static IEnumerable<CodeInstruction> CalculateSegmentPosition(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = new List<CodeInstruction>(instructions);
        bool found1 = false;
        bool found2 = false;
        LocalBuilder localFlags = null;
        for( int i = 0; i < codes.Count; ++i )
        {
            // The function has code:
            // NetInfo.Lane lane = info.m_lanes[position.m_lane];
            // Verify that the result is stored in loc #2.
            if( codes[ i ].opcode == OpCodes.Ldloc_2
                && i + 1 < codes.Count
                && codes[ i + 1 ].opcode == OpCodes.Ldfld && codes[ i + 1 ].operand.ToString() == "System.Single m_stopOffset" )
            {
                found1 = true;
            }
            // The function has code:
            // if ((instance.m_segments.m_buffer[position.m_segment].m_flags & NetSegment.Flags.Invert) != 0)
            // Store the result of the condition.
            if( codes[ i ].opcode == OpCodes.Ldfld && codes[ i ].operand.ToString() == "NetSegment+Flags m_flags" )
            {
                codes.Insert( i + 1, new CodeInstruction( OpCodes.Dup ));
                localFlags = generator.DeclareLocal( typeof( NetSegment.Flags ));
                codes.Insert( i + 2, new CodeInstruction( OpCodes.Stloc_S, localFlags.LocalIndex )); // store the result
            }
            // The function has code:
            // instance.m_lanes.m_buffer[laneID].CalculateStopPositionAndDirection((float)(int)offset * 0.003921569f, num, out pos, out dir);
            // Replace entirely with:
            // CalculateModifiedStopPosition(instance.m_lanes.m_buffer[laneID], (float)(int)offset * 0.003921569f,
            //     num, vehicleId, vehicleData, lane, localFlags, out pos, out dir);
            if( found1 && localFlags != null && codes[ i ].opcode == OpCodes.Ldarg_S && codes[ i ].operand.ToString() == "5"
                && i + 7 < codes.Count
                && codes[ i + 2 ].opcode == OpCodes.Ldc_R4
                && codes[ i + 3 ].opcode == OpCodes.Mul
                && codes[ i + 7 ].opcode == OpCodes.Call
                && codes[ i + 7 ].operand.ToString() == "Void CalculateStopPositionAndDirection(Single, Single, Vector3 ByRef, Vector3 ByRef)" )
            {
                // After the Mul at i+3, stack has: [ref NetLane] [laneOffset].
                // Insert the remaining arguments then call our replacement.
                codes.Insert( i + 4, new CodeInstruction( OpCodes.Ldarg_1 )); // vehicleId
                codes.Insert( i + 5, new CodeInstruction( OpCodes.Ldarg_2 )); // vehicleData
                codes.Insert( i + 6, new CodeInstruction( OpCodes.Ldloc_2 )); // lane (loc #2)
                codes.Insert( i + 7, new CodeInstruction( OpCodes.Ldloc_S, localFlags.LocalIndex )); // flags
                // Ldloc_3 (stopOffset) was at original i+4, now shifted to i+4+4=i+8.
                // Ldarg_S 6 (out pos) was at original i+5, now shifted to i+9.
                // Ldarg_S 7 (out dir) was at original i+6, now shifted to i+10.
                // Call CalculateStopPositionAndDirection was at original i+7, now shifted to i+11.
                // Replace the CalculateStopPositionAndDirection call with our replacement.
                codes[ i + 11 ] = new CodeInstruction( OpCodes.Call,
                    typeof( BusAI_Patch ).GetMethod( nameof( CalculateModifiedStopPosition )));
                found2 = true;
                break;
            }
        }
        if( !found1 || !found2 )
            Utils.LogError("BetterBusStopPosition: Failed to patch BusAI.CalculateSegmentPosition()");
        return codes;
    }

    // Replaces NetLane.CalculateStopPositionAndDirection for bus stops.
    // The vanilla CalculateStopPositionAndDirection uses SmootherStep(0.5, 0, |laneOffset - 0.5|)
    // to fade the lateral curb offset, peaking at 1.0 when laneOffset=0.5 (lane centre).
    // BBSP moves the stop forward (targetOffset > 0.5). At targetOffset ~0.7, SmootherStep gives
    // only ~0.64 curb pull, causing buses to swoop — pulling to curb near 0.5 then drifting back.
    // Applies positioning based on selected BBSP logic mode:
    // - Disabled: Vanilla behavior
    // - OriginalLogic: Exact original BBSP code using vanilla method with modified offset
    // - UpdatedLogic: Improved logic with direct Bezier access and full curb displacement
    public static void CalculateModifiedStopPosition( ref NetLane lane, float laneOffset, ushort vehicleID,
        ref Vehicle vehicleData, NetInfo.Lane laneInfo, NetSegment.Flags flags,
        float stopOffset, out Vector3 pos, out Vector3 dir )
    {
        var mode = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.BbspLogic;

        if( mode == (int)ImprovedPublicTransport.Settings.Settings.BbspLogicModes.Disabled )
        {
            // Vanilla behavior: use original offset as-is
            lane.CalculateStopPositionAndDirection( laneOffset, stopOffset, out pos, out dir );
            return;
        }

        if( mode == (int)ImprovedPublicTransport.Settings.Settings.BbspLogicModes.OriginalLogic )
        {
            // Exact original BBSP code: calculate modified offset, pass to vanilla method
            float modifiedOffset = laneOffset; // fallback
            float laneLength = lane.m_length;

            // Return vanilla offset if vehicle is leaving
            if( ( vehicleData.m_flags & Vehicle.Flags.Leaving ) == 0 && laneLength >= 1f )
            {
                float margin = laneLength / 6;
                float vehicleLength = vehicleData.Info.m_generatedInfo != null ? vehicleData.Info.m_generatedInfo.m_size.z : 0f;
                float newStopOffset = 1 - ( margin + vehicleLength / 2 ) / laneLength;

                if( newStopOffset >= 0.5f )
                {
                    bool inverted = ( laneInfo.m_finalDirection & NetInfo.Direction.Backward ) != 0;
                    if(( flags & NetSegment.Flags.Invert ) != 0 )
                        inverted = !inverted;

                    float adjusted = inverted ? ( 1f - laneOffset ) : laneOffset;
                    adjusted *= 2 * newStopOffset;
                    modifiedOffset = inverted ? ( 1f - adjusted ) : adjusted;
                }
            }

            // Use vanilla method with modified offset (applies SmootherStep curve to stopOffset)
            lane.CalculateStopPositionAndDirection( modifiedOffset, stopOffset, out pos, out dir );
            return;
        }

        // Mode == UpdatedLogic: Improved implementation with direct Bezier calculation
        float targetOffset = laneOffset;
        float laneLength2 = lane.m_length;

        if( laneLength2 >= 1f && ( vehicleData.m_flags & Vehicle.Flags.Leaving ) == 0 )
        {
            float margin = laneLength2 / 6;
            float vehicleLength = vehicleData.Info.m_generatedInfo != null ? vehicleData.Info.m_generatedInfo.m_size.z : 0f;
            float newStopOffset = 1 - ( margin + vehicleLength / 2 ) / laneLength2;

            if( newStopOffset >= 0.5f )
            {
                bool inverted = ( laneInfo.m_finalDirection & NetInfo.Direction.Backward ) != 0;
                if(( flags & NetSegment.Flags.Invert ) != 0 )
                    inverted = !inverted;

                float adjusted = inverted ? ( 1f - laneOffset ) : laneOffset;
                adjusted *= 2 * newStopOffset;
                targetOffset = inverted ? ( 1f - adjusted ) : adjusted;
            }
        }

        // Calculate position and direction directly from Bezier
        pos = lane.m_bezier.Position( targetOffset );
        dir = lane.m_bezier.Tangent( targetOffset );

        // Apply full curb displacement directly (no SmootherStep tapering)
        if( stopOffset != 0f )
            pos += Vector3.Cross( Vector3.up, dir ).normalized * stopOffset;
    }
}

[HarmonyPatch(typeof(TrolleybusAI))]
public static class TrolleybusAI_Patch
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(CalculateSegmentPosition))]
    public static IEnumerable<CodeInstruction> CalculateSegmentPosition(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return BusAI_Patch.CalculateSegmentPosition( instructions, generator );
    }
}

}
