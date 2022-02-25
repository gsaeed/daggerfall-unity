// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2023 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Numidium
// Contributors:    
// 
// Notes:
//

using DaggerfallConnect;
using DaggerfallWorkshop.Game.Player;
using DaggerfallWorkshop.Game.UserInterface;
using FullSerializer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions.Must;
using Enum = System.Enum;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    [FullSerializer.fsObject("v1")]
    public class DFCareerArray
    {
        public Dictionary<string, CharacterDocument> DfCareers;

        public DFCareerArray()
        {
            DfCareers = new Dictionary<string, CharacterDocument>();
        }
    }



    /// <summary>
    /// Implements custom class creator window.
    /// </summary>
    public class CreateCharCustomClass : DaggerfallPopupWindow
    {
        const string nativeImgName = "CUST00I0.IMG";
        const string nativeDaggerImgName = "CUST08I0.IMG";
        public static readonly fsSerializer _serializer = new fsSerializer();
        const int maxHpPerLevel = 30;
        const int minHpPerLevel = 4;
        const int defaultHpPerLevel = 8;
        const int minDifficultyPoints = -12;
        const int maxDifficultyPoints = 40;

        const float daggerTrailLingerTime = 1.0f;

        private static DFCareerArray myDfCareers;
        public static DFCareerArray fullDFCareers;
        public static DFCareer selectedTemplateCareer = new DFCareer();
        public static bool templateSelected = false;

        const int strNameYourClass = 301;
        const int strSetSkills = 300;
        const int strDistributeStats = 302;
        const int strAdvancingDaggerInRed = 306;
        Texture2D nativeTexture;
        Texture2D nativeDaggerTexture;
        DaggerfallFont font;
        StatsRollout statsRollout;
        TextBox nameTextBox = new TextBox();
        DFCareer createdClass;
        int lastSkillButtonId;
        Dictionary<string, DFCareer.Skills> skillsDict;
        List<string> skillsList;
        Dictionary<string, int> helpDict;
        int difficultyPoints = 0;
        int advantageAdjust = 0;
        int disadvantageAdjust = 0;
        List<CreateCharSpecialAdvantageWindow.SpecialAdvDis> advantages = new List<CreateCharSpecialAdvantageWindow.SpecialAdvDis>();
        List<CreateCharSpecialAdvantageWindow.SpecialAdvDis> disadvantages = new List<CreateCharSpecialAdvantageWindow.SpecialAdvDis>();
        short merchantsRep = 0;
        short peasantsRep = 0;
        short scholarsRep = 0;
        short nobilityRep = 0;
        short underworldRep = 0;

        IMECompositionMode prevIME;

        #region Windows

        CreateCharReputationWindow createCharReputationWindow;
        CreateCharSpecialAdvantageWindow createCharSpecialAdvantageWindow;
        CreateCharSpecialAdvantageWindow createCharSpecialDisadvantageWindow;
        DaggerfallListPickerWindow helpPicker;
        DaggerfallListPickerWindow skillPicker;

        #endregion

        #region UI Panels

        Panel daggerPanel = new Panel();

        #endregion

        #region UI Rects

        Rect[] skillButtonRects = new Rect[]
        {
            new Rect(66, 31, 108, 8),
            new Rect(66, 41, 108, 8),
            new Rect(66, 51, 108, 8),
            new Rect(66, 80, 108, 8),
            new Rect(66, 90, 108, 8),
            new Rect(66, 100, 108, 8),
            new Rect(66, 129, 108, 8),
            new Rect(66, 139, 108, 8),
            new Rect(66, 149, 108, 8),
            new Rect(66, 159, 108, 8),
            new Rect(66, 169, 108, 8),
            new Rect(66, 179, 108, 8)
        };
        Rect hitPointsUpButtonRect = new Rect(252, 46, 8, 10);
        Rect hitPointsDownButtonRect = new Rect(252, 57, 8, 10);
        Rect helpButtonRect = new Rect(249, 74, 66, 22);
        Rect templateButtonRect = new Rect(249, 0, 66, 16);
        Rect specialAdvantageButtonRect = new Rect(249, 98, 66, 22);
        Rect specialDisadvantageButtonRect = new Rect(249, 122, 66, 22);
        Rect reputationButtonRect = new Rect(249, 146, 66, 22);
        Rect resetButtonRect = new Rect(0, 0, 0,0);
        Rect exitButtonRect = new Rect(263, 172, 38, 21);

        #endregion

        #region Buttons

        Button[] skillButtons = new Button[12];
        Button hitPointsUpButton;
        Button hitPointsDownButton;
        Button helpButton;
        Button specialAdvantageButton;
        Button specialDisadvantageButton;
        Button reputationButton;
        Button resetButton;
        private Button templateButton;
        Button exitButton;

        #endregion

        #region Text Labels

        TextLabel[] skillLabels = new TextLabel[12];
        TextLabel hpLabel;

        #endregion

        public CreateCharCustomClass(IUserInterfaceManager uiManager)
            : base(uiManager)
        {
        }

        #region Setup Methods

        protected override void Setup()
        {
            if (IsSetup)
                return;

            myDfCareers = LoadCreatedClass();

            // Load native textures
            nativeTexture = DaggerfallUI.GetTextureFromImg(nativeImgName);
            nativeDaggerTexture = DaggerfallUI.GetTextureFromImg(nativeDaggerImgName);
            if (!nativeTexture || !nativeDaggerTexture)
                throw new Exception("CreateCharCustomClass: Could not load native texture.");

            // Setup native panel background
            NativePanel.BackgroundTexture = nativeTexture;

            // Add stats rollout
            statsRollout = new StatsRollout(false, true);
            statsRollout.Position = new Vector2(0, 0);
            NativePanel.Components.Add(statsRollout);

            // Add name textbox
            nameTextBox.Position = new Vector2(100, 5);
            nameTextBox.Size = new Vector2(214, 7);
            NativePanel.Components.Add(nameTextBox);

            // Initialize character class
            createdClass = new DFCareer();
            createdClass.HitPointsPerLevel = defaultHpPerLevel;
            createdClass.SpellPointMultiplier = DFCareer.SpellPointMultipliers.Times_0_50;
            createdClass.SpellPointMultiplierValue = .5f;

            // Initiate UI components
            font = DaggerfallUI.DefaultFont;
            SetupButtons();
            hpLabel = DaggerfallUI.AddTextLabel(font, new Vector2(285, 55), createdClass.HitPointsPerLevel.ToString(), NativePanel);
            daggerPanel.Size = new Vector2(24, 9);
            daggerPanel.BackgroundTexture = nativeDaggerTexture;
            NativePanel.Components.Add(daggerPanel);
            UpdateDifficulty();

            // Setup help dictionary
            helpDict = new Dictionary<string, int> 
            {
                { TextManager.Instance.GetLocalizedText("helpAttributes"), 2402 },
                { TextManager.Instance.GetLocalizedText("helpClassName"), 2401 },
                { TextManager.Instance.GetLocalizedText("helpGeneral"), 2400 },
                { TextManager.Instance.GetLocalizedText("helpReputations"), 2406 },
                { TextManager.Instance.GetLocalizedText("helpSkillAdvancement"), 2407 },
                { TextManager.Instance.GetLocalizedText("helpSkills"), 2403 },
                { TextManager.Instance.GetLocalizedText("helpSpecialAdvantages"), 2404 },
                { TextManager.Instance.GetLocalizedText("helpSpecialDisadvantages"), 2405 }
            };

            // Setup skills dictionary
            skillsDict = new Dictionary<string, DFCareer.Skills>();
            foreach (DFCareer.Skills skill in Enum.GetValues(typeof(DFCareer.Skills)))
            {
                string name = DaggerfallUnity.Instance.TextProvider.GetSkillName(skill);
                if(!string.IsNullOrEmpty(name))
                    skillsDict.Add(name, skill);
            }
            skillsList = new List<string>(skillsDict.Keys);
            skillsList.Sort(); // Sort skills alphabetically a la classic.

            createCharSpecialAdvantageWindow = new CreateCharSpecialAdvantageWindow(uiManager, advantages, disadvantages, createdClass, this);
            createCharSpecialDisadvantageWindow = new CreateCharSpecialAdvantageWindow(uiManager, disadvantages, advantages, createdClass, this, true);

            IsSetup = true;
        }

        public override void OnPush()
        {
            base.OnPush();

            // Enable IME composition during input
            prevIME = Input.imeCompositionMode;
            Input.imeCompositionMode = IMECompositionMode.On;
        }

        public override void OnPop()
        {
            base.OnPop();

            // Restore previous IME composition mode
            Input.imeCompositionMode = prevIME;
        }

        protected void SetupButtons()
        {
            // Add skill selector buttons
            for (int i = 0; i < skillButtons.Length; i++) 
            {
                skillButtons[i] = DaggerfallUI.AddButton(skillButtonRects[i], NativePanel);
                skillButtons[i].Tag = i;
                skillButtons[i].OnMouseClick += skillButton_OnMouseClick;
                skillButtons[i].ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.ButtonClick);
                skillLabels[i] = DaggerfallUI.AddTextLabel(font, new Vector2(3, 2), string.Empty, skillButtons[i]);
            }
            // HP spinners
            hitPointsUpButton = DaggerfallUI.AddButton(hitPointsUpButtonRect, NativePanel);
            hitPointsUpButton.OnMouseClick += HitPointsUpButton_OnMouseClick;
            hitPointsUpButton.ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.ButtonClick);
            hitPointsDownButton = DaggerfallUI.AddButton(hitPointsDownButtonRect, NativePanel);
            hitPointsDownButton.OnMouseClick += HitPointsDownButton_OnMouseClick;
            hitPointsDownButton.ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.ButtonClick);

            // Help topics
            helpButton = DaggerfallUI.AddButton(helpButtonRect, NativePanel);
            helpButton.OnMouseClick += HelpButton_OnMouseClick;
            helpButton.ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.ButtonClick);

            // Special Advantages/Disadvantages
            specialAdvantageButton = DaggerfallUI.AddButton(specialAdvantageButtonRect, NativePanel);
            specialAdvantageButton.OnMouseClick += specialAdvantageButton_OnMouseClick;
            specialAdvantageButton.ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.ButtonClick);
            specialDisadvantageButton = DaggerfallUI.AddButton(specialDisadvantageButtonRect, NativePanel);
            specialDisadvantageButton.OnMouseClick += specialDisadvantageButton_OnMouseClick;
            specialDisadvantageButton.ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.ButtonClick);

            // Reputations
            reputationButton = DaggerfallUI.AddButton(reputationButtonRect, NativePanel);
            reputationButton.OnMouseClick += ReputationButton_OnMouseClick;
            reputationButton.ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.ButtonClick);


            // (Hidden) Reset bonus pool
            resetButton = DaggerfallUI.AddButton(resetButtonRect, NativePanel);
            resetButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.ResetBonusPool);
            resetButton.OnKeyboardEvent += ResetButton_OnKeyboardEvent;
            //Template Button
            templateButton = DaggerfallUI.AddButton(templateButtonRect, NativePanel);
            templateButton.Label.Text = "Use Template";
            templateButton.OnMouseClick += templateButton_OnMouseClick;
            templateButton.ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.ButtonClick);

            // Exit button
            exitButton = DaggerfallUI.AddButton(exitButtonRect, NativePanel);
            exitButton.OnMouseClick += ExitButton_OnMouseClick;
            exitButton.ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.ButtonClick);
        }

        #endregion

        public override void Update()
        {
            base.Update();
        }

        public override void Draw()
        {
            base.Draw();
        }

        #region Event Handlers

        void skillButton_OnMouseClick(BaseScreenComponent sender, Vector2 pos)
        {
            skillPicker = new DaggerfallListPickerWindow(uiManager, this);
            skillPicker.OnItemPicked += SkillPicker_OnItemPicked;
            foreach (string skillName in skillsList)
            {
                skillPicker.ListBox.AddItem(skillName);
            }
            lastSkillButtonId = (int)sender.Tag;
            uiManager.PushWindow(skillPicker);
        }

        void SkillPicker_OnItemPicked(int index, string skillName)
        {
            skillPicker.CloseWindow();
            switch (lastSkillButtonId)
            {
                case 0:
                    createdClass.PrimarySkill1 = skillsDict[skillName];
                    break;
                case 1:
                    createdClass.PrimarySkill2 = skillsDict[skillName];
                    break;
                case 2:
                    createdClass.PrimarySkill3 = skillsDict[skillName];
                    break;
                case 3:
                    createdClass.MajorSkill1 = skillsDict[skillName];
                    break;
                case 4:
                    createdClass.MajorSkill2 = skillsDict[skillName];
                    break;
                case 5:
                    createdClass.MajorSkill3 = skillsDict[skillName];
                    break;
                case 6:
                    createdClass.MinorSkill1 = skillsDict[skillName];
                    break;
                case 7:
                    createdClass.MinorSkill2 = skillsDict[skillName];
                    break;
                case 8:
                    createdClass.MinorSkill3 = skillsDict[skillName];
                    break;
                case 9:
                    createdClass.MinorSkill4 = skillsDict[skillName];
                    break;
                case 10:
                    createdClass.MinorSkill5 = skillsDict[skillName];
                    break;
                case 11:
                    createdClass.MinorSkill6 = skillsDict[skillName];
                    break;
                default:
                    return;
            }
            skillsList.Remove(skillName);
            if (skillLabels[lastSkillButtonId].Text != string.Empty)
            {
                skillsList.Add(skillLabels[lastSkillButtonId].Text);
            }
            skillsList.Sort();
            skillLabels[lastSkillButtonId].Text = skillName;
        }

        public void HitPointsUpButton_OnMouseClick(BaseScreenComponent sender, Vector2 pos)
        {
            if (createdClass.HitPointsPerLevel != maxHpPerLevel)
            {
                createdClass.HitPointsPerLevel++;
                hpLabel.Text = createdClass.HitPointsPerLevel.ToString();
                UpdateDifficulty();
            }
        }

        public void HitPointsDownButton_OnMouseClick(BaseScreenComponent sender, Vector2 pos)
        {
            if (createdClass.HitPointsPerLevel != minHpPerLevel)
            {
                createdClass.HitPointsPerLevel--;
                hpLabel.Text = createdClass.HitPointsPerLevel.ToString();
                UpdateDifficulty();
            }
        }

        void HelpButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            helpPicker = new DaggerfallListPickerWindow(uiManager, this);
            foreach (string str in helpDict.Keys)
            {
                helpPicker.ListBox.AddItem(str);
            }
            helpPicker.OnItemPicked += HelpPicker_OnItemPicked;
            uiManager.PushWindow(helpPicker);
        }

        void HelpPicker_OnItemPicked(int index, string itemString)
        {
            helpPicker.CloseWindow();
            DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, this);
            messageBox.SetTextTokens(helpDict[itemString]);
            messageBox.ClickAnywhereToClose = true;
            messageBox.Show();
        }

        public void templateButton_OnMouseClick(BaseScreenComponent sender, Vector2 pos)
        {
            var createCharTemplateSelectWindow = new CreateCharTemplateSelect(uiManager);
            createCharTemplateSelectWindow.OnClose += createCharTemplateSelect_OnClose;
            uiManager.PushWindow(createCharTemplateSelectWindow);
        }

        public void createCharTemplateSelect_OnClose()
        {
            if (templateSelected)
            {
                var cd = new CharacterDocument();
                if (!fullDFCareers.DfCareers.TryGetValue(selectedTemplateCareer.Name, out cd))
                {
                    Debug.LogError($"Create Custom Class unable to find template for {selectedTemplateCareer.Name}");
                }
                else
                {
                    //reputations
                    merchantsRep = cd.reputationMerchants;
                    peasantsRep = cd.reputationCommoners;
                    nobilityRep = cd.reputationNobility;
                    scholarsRep = cd.reputationScholars;
                    underworldRep = cd.reputationUnderworld;

                    //stats
                    statsRollout.WorkingStats.SetPermanentStatValue(DFCareer.Stats.Strength, cd.career.Strength);
                    statsRollout.WorkingStats.SetPermanentStatValue(DFCareer.Stats.Intelligence, cd.career.Intelligence);
                    statsRollout.WorkingStats.SetPermanentStatValue(DFCareer.Stats.Willpower, cd.career.Willpower);
                    statsRollout.WorkingStats.SetPermanentStatValue(DFCareer.Stats.Agility, cd.career.Agility);
                    statsRollout.WorkingStats.SetPermanentStatValue(DFCareer.Stats.Endurance, cd.career.Endurance);
                    statsRollout.WorkingStats.SetPermanentStatValue(DFCareer.Stats.Personality, cd.career.Personality);
                    statsRollout.WorkingStats.SetPermanentStatValue(DFCareer.Stats.Speed, cd.career.Speed);
                    statsRollout.WorkingStats.SetPermanentStatValue(DFCareer.Stats.Luck, cd.career.Luck);

                    //primary skills
                    createdClass.PrimarySkill1 = cd.career.PrimarySkill1;
                    skillLabels[0].Text = createdClass.PrimarySkill1.ToString();

                    createdClass.PrimarySkill2 = cd.career.PrimarySkill2;
                    skillLabels[1].Text = createdClass.PrimarySkill2.ToString();

                    createdClass.PrimarySkill3 = cd.career.PrimarySkill3;
                    skillLabels[2].Text = createdClass.PrimarySkill3.ToString();

                    //major skills
                    createdClass.MajorSkill1 = cd.career.MajorSkill1;
                    skillLabels[3].Text = createdClass.MajorSkill1.ToString();

                    createdClass.MajorSkill2 = cd.career.MajorSkill2;
                    skillLabels[4].Text = createdClass.MajorSkill2.ToString();

                    createdClass.MajorSkill3 = cd.career.MajorSkill3;
                    skillLabels[5].Text = createdClass.MajorSkill3.ToString();

                    //minor skills
                    createdClass.MinorSkill1 = cd.career.MinorSkill1;
                    skillLabels[6].Text = createdClass.MinorSkill1.ToString();

                    createdClass.MinorSkill2 = cd.career.MinorSkill2;
                    skillLabels[7].Text = createdClass.MinorSkill2.ToString();

                    createdClass.MinorSkill3 = cd.career.MinorSkill3;
                    skillLabels[8].Text = createdClass.MinorSkill3.ToString();

                    createdClass.MinorSkill4 = cd.career.MinorSkill4;
                    skillLabels[9].Text = createdClass.MinorSkill4.ToString();

                    createdClass.MinorSkill5 = cd.career.MinorSkill5;
                    skillLabels[10].Text = createdClass.MinorSkill5.ToString();

                    createdClass.MinorSkill6 = cd.career.MinorSkill6;
                    skillLabels[11].Text = createdClass.MinorSkill6.ToString();

                    //special advantages
                    PopulateSpecialAdvantages(cd);


                    //hit points
                    createdClass.HitPointsPerLevel = cd.career.HitPointsPerLevel;
                    hpLabel.Text = createdClass.HitPointsPerLevel.ToString();


                    statsRollout.UpdateStatLabels();
                    UpdateDifficulty();
                }
            }
        }

        void AddSpecialAdvantage(string primary, string secondary, bool isDisadvantage)
        {
            var sd = new CreateCharSpecialAdvantageWindow.SpecialAdvDis
            {
                primaryStringKey = primary,
                secondaryStringKey = secondary,
                difficulty = CreateCharSpecialAdvantageWindow.GetAdvDisAdjustment(primary, secondary),
            };

            if (isDisadvantage)
                disadvantages.Add(sd);
            else
                advantages.Add(sd);
        }

        void PopulateSpecialAdvantages(CharacterDocument cd)
        {
            createCharSpecialAdvantageWindow.InitializeAdjustmentDict();
            advantages = new List<CreateCharSpecialAdvantageWindow.SpecialAdvDis>();
            disadvantages = new List<CreateCharSpecialAdvantageWindow.SpecialAdvDis>();

            if (cd.career.AnimalsAttackModifier == DFCareer.AttackModifier.Bonus)
                AddSpecialAdvantage(HardStrings.bonusToHit, HardStrings.animals, false);

            if (cd.career.DaedraAttackModifier == DFCareer.AttackModifier.Bonus)
                AddSpecialAdvantage(HardStrings.bonusToHit, HardStrings.daedra, false);

            if (cd.career.HumanoidAttackModifier == DFCareer.AttackModifier.Bonus)
                AddSpecialAdvantage(HardStrings.bonusToHit, HardStrings.humanoid, false);

            if (cd.career.UndeadAttackModifier == DFCareer.AttackModifier.Bonus)
                AddSpecialAdvantage(HardStrings.bonusToHit, HardStrings.undead, false);

            if (cd.career.AnimalsAttackModifier == DFCareer.AttackModifier.Phobia)
                AddSpecialAdvantage(HardStrings.phobia, HardStrings.animals, true);

            if (cd.career.DaedraAttackModifier == DFCareer.AttackModifier.Phobia)
                AddSpecialAdvantage(HardStrings.phobia, HardStrings.daedra, true);

            if (cd.career.HumanoidAttackModifier == DFCareer.AttackModifier.Phobia)
                AddSpecialAdvantage(HardStrings.phobia, HardStrings.humanoid, true);

            if (cd.career.UndeadAttackModifier == DFCareer.AttackModifier.Phobia)
                AddSpecialAdvantage(HardStrings.phobia, HardStrings.undead, true);

            if (cd.career.ExpertProficiencies > 0)
            {
                if ((cd.career.ExpertProficiencies & DFCareer.ProficiencyFlags.Axes) > 0)
                    AddSpecialAdvantage(HardStrings.expertiseIn, HardStrings.axe, false);
                if ((cd.career.ExpertProficiencies & DFCareer.ProficiencyFlags.BluntWeapons) > 0)
                    AddSpecialAdvantage(HardStrings.expertiseIn, HardStrings.bluntWeapon, false);
                if ((cd.career.ExpertProficiencies & DFCareer.ProficiencyFlags.HandToHand) > 0)
                    AddSpecialAdvantage(HardStrings.expertiseIn, HardStrings.handToHand, false);
                if ((cd.career.ExpertProficiencies & DFCareer.ProficiencyFlags.LongBlades) > 0)
                    AddSpecialAdvantage(HardStrings.expertiseIn, HardStrings.longBlade, false);
                if ((cd.career.ExpertProficiencies & DFCareer.ProficiencyFlags.MissileWeapons) > 0)
                    AddSpecialAdvantage(HardStrings.expertiseIn, HardStrings.missileWeapon, false);
                if ((cd.career.ExpertProficiencies & DFCareer.ProficiencyFlags.ShortBlades) > 0)
                    AddSpecialAdvantage(HardStrings.expertiseIn, HardStrings.shortBlade, false);
            }

            if (cd.career.ForbiddenProficiencies > 0)
            {
                if ((cd.career.ForbiddenProficiencies & DFCareer.ProficiencyFlags.Axes) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenWeaponry, HardStrings.axe, true);
                if ((cd.career.ForbiddenProficiencies & DFCareer.ProficiencyFlags.BluntWeapons) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenWeaponry, HardStrings.bluntWeapon, true);
                if ((cd.career.ForbiddenProficiencies & DFCareer.ProficiencyFlags.HandToHand) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenWeaponry, HardStrings.handToHand, true);
                if ((cd.career.ForbiddenProficiencies & DFCareer.ProficiencyFlags.LongBlades) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenWeaponry, HardStrings.longBlade, true);
                if ((cd.career.ForbiddenProficiencies & DFCareer.ProficiencyFlags.MissileWeapons) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenWeaponry, HardStrings.missileWeapon, true);
                if ((cd.career.ForbiddenProficiencies & DFCareer.ProficiencyFlags.ShortBlades) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenWeaponry, HardStrings.shortBlade, true);
            }

            if (cd.career.Disease > 0)
            {
                switch (cd.career.Disease)
                {
                    case DFCareer.Tolerance.CriticalWeakness:
                        AddSpecialAdvantage(HardStrings.criticalWeakness, HardStrings.toDisease, true);
                        break;
                    case DFCareer.Tolerance.LowTolerance:
                        AddSpecialAdvantage(HardStrings.lowTolerance, HardStrings.toDisease, true);
                        break;
                    case DFCareer.Tolerance.Resistant:
                        AddSpecialAdvantage(HardStrings.resistance, HardStrings.toDisease, false);
                        break;
                    case DFCareer.Tolerance.Immune:
                        AddSpecialAdvantage(HardStrings.immunity, HardStrings.toDisease, false);
                        break;
                }
            }

            if (cd.career.Fire > 0)
            {
                switch (cd.career.Fire)
                {
                    case DFCareer.Tolerance.CriticalWeakness:
                        AddSpecialAdvantage(HardStrings.criticalWeakness, HardStrings.toFire, true);
                        break;
                    case DFCareer.Tolerance.LowTolerance:
                        AddSpecialAdvantage(HardStrings.lowTolerance, HardStrings.toFire, true);
                        break;
                    case DFCareer.Tolerance.Resistant:
                        AddSpecialAdvantage(HardStrings.resistance, HardStrings.toFire, false);
                        break;
                    case DFCareer.Tolerance.Immune:
                        AddSpecialAdvantage(HardStrings.immunity, HardStrings.toFire, false);
                        break;
                }
            }

            if (cd.career.Frost > 0)
            {
                switch (cd.career.Frost)
                {
                    case DFCareer.Tolerance.CriticalWeakness:
                        AddSpecialAdvantage(HardStrings.criticalWeakness, HardStrings.toFrost, true);
                        break;
                    case DFCareer.Tolerance.LowTolerance:
                        AddSpecialAdvantage(HardStrings.lowTolerance, HardStrings.toFrost, true);
                        break;
                    case DFCareer.Tolerance.Resistant:
                        AddSpecialAdvantage(HardStrings.resistance, HardStrings.toFrost, false);
                        break;
                    case DFCareer.Tolerance.Immune:
                        AddSpecialAdvantage(HardStrings.immunity, HardStrings.toFrost, false);
                        break;
                }
            }

            if (cd.career.Magic > 0)
            {
                switch (cd.career.Magic)
                {
                    case DFCareer.Tolerance.CriticalWeakness:
                        AddSpecialAdvantage(HardStrings.criticalWeakness, HardStrings.toMagic, true);
                        break;
                    case DFCareer.Tolerance.LowTolerance:
                        AddSpecialAdvantage(HardStrings.lowTolerance, HardStrings.toMagic, true );
                        break;
                    case DFCareer.Tolerance.Resistant:
                        AddSpecialAdvantage(HardStrings.resistance, HardStrings.toMagic, false);
                        break;
                    case DFCareer.Tolerance.Immune:
                        AddSpecialAdvantage(HardStrings.immunity, HardStrings.toMagic, false);
                        break;
                }
            }


            if (cd.career.Paralysis > 0)
            {
                switch (cd.career.Paralysis)
                {
                    case DFCareer.Tolerance.CriticalWeakness:
                        AddSpecialAdvantage(HardStrings.criticalWeakness, HardStrings.toParalysis, true);
                        break;
                    case DFCareer.Tolerance.LowTolerance:
                        AddSpecialAdvantage(HardStrings.lowTolerance, HardStrings.toParalysis, true);
                        break;
                    case DFCareer.Tolerance.Resistant:
                        AddSpecialAdvantage(HardStrings.resistance, HardStrings.toParalysis, false);
                        break;
                    case DFCareer.Tolerance.Immune:
                        AddSpecialAdvantage(HardStrings.immunity, HardStrings.toParalysis, false);
                        break;
                }
            }

            if (cd.career.Poison > 0)
            {
                switch (cd.career.Poison)
                {
                    case DFCareer.Tolerance.CriticalWeakness:
                        AddSpecialAdvantage(HardStrings.criticalWeakness, HardStrings.toPoison, true);
                        break;
                    case DFCareer.Tolerance.LowTolerance:
                        AddSpecialAdvantage(HardStrings.lowTolerance, HardStrings.toPoison, true);
                        break;
                    case DFCareer.Tolerance.Resistant:
                        AddSpecialAdvantage(HardStrings.resistance, HardStrings.toPoison, false);
                        break;
                    case DFCareer.Tolerance.Immune:
                        AddSpecialAdvantage(HardStrings.immunity, HardStrings.toPoison, false);
                        break;
                }
            }

            if (cd.career.Shock > 0)
            {
                switch (cd.career.Shock)
                {
                    case DFCareer.Tolerance.CriticalWeakness:
                        AddSpecialAdvantage(HardStrings.criticalWeakness, HardStrings.toShock, true);
                        break;
                    case DFCareer.Tolerance.LowTolerance:
                        AddSpecialAdvantage(HardStrings.lowTolerance, HardStrings.toShock, true);
                        break;
                    case DFCareer.Tolerance.Resistant:
                        AddSpecialAdvantage(HardStrings.resistance, HardStrings.toShock, false);
                        break;
                    case DFCareer.Tolerance.Immune:
                        AddSpecialAdvantage(HardStrings.immunity, HardStrings.toShock, false);
                        break;
                }
            }

            if (cd.career.RapidHealing > 0)
            {
                switch (cd.career.RapidHealing)
                {
                    case DFCareer.RapidHealingFlags.Always:
                        AddSpecialAdvantage(HardStrings.rapidHealing, HardStrings.general, false);
                        break;
                    case DFCareer.RapidHealingFlags.InDarkness:
                        AddSpecialAdvantage(HardStrings.rapidHealing, HardStrings.inDarkness, false);
                        break;
                    case DFCareer.RapidHealingFlags.InLight:
                        AddSpecialAdvantage(HardStrings.rapidHealing, HardStrings.inLight, false);
                        break;

                }

            }

            if (cd.career.SpellAbsorption > 0)
            {
                switch (cd.career.SpellAbsorption)
                {
                    case DFCareer.SpellAbsorptionFlags.Always:
                        AddSpecialAdvantage(HardStrings.spellAbsorption, HardStrings.general, false);
                        break;
                    case DFCareer.SpellAbsorptionFlags.InDarkness:
                        AddSpecialAdvantage(HardStrings.spellAbsorption, HardStrings.inDarkness, false);
                        break;
                    case DFCareer.SpellAbsorptionFlags.InLight:
                        AddSpecialAdvantage(HardStrings.spellAbsorption, HardStrings.inLight, false);
                        break;

                }

            }


            if (cd.career.Regeneration > 0)
            {
                switch (cd.career.Regeneration)
                {
                    case DFCareer.RegenerationFlags.Always:
                        AddSpecialAdvantage(HardStrings.regenerateHealth, HardStrings.general, false);
                        break;
                    case DFCareer.RegenerationFlags.InDarkness:
                        AddSpecialAdvantage(HardStrings.regenerateHealth, HardStrings.inDarkness, false);
                        break;
                    case DFCareer.RegenerationFlags.InLight:
                        AddSpecialAdvantage(HardStrings.regenerateHealth, HardStrings.inLight, false);
                        break;
                    case DFCareer.RegenerationFlags.InWater:
                        AddSpecialAdvantage(HardStrings.regenerateHealth, HardStrings.whileImmersed, false);
                        break;

                }

            }

            if (cd.career.DamageFromHolyPlaces)
            {
                AddSpecialAdvantage(HardStrings.damage, HardStrings.fromHolyPlaces, true);
            }

            if (cd.career.DamageFromSunlight)
            {
                AddSpecialAdvantage(HardStrings.damage, HardStrings.fromSunlight, true);
            }

            if (cd.career.SpellPointMultiplierValue > 0f)
            {
                if (cd.career.SpellPointMultiplierValue > 2f )
                    AddSpecialAdvantage(HardStrings.increasedMagery, HardStrings.intInSpellPoints3, false);
                else if (cd.career.SpellPointMultiplierValue > 1.8f)
                    AddSpecialAdvantage(HardStrings.increasedMagery, HardStrings.intInSpellPoints2, false);
                else if (cd.career.SpellPointMultiplierValue > 1.6f)
                    AddSpecialAdvantage(HardStrings.increasedMagery, HardStrings.intInSpellPoints175, false);
                else if (cd.career.SpellPointMultiplierValue > 1.4f)
                    AddSpecialAdvantage(HardStrings.increasedMagery, HardStrings.intInSpellPoints15, false);
                else if (cd.career.SpellPointMultiplierValue > 0.6f)
                    AddSpecialAdvantage(HardStrings.increasedMagery, HardStrings.intInSpellPoints, false);

            }

            if (cd.career.DarknessPoweredMagery > 0)
            {
                switch (cd.career.DarknessPoweredMagery)
                {
                    case DFCareer.DarknessMageryFlags.ReducedPowerInLight:
                        AddSpecialAdvantage(HardStrings.darknessPoweredMagery, HardStrings.lowerMagicAbilityDaylight, true);
                        break;
                    case DFCareer.DarknessMageryFlags.UnableToCastInLight:
                        AddSpecialAdvantage(HardStrings.darknessPoweredMagery, HardStrings.unableToUseMagicInDaylight, true);
                        break;
                }

            }

            if (cd.career.ForbiddenArmors > 0)
            {
                if ((cd.career.ForbiddenArmors & DFCareer.ArmorFlags.Chain) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenArmorType, HardStrings.chain, true);
                if ((cd.career.ForbiddenArmors & DFCareer.ArmorFlags.Leather) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenArmorType, HardStrings.leather, true);
                if ((cd.career.ForbiddenArmors & DFCareer.ArmorFlags.Plate) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenArmorType, HardStrings.plate, true);
            }

            if (cd.career.ForbiddenMaterials > 0)
            {
                if ((cd.career.ForbiddenMaterials & DFCareer.MaterialFlags.Adamantium) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenMaterial, HardStrings.adamantium, true);
                if ((cd.career.ForbiddenMaterials & DFCareer.MaterialFlags.Daedric) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenMaterial, HardStrings.daedric, true);
                if ((cd.career.ForbiddenMaterials & DFCareer.MaterialFlags.Dwarven) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenMaterial, HardStrings.dwarven, true);
                if ((cd.career.ForbiddenMaterials & DFCareer.MaterialFlags.Ebony) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenMaterial, HardStrings.ebony, true);
                if ((cd.career.ForbiddenMaterials & DFCareer.MaterialFlags.Elven) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenMaterial, HardStrings.elven, true);
                if ((cd.career.ForbiddenMaterials & DFCareer.MaterialFlags.Iron) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenMaterial, HardStrings.iron, true);
                if ((cd.career.ForbiddenMaterials & DFCareer.MaterialFlags.Mithril) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenMaterial, HardStrings.mithril, true);
                if ((cd.career.ForbiddenMaterials & DFCareer.MaterialFlags.Orcish) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenMaterial, HardStrings.orcish, true);
                if ((cd.career.ForbiddenMaterials & DFCareer.MaterialFlags.Silver) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenMaterial, HardStrings.silver, true);
                if ((cd.career.ForbiddenMaterials & DFCareer.MaterialFlags.Steel) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenMaterial, HardStrings.steel, true);

            }

            if (cd.career.ForbiddenShields > 0)
            {
                if ((cd.career.ForbiddenShields & DFCareer.ShieldFlags.Buckler) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenShieldTypes, HardStrings.buckler, true);
                if ((cd.career.ForbiddenShields & DFCareer.ShieldFlags.KiteShield) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenShieldTypes, HardStrings.kiteShield, true);
                if ((cd.career.ForbiddenShields & DFCareer.ShieldFlags.RoundShield) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenShieldTypes, HardStrings.roundShield, true);
                if ((cd.career.ForbiddenShields & DFCareer.ShieldFlags.TowerShield) > 0)
                    AddSpecialAdvantage(HardStrings.forbiddenShieldTypes, HardStrings.towerShield, true);
            }

            if (cd.career.LightPoweredMagery > 0)
            {
                switch (cd.career.LightPoweredMagery)
                {
                    case DFCareer.LightMageryFlags.ReducedPowerInDarkness:
                        AddSpecialAdvantage(HardStrings.lightPoweredMagery, HardStrings.lowerMagicAbilityDarkness, true);
                        break;
                    case DFCareer.LightMageryFlags.UnableToCastInDarkness:
                        AddSpecialAdvantage(HardStrings.lightPoweredMagery, HardStrings.unableToUseMagicInDarkness, true);
                        break;
                }

            }

            if (cd.career.NoRegenSpellPoints)
            {
                AddSpecialAdvantage(HardStrings.inabilityToRegen, string.Empty, true);
            }

            if (cd.career.AcuteHearing)
            {
                AddSpecialAdvantage(HardStrings.acuteHearing, string.Empty, false);
            }

            if (cd.career.Athleticism)
            {
                AddSpecialAdvantage(HardStrings.athleticism, string.Empty, false);
            }

            if (cd.career.AdrenalineRush)
            {
                AddSpecialAdvantage(HardStrings.adrenalineRush, string.Empty, false);
            }
        }


        public void specialAdvantageButton_OnMouseClick(BaseScreenComponent sender, Vector2 pos)
        {
            createCharSpecialAdvantageWindow = new CreateCharSpecialAdvantageWindow(uiManager, advantages, disadvantages, createdClass, this);
            uiManager.PushWindow(createCharSpecialAdvantageWindow);

        }

        public void specialDisadvantageButton_OnMouseClick(BaseScreenComponent sender, Vector2 pos)
        {
            createCharSpecialDisadvantageWindow = new CreateCharSpecialAdvantageWindow(uiManager, disadvantages, advantages, createdClass, this, true);
            uiManager.PushWindow(createCharSpecialDisadvantageWindow);
        }

        void ReputationButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            createCharReputationWindow = new CreateCharReputationWindow(uiManager, this);
            uiManager.PushWindow(createCharReputationWindow);
        }

        protected virtual void ResetButton_OnKeyboardEvent(BaseScreenComponent sender, Event keyboardEvent)
        {
            if (keyboardEvent.type == EventType.KeyDown)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                statsRollout.BonusPool = 0;
            }
        }

        void ExitButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallMessageBox messageBox;

            // Is the class name set?
            if (nameTextBox.Text.Length == 0) 
            {
                messageBox = new DaggerfallMessageBox(uiManager, this);
                messageBox.SetTextTokens(strNameYourClass);
                messageBox.ClickAnywhereToClose = true;
                messageBox.Show();
                return;
            } 

            // Are all skills set?
            for (int i = 0; i < skillLabels.Length; i++) 
            {
                if (skillLabels [i].Text == string.Empty)
                {
                    messageBox = new DaggerfallMessageBox(uiManager, this);
                    messageBox.SetTextTokens(strSetSkills);
                    messageBox.ClickAnywhereToClose = true;
                    messageBox.Show();
                    return;
                }
            }

            // Are all attribute points distributed?
            if (statsRollout.BonusPool != 0) 
            {
                messageBox = new DaggerfallMessageBox(uiManager, this);
                messageBox.SetTextTokens(strDistributeStats);
                messageBox.ClickAnywhereToClose = true;
                messageBox.Show();
                return;
            }

            // Is AdvancementMultiplier off limits?
            if (difficultyPoints < minDifficultyPoints || difficultyPoints > maxDifficultyPoints)
            {
                messageBox = new DaggerfallMessageBox(uiManager, this);
                messageBox.SetTextTokens(strAdvancingDaggerInRed);
                messageBox.ClickAnywhereToClose = true;
                messageBox.Show();
                return;
            }

            // Set advantages/disadvantages
            if (createCharSpecialAdvantageWindow != null)
            {
                createCharSpecialAdvantageWindow.ParseCareerData();
            }
            if (createCharSpecialDisadvantageWindow != null)
            {
                createCharSpecialDisadvantageWindow.ParseCareerData();
            }

            ShouldSaveCustomClass();
        }

        #endregion

        #region Private methods


        private void ShouldSaveCustomClass()
        {
            DaggerfallMessageBox messageBox;
            messageBox = new DaggerfallMessageBox(uiManager, this);
            messageBox.SetText("Save this class for future use?");
            messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
            messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
            messageBox.OnButtonClick += ConfirmSavePopup_OnButtonClick;
            messageBox.Show();

        }

        void ConfirmSavePopup_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.No)
            {
                sender.CloseWindow();
                CloseWindow();
                return;
            }
            sender.CloseWindow();
            createdClass.Strength = Stats.WorkingStats.LiveStrength;
            createdClass.Intelligence = Stats.WorkingStats.LiveIntelligence;
            createdClass.Willpower = Stats.WorkingStats.LiveWillpower;
            createdClass.Agility = Stats.WorkingStats.LiveAgility;
            createdClass.Endurance = Stats.WorkingStats.LiveEndurance;
            createdClass.Personality = Stats.WorkingStats.LivePersonality;
            createdClass.Speed = Stats.WorkingStats.LiveSpeed;
            createdClass.Luck = Stats.WorkingStats.LiveLuck;
            createdClass.Name = ClassName;

            // Read all CLASS*.CFG files and add to listbox
            string[] files = Directory.GetFiles(DaggerfallUnity.Instance.Arena2Path, "CLASS*.CFG");
            List<string> coreClasses = new List<string>();
            if (files.Length > 0)
            {
                for (int i = 0; i < files.Length - 1; i++)
                {
                    DaggerfallConnect.Arena2.ClassFile classFile = new DaggerfallConnect.Arena2.ClassFile(files[i]);
                    coreClasses.Add(classFile.Career.Name.ToUpper());
                }
            }

            if (coreClasses.Contains(createdClass.Name.ToUpper()))
            {
                var msgBox = new DaggerfallMessageBox(uiManager, this);
                msgBox.EnableVerticalScrolling(80);
                msgBox.SetText("Class Name overwrites core game class names, custom class cannot be saved.");

                msgBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.OK);
                msgBox.OnButtonClick += (sndr, button) =>
                {
                    sndr.CancelWindow();
                };

                msgBox.Show();
                return;
            }

            var myDFCareersUpper = new DFCareerArray();
            foreach (KeyValuePair<string, CharacterDocument> cd in myDfCareers.DfCareers)
                myDFCareersUpper.DfCareers.Add(cd.Key.ToUpper(), cd.Value);

            if (myDfCareers.DfCareers.Count == 0 ||  !myDFCareersUpper.DfCareers.ContainsKey(createdClass.Name.ToUpper()))
            {
                var cd = new CharacterDocument
                {
                    career = createdClass,
                    reputationMerchants = merchantsRep,
                    reputationCommoners = peasantsRep,
                    reputationNobility =  nobilityRep,
                    reputationScholars =  scholarsRep,
                    reputationUnderworld = underworldRep,
                    isCustom = true
                };
                myDfCareers.DfCareers.Add(createdClass.Name, cd);
                SaveCreatedClass();
                CloseWindow();
            }
            else
            {
                DaggerfallMessageBox messageBox;
                messageBox = new DaggerfallMessageBox(uiManager, this);
                messageBox.SetText("Custom class with the same name already exists, do you want to overwrite it with this new class?");
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
                messageBox.OnButtonClick += ConfirmReplacePopup_OnButtonClick;
                messageBox.Show();
            }

            return;
        }

        void ConfirmReplacePopup_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                myDfCareers.DfCareers.Remove(createdClass.Name);
                var cd = new CharacterDocument
                {
                    career = createdClass,
                    reputationMerchants = merchantsRep,
                    reputationCommoners = peasantsRep,
                    reputationNobility = nobilityRep,
                    reputationScholars = scholarsRep,
                    reputationUnderworld = underworldRep,
                    isCustom =  true,
                };
                myDfCareers.DfCareers.Add(createdClass.Name, cd);
                sender.CloseWindow();
                SaveCreatedClass();
                CloseWindow();
            }
            else
            {
                sender.CloseWindow();
            }


            return;
        }

        void SaveCreatedClass()
        {
            if (myDfCareers.DfCareers.Count == 0)
                return;
            fsData sData = null;

            var result = _serializer.TrySerialize<DFCareerArray>(myDfCareers, out sData);
            if (result.Failed)
                return;

            var filename = Application.persistentDataPath + @"/customClass.json";
            File.WriteAllText(filename, fsJsonPrinter.PrettyJson(sData));
            return;
        }

        DFCareerArray LoadCreatedClass()
        {
            var filename = Application.persistentDataPath + @"/customClass.json";
            var careers = new DFCareerArray();

            if (!File.Exists(filename))
            {
                return careers;
            }
            else
            {
               if( _serializer.TryDeserialize(fsJsonParser.Parse(File.ReadAllText(filename)), ref careers).Failed)
                return careers;

               return careers;

            }
        }

        private void UpdateDifficulty()
        {
            const int defaultDaggerX = 220;
            const int defaultDaggerY = 115;
            const int minDaggerY = 46;     // Visually clamp to gauge size
            const int maxDaggerY = 186;    // Visually clamp to gauge size

            // hp adjustment
            if (createdClass.HitPointsPerLevel >= defaultHpPerLevel)
            {
                difficultyPoints = createdClass.HitPointsPerLevel - defaultHpPerLevel; // +1 pt for each hp above default
            } 
            else
            {
                difficultyPoints = -(2 * (defaultHpPerLevel - createdClass.HitPointsPerLevel)); // -2 pts for each hp below default
            }

            // adjustments for special advantages/disadvantages
            difficultyPoints += advantageAdjust + disadvantageAdjust;

            // Set level advancement difficulty
            createdClass.AdvancementMultiplier = 0.3f + (2.7f * (float)(difficultyPoints + 12) / 52f);

            // Reposition the difficulty dagger
            int daggerY = 0;
            if (difficultyPoints >= 0)
            {
                daggerY = Math.Max(minDaggerY, (int)(defaultDaggerY - (37 * (difficultyPoints / 40f))));
            } 
            else
            {
                daggerY = Math.Min(maxDaggerY, (int)(defaultDaggerY + (41 * (-difficultyPoints / 12f))));
            }

            daggerPanel.Position = new Vector2(defaultDaggerX, daggerY);
            DaggerfallUI.Instance.StartCoroutine(AnimateDagger());
        }

        IEnumerator AnimateDagger()
        {
            Panel daggerTrailPanel = new Panel();
            daggerTrailPanel.Position = daggerPanel.Position;
            daggerTrailPanel.Size = daggerPanel.Size;
            daggerTrailPanel.BackgroundColorTexture = nativeDaggerTexture;
            daggerTrailPanel.BackgroundColor = new Color32(255, 255, 255, 255);
            NativePanel.Components.Add(daggerTrailPanel);
            float daggerTrailTime = daggerTrailLingerTime;

            while ((daggerTrailTime -= Time.unscaledDeltaTime) >= 0f)
            {
                daggerTrailPanel.BackgroundColor = new Color32(255, 255, 255, (byte)(255 * daggerTrailTime / daggerTrailLingerTime));
                yield return new WaitForEndOfFrame();
            }
            NativePanel.Components.Remove(daggerTrailPanel);
            daggerTrailPanel.Dispose();
        }

        #endregion

        #region Properties

        public int AdvantageAdjust
        {
            set { advantageAdjust = value; UpdateDifficulty(); }
        }

        public int DisadvantageAdjust
        {
            set { disadvantageAdjust = value; UpdateDifficulty(); }
        }

        public short MerchantsRep
        {
            get { return merchantsRep; }
            set { merchantsRep = value; }
        }

        public short PeasantsRep
        {
            get { return peasantsRep; }
            set { peasantsRep = value; }
        }

        public short ScholarsRep
        {
            get { return scholarsRep; }
            set { scholarsRep = value; }
        }

        public short NobilityRep
        {
            get { return nobilityRep; }
            set { nobilityRep = value; }
        }

        public short UnderworldRep
        {
            get { return underworldRep; }
            set { underworldRep = value; }
        }

        public DFCareer CreatedClass
        {
            get { return createdClass; }
        }

        public string ClassName
        {
            get { return nameTextBox.Text; }
        }

        public StatsRollout Stats
        {
            get { return statsRollout; }
        }

        #endregion
    }    
}