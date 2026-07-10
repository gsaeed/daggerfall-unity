// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2023 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Lypyl (lypyl@dfworkshop.net)
// Contributors:    TheLacus
//
// Notes:
//

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

using System.Text;

using FullSerializer;
using UnityEngine.Rendering.PostProcessing;
using static DaggerfallWorkshop.Game.UserInterfaceWindows.DaggerfallMessageBox;


public class ModLoaderInterfaceWindow : DaggerfallPopupWindow
{
    private enum Stage
    {
        None,
        Cleanup,
        CheckDependencies,
        Close
    }

    struct ModSettings
    {
        public ModInfo modInfo;
        public bool enabled;
    }

    #region Fields

    DaggerfallMessageBox ModDescriptionMessageBox;
    public static bool DisableConflicts = false;
    public static bool DisableModWhenDependenciesNotAvailable = false;
    readonly Panel ModPanel = new Panel();
    readonly Panel ModListPanel = new Panel();

    readonly ListBox modList = new ListBox();
    readonly VerticalScrollBar modListScrollBar = new VerticalScrollBar();

    readonly Button increaseLoadOrderButton  = new Button();
    readonly Button decreaseLoadOrderButton  = new Button();
    readonly Button backButton               = new Button();
    readonly Button refreshButton            = new Button();
    readonly Button enableAllButton          = new Button();
    readonly Button disableAllButton         = new Button();
    readonly Button copyToClipboardButton    = new Button();
    readonly Button saveAndCloseButton       = new Button();
    readonly Button buildModSupport          = new Button();
    readonly Button extractFilesButton       = new Button();

    readonly Button extractAllFilesButton        = new Button();
    readonly Button showModDescriptionButton     = new Button();
    readonly Button modSettingsButton            = new Button();
    readonly TextLabel modCount            = new TextLabel();

    readonly Checkbox modEnabledCheckBox         = new Checkbox();
    readonly TextLabel modLoadPriorityLabel      = new TextLabel();
    readonly TextLabel modTitleLabel             = new TextLabel();
    readonly TextLabel modVersionLabel           = new TextLabel();
    readonly TextLabel modAuthorLabel            = new TextLabel();
    readonly TextLabel modAuthorContactLabel     = new TextLabel();
    readonly TextLabel modDFTFUVersionLabel      = new TextLabel();

    readonly TextBox modsSearch                  = new TextBox();
    readonly Button modsPreviousButton           = new Button();
    readonly Button modsNextButton               = new Button();

    readonly TextLabel modsFound = new TextLabel();

    private string modFilterText = string.Empty;
    private TextBox modFilter = new TextBox();
    private Checkbox modsettingCheckbox = new Checkbox();


    readonly Color backgroundColor = new Color(0, 0, 0, 0.7f);
    readonly Color unselectedTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    readonly Color selectedTextColor = new Color(0.0f, 0.8f, 0.0f, 1.0f);
    readonly Color textColor = new Color(0.0f, 0.5f, 0.0f, 0.4f);
    readonly Color disabledModTextColor = new Color(0.35f, 0.35f, 0.35f, 1);
    readonly Color disabledButtonBackground = new Color(0.35f, 0.35f, 0.35f, 0.4f);
    private Texture2D arrowUpTexture;
    private Texture2D arrowDownTexture;
    Stage currentStage = Stage.None;
    bool moveNextStage = false;

    int currentSelection = -1;
    ModSettings[] modSettings;

    #endregion

    #region Constructors

    public ModLoaderInterfaceWindow(IUserInterfaceManager uiManager)
    : base(uiManager)
    {
    }

    #endregion

    #region Methods

