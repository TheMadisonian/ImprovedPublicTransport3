using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace RealisticWalkingSpeed.Patches
{
    public class CitizenAnimationSpeedHarmonyPatch : IHarmonyPatch
    {
        private readonly Harmony _harmony;

        public CitizenAnimationSpeedHarmonyPatch(Harmony harmony)
        {
            _harmony = harmony ?? throw new ArgumentNullException(nameof(harmony));
        }

        public void Apply()
        {
            try
            {
                var setRenderParametersMethodInfo = typeof(CitizenInfo)
                    .GetMethod("SetRenderParameters", BindingFlags.Instance | BindingFlags.Public);
                var setRenderParametersTranspilerMethodInfo = GetType()
                    .GetMethod(nameof(SetRenderParametersTranspiler), BindingFlags.Static | BindingFlags.NonPublic);
                
                if (setRenderParametersMethodInfo == null)
                {
                    Utils.LogError("CitizenAnimationSpeedHarmonyPatch: Could not find SetRenderParameters method");
                    return;
                }

                _harmony.Patch(setRenderParametersMethodInfo, null, null, new HarmonyMethod(setRenderParametersTranspilerMethodInfo));
                Utils.Log("CitizenAnimationSpeedHarmonyPatch: Successfully patched SetRenderParameters");
            }
            catch (System.Exception ex)
            {
                Utils.LogError($"CitizenAnimationSpeedHarmonyPatch: Failed to apply patch: {ex.Message}\n{ex.StackTrace}");
            }
        }

        static IEnumerable<CodeInstruction> SetRenderParametersTranspiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var hookOperand = typeof(Animator).GetMethod(
                "SetFloat",
                BindingFlags.Public | BindingFlags.Instance,
                Type.DefaultBinder,
                new []
                {
                    typeof(int),
                    typeof(float)
                },
                null
            );

            var codes = new List<CodeInstruction>(codeInstructions);
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (!(code.opcode == OpCodes.Callvirt && code.operand == hookOperand))
                {
                    continue;
                }

                //float magnitude = velocity.magnitude;
                //->
                //float magnitude = velocity.magnitude * 2.1f;
                codes.InsertRange(i - 6, new[] {
                    new CodeInstruction(OpCodes.Ldc_R4, 2.1f), //TODO finetune per CitizenInfo
                    new CodeInstruction(OpCodes.Mul)
                });

                break;
            }

            return codes;
        }
    }
}
