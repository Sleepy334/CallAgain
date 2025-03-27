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
        private UICheckBox? m_chkCallAgainEnabled = null;
        private SettingsSlider? m_CallAgainUpdateRateSlider = null;
        private SettingsSlider? m_oHealthcareThresholdSlider = null;
        private SettingsSlider? m_oDeathcareThresholdSlider = null;
        private SettingsSlider? m_oGoodsThresholdSlider = null;
        private SettingsSlider? m_oGarbageThresholdSlider = null;
        private UICheckBox? m_checkDespawnCargoTrucks = null;

        private UILabel? m_txtHealthcare = null;
        private UILabel? m_txtDeathcare = null;
        private UILabel? m_txtGoods = null;
        private UILabel? m_txtGarbage = null;

        float fTEXT_SCALE = 1.0f;

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

            UIHelper oGeneralGroup = (UIHelper)tabCallAgain.AddGroup(Localization.Get("GROUP_CALLAGAIN_GENERAL"));
            m_chkCallAgainEnabled = (UICheckBox)oGeneralGroup.AddCheckbox(Localization.Get("CallAgainEnabled"), oSettings.CallAgainEnabled, OnCallAgainChanged);
            oGeneralGroup.AddButton(Localization.Get("buttonResetSettings"), OnResetClicked);

            // Health care threshold Slider
            UIHelper oHealthcareGroup = (UIHelper)tabCallAgain.AddGroup(Localization.Get("GROUP_CALLAGAIN_HEALTHCARE"));
            UIPanel pnlPanelHealth = (UIPanel)oHealthcareGroup.self;
            m_oHealthcareThresholdSlider = SettingsSlider.Create(oHealthcareGroup, Localization.Get("CallAgainHealthcareThreshold"), fTEXT_SCALE, 0f, 255f, 1f, (float)oSettings.HealthcareThreshold, OnHealthcareThresholdValueChanged);
            AddDescription(pnlPanelHealth, "CallAgainDescriptionThreshold", pnlPanelHealth, 1.0f, Localization.Get("CallAgainDescriptionThreshold")); 

            // Death care threshold Slider
            UIHelper oDeathcareGroup = (UIHelper)tabCallAgain.AddGroup(Localization.Get("GROUP_CALLAGAIN_DEATHCARE"));
            UIPanel pnlPanelDeathcare = (UIPanel)oDeathcareGroup.self;
            m_oDeathcareThresholdSlider = SettingsSlider.Create(oDeathcareGroup, Localization.Get("CallAgainDeathcareThreshold"), fTEXT_SCALE, 0f, 255f, 1f, (float)oSettings.DeathcareThreshold, OnDeathcareThresholdValueChanged);
            AddDescription(pnlPanelDeathcare, "CallAgainDescriptionThreshold", pnlPanelDeathcare, 1.0f, Localization.Get("CallAgainDescriptionThreshold"));

            // Garbage threshold Slider
            UIHelper oGarbageGroup = (UIHelper)tabCallAgain.AddGroup(Localization.Get("GROUP_CALLAGAIN_GARBAGE"));
            UIPanel pnlPanelGarbage = (UIPanel)oGarbageGroup.self;
            m_oGarbageThresholdSlider = SettingsSlider.Create(oGarbageGroup, Localization.Get("CallAgainGarbageThreshold"), fTEXT_SCALE, 1500f, 4000f, 1f, (float)oSettings.GarbageThreshold, OnGarbageThresholdValueChanged);
            AddDescription(pnlPanelGarbage, "CallAgainDescriptionThreshold", pnlPanelGarbage, 1.0f, Localization.Get("CallAgainDescriptionThreshold"));

            // Goods threshold Slider
            UIHelper oGoodsGroup = (UIHelper)tabCallAgain.AddGroup(Localization.Get("GROUP_CALLAGAIN_GOODS"));
            UIPanel pnlPanelGoods = (UIPanel)oGoodsGroup.self;
            m_oGoodsThresholdSlider = SettingsSlider.Create(oGoodsGroup, Localization.Get("CallAgainGoodsThreshold"), fTEXT_SCALE, 0f, 255f, 1f, (float)oSettings.GoodsThreshold, OnGoodsThresholdValueChanged);
            AddDescription(pnlPanelGoods, "CallAgainDescriptionThreshold", pnlPanelGoods, 1.0f, Localization.Get("CallAgainDescriptionThreshold")); 

            UIHelper oCaroStationGroup = (UIHelper)tabCallAgain.AddGroup(Localization.Get("GROUP_CALLAGAIN_CARGOSTATION"));
            m_checkDespawnCargoTrucks = (UICheckBox)oCaroStationGroup.AddCheckbox(Localization.Get("DespawnReturningCargoTrucks"), oSettings.DespawnReturningCargoTrucks, OnDespawnReturningCargoTrucksChanged);

            UpdateCallAgainEnabled();
        }

        public void SetupStatisticsTab(UIHelper tabStatistics)
        {
            UIScrollablePanel pnlPanel = (UIScrollablePanel)tabStatistics.self;
            m_txtHealthcare = AddDescription(pnlPanel, "StatisticsHealthcare", pnlPanel, 1.0f, "Healthcare callbacks: " + CallAgainStats.GetCallCount(TransferManager.TransferReason.Sick));
            m_txtDeathcare = AddDescription(pnlPanel, "StatisticsDeathcare", pnlPanel, 1.0f, "Deathcare callbacks: " + CallAgainStats.GetCallCount(TransferManager.TransferReason.Dead));
            m_txtGoods = AddDescription(pnlPanel, "StatisticsGoods", pnlPanel, 1.0f, "Goods callbacks: " + CallAgainStats.GetCallCount(TransferManager.TransferReason.Goods));
            m_txtGarbage = AddDescription(pnlPanel, "StatisticsGarbage", pnlPanel, 1.0f, "Garbage callbacks: " + CallAgainStats.GetCallCount(TransferManager.TransferReason.Garbage));
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

        public void OnResetClicked()
        {
            ModSettings.ResetSettings();
            CallAgainStats.Init();

            // Update fields
            ModSettings oSettings = ModSettings.GetSettings();
            m_chkCallAgainEnabled.isChecked = oSettings.CallAgainEnabled;
            m_oHealthcareThresholdSlider.SetValue(oSettings.HealthcareThreshold);
            m_oDeathcareThresholdSlider.SetValue(oSettings.DeathcareThreshold);
            m_oGoodsThresholdSlider.SetValue(oSettings.GoodsThreshold);
            m_oGarbageThresholdSlider.SetValue(oSettings.GarbageThreshold);
        }

        public void UpdateStatistics()
        {
            if (m_txtHealthcare != null)
            {
                m_txtHealthcare.text = "Healthcare callbacks: " + CallAgainStats.GetCallCount(TransferManager.TransferReason.Sick);
            }
            if (m_txtDeathcare != null)
            {
                m_txtDeathcare.text = "Deathcare callbacks: " + CallAgainStats.GetCallCount(TransferManager.TransferReason.Dead);
            }
            if (m_txtGoods != null)
            {
                m_txtGoods.text = "Goods callbacks: " + CallAgainStats.GetCallCount(TransferManager.TransferReason.Goods);
            }
            if (m_txtGarbage != null)
            {
                m_txtGarbage.text = "Garbage callbacks: " + CallAgainStats.GetCallCount(TransferManager.TransferReason.Garbage);
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

        public void OnGarbageThresholdValueChanged(float value)
        {
            ModSettings oSettings = ModSettings.GetSettings();
            oSettings.GarbageThreshold = (int)value;
            oSettings.Save();
        }

        public void OnGoodsThresholdValueChanged(float value)
        {
            ModSettings oSettings = ModSettings.GetSettings();
            oSettings.GoodsThreshold = (int)value;
            oSettings.Save();
        }

        public void OnDeathcareThresholdValueChanged(float value)
        {
            ModSettings oSettings = ModSettings.GetSettings();
            oSettings.DeathcareThreshold = (int)value;
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

        public void UpdateCallAgainEnabled()
        {
            ModSettings oSettings = ModSettings.GetSettings();

            m_oHealthcareThresholdSlider.Enable(oSettings.CallAgainEnabled);
            m_oDeathcareThresholdSlider.Enable(oSettings.CallAgainEnabled);
            m_oGoodsThresholdSlider.Enable(oSettings.CallAgainEnabled);
            m_oGarbageThresholdSlider.Enable(oSettings.CallAgainEnabled);

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