    protected override void Setup()
    {
        arrowUpTexture = Resources.Load<Texture2D>("chevron_up");
        arrowDownTexture = Resources.Load<Texture2D>("chevron_down");

        ParentPanel.BackgroundColor = Color.clear;

        ModListPanel.Outline.Enabled = true;
        ModListPanel.BackgroundColor = backgroundColor;
        ModListPanel.HorizontalAlignment = HorizontalAlignment.Left;
        ModListPanel.VerticalAlignment = VerticalAlignment.Middle;
        ModListPanel.Size = new Vector2(120, 175);
        NativePanel.Components.Add(ModListPanel);

        //modsFound.HorizontalAlignment = HorizontalAlignment.Left;
        //modsFound.Position = new Vector2(0, 20);
        //modsFound.Text = string.Format("{0}: ", ModManager.GetText("modsFound"));
        //ModListPanel.Components.Add(modsFound);

        //modFilter.HorizontalAlignment = HorizontalAlignment.Left;
        modFilter.Position = new Vector2(10, 12); ;
        modFilter.Text = "";
        modFilter.DefaultText = "Enter Filter";
        modFilter.OnType += filterMods;
        ModListPanel.Components.Add(modFilter);

        modCount.HorizontalAlignment = HorizontalAlignment.Right;
        modCount.Text = "        ";
        modCount.Position = new Vector2(10, 20);
        ModListPanel.Components.Add(modCount);


        //modsSearch.Position = new Vector2(60, 20);
        //modsSearch.Size = new Vector2(34, 10);
        //modsSearch.Text = "";
        //modsSearch.MaxCharacters = 8;
        //modsSearch.DefaultText = ModManager.GetText("modsSearch");
        //modsSearch.UseFocus = true;
        //modsSearch.OverridesHotkeySequences = true;
        //ModListPanel.Components.Add(modsSearch);

        //modsPreviousButton.Position = new Vector2(102, 18);
        //modsPreviousButton.Size = new Vector2(8, 8);
        //modsPreviousButton.Outline.Enabled = true;
        //modsPreviousButton.BackgroundColor = textColor;
        //modsPreviousButton.BackgroundTexture = arrowUpTexture;
        //modsPreviousButton.OnMouseClick += ModsPreviousButton_OnMouseClick;
        //ModListPanel.Components.Add(modsPreviousButton);

        //modsNextButton.Position = new Vector2(111, 18);
        //modsNextButton.Size = new Vector2(8, 8);
        //modsNextButton.Outline.Enabled = true;
        //modsNextButton.BackgroundColor = textColor;
        //modsNextButton.BackgroundTexture = arrowDownTexture;
        //modsNextButton.OnMouseClick += ModsNextButton_OnMouseClick;
        //ModListPanel.Components.Add(modsNextButton);

        modsettingCheckbox.IsChecked = false;
        modsettingCheckbox.Position = new Vector2(0, 20);
        modsettingCheckbox.Label.Text = "Settings Only";
        modsettingCheckbox.OnToggleState += ModsettingCheckboxOnOnToggleState;
        ModListPanel.Components.Add(modsettingCheckbox);


        modList.BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        modList.Size = new Vector2(110, 115);
        modList.HorizontalAlignment = HorizontalAlignment.Center;
        modList.VerticalAlignment = VerticalAlignment.Middle;
        modList.TextColor = unselectedTextColor;
        modList.SelectedTextColor = textColor;
        modList.ShadowPosition = Vector2.zero;
        modList.RowsDisplayed = 14;
        modList.RowAlignment = HorizontalAlignment.Left;
        modList.LeftMargin += 4;
        modList.SelectedShadowPosition = DaggerfallUI.DaggerfallDefaultShadowPos;
        modList.SelectedShadowColor = Color.black;
        modList.OnScroll += ModList_OnScroll;
        ModListPanel.Components.Add(modList);

        modListScrollBar.Size = new Vector2(5, 115);
        modListScrollBar.HorizontalAlignment = HorizontalAlignment.Right;
        modListScrollBar.VerticalAlignment = VerticalAlignment.Middle;
        modListScrollBar.Position = new Vector2(100, 12);
        modListScrollBar.BackgroundColor = Color.grey;
        modListScrollBar.DisplayUnits = 14;
        modListScrollBar.TotalUnits = modList.Count;
        modListScrollBar.OnScroll += ModListScrollBar_OnScroll;
        ModListPanel.Components.Add(modListScrollBar);
        modList.ScrollToSelected();

        backButton.Size = new Vector2(45, 12);
        backButton.Label.Text = string.Format("< {0}", ModManager.GetText("backToOptions"));
        backButton.Label.ShadowPosition = Vector2.zero;
        backButton.Label.TextColor = Color.gray;
        backButton.ToolTip = defaultToolTip;
        backButton.ToolTipText = ModManager.GetText("backToOptionsInfo");
        backButton.VerticalAlignment = VerticalAlignment.Top;
        backButton.HorizontalAlignment = HorizontalAlignment.Left;
        backButton.OnMouseClick +=  BackButton_OnMouseClick;
        //backButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.GameSetupBackToOptions);
        ModListPanel.Components.Add(backButton);

        increaseLoadOrderButton.Size = new Vector2(40, 12);
        increaseLoadOrderButton.Position = new Vector2(42, 150);
        increaseLoadOrderButton.Outline.Enabled = true;
        increaseLoadOrderButton.BackgroundColor = textColor;
        increaseLoadOrderButton.Label.Text = ModManager.GetText("increase");
        increaseLoadOrderButton.OnMouseClick += IncreaseLoadOrderButton_OnMouseClick;
        ModListPanel.Components.Add(increaseLoadOrderButton);

        decreaseLoadOrderButton.Size = new Vector2(40, 12);
        decreaseLoadOrderButton.Position = new Vector2(1, 150);
        decreaseLoadOrderButton.Outline.Enabled = true;
        decreaseLoadOrderButton.BackgroundColor = textColor;
        decreaseLoadOrderButton.Label.Text = ModManager.GetText("lower");
        decreaseLoadOrderButton.OnMouseClick += DecreaseLoadOrderButton_OnMouseClick;
        ModListPanel.Components.Add(decreaseLoadOrderButton);

        enableAllButton.Size = new Vector2(40, 12);
        enableAllButton.Position = new Vector2(1, 163);
        enableAllButton.Outline.Enabled = true;
        enableAllButton.BackgroundColor = textColor;
        enableAllButton.VerticalAlignment = VerticalAlignment.Bottom;
        enableAllButton.Label.Text = ModManager.GetText("enableAll");
        enableAllButton.ToolTipText = ModManager.GetText("enableAllInfo");
        enableAllButton.OnMouseClick += EnableAllButton_OnMouseClick;
        ModListPanel.Components.Add(enableAllButton);

        disableAllButton.Size = new Vector2(40, 12);
        disableAllButton.Position = new Vector2(42, 163);
        disableAllButton.Outline.Enabled = true;
        disableAllButton.BackgroundColor = textColor;
        disableAllButton.VerticalAlignment = VerticalAlignment.Bottom;
        disableAllButton.Label.Text = ModManager.GetText("disableAll");
        disableAllButton.ToolTipText = ModManager.GetText("disableAllInfo");
        disableAllButton.OnMouseClick += DisableAllButton_OnMouseClick;
        ModListPanel.Components.Add(disableAllButton);

        copyToClipboardButton.Size = new Vector2(36, 12);
        copyToClipboardButton.Position = new Vector2(83, 163);
        copyToClipboardButton.Outline.Enabled = true;
        copyToClipboardButton.BackgroundColor = textColor;
        disableAllButton.VerticalAlignment = VerticalAlignment.Bottom;
        copyToClipboardButton.Label.Text = ModManager.GetText("copyToClipboard");
        copyToClipboardButton.ToolTipText = ModManager.GetText("copyToClipboardInfo");
        copyToClipboardButton.OnMouseClick += CopyToClipboardButton_OnMouseClick;
        ModListPanel.Components.Add(copyToClipboardButton);

        //Add main mod panel
        ModPanel.Outline.Enabled = true;
        ModPanel.BackgroundColor = backgroundColor;
        ModPanel.HorizontalAlignment = HorizontalAlignment.Right;
        ModPanel.VerticalAlignment = VerticalAlignment.Middle;
        ModPanel.Size = new Vector2(200, 175);
        NativePanel.Components.Add(ModPanel);

        modEnabledCheckBox.Label.Text = ModManager.GetText("enabled");
        modEnabledCheckBox.Label.TextColor = selectedTextColor;
        modEnabledCheckBox.CheckBoxColor = selectedTextColor;
        modEnabledCheckBox.ToolTip = defaultToolTip;
        modEnabledCheckBox.ToolTipText = ModManager.GetText("enabledInfo");
        modEnabledCheckBox.IsChecked = true;
        modEnabledCheckBox.Position = new Vector2(1, 25);
        modEnabledCheckBox.OnToggleState += ModEnabledCheckBox_OnToggleState;
        ModPanel.Components.Add(modEnabledCheckBox);

        modLoadPriorityLabel.Position = new Vector2(60, 25);
        ModPanel.Components.Add(modLoadPriorityLabel);

        modTitleLabel.Position = new Vector2(0, 5);
        modTitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        modTitleLabel.MaxCharacters = 40;
        ModPanel.Components.Add(modTitleLabel);

        modVersionLabel.Position = new Vector2(5, 40);
        modVersionLabel.MaxCharacters = 40;
        ModPanel.Components.Add(modVersionLabel);

        modAuthorLabel.Position = new Vector2(5, 50);
        modAuthorLabel.MaxCharacters = 40;
        ModPanel.Components.Add(modAuthorLabel);

        modAuthorContactLabel.Position = new Vector2(5, 60);
        modAuthorContactLabel.MaxCharacters = 40;
        ModPanel.Components.Add(modAuthorContactLabel);

        modDFTFUVersionLabel.Position = new Vector2(5, 70);
        modDFTFUVersionLabel.MaxCharacters = 40;
        ModPanel.Components.Add(modDFTFUVersionLabel);

        showModDescriptionButton.Position = new Vector2(5, 95);
        showModDescriptionButton.Size = new Vector2(75, 12);
        showModDescriptionButton.HorizontalAlignment = HorizontalAlignment.Center;
        showModDescriptionButton.Label.Text = ModManager.GetText("modDescription");
        showModDescriptionButton.BackgroundColor = textColor;
        showModDescriptionButton.Outline.Enabled = true;
        showModDescriptionButton.OnMouseClick += ShowModDescriptionPopUp_OnMouseClick;
        ModPanel.Components.Add(showModDescriptionButton);

        refreshButton.Size = new Vector2(50, 12);
        refreshButton.Position = new Vector2(5, 139);
        refreshButton.Outline.Enabled = true;
        refreshButton.BackgroundColor = textColor;
        refreshButton.HorizontalAlignment = HorizontalAlignment.Center;
        refreshButton.Label.Text = ModManager.GetText("refresh");
        refreshButton.Label.ToolTipText = ModManager.GetText("refreshInfo");
        refreshButton.OnMouseClick += RefreshButton_OnMouseClick;
        //refreshButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.GameSetupRefresh);
        ModPanel.Components.Add(refreshButton);

        saveAndCloseButton.Size = new Vector2(70, 12);
        saveAndCloseButton.Outline.Enabled = true;
        saveAndCloseButton.BackgroundColor = textColor;
        saveAndCloseButton.VerticalAlignment = VerticalAlignment.Bottom;
        saveAndCloseButton.HorizontalAlignment = HorizontalAlignment.Left;
        saveAndCloseButton.Label.Text = ModManager.GetText("saveClose");
        saveAndCloseButton.Label.ToolTipText = ModManager.GetText("saveCloseInfo");
        saveAndCloseButton.OnMouseClick += SaveAndCloseButton_OnMouseClick;
        //saveAndCloseButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.GameSetupSaveAndClose);
        ModPanel.Components.Add(saveAndCloseButton);

        buildModSupport.Size = new Vector2(50, 12);
        buildModSupport.Outline.Enabled = true;
        buildModSupport.BackgroundColor = textColor;
        buildModSupport.VerticalAlignment = VerticalAlignment.Bottom;
        buildModSupport.HorizontalAlignment = HorizontalAlignment.Right;
        buildModSupport.Label.Text = "Build Mod File";
        buildModSupport.OnMouseClick += BuildModSupportFile;
        ModPanel.Components.Add(buildModSupport);


        extractAllFilesButton.Size = new Vector2(50, 12);
        extractAllFilesButton.Outline.Enabled = true;
        extractAllFilesButton.BackgroundColor = textColor;
        extractAllFilesButton.VerticalAlignment = VerticalAlignment.Bottom;
        extractAllFilesButton.HorizontalAlignment = HorizontalAlignment.Center;
        extractAllFilesButton.Label.Text = "Extract all Text";
        extractAllFilesButton.OnMouseClick += ExtractAllTextFiles;
        ModPanel.Components.Add(extractAllFilesButton);

        extractFilesButton.Size = new Vector2(60, 12);
        extractFilesButton.Position = new Vector2(5, 117);
        extractFilesButton.Outline.Enabled = true;
        extractFilesButton.BackgroundColor = textColor;
        extractFilesButton.HorizontalAlignment = HorizontalAlignment.Center;
        extractFilesButton.Label.Text = ModManager.GetText("extractText");
        extractFilesButton.Label.ToolTipText = ModManager.GetText("extractTextInfo");
        extractFilesButton.OnMouseClick += ExtractFilesButton_OnMouseClick;
        ModPanel.Components.Add(extractFilesButton);

        modSettingsButton.Size = new Vector2(60, 12);
        modSettingsButton.Position = new Vector2(5, 103);
        modSettingsButton.Outline.Enabled = true;
        modSettingsButton.BackgroundColor = textColor;
        modSettingsButton.HorizontalAlignment = HorizontalAlignment.Center;
        modSettingsButton.Label.Text = ModManager.GetText("settings");
        modSettingsButton.Label.ToolTipText = ModManager.GetText("settingsInfo");
        modSettingsButton.OnMouseClick += ModSettingsButton_OnMouseClick;
        modSettingsButton.Enabled = false;
        ModPanel.Components.Add(modSettingsButton);

        GetLoadedMods();
        UpdateModPanel();
    }

