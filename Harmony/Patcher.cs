using System;
using System.Collections.Generic;
using HarmonyLib;
using SleepyCommon;
using UnityEngine;

namespace CallAgain
{
    public static class Patcher {
        public const string HarmonyId = "Sleepy.CallAgain";

        private static bool s_patched = false;
        private static int s_iHarmonyPatches = 0;

        public static int GetHarmonyPatchCount() 
        { 
            return s_iHarmonyPatches; 
        }

        public static void PatchAll() 
        {
            if (!s_patched)
            {
                Debug.Log("CallAgain: Patching...");

                s_patched = true;
                var harmony = new Harmony(HarmonyId);

                List<Type> patchList = new List<Type>();

                if (!DependencyUtilities.IsTransferManagerRunning())
                {
                    // Patch offer bugs in main game
                    patchList.Add(typeof(Patch.AirportBuildingAIPatch));
                    patchList.Add(typeof(Patch.CommonBuildingAIPatch));
                }

                s_iHarmonyPatches = patchList.Count;

                if (patchList.Count > 0)
                {
                    string sMessage = "Patching the following functions:\r\n";
                    foreach (var patchType in patchList)
                    {
                        sMessage += patchType.ToString() + "\r\n";
                        harmony.CreateClassProcessor(patchType).Patch();
                    }
                    Debug.Log(sMessage);
                }
                else
                {
                    Debug.Log("Transfer Manager CE detected, no patches performed.");
                }
            }
        }

        public static void UnpatchAll() {
            if (s_patched)
            {
                var harmony = new Harmony(HarmonyId);
                harmony.UnpatchAll(HarmonyId);
                s_patched = false;
                Debug.Log("CallAgain: Unpatching...");
            }
        }

        public static int GetPatchCount()
        {
            var harmony = new Harmony(HarmonyId);
            var methods = harmony.GetPatchedMethods();
            int i = 0;
            foreach (var method in methods)
            {
                var info = Harmony.GetPatchInfo(method);
                if (info.Owners?.Contains(harmony.Id) == true)
                {
                    i++;
                }
            }

            return i;
        }
    }
}
