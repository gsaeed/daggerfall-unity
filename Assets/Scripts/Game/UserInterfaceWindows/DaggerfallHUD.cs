// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2023 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:
//
// Notes:
//

using System;
using System.Diagnostics;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using UnityEngine;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Utility;
using Debug = UnityEngine.Debug;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    /// <summary>
    /// Implements heads-up-display for default view mode.
    /// TODO: Migrate to full DaggerfallBaseWindow setup
    /// </summary>
    public class DaggerfallHUD : DaggerfallBaseWindow
    {
        const int midScreenTextDefaultY = 146;

        Color realArrowsColor = new Color(0.6f, 0.6f, 0.6f);
        Color conjuredArrowsColor = new Color(0.18f, 0.32f, 0.48f, 0.5f);

        float crosshairScale = 0.75f;

        PopupText popupText = new PopupText();
        TextLabel midScreenTextLabel = new TextLabel();
        TextLabel arrowCountTextLabel = new TextLabel();
        private TextLabel poisonTextLabel = new TextLabel();
        private TextLabel WeaponHealthTextLabel = new TextLabel();
        private TextLabel SelectedSpellLabel = new TextLabel();
        HUDCrosshair crosshair = new HUDCrosshair();
        public HUDVitals vitals = new HUDVitals();
        HUDBreathBar breathBar = new HUDBreathBar();
        HUDCompass compass = new HUDCompass();
        HUDFlickerController flickerController = new HUDFlickerController();
        HUDInteractionModeIcon interactionModeIcon;
        HUDPlaceMarker placeMarker = new HUDPlaceMarker();
        EscortingNPCFacePanel escortingFaces = new EscortingNPCFacePanel();
        HUDQuestDebugger questDebugger = new HUDQuestDebugger();
        HUDActiveSpells activeSpells = new HUDActiveSpells();
        HUDLarge largeHUD = new HUDLarge();
        Panel debugPanel = null;
        bool renderHUD = true;
        bool startupComplete = false;

        float midScreenTextTimer = -1;
        float midScreenTextDelay = 1.5f;

        public bool ShowPopupText { get; set; }
        public bool ShowMidScreenText { get; set; }
        public bool ShowCrosshair { get; set; }
        public bool ShowVitals { get; set; }
        public bool ShowBreathBar { get; set; }
        public bool ShowCompass { get; set; }
        public bool ShowInteractionModeIcon { get; set; }
        public bool ShowLocalQuestPlaces { get; set; }
        public bool ShowEscortingFaces { get; set; }
        public bool ShowActiveSpells { get; set; }
        public bool ShowArrowCount { get; set; }

        public HUDLarge LargeHUD
        {
            get { return largeHUD; }
        }

        public PopupText PopupText
        {
            get { return popupText; }
        }

        public float CrosshairScale
        {
            get { return crosshairScale; }
            set { crosshairScale = value; }
        }

        public HUDVitals HUDVitals
        {
            get { return vitals; }
        }

        public HUDBreathBar HUDBreathBar
        {
            get { return breathBar; }
        }

        public HUDCompass HUDCompass
        {
            get { return compass; }
        }

        public EscortingNPCFacePanel EscortingFaces
        {
            get { return escortingFaces; }
        }

        public HUDPlaceMarker PlaceMarker
        {
            get { return placeMarker; }
        }

        public HUDQuestDebugger QuestDebugger
        {
            get { return questDebugger; }
        }

        public HUDActiveSpells ActiveSpells
        {
            get { return activeSpells; }
        }

        // Extra debug panel used by random systems. Only one active at a time
        public Panel DebugPanel
        {
            get
            {
                return debugPanel;
            }

            set
            {
                if(debugPanel != null)
                {
                    ParentPanel.Components.Remove(debugPanel);
                }

                debugPanel = value;

                if(debugPanel != null)
                {
                    ParentPanel.Components.Add(debugPanel);
                }
            }
        }

        public DaggerfallHUD(IUserInterfaceManager uiManager)
            :base(uiManager)
        {
            interactionModeIcon = new HUDInteractionModeIcon(crosshair);
            parentPanel.BackgroundColor = Color.clear;
            ShowPopupText = true;
            ShowMidScreenText = true;
            ShowCrosshair = DaggerfallUnity.Settings.Crosshair;
            ShowVitals = true;
            ShowBreathBar = true;
            ShowCompass = true;
            ShowInteractionModeIcon = DaggerfallUnity.Settings.InteractionModeIcon.ToLower() != "none";
            ShowEscortingFaces = true;
            ShowLocalQuestPlaces = true;
            ShowActiveSpells = true;
            ShowArrowCount = DaggerfallUnity.Settings.EnableArrowCounter;

            ParentPanel.Components.Add(largeHUD);
            ParentPanel.Components.Add(crosshair);
            ParentPanel.Components.Add(vitals);
            ParentPanel.Components.Add(breathBar);
            ParentPanel.Components.Add(compass);
            ParentPanel.Components.Add(interactionModeIcon);
            ParentPanel.Components.Add(flickerController);
        }

        protected override void Setup()
        {
            activeSpells.Size = NativePanel.Size;
            activeSpells.HorizontalAlignment = HorizontalAlignment.Center;
            NativePanel.Components.Add(activeSpells);

            popupText.Size = NativePanel.Size;
            NativePanel.Components.Add(popupText);

            midScreenTextLabel.HorizontalAlignment = HorizontalAlignment.Center;
            midScreenTextLabel.Position = new Vector2(0, midScreenTextDefaultY);
            NativePanel.Components.Add(midScreenTextLabel);

            placeMarker.Size = new Vector2(640, 400);
            placeMarker.AutoSize = AutoSizeModes.ScaleFreely;
            ParentPanel.Components.Add(placeMarker);

            escortingFaces.Size = NativePanel.Size;
            escortingFaces.AutoSize = AutoSizeModes.ScaleToFit;
            ParentPanel.Components.Add(escortingFaces);

            questDebugger.Size = new Vector2(640, 400);
            questDebugger.AutoSize = AutoSizeModes.ScaleToFit;
            ParentPanel.Components.Add(questDebugger);

            arrowCountTextLabel.TextColor = realArrowsColor;
            arrowCountTextLabel.ShadowPosition = Vector2.zero;
            ParentPanel.Components.Add(arrowCountTextLabel);

            poisonTextLabel.TextColor = Color.green;
            poisonTextLabel.ShadowPosition = Vector2.zero;
            poisonTextLabel.Text = "Poison";
            parentPanel.Components.Add(poisonTextLabel);

            WeaponHealthTextLabel.TextColor = Color.green;
            WeaponHealthTextLabel.ShadowPosition = Vector2.zero;
            WeaponHealthTextLabel.Text = "XX";
            parentPanel.Components.Add(WeaponHealthTextLabel);

            SelectedSpellLabel.TextColor = Color.green;
            SelectedSpellLabel.ShadowPosition = Vector2.zero;
            SelectedSpellLabel.Text = "Spell:";
            parentPanel.Components.Add(SelectedSpellLabel);

        }

        public override void Update()
        {
            // Update HUD visibility
            popupText.Enabled = ShowPopupText;
            midScreenTextLabel.Enabled = ShowMidScreenText;
            crosshair.Enabled = ShowCrosshair;
            if (GameManager.Instance.PlayerEntity.CurrentHealth == GameManager.Instance.PlayerEntity.MaxHealth &&
                GameManager.Instance.PlayerEntity.CurrentMagicka == GameManager.Instance.PlayerEntity.MaxMagicka &&
                (GameManager.Instance.PlayerEntity.CurrentFatigue == GameManager.Instance.PlayerEntity.MaxFatigue ||
                 !DaggerfallUnity.Settings.ShowVitalsWhenFatigueBelowMaximum))
                vitals.Enabled = false;
            else
                vitals.Enabled = ShowVitals;
            breathBar.Enabled = ShowBreathBar;
            compass.Enabled = ShowCompass;
            interactionModeIcon.Enabled = ShowInteractionModeIcon;
            placeMarker.Enabled = ShowLocalQuestPlaces;
            escortingFaces.EnableBorder = ShowEscortingFaces;
            questDebugger.Enabled = !(questDebugger.State == HUDQuestDebugger.DisplayState.Nothing);
            activeSpells.Enabled = ShowActiveSpells;

            poisonTextLabel.Enabled = true;
            SelectedSpellLabel.Enabled = true;
            WeaponHealthTextLabel.Enabled = true;

            // Large HUD will force certain other HUD elements off as they conflict in space or utility
            bool largeHUDwasEnabled = largeHUD.Enabled;
            bool largeHUDEnabled = DaggerfallUnity.Settings.LargeHUD;
            if (largeHUDEnabled)
            {
                largeHUD.Enabled = true;
                vitals.Enabled = false;
                compass.Enabled = false;
                interactionModeIcon.Enabled = false;

                // Automatically scale to fit screen width or use custom scale
                largeHUD.AutoSize = (DaggerfallUnity.Settings.LargeHUDDocked) ? AutoSizeModes.ScaleToFit : AutoSizeModes.Scale;

                // Alignment when large HUD is undocked - 0=None/Default (centred), 1=Left, 2=Center, 3=Right
                if (!DaggerfallUnity.Settings.LargeHUDDocked)
                {
                    largeHUD.HorizontalAlignment = (HorizontalAlignment)DaggerfallUnity.Settings.LargeHUDUndockedAlignment;
                    if (largeHUD.HorizontalAlignment == HorizontalAlignment.None)
                        largeHUD.HorizontalAlignment = HorizontalAlignment.Center;
                }
            }
            else
            {
                largeHUD.Enabled = false;
            }

            if (largeHUDEnabled != largeHUDwasEnabled)
                RaiseOnLargeHUDToggleEvent();

            // Scale large HUD
            largeHUD.CustomScale = NativePanel.LocalScale;
            if (!DaggerfallUnity.Settings.LargeHUDDocked)
                largeHUD.CustomScale *= DaggerfallUnity.Settings.LargeHUDUndockedScale;

            // Scale HUD elements
            compass.Scale = NativePanel.LocalScale;
            vitals.Scale = new Vector2(NativePanel.LocalScale.x * DaggerfallUnity.Settings.DisplayHUDScaleAdjust, NativePanel.LocalScale.y * DaggerfallUnity.Settings.DisplayHUDScaleAdjust);
            breathBar.Scale = new Vector2(NativePanel.LocalScale.x * DaggerfallUnity.Settings.DisplayHUDScaleAdjust, NativePanel.LocalScale.y * DaggerfallUnity.Settings.DisplayHUDScaleAdjust);
            crosshair.CrosshairScale = CrosshairScale;

            interactionModeIcon.Scale = NativePanel.LocalScale;
            arrowCountTextLabel.Scale = new Vector2(NativePanel.LocalScale.x * DaggerfallUnity.Settings.DisplayHUDScaleAdjust, NativePanel.LocalScale.y * DaggerfallUnity.Settings.DisplayHUDScaleAdjust);
            poisonTextLabel.Scale =  new Vector2(NativePanel.LocalScale.x * DaggerfallUnity.Settings.DisplayHUDScaleAdjust, NativePanel.LocalScale.y * DaggerfallUnity.Settings.DisplayHUDScaleAdjust);
            WeaponHealthTextLabel.Scale =  new Vector2(NativePanel.LocalScale.x * DaggerfallUnity.Settings.DisplayHUDScaleAdjust, NativePanel.LocalScale.y * DaggerfallUnity.Settings.DisplayHUDScaleAdjust);
            SelectedSpellLabel.Scale =  new Vector2(NativePanel.LocalScale.x * DaggerfallUnity.Settings.DisplayHUDScaleAdjust, NativePanel.LocalScale.y * DaggerfallUnity.Settings.DisplayHUDScaleAdjust);

            interactionModeIcon.displayScaleAdjust = DaggerfallUnity.Settings.DisplayHUDScaleAdjust;



            // Align compass to screen panel
            Rect screenRect = ParentPanel.Rectangle;
            float compassX = screenRect.x + screenRect.width - (compass.Size.x);
            float compassY = screenRect.y + screenRect.height - (compass.Size.y);
            compass.Position = new Vector2(compassX, compassY);

            // Update midscreen text timer and remove once complete
            if (midScreenTextTimer != -1)
            {
                midScreenTextTimer += Time.deltaTime;
                if (midScreenTextTimer > midScreenTextDelay)
                {
                    midScreenTextTimer = -1;
                    midScreenTextLabel.Text = string.Empty;
                }
            }

            // Update arrow count if player holding an unsheathed bow
            // TODO: Find a spot for arrow counter when large HUD enabled (remembering player could be in 320x200 retro mode)
            arrowCountTextLabel.Enabled = false;
            EquipSlots slot = DaggerfallUnity.Settings.BowLeftHandWithSwitching ? EquipSlots.LeftHand : EquipSlots.RightHand;
            DaggerfallUnityItem held = GameManager.Instance.PlayerEntity.ItemEquipTable.GetItem(slot);
            EquipSlots otherSlot = !DaggerfallUnity.Settings.BowLeftHandWithSwitching ? EquipSlots.LeftHand : EquipSlots.RightHand;
            DaggerfallUnityItem otherHeld = GameManager.Instance.PlayerEntity.ItemEquipTable.GetItem(otherSlot);


            if (!largeHUDEnabled && ShowArrowCount && (!FormulaHelper.PlayerWeaponSheathed()))
            {

                if (otherHeld != null && otherHeld.TemplateIndex == 289)
                    held = otherHeld;

                if (held != null && held.ItemGroup == ItemGroups.Weapons &&
                    (held.TemplateIndex == (int)Weapons.Long_Bow || held.TemplateIndex == (int)Weapons.Short_Bow || held.TemplateIndex == 289))
                {
                    // Arrow count label position is offset to left of compass and centred relative to compass height
                    // This is done every frame to handle adaptive resolutions
                    Vector2 arrowLabelPos = new Vector2(screenRect.width, screenRect.height);
                    arrowLabelPos.x -= compass.Size.x + arrowCountTextLabel.TextWidth + 8;
                    arrowLabelPos.y -= compass.Size.y / 2 + arrowCountTextLabel.TextHeight / 2;

                    DaggerfallUnityItem arrows = GameManager.Instance.PlayerEntity.Items.GetItem(ItemGroups.Weapons, (int)Weapons.Arrow, allowQuestItem: false, priorityToConjured: true);
                    arrowCountTextLabel.Text = (arrows != null) ? arrows.stackCount.ToString() : "0";
                    arrowCountTextLabel.TextColor = (arrows != null && arrows.IsSummoned) ? conjuredArrowsColor : realArrowsColor;
                    arrowCountTextLabel.TextScale = NativePanel.LocalScale.x * DaggerfallUnity.Settings.DisplayHUDScaleAdjust;
                    arrowCountTextLabel.Position = arrowLabelPos;
                    arrowCountTextLabel.Enabled = true;
                }
            }

            poisonTextLabel.Enabled = true;
            var weapon = GameManager.Instance.WeaponManager.GetWeapon();

            {
                poisonTextLabel.Enabled = true;
                poisonTextLabel.TextScale = NativePanel.LocalScale.x * DaggerfallUnity.Settings.DisplayHUDScaleAdjust;
                poisonTextLabel.TextColor = FormulaHelper.WeaponHealthColor(1, weapon);
                poisonTextLabel.Text = FormulaHelper.WeaponHealthMessage(1, weapon );
                poisonTextLabel.Position = new Vector2(HUDVitals.Size.x + 20, screenRect.height - 20);
            }

            WeaponHealthTextLabel.Enabled = true;
            
            {
                WeaponHealthTextLabel.Enabled = true;
                WeaponHealthTextLabel.TextScale = NativePanel.LocalScale.x * DaggerfallUnity.Settings.DisplayHUDScaleAdjust;
                WeaponHealthTextLabel.TextColor = FormulaHelper.WeaponHealthColor(2, weapon);
                WeaponHealthTextLabel.Text = $"{FormulaHelper.WeaponHealthMessage(2, weapon)}";
                WeaponHealthTextLabel.Position = new Vector2(HUDVitals.Size.x + 20, screenRect.height - 40);

            }

            SelectedSpellLabel.Enabled = true;
            {
                SelectedSpellLabel.Enabled = true;
                SelectedSpellLabel.TextScale = NativePanel.LocalScale.x * DaggerfallUnity.Settings.DisplayHUDScaleAdjust;
                SelectedSpellLabel.TextColor = FormulaHelper.WeaponHealthColor(3, weapon);
                SelectedSpellLabel.Text = FormulaHelper.WeaponHealthMessage(3, weapon);
                SelectedSpellLabel.Position = new Vector2(HUDVitals.Size.x + 20, screenRect.height - 60);
            }

            HotkeySequence.KeyModifiers keyModifiers = HotkeySequence.GetKeyboardKeyModifiers();
            // Cycle quest debugger state
            if (DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.DebuggerToggle).IsDownWith(keyModifiers))
            {
                if (DaggerfallUnity.Settings.EnableQuestDebugger)
                    questDebugger.NextState();
            }

            if (DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.Pause).IsUpWith(keyModifiers))
            {
                DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenPauseOptionsDialog);
            }

            // Toggle large HUD rendering
            if (DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.LargeHUDToggle).IsDownWith(keyModifiers))
            {
                DaggerfallUnity.Settings.LargeHUD = !DaggerfallUnity.Settings.LargeHUD;
            }

            // Toggle HUD rendering
            if (DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.HUDToggle).IsDownWith(keyModifiers))
            {
                renderHUD = !renderHUD;
            }

            // Toggle Retro Renderer Postprocessing
            if (DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.ToggleRetroPP).IsDownWith(keyModifiers))
            {
                RetroRenderer retrorenderer = GameManager.Instance.RetroRenderer;
                if (retrorenderer)
                    retrorenderer.TogglePostprocessing();
            }

            flickerController.NextCycle();

            // Don't display persistent HUD elements during initial startup
            // Prevents HUD elements being shown briefly at wrong size/scale at game start
            if (!startupComplete && !GameManager.Instance.IsPlayingGame())
            {
                largeHUD.Enabled = false;
                vitals.Enabled = false;
                crosshair.Enabled = false;
                compass.Enabled = false;
            }
            else
            {
                startupComplete = true;
            }

            base.Update();
        }

        public override void Draw()
        {
            if (renderHUD)
                base.Draw();
        }

        public void SetMidScreenText(string message, float delay = 1.5f)
        {
            // Adjust position for variable sized large HUD
            // Text will remain in default position unless it needs to avoid being drawn under HUD
            if (DaggerfallUI.Instance.DaggerfallHUD != null && DaggerfallUnity.Settings.LargeHUD)
            {
                float offset = Screen.height - DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.ScreenHeight;
                float localY = (offset / midScreenTextLabel.LocalScale.y) - 7;
                if (localY < midScreenTextDefaultY)
                    midScreenTextLabel.Position = new Vector2(0, (int)localY);
                else
                    midScreenTextLabel.Position = new Vector2(0, midScreenTextDefaultY);
            }

            // Set text and start timing
            midScreenTextLabel.Text = message;
            midScreenTextTimer = 0;
            midScreenTextDelay = delay;
            GameManager.Instance.PlayerEntity.Notebook.AddMessage(message);
        }

        #region Events

        // OnLargeHUDToggleEvent
        public delegate void OnLargeHUDToggleHandler();
        public static event OnLargeHUDToggleHandler OnLargeHUDToggle;
        protected virtual void RaiseOnLargeHUDToggleEvent()
        {
            if (OnLargeHUDToggle != null)
                try
                {
                    OnLargeHUDToggle();
                }
                catch (Exception e)
                {
                    var del = OnLargeHUDToggle;
                    var str = string.Empty;
                    var currMethod = new StackTrace().GetFrame(0).GetMethod();
                    var className = currMethod.ReflectedType != null ? currMethod.ReflectedType.FullName : string.Empty;
                    if (del != null && del.Method != null && del.Method.DeclaringType != null)
                    {
                        className = del.Method.DeclaringType.FullName;
                        currMethod = del.Method;
                    }

                    str += $"Exception running {className}.{currMethod.Name}\n{e.Message}\n{e}";
                    UnityEngine.Debug.LogError(str);
                }
        }

        #endregion
    }
}