    private void ModsettingCheckboxOnOnToggleState()
    {
        GetLoadedMods();
        UpdateModPanel();
    }

    void modFilterMeButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
    {
        modFilterText = modFilter.Text;

        GetLoadedMods();
        UpdateModPanel();
    }
    void filterMods()
    {
        modFilterText = modFilter.Text;
        GetLoadedMods();
        UpdateModPanel();
    }

    public override void Update()
    {
        base.Update();

        if (modFilter.HasFocus() && (Input.GetKeyDown(KeyCode.Return)))
        {
            SetFocus(null);
            filterMods();
        }

        if (currentSelection != modList.SelectedIndex && modList.Count > 0)
        {
            currentSelection = modList.SelectedIndex;
            UpdateModPanel();
        }

        modListScrollBar.TotalUnits = modList.Count;
        modListScrollBar.DisplayUnits = modList.RowsDisplayed;

        if (modListScrollBar.DraggingThumb)
        {
            modList.ScrollIndex = modListScrollBar.ScrollIndex;
        }
        else
        {
            modListScrollBar.ScrollIndex = modList.ScrollIndex;
        }

        if (moveNextStage)
        {
            moveNextStage = false;
            MoveNextStage();
        }
    }

    bool GetModSettings(ref ModSettings ms)
    {
         if (modList.SelectedIndex < 0 || modList.SelectedIndex > modSettings.Count())
            return false;

         ms = modSettings[modList.SelectedIndex];
         return ms.modInfo != null;
    }

    void GetLoadedMods(bool allMods = false)
    {
        List<Mod> mods = new List<Mod>();
        int n = 0;
        if (!modsettingCheckbox.IsChecked && (allMods || modFilterText.Length == 0))
        {
            modCount.Text = $"{ModManager.Instance.GetAllModsCount()} Mods   ";
            increaseLoadOrderButton.Enabled = true;
            decreaseLoadOrderButton.Enabled = true;
            enableAllButton.Label.Text = "Enable All";
            disableAllButton.Label.Text = "Disable All";
            //enableAllButton.Enabled = true;
            //disableAllButton.Enabled = true;
            mods = ModManager.Instance.GetAllMods().ToList<Mod>();
        }
        else
        {
            increaseLoadOrderButton.Enabled = false;
            decreaseLoadOrderButton.Enabled = false;
            enableAllButton.Label.Text = "Enable Sel.";
            disableAllButton.Label.Text = "Disable Sel.";
            //enableAllButton.Enabled = false;
            //disableAllButton.Enabled = false;
            foreach (var m in ModManager.Instance.GetAllMods())
            {
                Mod pM;
                Mod cM = m;
                pM = ModManager.Instance.patchMods.FirstOrDefault(x => x.ModInfo.GUID == m.ModInfo.GUID && x.HasSettings);
                if (pM != null)
                    cM = pM;
                if ((modFilterText.Length == 0 && modsettingCheckbox.IsChecked && cM.HasSettings) ||
                    (modFilterText.Length > 0 && modsettingCheckbox.IsChecked && cM.HasSettings &&
                     m.ModInfo.ModTitle.ToUpper().Contains(modFilterText.ToUpper())) ||
                    (modFilterText.Length > 0 && !modsettingCheckbox.IsChecked &&
                     m.ModInfo.ModTitle.ToUpper().Contains(modFilterText.ToUpper())))
                {
                    mods.Add(m);
                    n++;
                }
            }
            modCount.Text = $"{mods.Count} of {ModManager.Instance.GetAllModsCount()} Mods   ";
        }

        modList.ClearItems();

        if(modSettings == null || modSettings.Length != mods.Count)
        {
            modSettings = new ModSettings[mods.Count];
        }

        for (int i = 0; i < mods.Count; i++)
        {
            ModSettings modsett = new ModSettings();
            modsett.modInfo = mods[i].ModInfo;
            modsett.enabled = mods[i].Enabled;
            modSettings[i] = modsett;
            modList.AddItem(modsett.modInfo.ModTitle, out ListBox.ListItem item);
            item.textColor = modsett.enabled ? unselectedTextColor : disabledModTextColor;
        }

        if (modList.SelectedIndex < 0 || modList.SelectedIndex >= modList.Count)
        {
            modList.SelectedIndex = 0;
        }
        mods = null;
    }

