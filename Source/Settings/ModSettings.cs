using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace CallAgain.Settings
{
    public class ModSettings
    {
        private static ModSettings s_settings = null;

        public static ModSettings GetSettings()
        {
            if (s_settings == null)
            {
                s_settings = ModSettings.Load();
            }
            return s_settings;
        }

        public static void ResetSettings()
        {
            s_settings = new ModSettings();
            s_settings.Save();
        }

        [XmlIgnore]
        const string SETTINGS_FILE_NAME = "CallAgainSettings";

        [XmlIgnore]
        private static readonly string SettingsFileName = "CallAgainSettings.xml";

        [XmlIgnore]
        private static readonly string UserSettingsDir = ColossalFramework.IO.DataLocation.localApplicationData;

        [XmlIgnore]
        private static readonly string SettingsFile = Path.Combine(UserSettingsDir, SettingsFileName);

        public bool CallAgainEnabled
        {
            get;
            set;
        } = true;

        public int HealthcareThreshold
        {
            get;
            set;
        } = 30;

        public int DeathcareThreshold
        {
            get;
            set;
        } = 40;

        public int GoodsThreshold
        {
            get;
            set;
        } = 40;

        public int GarbageThreshold
        {
            get;
            set;
        } = 3000;

        public bool DespawnReturningCargoTrucks
        {
            get;
            set;
        } = false;

        public string PreferredLanguage
        {
            get;
            set;
        } = "System Default";

        static ModSettings()
        {
            if (GameSettings.FindSettingsFileByName(SETTINGS_FILE_NAME) == null)
            {
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = SETTINGS_FILE_NAME } });
            }
        }

        public static ModSettings Load()
        {
            Debug.Log("Loading settings: " + SettingsFile); 
            try
            {
                // Read settings file.
                if (File.Exists(SettingsFile))
                {
                    using (StreamReader reader = new StreamReader(SettingsFile))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModSettings));
                        ModSettings? oSettings = xmlSerializer.Deserialize(reader) as ModSettings;
                        if (oSettings != null)
                        {
                            return oSettings;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            return new ModSettings();
        }

        /// <summary>
        /// Save settings to XML file.
        /// </summary>
        public void Save()
        {
            try
            {
                // Pretty straightforward.
                using (StreamWriter writer = new StreamWriter(SettingsFile))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModSettings)); 
                    xmlSerializer.Serialize(writer, ModSettings.GetSettings());
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Saving settings file failed.", ex); 
            }
        }
    }
}
