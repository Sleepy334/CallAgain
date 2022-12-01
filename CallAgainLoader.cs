using ICities;
using SleepyCommon;

namespace CallAgain
{
    public class CallAgainLoader : LoadingExtensionBase
    {
        private static bool s_loaded = false;

        public static bool IsLoaded() { return s_loaded; }

        private static bool ActiveInMode(LoadMode mode)
        {
            switch (mode)
            {
                case LoadMode.NewGame:
                case LoadMode.NewGameFromScenario:
                case LoadMode.LoadGame:
                    return true;

                default:
                    return false;
            }
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            if (CallAgainMain.IsEnabled && ActiveInMode(mode))
            {
                s_loaded = true;

                // Patch game using Harmony
                ApplyHarmonyPatches();
            }
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
        }

        public bool ApplyHarmonyPatches()
        {
            // Harmony
            if (DependencyUtilities.IsHarmonyRunning())
            {
                Patcher.PatchAll();

                if (!IsHarmonyValid())
                {
                    RemoveHarmonyPathes();

                    string strMessage = "Harmony patching failed\r\n";
                    strMessage += "\r\n";
                    strMessage += "You could try Compatibility Report to check for mod compatibility or use Load Order Mod to ensure your mods are loaded in the correct order.";
                    Prompt.ErrorFormat(CallAgainMain.Title, strMessage);
                    return false;
                }
            }

            return true;
        }

        public void RemoveHarmonyPathes()
        {
            if (s_loaded && DependencyUtilities.IsHarmonyRunning())
            {
                Patcher.UnpatchAll();
            }
        }

        public bool IsHarmonyValid()
        {
            if (DependencyUtilities.IsHarmonyRunning())
            {
                int iHarmonyPatches = Patcher.GetPatchCount();
                Debug.Log("Harmony patches: " + iHarmonyPatches);
                return iHarmonyPatches == Patcher.GetHarmonyPatchCount();
            }

            return false;
        }
    }
}