    void UpdateModPanel()
    {
        modLoadPriorityLabel.Text   = string.Format("{0}: ", ModManager.GetText("modLoadPriority"));
        modTitleLabel.Text          = string.Format("{0}: ", ModManager.GetText("modTitle"));
        modVersionLabel.Text        = string.Format("{0}: ", ModManager.GetText("modVersion"));
        modAuthorLabel.Text         = string.Format("{0}: ", ModManager.GetText("modAuthor"));
        modAuthorContactLabel.Text  = string.Format("{0}: ", ModManager.GetText("modAuthorContact"));
        modDFTFUVersionLabel.Text   = string.Format("{0}: ", ModManager.GetText("modDFTFUVersion"));

        if (modSettings.Length < 1 || currentSelection < 0)
        {
            return;
        }


        ModSettings ms = modSettings[modList.SelectedIndex];

        if (ms.modInfo == null)
            return;

        modEnabledCheckBox.IsChecked = ms.enabled;
        modLoadPriorityLabel.Text   += modList.SelectedIndex;
        modTitleLabel.Text          += ms.modInfo.ModTitle;
        modVersionLabel.Text        += ms.modInfo.ModVersion;
        modAuthorLabel.Text         += ms.modInfo.ModAuthor;
        modAuthorContactLabel.Text  += ms.modInfo.ContactInfo;
        modDFTFUVersionLabel.Text   += ms.modInfo.DFUnity_Version;


        Mod mod = ModManager.Instance.GetMod(ms.modInfo.ModTitle);
        if (modFilter.Text.Length > 0)
            modLoadPriorityLabel.Text += " - " + mod.LoadPriority;

        modDFTFUVersionLabel.TextColor = mod.IsGameVersionSatisfied() == false ? Color.red : DaggerfallUI.DaggerfallDefaultTextColor;

#if UNITY_EDITOR
        if (mod.IsVirtual)
            modTitleLabel.Text += " (debug)";
#endif

        bool hasDescription = !string.IsNullOrWhiteSpace(ms.modInfo.ModDescription);
        showModDescriptionButton.BackgroundColor = hasDescription ? textColor : disabledButtonBackground;

        // Update buttons
        var cM = mod;
        Mod pM;
        pM = ModManager.Instance.patchMods.FirstOrDefault(x => x.ModInfo.GUID == mod.ModInfo.GUID);
        if (pM != null)
            cM = pM;
        if (cM.HasSettings)
        {
            modSettingsButton.Enabled = true;
            showModDescriptionButton.Position = new Vector2(5, 83);
            extractFilesButton.Position = new Vector2(5, 123);
            refreshButton.Position = new Vector2(5, 143);
        }
        else
        {
            modSettingsButton.Enabled = false;
            showModDescriptionButton.Position = new Vector2(5, 95);
            extractFilesButton.Position = new Vector2(5, 117);
            refreshButton.Position = new Vector2(5, 139);
        }
    }

    private void CleanConfigurationDirectory()
    {
        DaggerfallMessageBox clean2ConfigMessageBox = new DaggerfallMessageBox(uiManager, this);

        var unknownDirectories = Directory.GetDirectories(ModManager.Instance.ModDataDirectory)
            .Select(x => new DirectoryInfo(x))
            .Where(x => ModManager.Instance.GetModFromGUID(x.Name) == null)
            .ToArray();
#if !UNITY_EDITOR

        var unknownDirStr = string.Empty;
        if (unknownDirectories.Length > 0)
        {
            unknownDirStr = ModManager.GetText("cleanConfigurationDir");
            foreach (var dir in unknownDirectories)
            {
                unknownDirStr += $"\r{dir.Name}";
            }

            var cleanConfigMessageBox = new DaggerfallMessageBox(uiManager, this);
            cleanConfigMessageBox.ParentPanel.BackgroundTexture = null;
            cleanConfigMessageBox.EnableVerticalScrolling(80);
            cleanConfigMessageBox.SetText(unknownDirStr);
            cleanConfigMessageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
            cleanConfigMessageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
            cleanConfigMessageBox.OnButtonClick += (messageBox, messageBoxButton) =>
            {

                var checkDel = false;
                if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
                {
                    checkDel = true;
                    clean2ConfigMessageBox.ParentPanel.BackgroundTexture = null;
                    clean2ConfigMessageBox.SetText("Are you sure?");
                    clean2ConfigMessageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                    clean2ConfigMessageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
                    clean2ConfigMessageBox.OnButtonClick += (message2Box, messageBox2Button) =>
                    {
                        if (messageBox2Button == DaggerfallMessageBox.MessageBoxButtons.Yes)
                        {
                            foreach (var directory in unknownDirectories)
                                directory.Delete(true);
                        }

                        message2Box.CancelWindow();
                        moveNextStage = true;
                    };
                    

                }
                cleanConfigMessageBox.CloseWindow();
                if (checkDel)
                    uiManager.PushWindow(clean2ConfigMessageBox);
                else
                    moveNextStage = true;
            };
            uiManager.PushWindow(cleanConfigMessageBox);
        }
        else
#endif
        {
            moveNextStage = true;
        }
    }

    public static Mod GetModFromName(string name)
    {
        return ModManager.Instance.Mods.FirstOrDefault(x => x.FileName.Equals(name.ToLower(), StringComparison.Ordinal));
    }

