using ColossalFramework.UI;
using ICities;
using CallAgain.Settings;
using UnityEngine;
using CallAgain;
using CallAgain.Util;

namespace CallAgain
{
    public class SettingsUI
    {
        private SettingsSlider? m_CallAgainUpdateRateSlider = null;
        private SettingsSlider? m_oHealthcareThresholdSlider = null;
        private SettingsSlider? m_oHealthcareRateSlider = null;
        private SettingsSlider? m_oDeathcareThresholdSlider = null;
        private SettingsSlider? m_oDeathcareRateSlider = null;
        private SettingsSlider? m_oGoodsThresholdSlider = null;
        private SettingsSlider? m_oGoodsRateSlider = null;
        private SettingsSlider? m_oGarbageThresholdSlider = null;
        private SettingsSlider? m_oGarbageRateSlider = null;
        private UICheckBox? m_checkDespawnCargoTrucks = null;

        UILabel? m_txtHealthcare = null;
        UILabel? m_txtDeathcare = null;
        UILabel? m_txtGoods = null;
        UILabel? m_txtGarbage = null;

        public SettingsUI()
        {
        }

        public void OnSettingsUI(UIHelper helper)
        {
            // Title
            UIComponent pnlMain = (UIComponent)helper.self;
            UILabel txtTitle = AddDescription(pnlMain, "title", pnlMain, 1.0f, CallAgainMain.Title);
            txtTitle.textScale = 1.2f;

            // Add tabstrip.
            ExtUITabstrip tabStrip = ExtUITabstrip.Create(helper);
            tabStrip.eventSelectedIndexChanged += OnTabChanged;
            tabStrip.eventVisibilityChanged += OnTabVisibilityChanged;
            UIHelper tabCallAgain = tabStrip.AddTabPage(Localization.Get("tabCallAgain"), true);
            UIHelper tabStatistics = tabStrip.AddTabPage(Localization.Get("tabStatistics"), true);

            // Setup tabs
            SetupCallAgainTab(tabCallAgain);
            SetupStatisticsTab(tabStatistics);
        }
        
        public void SetupCallAgainTab(UIHelper tabCallAgain)
        {
            ModSettings oSettings = ModSettings.GetSettings();

            UIHelper groupLocalisation = (UIHelper)tabCallAgain.AddGroup(Localization.Get("GROUP_LOCALISATION"));
            groupLocalisation.AddDropdown(Localization.Get("dropdownLocalization"), Localization.GetLoadedLanguages(), Localization.GetLanguageIndexFromCode(oSettings.PreferredLanguage), OnLocalizationDropDownChanged);

            tabCallAgain.AddCheckbox(Localization.Get("CallAgainEnabled"), oSettings.CallAgainEnabled, OnCallAgainChanged);
            m_CallAgainUpdateRateSlider = SettingsSlider.Create(tabCallAgain, Localization.Get("sliderCallAgainUpdateRate"), 2f, 10f, 1f, (float)oSettings.CallAgainUpdateRate, OnCallAgainUpdateRateValueChanged);
            UIScrollablePanel pnlPanel3 = (UIScrollablePanel)tabCallAgain.self;
            UILabel txtLabel1 = AddDescription(pnlPanel3, "CallAgainDescriptionThreshold", pnlPanel3, 1.0f, Localization.Get("CallAgainDescriptionThreshold"));
            UILabel txtLabel2 = AddDescription(pnlPanel3, "CallAgainDescriptionRate", pnlPanel3, 1.0f, Localization.Get("CallAgainDescriptionRate"));

            // Health care threshold Slider
            UIHelper oHealthcareGroup = (UIHelper)tabCallAgain.AddGroup(Localization.Get("GROUP_CALLAGAIN_HEALTHCARE"));
            m_oHealthcareThresholdSlider = SettingsSlider.Create(oHealthcareGroup, Localization.Get("CallAgainHealthcareThreshold"), 0f, 255f, 1f, (float)oSettings.HealthcareThreshold, OnHealthcareThresholdValueChanged);
            m_oHealthcareRateSlider = SettingsSlider.Create(oHealthcareGroup, Localization.Get("CallAgainHealthcareRate"), 1f, 30f, 1f, (float)oSettings.HealthcareRate, OnHealthcareRateValueChanged);

            // Death care threshold Slider
            UIHelper oDeathcareGroup = (UIHelper)tabCallAgain.AddGroup(Localization.Get("GROUP_CALLAGAIN_DEATHCARE"));
            m_oDeathcareThresholdSlider = SettingsSlider.Create(oDeathcareGroup, Localization.Get("CallAgainDeathcareThreshold"), 0f, 255f, 1f, (float)oSettings.DeathcareThreshold, OnDeathcareThresholdValueChanged);
            m_oDeathcareRateSlider = SettingsSlider.Create(oDeathcareGroup, Localization.Get("CallAgainDeathcareRate"), 1f, 30f, 1f, (float)oSettings.DeathcareRate, OnDeathcareRateValueChanged);

            // Goods threshold Slider
            UIHelper oGoodsGroup = (UIHelper)tabCallAgain.AddGroup(Localization.Get("GROUP_CALLAGAIN_GOODS"));
            m_oGoodsThresholdSlider = SettingsSlider.Create(oGoodsGroup, Localization.Get("CallAgainGoodsThreshold"), 0f, 255f, 1f, (float)oSettings.GoodsThreshold, OnGoodsThresholdValueChanged);
            m_oGoodsRateSlider = SettingsSlider.Create(oGoodsGroup, Localization.Get("CallAgainGoodsRate"), 1f, 30f, 1f, (float)oSettings.GoodsRate, OnGoodsRateValueChanged);

            // Garbage threshold Slider
            UIHelper oGarbageGroup = (UIHelper)tabCallAgain.AddGroup(Localization.Get("GROUP_CALLAGAIN_GARBAGE"));
            m_oGarbageThresholdSlider = SettingsSlider.Create(oGarbageGroup, Localization.Get("CallAgainGarbageThreshold"), 1500f, 4000f, 1f, (float)oSettings.GarbageThreshold, OnGarbageThresholdValueChanged);
            m_oGarbageRateSlider = SettingsSlider.Create(oGarbageGroup, Localization.Get("CallAgainGarbageRate"), 1f, 30f, 1f, (float)oSettings.GarbageRate, OnGarbageRateValueChanged);

            UIHelper oCaroStationGroup = (UIHelper)tabCallAgain.AddGroup(Localization.Get("GROUP_CALLAGAIN_CARGOSTATION"));
            m_checkDespawnCargoTrucks = (UICheckBox)oCaroStationGroup.AddCheckbox(Localization.Get("DespawnReturningCargoTrucks"), oSettings.DespawnReturningCargoTrucks, OnDespawnReturningCargoTrucksChanged);

            UpdateCallAgainEnabled();
        }

