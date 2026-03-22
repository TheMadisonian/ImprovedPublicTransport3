using ColossalFramework;
using System;
using System.Collections.Generic;

namespace ElevatedStopsEnabler
{
    static class RoadBridgeAiExt
    {
		public static void UpdateSegmentStopFlags(this RoadBridgeAI roadbridge, ushort segmentID, ref NetSegment data)
		{
			roadbridge.UpdateSegmentFlags(segmentID, ref data);
			NetSegment.Flags flags = data.m_flags & ~(NetSegment.Flags.StopRight | NetSegment.Flags.StopLeft | NetSegment.Flags.StopRight2 | NetSegment.Flags.StopLeft2);

			if (roadbridge.m_info.m_lanes == null) 
				return;

			NetManager instance = Singleton<NetManager>.instance;
			bool inverted = (data.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
			uint lane = instance.m_segments.m_buffer[segmentID].m_lanes;

			for (int i = 0; i < roadbridge.m_info.m_lanes.Length && lane != 0U; i += 1)
			{
				NetLane.Flags laneFlags = (NetLane.Flags)instance.m_lanes.m_buffer[(int)(UIntPtr)lane].m_flags;

				if ((laneFlags & NetLane.Flags.Stop) != 0)
				{
					if (roadbridge.m_info.m_lanes[i].m_position < 0f != inverted)
						flags |= NetSegment.Flags.StopLeft;
					else
						flags |= NetSegment.Flags.StopRight;
				}
				else if ((laneFlags & NetLane.Flags.Stop2) != 0)
				{
					if (roadbridge.m_info.m_lanes[i].m_position < 0f != inverted)
						flags |= NetSegment.Flags.StopLeft2;
					else
						flags |= NetSegment.Flags.StopRight2;
				}

				lane = instance.m_lanes.m_buffer[(int)((UIntPtr)lane)].m_nextLane;
			}

			data.m_flags = flags;
		}
	}
}