    /* Populate Dependencies flow
         * read config file into table
         * close config file
         * loop through mods
         *      get mod from filename
         *      if mod has dependencies, check if this one exists
         *             if it exists and mod override setting, replace it else error message and continue
         *             if it doesn't exist, add it
         *
         *     Added global mod order configuration.  The file is called mod_order.txt in Daggerfall Unity appdata location

        The format is to use tab delimited
        Mod     Override        modName IsOptional      IsPeer  Version
        Mod = the mod to apply the rule
        Override is true or false and dictates if you overWrite an existing rule
        modName is the dependency for the mod
        IsOptional is true or false - does this mod need to exist in the order
        IsPeer is true or false, if false, dependent mod must be earlier in the load order
        Version is optional but if it exists, it checks for a minimum version for the dependent mod
    */
    private void PopulateDependencies()
    {
        string sep = "\t";
        string conflictStr = "";
        bool conflictFound = false;
        string lineNoQuotes;

        if (ModManager.Instance.mods == null || ModManager.Instance.mods.Count == 0)
            return;

        var filename = Application.persistentDataPath + @"/mod_order.txt";
        if (!File.Exists(filename))
        {
            string str;
            str = "# Add entries to this file using tab delimited spacing.\n";
            str += "#\n";
            str += "# The columns are: Mod     Override        modName IsOptional      IsPeer   IsConflict  Version\n";
            str += "#\n";
            str += "# Mod = the mod to apply the rule.\n";
            str += "# Override is true or false and dictates if you overWrite an existing rule.\n";
            str += "# modName is the dependency for the mod or the order (0 for first or -1 for last) or position (Top, NearTop, NearBottom, Bottom)\n";
            str += "# IsOptional is true or false, does the dependent mod need to exist?\n";
            str += "# IsPeer is true or false, if false, dependent mod must appear earlier in load order\n";
            str += "# IsConflict is true or false, if true, dependent mod should not be run with Mod\n";
            str += "# Version is optional but if it exists, checks that current dependent mod is at least at this version level.\n";
            str += "#\n";
            str += "#\n";
            str += "$Disable Conflicts = false\n";
            str += "\n";
            str += "#Mod\tOverride\tmodName\tIsOptional\tIsPeer\tIsConflict\tVersion\n";
            File.WriteAllText(Application.persistentDataPath + @"/mod_order.txt", str);
            return;
        }

        foreach (string line in System.IO.File.ReadLines(filename))
        {
            lineNoQuotes = line;
            if (line.Contains("\""))
                lineNoQuotes = line.Replace("\"", "");

            if (lineNoQuotes.Length == 0 || lineNoQuotes[0] == '#')
                continue;

            if (lineNoQuotes[0] == '$')
            {
                var flds = lineNoQuotes.Split('=');
                if (flds.Length < 2)
                    continue;

                var key = flds[0].Trim().ToLower();
                var value = flds[1].Trim().ToLower();

                if (key == "$disable conflicts")
                    DisableConflicts = value == "true" ? true : false;
                if (key == "$disablemodwhendependenciesnotavailable")
                    DisableModWhenDependenciesNotAvailable = value == "true" ? true : false;

                continue;
            }

            var fields = lineNoQuotes.Split(sep.ToCharArray());
            if (fields.Length < 2)
                continue;

            var modName = fields[0].Trim();
            var action = fields[1].Trim().ToLower();
            var target = GetModFromName(modName);

            if (action == "ignore in-mod dep checks")
            {
                if (fields.Length < 3)
                    continue;

                if (target != null && target.ModInfo != null && target.ModInfo.Dependencies != null)
                {
                    var dependencies = target.ModInfo.Dependencies;
                    if (dependencies != null)
                    {
                        var depName = fields[2].Trim().ToLower();
                        var newDependencies = new List<ModDependency>();
                        foreach (var dep in dependencies)
                        {
                            if (dep.Name != depName)
                                newDependencies.Add(dep);
                        }

                        target.ModInfo.Dependencies = newDependencies.ToArray();
                    }
                }
                continue;
            }

            if (fields.Length < 3)
                continue;

            if (target != null && target.Enabled)
            {
                var depName = fields[2].Trim().ToLower();
                var depTarget = GetModFromName(depName);
                var pos = 0;

                if (depTarget == null)
                {
                    int a;
                    if (int.TryParse(depName, out a))
                    {
                        Debug.Log($"SortOrder - Changing loadpriority of {target.ModInfo.ModTitle} to {a} from {target.LoadPriority}");
                        ChangePriority(target, a);
                    }
                    else if (depName == "top")
                    {
                        var rangeFrom = 0;
                        var rangeTo = (int)(ModManager.Instance.mods.Count * 0.10f);
                        pos = UnityEngine.Random.Range(rangeFrom, rangeTo);
                        if (target.LoadPriority > rangeTo)
                        {
                            Debug.Log($"SortOrder - Changing load priority of {target.ModInfo.ModTitle} flagged as {fields[2]} to {pos} from {target.LoadPriority} Range for {fields[2]} is {rangeFrom} - {rangeTo}");
                            ChangePriority(target, pos);
                        }
                        else
                        {
                            Debug.Log($"SortOrder - {target.ModInfo.ModTitle} was flagged for |{fields[2]} which is {rangeFrom} - {rangeTo} and already has load order of {target.LoadPriority} - so it was skipped");
                        }
                    }
                    else if (depName == "neartop" || depName == "near top")
                    {
                        var rangeFrom = 0;
                        var rangeTo = (int)(ModManager.Instance.mods.Count * 0.20f);
                        pos = UnityEngine.Random.Range(rangeFrom, rangeTo);
                        if (target.LoadPriority > rangeTo)
                        {
                            Debug.Log($"SortOrder - Changing load priority of {target.ModInfo.ModTitle} flagged as {fields[2]} to {pos} from {target.LoadPriority} Range for {fields[2]} is {rangeFrom} - {rangeTo}");
                            ChangePriority(target, pos);
                        }
                        else
                        {
                            Debug.Log($"SortOrder - {target.ModInfo.ModTitle} was flagged for |{fields[2]} which is {rangeFrom} - {rangeTo} and already has load order of {target.LoadPriority} - so it was skipped");
                        }
                    }
                    else if (depName == "nearbottom" || depName == "near bottom")
                    {
                        var rangeTo = ModManager.Instance.mods.Count;
                        var rangeFrom = (int)(ModManager.Instance.mods.Count * 0.80f);
                        pos = UnityEngine.Random.Range(rangeFrom, rangeTo);
                        if (target.LoadPriority < rangeFrom)
                        {
                            Debug.Log($"SortOrder - Changing load priority of {target.ModInfo.ModTitle} flagged as {fields[2]} to {pos} from {target.LoadPriority} Range for {fields[2]} is {rangeFrom} - {rangeTo}");
                            ChangePriority(target, pos);
                        }
                        else
                        {
                            Debug.Log($"SortOrder - {target.ModInfo.ModTitle} was flagged for |{fields[2]} which is {rangeFrom} - {rangeTo} and already has load order of {target.LoadPriority} - so it was skipped");
                        }
                    }
                    else if (depName == "bottom")
                    {
                        var rangeTo = ModManager.Instance.mods.Count;
                        var rangeFrom = (int)(ModManager.Instance.mods.Count * 0.90f);
                        pos = UnityEngine.Random.Range(rangeFrom, rangeTo);
                        if (target.LoadPriority < rangeFrom)
                        {
                            Debug.Log($"SortOrder - Changing load priority of {target.ModInfo.ModTitle} flagged as {fields[2]} to {pos} from {target.LoadPriority} Range for {fields[2]} is {rangeFrom} - {rangeTo}");
                            ChangePriority(target, pos);
                        }
                        else
                        {
                            Debug.Log($"SortOrder - {target.ModInfo.ModTitle} was flagged for |{fields[2]} which is {rangeFrom} - {rangeTo} and already has load order of {target.LoadPriority} - so it was skipped");
                        }
                    }
                }
                else
                {
                    var isConflict = fields.Length > 5 && fields[5].Trim().ToLower() == "true";
                    if (target.Enabled && depTarget.Enabled && isConflict)
                    {
                        if (DisableConflicts && DaggerfallWorkshop.DaggerfallUnity.Settings.BinarySearch == 0)
                        {
                            conflictStr += $"\r{fields[0]} conflicts with {fields[2]}, {fields[2]} was disabled.";
                            depTarget.Enabled = false;
                        }
                        else
                        {
                            conflictStr += $"\r{fields[0]} conflicts with {fields[2]}, you should disable one of them.\r";
                        }

                        conflictFound = true;
                        continue;
                    }
                }

                var isOptional = fields.Length > 3 && fields[3].Trim().ToLower() == "true";
                var isPeer = fields.Length > 4 && fields[4].Trim().ToLower() == "true";
                string version = null;
                if (fields.Length > 6)
                {
                    version = fields[6].Trim().ToLower();
                    if (!(version.Length > 0 && version[0] >= '0' && version[0] <= '9'))
                        version = null;
                }

                bool found = false;
                if (target.ModInfo.Dependencies != null && target.ModInfo.Dependencies.Length > 0)
                {
                    for (var n = 0; n < target.ModInfo.Dependencies.Length; n++)
                    {
                        if (target.ModInfo.Dependencies[n].Name == depName)
                        {
                            found = true;

                            if (action == "true")
                            {
                                target.ModInfo.Dependencies[n].IsOptional = isOptional;
                                target.ModInfo.Dependencies[n].IsPeer = isPeer;
                                if (fields.Length > 6)
                                    target.ModInfo.Dependencies[n].Version = version;
                            }

                            break;
                        }
                    }

                    if (!found)
                    {
                        var l = target.ModInfo.Dependencies.Length;
                        Array.Resize(ref target.ModInfo.Dependencies, l + 1);
                        target.ModInfo.Dependencies[l].Name = depName;
                        target.ModInfo.Dependencies[l].IsOptional = isOptional;
                        target.ModInfo.Dependencies[l].IsPeer = isPeer;
                        if (fields.Length > 6)
                            target.ModInfo.Dependencies[l].Version = version;
                    }
                }
                else
                {
                    target.ModInfo.Dependencies = new ModDependency[1];
                    target.ModInfo.Dependencies[0].Name = depName;
                    target.ModInfo.Dependencies[0].IsOptional = isOptional;
                    target.ModInfo.Dependencies[0].IsPeer = isPeer;
                    if (fields.Length > 6)
                        target.ModInfo.Dependencies[0].Version = version;
                }
            }
        }

        if (conflictFound)
        {
            var msgBox = new DaggerfallMessageBox(uiManager, this, true);
            msgBox.EnableVerticalScrolling(80);
            msgBox.SetText(conflictStr);

            msgBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.OK);
            msgBox.OnButtonClick += (sender, button) =>
            {
                Debug.LogWarning(conflictStr);
                sender.CancelWindow();
            };

            msgBox.Show();
        }
        return;
    }
    private void ChangePriority(Mod target, int dest)
    {
        var origin = ModManager.Instance.GetLoadPriority(target.FileName);
        if (dest >= 0)
        {
            if (origin < dest)
            {
                if (dest >= ModManager.Instance.mods.Count)
                    dest = ModManager.Instance.mods.Count - 1;
                for (int i = origin; i < dest; i++)
                {
                    var m1 = ModManager.Instance.mods[i];
                    var m2 = ModManager.Instance.mods[i + 1];
                    m1.LoadPriority += 1;
                    m2.LoadPriority -= 1;
                    ModManager.Instance.mods[i] = m2;
                    ModManager.Instance.mods[i + 1] = m1;
                }
            }
            else if (origin > dest)
            {
                if (dest > ModManager.Instance.mods.Count)
                    dest = ModManager.Instance.mods.Count;

                for (int i = origin; i > dest; i--)
                {
                    var m1 = ModManager.Instance.mods[i];
                    var m2 = ModManager.Instance.mods[i - 1];
                    m1.LoadPriority -= 1;
                    m2.LoadPriority += 1;
                    ModManager.Instance.mods[i] = m2;
                    ModManager.Instance.mods[i - 1] = m1;
                }
            }
        }
        else
        {
            dest = ModManager.Instance.mods.Count + dest;
            if (dest < 0)
                return;
            if (origin >= dest)
                return;
            
            for (int i = origin; i < dest; i++)
            {
                var m1 = ModManager.Instance.mods[i];
                var m2 = ModManager.Instance.mods[i + 1];
                m1.LoadPriority += 1;
                m2.LoadPriority -= 1;
                ModManager.Instance.mods[i] = m2;
                ModManager.Instance.mods[i + 1] = m1;
            }
        }
    }

    private void CheckDependencies()
    {
        bool hasSortIssues = false;
        List<string> errorMessages = null;
        var modErrorMessages = new List<string>();
        PopulateDependencies();
        foreach (Mod mod in ModManager.Instance.Mods.Where(x => x.Enabled))
        {
            bool? isGameVersionSatisfied = mod.IsGameVersionSatisfied();
            if (!isGameVersionSatisfied.HasValue)
                Debug.LogErrorFormat("Mod {0} requires unknown game version ({1}).", mod.Title, mod.ModInfo.DFUnity_Version);
            else if (!isGameVersionSatisfied.Value)
                modErrorMessages.Add(string.Format(ModManager.GetText("gameVersionUnsatisfied"), mod.ModInfo.DFUnity_Version));

            ModManager.Instance.CheckModDependencies(mod, modErrorMessages, ref hasSortIssues);
            if (modErrorMessages.Count > 0)
            {
                if (errorMessages == null)
                {
                    errorMessages = new List<string>();
                    errorMessages.Add(ModManager.GetText("dependencyErrorMessage"));
                    errorMessages.Add(string.Empty);
                }

                errorMessages.Add(string.Format("Error {0}", mod.Title));
                errorMessages.AddRange(modErrorMessages);
                errorMessages.Add(string.Empty);
                modErrorMessages.Clear();
            }
        }

        if (errorMessages != null && errorMessages.Count > 0)
        {
            foreach (var errorMsg in errorMessages)
            {
                Debug.Log(errorMsg);
            }

            if (hasSortIssues)
                errorMessages.Add(ModManager.GetText("sortModsQuestion"));

            var messageBox = new DaggerfallMessageBox(uiManager, this);
            messageBox.EnableVerticalScrolling(80);
            messageBox.SetText(errorMessages.ToArray());
            if (hasSortIssues)
            {
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
                messageBox.OnButtonClick += (sender, button) =>
                {
                    if (button == DaggerfallMessageBox.MessageBoxButtons.Yes)
                    {
                        ModManager.Instance.AutoSortMods();
                        Debug.Log($"Mods have been sorted automatically: Errors Encountered {ModManager.ErrorsEncountered}");
                    }

                    sender.CancelWindow();
                    moveNextStage = true;
                };
            }
            else
            {
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.OK, true);
                messageBox.OnButtonClick += (sender, button) =>
                {
                    sender.CancelWindow();
                    moveNextStage = true;
                };
            }
            messageBox.Show();
        }
        else
        {
            moveNextStage = true;
        }
    }

    private void SaveAndClose()
    {
        ModManager.WriteModSettings();
        CloseWindow();
    }

    private void MoveNextStage()
    {
        if (DaggerfallUnity.Settings.BinarySearch > 0)
            currentStage = (Stage)2;
        
        switch (currentStage = (Stage)((int)currentStage + 1))
        {
            case Stage.Cleanup:
                CleanConfigurationDirectory();
                break;
            case Stage.CheckDependencies:
                CheckDependencies();
                break;
            default:
                SaveAndClose();
                break;
        }
    }