        public void SetupStatisticsTab(UIHelper tabStatistics)
        {
            UIScrollablePanel pnlPanel = (UIScrollablePanel)tabStatistics.self;
            m_txtHealthcare = AddDescription(pnlPanel, "StatisticsHealthcare", pnlPanel, 1.0f, "Healthcare callbacks: " + CallAgainStats.s_CallbackStats[TransferManager.TransferReason.Sick]);
            m_txtDeathcare = AddDescription(pnlPanel, "StatisticsDeathcare", pnlPanel, 1.0f, "Deathcare callbacks: " + CallAgainStats.s_CallbackStats[TransferManager.TransferReason.Dead]);
            m_txtGoods = AddDescription(pnlPanel, "StatisticsGoods", pnlPanel, 1.0f, "Goods callbacks: " + CallAgainStats.s_CallbackStats[TransferManager.TransferReason.Goods]);
            m_txtGarbage = AddDescription(pnlPanel, "StatisticsGarbage", pnlPanel, 1.0f, "Garbage callbacks: " + CallAgainStats.s_CallbackStats[TransferManager.TransferReason.Garbage]);
        }

        /* 
         * Code adapted from PropAnarchy under MIT license
         */
        private static readonly Color32 m_greyColor = new Color32(0xe6, 0xe6, 0xe6, 0xee);
        private static UILabel AddDescription(UIComponent panel, string name, UIComponent alignTo, float fontScale, string text)
        {
            UILabel desc = panel.AddUIComponent<UILabel>();
            desc.name = name;
            desc.width = panel.width - 80;
            desc.wordWrap = true;
            desc.autoHeight = true;
            desc.textScale = fontScale;
            desc.textColor = m_greyColor;
            desc.text = text;
            desc.relativePosition = new Vector3(alignTo.relativePosition.x + 26f, alignTo.relativePosition.y + alignTo.height + 10);
            return desc;
        }

