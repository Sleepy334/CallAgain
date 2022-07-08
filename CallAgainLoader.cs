using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace CallAgain
{
    public class CallAgainLoader : LoadingExtensionBase
    {
        private static bool s_loaded = false;
        private static UITextureAtlas? s_atlas = null;

        public static bool IsLoaded() { return s_loaded; }

        public override void OnLevelLoaded(LoadMode mode)
        {
            if (CallAgainMain.IsEnabled && (mode == LoadMode.LoadGame || mode == LoadMode.NewGame))
            {
                s_loaded = true;
            }
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
        }
    }
}
