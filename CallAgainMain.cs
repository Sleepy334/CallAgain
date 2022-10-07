using CallAgain.Util;
using ICities;

namespace CallAgain
{
    public class CallAgainMain : IUserMod
	{
		private static string Version = "v1.5.1"; 
#if TEST_RELEASE || TEST_DEBUG
		public static string ModName => "TransferManager CE " + Version + " TEST";
		public static string Title => ModName;
#else
		public static string ModName => "CallAgain " + Version; 
		public static string Title => "Call Again " + " " + Version;
#endif

		public static bool IsDebug = false;
		public static bool IsEnabled = false;

		public string Name
		{
			get { return ModName; }
		}

		public string Description
		{
			get { 
				return "Call Again: I'm sick and no ambulances are coming, why don't you call them again dear." +
						  "CallAgain will detect when a building has sick or dead people inside and no vehicles are responding and will add a new high priority transfer offer to hopefully get a response. The threshold when call again activates and the frequency of requests can be controlled in the settings."; 
			}
		}

		public void OnEnabled()
		{
			IsEnabled = true;

			Localization.LoadAllLanguageFiles();
			CallAgainStats.Init();
		}

		public void OnDisabled()
		{
			IsEnabled = false;
		}

		// Sets up a settings user interface
		public void OnSettingsUI(UIHelper helper)
		{
			SettingsUI settingsUI = new SettingsUI();
			settingsUI.OnSettingsUI(helper);
		}
    }
}