        public void UpdateStatistics()
        {
            if (m_txtHealthcare != null)
            {
                m_txtHealthcare.text = "Healthcare callbacks: " + CallAgainStats.s_CallbackStats[TransferManager.TransferReason.Sick];
            }
            if (m_txtDeathcare != null)
            {
                m_txtDeathcare.text = "Deathcare callbacks: " + CallAgainStats.s_CallbackStats[TransferManager.TransferReason.Dead];
            }
            if (m_txtGoods != null)
            {
                m_txtGoods.text = "Goods callbacks: " + CallAgainStats.s_CallbackStats[TransferManager.TransferReason.Goods];
            }
            if (m_txtGarbage != null)
            {
                m_txtGarbage.text = "Garbage callbacks: " + CallAgainStats.s_CallbackStats[TransferManager.TransferReason.Garbage];
            }
        }

        public void OnTabChanged(UIComponent component, int iTabIndex)
        {
            UpdateStatistics();
        }

        public void OnTabVisibilityChanged(UIComponent component, bool bVisible)
        {
            if (bVisible)
            {
                UpdateStatistics();
            }
        }

        public void OnLocalizationDropDownChanged(int value)
        {
            ModSettings oSettings = ModSettings.GetSettings();
            oSettings.PreferredLanguage = Localization.GetLoadedCodes()[value];
            oSettings.Save();
        }

        public void OnDespawnReturningCargoTrucksChanged(bool value)
        {
            ModSettings oSettings = ModSettings.GetSettings();
            oSettings.DespawnReturningCargoTrucks = value;
            oSettings.Save();
        }

        public void OnCallAgainUpdateRateValueChanged(float value)
        {
            ModSettings oSettings = ModSettings.GetSettings();
            oSettings.CallAgainUpdateRate = (int)value;
            oSettings.Save();
        }

        public void OnGarbageThresholdValueChanged(float value)
        {
            ModSettings oSettings = ModSettings.GetSettings();
            oSettings.GarbageThreshold = (int)value;
            oSettings.Save();
        }

        public void OnGarbageRateValueChanged(float value)
        {
            ModSettings oSettings = ModSettings.GetSettings();
            oSettings.GarbageRate = (int)value;
            oSettings.Save();
        }

        public void OnGoodsThresholdValueChanged(float value)
        {
            ModSettings oSettings = ModSettings.GetSettings();
            oSettings.GoodsThreshold = (int)value;
            oSettings.Save();
        }

        public void OnGoodsRateValueChanged(float value)
        {
            ModSettings oSettings = ModSettings.GetSettings();
            oSettings.GoodsRate = (int)value;
            oSettings.Save();
        }

        public void OnDeathcareThresholdValueChanged(float value)
        {
            ModSettings oSettings = ModSettings.GetSettings();
            oSettings.DeathcareThreshold = (int)value;
            oSettings.Save();
        }

        public void OnDeathcareRateValueChanged(float value)
        {
            ModSettings oSettings = ModSettings.GetSettings();
            oSettings.DeathcareRate = (int)value;
            oSettings.Save();
        }

        public void OnCallAgainChanged(bool enabled)
        {
            ModSettings oSettings = ModSettings.GetSettings();
            oSettings.CallAgainEnabled = enabled;
            oSettings.Save();

            UpdateCallAgainEnabled();
        }

        public void OnHealthcareThresholdValueChanged(float value)
        {
            ModSettings oSettings = ModSettings.GetSettings();
            oSettings.HealthcareThreshold = (int)value;
            oSettings.Save();
        }

        public void OnHealthcareRateValueChanged(float value)
        {
            ModSettings oSettings = ModSettings.GetSettings();
            oSettings.HealthcareRate = (int)value;
            oSettings.Save();
        }

        public void UpdateCallAgainEnabled()
        {
            ModSettings oSettings = ModSettings.GetSettings();

            m_CallAgainUpdateRateSlider.Enable(oSettings.CallAgainEnabled);
            m_oHealthcareThresholdSlider.Enable(oSettings.CallAgainEnabled);
            m_oHealthcareRateSlider.Enable(oSettings.CallAgainEnabled);
            m_oDeathcareThresholdSlider.Enable(oSettings.CallAgainEnabled);
            m_oDeathcareRateSlider.Enable(oSettings.CallAgainEnabled);
            m_oGoodsThresholdSlider.Enable(oSettings.CallAgainEnabled);
            m_oGoodsRateSlider.Enable(oSettings.CallAgainEnabled);
            m_oGarbageThresholdSlider.Enable(oSettings.CallAgainEnabled);
            m_oGarbageRateSlider.Enable(oSettings.CallAgainEnabled);

            if (oSettings.CallAgainEnabled)
            {
                m_checkDespawnCargoTrucks.Enable();
            }
            else
            {
                m_checkDespawnCargoTrucks.Disable();
            }
        }
    }
}
