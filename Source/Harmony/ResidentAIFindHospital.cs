using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;

namespace CallAgain
{
    [HarmonyPatch]
    public static class ResidentAIFindHospital
    {
        // There is a bug in ResidentAI.FindHospital where it adds Childcare and Eldercare offers as AddOutgoingOffer half the time when it should always be AddIncomingOffer for a citizen
        [HarmonyPatch(typeof(ResidentAI), "FindHospital")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FindHospitalTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo methodAddOutgoingOffer = AccessTools.Method(typeof(TransferManager), nameof(TransferManager.AddOutgoingOffer));

            bool bPatched = false;
            int iAddOutgoingCount = 0;

            // Instruction enumerator.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction.
                CodeInstruction instruction = instructionsEnumerator.Current;

                if (!bPatched)
                {
                    // We want to patch after the second call to AddOutgoingOffer
                    if (instruction.Calls(methodAddOutgoingOffer))
                    {
                        iAddOutgoingCount++;
                    }

                    // Now look for loading of argument "reason" (Argument 3)
                    if (iAddOutgoingCount == 2 && instruction.opcode == OpCodes.Ldarg_3)
                    {
                        // We want to change this to always use transfer reason Sick
                        yield return new CodeInstruction(OpCodes.Ldc_I4_S, (int)TransferManager.TransferReason.Sick) { labels = instruction.labels }; // Copy labels from Ldarg_3 instruction (if any)
                        bPatched = true;
                        continue; // Dont return original instruction
                    }
                }

                // Return normal instruction
                yield return instructionsEnumerator.Current;
            }

            Debug.Log($"FindHospitalTranspiler - Patching of ResidentAI.FindHospital {(bPatched ? "succeeded" : "failed")}.");
        }
    }
}