#endregion

#region Events

    void DecreaseLoadOrderButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
    {
        if (modList.Count < 2)
            return;
        else if (modList.SelectedIndex == modList.Count - 1)    //last index already
            return;

        modList.SwapItems(modList.SelectedIndex, modList.SelectedIndex + 1);

        ModSettings temp = modSettings[modList.SelectedIndex];
        modSettings[modList.SelectedIndex] = modSettings[modList.SelectedIndex + 1];
        modSettings[modList.SelectedIndex + 1] = temp;

        var m1 = ModManager.Instance.mods[modList.SelectedIndex];
        var m2 = ModManager.Instance.mods[modList.SelectedIndex + 1];
        m1.LoadPriority += 1;
        m2.LoadPriority -= 1;
        ModManager.Instance.mods[modList.SelectedIndex] = m2;
        ModManager.Instance.mods[modList.SelectedIndex+ 1] = m1;
        modList.SelectedIndex++;
    }

    void IncreaseLoadOrderButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
    {
        if (modList.Count < 2)
            return;
        else if (modList.SelectedIndex == 0)    //first priority already
            return;

        modList.SwapItems(modList.SelectedIndex, modList.SelectedIndex - 1);
        ModSettings temp = modSettings[modList.SelectedIndex];
        modSettings[modList.SelectedIndex] = modSettings[modList.SelectedIndex - 1];
        modSettings[modList.SelectedIndex - 1] = temp;
        var m1 = ModManager.Instance.mods[modList.SelectedIndex -1];
        var m2 = ModManager.Instance.mods[modList.SelectedIndex];
        m1.LoadPriority += 1;
        m2.LoadPriority -= 1;
        ModManager.Instance.mods[modList.SelectedIndex - 1] = m2;
        ModManager.Instance.mods[modList.SelectedIndex] = m1;


        modList.SelectedIndex--;
    }

    void RefreshButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
    {
        ModManager.Instance.Refresh();
        int count = modSettings.Length;
        GetLoadedMods();
        if (modSettings.Length != count)
            currentSelection = -1;
        UpdateModPanel();
    }

    void SaveAndCloseButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
    {
        if (modSettings == null)
        {
            return;
        }

        GetLoadedMods(true);

        for (int i = 0; i < modSettings.Length; i++)
        {
            Mod mod = ModManager.Instance.GetMod(modSettings[i].modInfo.ModTitle);
            if (mod == null)
                continue;
            mod.Enabled = modSettings[i].enabled;
            mod.LoadPriority = i;
            mod = null;
        }

        MoveNextStage();
    }

    void ClearDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;

        System.IO.DirectoryInfo di = new DirectoryInfo(path);

        foreach (FileInfo file in di.GetFiles())
        {
            try
            {
                file.Delete();
            }
            catch(Exception)
            {

            }
        }
        foreach (DirectoryInfo dir in di.GetDirectories())
        {
            try
            {
                dir.Delete(true);
            }
            catch (Exception)
            {

            }
        }
    }

    void ExtractFilesButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
    {
        if (modSettings.Length < 1)
            return;

        Mod mod = ModManager.Instance.GetMod(modSettings[modList.SelectedIndex].modInfo.ModTitle);

        if (mod == null)
        {
            return;
        }

        string[] assets = mod.AssetNames;
        if (assets == null)
            return;

        string path = Path.Combine(DaggerfallUnityApplication.PersistentDataPath, "Mods", "ExtractedFiles", mod.FileName);

        ClearDirectory(path);

        Directory.CreateDirectory(path);

        for (int i = 0; i < assets.Length; i++)
        {
            string extension = Path.GetExtension(assets[i]);
            //if (!ModManager.textExtensions.Contains(extension))
              //  continue;

            var asset = mod.GetAsset<TextAsset>(assets[i]);
            if (asset == null)
                continue;

            if (assets[i].EndsWith(".bytes", StringComparison.Ordinal))
            {
                // Export binary asset without .bytes extension
                File.WriteAllBytes(Path.Combine(path, asset.name), asset.bytes);
            }
            else if (assets[i].EndsWith(".cs.txt", StringComparison.Ordinal))
            {
                // Export C# script without .txt extension
                File.WriteAllText(Path.Combine(path, asset.name), asset.text);
            }
            else
            {
                // Export text asset with original extension
                File.WriteAllText(Path.Combine(path, asset.name + extension), asset.text);
            }
        }

        var messageBox = new DaggerfallMessageBox(uiManager, this, true);
        messageBox.AllowCancel = true;
        messageBox.ClickAnywhereToClose = true;
        messageBox.ParentPanel.BackgroundTexture = null;
        messageBox.SetText(string.Format(ModManager.GetText("extractTextConfirmation"), path));
        uiManager.PushWindow(messageBox);
    }

    void BackButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
    {
        DaggerfallUI.UIManager.PopWindow();
    }

    void EnableAllButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
    {
        if (modSettings == null || modSettings.Length < 1)
            return;

        for (int i = 0; i < modSettings.Length; i++)
        {
            modSettings[i].enabled = true;
            modList.GetItem(i).textColor = unselectedTextColor;
            var m =
                ModManager.Instance.mods.Where(x => x.ModInfo.ModTitle == modSettings[i].modInfo.ModTitle).ToArray();
            if (m != null && m.Length > 0)
            {
                m[0].Enabled = true;
            }
        }

        UpdateModPanel();
    }

    void DisableAllButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
    {
        if (modSettings == null || modSettings.Length < 1)
            return;

        for (int i = 0; i < modSettings.Length; i++)
        {
            modSettings[i].enabled = false;
            modList.GetItem(i).textColor = disabledModTextColor;
            var m =
                ModManager.Instance.mods.Where(x => x.ModInfo.ModTitle == modSettings[i].modInfo.ModTitle).ToArray();
            if (m != null && m.Length > 0)
            {
                m[0].Enabled = false;
            }
        }

        UpdateModPanel();
    }

    private void CopyToClipboardButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
    {
        StringBuilder text = new StringBuilder();
        text.Append(String.Format("{0} {1} {2}\r\n", VersionInfo.DaggerfallUnityProductName, VersionInfo.DaggerfallUnityStatus, VersionInfo.DaggerfallUnityVersion));
        for (int i = 0; i < modSettings.Length; i++)
        {
            text.Append(String.Format("[{0}] {1} ({2}) - GUID {3}\r\n", modSettings[i].enabled ? 'x' : ' ', modSettings[i].modInfo.ModTitle, modSettings[i].modInfo.ModVersion, modSettings[i].modInfo.GUID));
        }
        UnityEngine.TextEditor textEditor = new UnityEngine.TextEditor();
        textEditor.text = text.ToString();
        textEditor.SelectAll();
        textEditor.Copy();

        DaggerfallMessageBox CopiedToClipboardMessageBox = new DaggerfallMessageBox(uiManager, this, true);
        CopiedToClipboardMessageBox.ClickAnywhereToClose = true;
        CopiedToClipboardMessageBox.ParentPanel.BackgroundTexture = null;

        CopiedToClipboardMessageBox.SetText(ModManager.GetText("modsCopiedToClipboard"));
        uiManager.PushWindow(CopiedToClipboardMessageBox);
    }


    void BuildModSupportFile(BaseScreenComponent sender, Vector2 position)
    {
        var confirmBox = new DaggerfallMessageBox(uiManager, this, true);
        confirmBox.AllowCancel = true;
        confirmBox.ClickAnywhereToClose = true;
        confirmBox.ParentPanel.BackgroundTexture = null;
        confirmBox.SetText("Build Mod Support is a long running process. Are you sure?");
        confirmBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
        confirmBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);

        confirmBox.OnButtonClick += (messageBox, button) =>
        {
            messageBox.CancelWindow();
            if (button != DaggerfallMessageBox.MessageBoxButtons.Yes)
                return;

            try
            {
                ModManager.Instance.SortMods();

                string path = Path.Combine(Application.persistentDataPath, "Mods");
                string csvPath = Path.Combine(path, "mods.csv");
                Directory.CreateDirectory(path);

                var mods = ModManager.Instance.Mods.ToList();
                var assetCount = new Dictionary<string, int>(StringComparer.Ordinal);

                for (int i = 0; i < mods.Count; i++)
                {
                    var assetNames = mods[i].AssetNames;
                    if (assetNames == null || assetNames.Length == 0)
                        continue;

                    for (int n = 0; n < assetNames.Length; n++)
                    {
                        int count;
                        if (assetCount.TryGetValue(assetNames[n], out count))
                            assetCount[assetNames[n]] = count + 1;
                        else
                            assetCount.Add(assetNames[n], 1);
                    }
                }

                var sb = new System.Text.StringBuilder(262144);
                sb.AppendLine("Mod Title,Mod Filename,Enabled,Load Priority, Asset List, Count");

                for (int i = 0; i < mods.Count; i++)
                {
                    var mod = mods[i];
                    var safeTitle = (mod.Title ?? string.Empty).Trim().Replace(',', '-');
                    var safeFileName = (mod.FileName ?? string.Empty).Trim().Replace(',', '-');

                    sb.Append(safeTitle).Append(',')
                        .Append(safeFileName).Append(',')
                        .Append(mod.Enabled).Append(',')
                        .Append(mod.LoadPriority).AppendLine();

                    var assetNames = mod.AssetNames;
                    if (assetNames == null || assetNames.Length == 0)
                        continue;

                    for (int n = 0; n < assetNames.Length; n++)
                    {
                        int cnt;
                        if (!assetCount.TryGetValue(assetNames[n], out cnt))
                            cnt = 1;

                        var safeAsset = (assetNames[n] ?? string.Empty).Trim().Replace(',', '-');

                        sb.Append(safeTitle).Append(',')
                            .Append(safeFileName).Append(',')
                            .Append(mod.Enabled).Append(',')
                            .Append(mod.LoadPriority).Append(',')
                            .Append(safeAsset).Append(',')
                            .Append(cnt).AppendLine();
                    }
                }

                File.WriteAllText(csvPath, sb.ToString());

                var completeBox = new DaggerfallMessageBox(uiManager, this, true);
                completeBox.AllowCancel = true;
                completeBox.ClickAnywhereToClose = true;
                completeBox.ParentPanel.BackgroundTexture = null;
                completeBox.SetText("process complete");
                uiManager.PushWindow(completeBox);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Build Mod Support failed: {ex.Message}");
            }
        };

        uiManager.PushWindow(confirmBox);
    }


    void ExtractAllTextFiles(BaseScreenComponent sender, Vector2 position)
    {
        var confirmBox = new DaggerfallMessageBox(uiManager, this, true);
        confirmBox.AllowCancel = true;
        confirmBox.ClickAnywhereToClose = true;
        confirmBox.ParentPanel.BackgroundTexture = null;
        confirmBox.SetText("Extract all Text is a long running process. Are you sure?");
        confirmBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
        confirmBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);

        confirmBox.OnButtonClick += (messageBox, button) =>
        {
            messageBox.CancelWindow();
            if (button != DaggerfallMessageBox.MessageBoxButtons.Yes)
                return;

            ModManager.Instance.SortMods();

            var mods = ModManager.Instance.Mods.ToList();
            var extractRoot = Path.Combine(Application.persistentDataPath, "Mods", "ExtractedFiles");

            for (int m = 0; m < mods.Count; m++)
            {
                var mod = mods[m];
                string path = string.Empty;

                try
                {
                    var assets = mod.AssetNames;
                    if (assets == null || assets.Length == 0)
                        continue;

                    path = Path.Combine(extractRoot, mod.FileName);
                    ClearDirectory(path);
                    Directory.CreateDirectory(path);

                    for (int i = 0; i < assets.Length; i++)
                    {
                        var assetName = assets[i];
                        var asset = mod.GetAsset<TextAsset>(assetName);
                        if (asset == null)
                            continue;

                        if (assetName.EndsWith(".bytes", StringComparison.Ordinal))
                        {
                            File.WriteAllBytes(Path.Combine(path, asset.name), asset.bytes);
                        }
                        else if (assetName.EndsWith(".cs.txt", StringComparison.Ordinal))
                        {
                            File.WriteAllText(Path.Combine(path, asset.name), asset.text);
                        }
                        else
                        {
                            var extension = Path.GetExtension(assetName);
                            File.WriteAllText(Path.Combine(path, asset.name + extension), asset.text);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log($"Mod Extract - unable to extract text for {mod.Title} at {path}, error {e.Message}");
                }
            }

            var completeBox = new DaggerfallMessageBox(uiManager, this, true);
            completeBox.AllowCancel = true;
            completeBox.ClickAnywhereToClose = true;
            completeBox.ParentPanel.BackgroundTexture = null;
            completeBox.SetText("process complete");
            uiManager.PushWindow(completeBox);
        };

        uiManager.PushWindow(confirmBox);
    }

    string RemoveComma(string str)
    {
        string[] strArray = str.Split(',');
        return String.Join(" ", strArray);
    }

    void ShowModDescriptionPopUp_OnMouseClick(BaseScreenComponent sender, Vector2 position)
    {
        if (modSettings == null || modSettings.Length < 1)
            return;
        else if (string.IsNullOrWhiteSpace(modSettings[currentSelection].modInfo.ModDescription))
            return;

        ModDescriptionMessageBox = new DaggerfallMessageBox(uiManager, this, true);
        ModDescriptionMessageBox.AllowCancel = true;
        ModDescriptionMessageBox.ClickAnywhereToClose = true;
        ModDescriptionMessageBox.ParentPanel.BackgroundTexture = null;

        Mod mod = ModManager.Instance.GetMod(modSettings[currentSelection].modInfo.ModTitle);
        List<string> modDescription = (mod.TryLocalize("Mod", "Description") ?? mod.ModInfo.ModDescription).Split('\n').ToList();
        modDescription.Add("");
        modDescription.Add(mod.ModInfo.GUID);
        UnityEngine.TextEditor textEditor = new UnityEngine.TextEditor();
        textEditor.text = mod.ModInfo.GUID;
        textEditor.SelectAll();
        textEditor.Copy();
        ModDescriptionMessageBox.SetText(modDescription.ToArray());
        uiManager.PushWindow(ModDescriptionMessageBox);
    }

    void ModSettingsButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
    {
        Mod mod = ModManager.Instance.GetMod(modSettings[modList.SelectedIndex].modInfo.ModTitle);
        if (ModManager.Instance.patchMods.Any(x => x.ModInfo.GUID == mod.ModInfo.GUID))
            mod = ModManager.Instance.patchMods.FirstOrDefault(x => x.ModInfo.GUID == mod.ModInfo.GUID);
        ModSettingsWindow modSettingsWindow = new ModSettingsWindow(DaggerfallUI.UIManager, mod);
        DaggerfallUI.UIManager.PushWindow(modSettingsWindow);
    }

    void ModEnabledCheckBox_OnToggleState()
    {
        if (modSettings == null || modSettings.Length < 1)
            return;

        ModSettings ms = modSettings[modList.SelectedIndex];

        if (ms.modInfo == null)
            return;

        modSettings[modList.SelectedIndex].enabled = modEnabledCheckBox.IsChecked;
        modList.SelectedValue.textColor = modEnabledCheckBox.IsChecked ? unselectedTextColor : disabledModTextColor;

        var m = ModManager.Instance.GetMod(modSettings[modList.SelectedIndex].modInfo.ModTitle);
        m.Enabled = modSettings[modList.SelectedIndex].enabled;

        UpdateModPanel();
    }

    private void ModsNextButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
    {
        if (modSettings == null || modSettings.Length < 1)
            return;

        int current = modList.SelectedIndex;
        while ((current = (current + 1) % modSettings.Length) != modList.SelectedIndex)
        {
            if (modSettings[current].modInfo.ModTitle.IndexOf(modsSearch.Text, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ScrollToSearchMatch(current);
                return;
            }
        }
        SearchNoMatchFound();
    }

    private void ModsPreviousButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
    {
        if (modSettings == null || modSettings.Length < 1)
            return;

        int current = modList.SelectedIndex;
        while ((current = (current + modSettings.Length - 1) % modSettings.Length) != modList.SelectedIndex)
        {
            if (modSettings[current].modInfo.ModTitle.IndexOf(modsSearch.Text, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ScrollToSearchMatch(current);
                return;
            }
        }
        SearchNoMatchFound();
    }

    private void ScrollToSearchMatch(int modIndex)
    {
        modList.SelectedIndex = modIndex;
        if (modIndex < modList.ScrollIndex)
            modList.ScrollIndex = modIndex;
        else if (modIndex > modList.ScrollIndex + modList.RowsDisplayed - 1)
            modList.ScrollIndex = modIndex - (modList.RowsDisplayed - 1);
        UpdateModPanel();
    }

    private void SearchNoMatchFound()
    {
        DaggerfallMessageBox NoMatchingMessageBox = new DaggerfallMessageBox(uiManager, this, true);
        NoMatchingMessageBox.ClickAnywhereToClose = true;
        NoMatchingMessageBox.ParentPanel.BackgroundTexture = null;

        NoMatchingMessageBox.SetText(ModManager.GetText("modsNoMatching"));
        uiManager.PushWindow(NoMatchingMessageBox);
    }

    void ModList_OnScroll()
    {
        modListScrollBar.ScrollIndex = modList.ScrollIndex;
    }

    void ModListScrollBar_OnScroll()
    {
        modList.ScrollIndex = modListScrollBar.ScrollIndex;
    }

#endregion
}
