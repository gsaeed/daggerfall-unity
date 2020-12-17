// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2020 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Player;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    /// <summary>
    /// Implements the select class window.
    /// </summary>
    public class CreateCharClassSelect : DaggerfallListPickerWindow
    {
        const int startClassDescriptionID = 2100;

        List<DFCareer> classList = new List<DFCareer>();
        DFCareer selectedClass;
        int selectedClassIndex = 0;

        public DFCareer SelectedClass
        {
            get { return selectedClass; }
        }

        public CreateCharClassSelect(IUserInterfaceManager uiManager, DaggerfallBaseWindow previous = null)
            : base(uiManager, previous)
        {
        }

        protected override void Setup()
        {
            base.Setup();

            // Read all CLASS*.CFG files and add to listbox
            string[] files = Directory.GetFiles(DaggerfallUnity.Instance.Arena2Path, "CLASS*.CFG");
            if (files != null && files.Length > 0)
            {
                for (int i = 0; i < files.Length - 1; i++)
                {
                    ClassFile classFile = new ClassFile(files[i]);
                    if (classFile.Career.Name == "Spellsword")
                    {
                        DFCareer c = PopulateCareer();
                        classList.Add(c);
                        listBox.AddItem(c.Name);
                    }
                    else
                    {
                        classList.Add(classFile.Career);
                        listBox.AddItem(classFile.Career.Name);
                    }
                }
            }

            // Last option is for creating custom classes
            listBox.AddItem("Custom");

            OnItemPicked += DaggerfallClassSelectWindow_OnItemPicked;
        }

        void DaggerfallClassSelectWindow_OnItemPicked(int index, string className)
        {
            if (index == classList.Count) // "Custom" option selected
            {
                selectedClass = null;
                selectedClassIndex = -1;
                CloseWindow();
            } 
            else 
            {
                selectedClass = classList[index];
                selectedClassIndex = index;
                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, this);
                TextFile.Token[] textTokens = DaggerfallUnity.Instance.TextProvider.GetRSCTokens(startClassDescriptionID + index);
                messageBox.SetTextTokens(textTokens);
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                Button noButton = messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
                noButton.ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.ButtonClick);
                messageBox.OnButtonClick += ConfirmClassPopup_OnButtonClick;
                uiManager.PushWindow(messageBox);

                AudioClip clip = DaggerfallUnity.Instance.SoundReader.GetAudioClip(SoundClips.SelectClassDrums);
                DaggerfallUI.Instance.AudioSource.PlayOneShot(clip, DaggerfallUnity.Settings.SoundVolume);
            }
        }

        void ConfirmClassPopup_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                sender.CloseWindow();
                CloseWindow();
            }
            else if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.No)
            {
                selectedClass = null;
                sender.CancelWindow();
            }
        }

        DFCareer PopulateCareer()
        {
           

            DFCareer c = new DFCareer
            {
                AcuteHearing = false,
                AdrenalineRush = false,
                AdvancementMultiplier = 1.546154f,
                Agility = 45,
                AnimalsAttackModifier = DFCareer.AttackModifier.Normal,
                Athleticism = false,
                Axes = DFCareer.Proficiency.Normal,
                BluntWeapons = DFCareer.Proficiency.Normal,
                DaedraAttackModifier = DFCareer.AttackModifier.Normal,
                DamageFromHolyPlaces = false,
                DamageFromSunlight = false,
                DarknessPoweredMagery = DFCareer.DarknessMageryFlags.Normal,
                Disease = DFCareer.Tolerance.Normal,
                Endurance = 40,
                Fire = DFCareer.Tolerance.Normal,
                ForbiddenMaterials = (DFCareer.MaterialFlags)0,
                Frost = DFCareer.Tolerance.Normal,
                HandToHand = DFCareer.Proficiency.Normal,
                HitPointsPerLevel = 30,
                HumanoidAttackModifier = DFCareer.AttackModifier.Normal,
                Intelligence = 65,
                LightPoweredMagery = DFCareer.LightMageryFlags.Normal,
                LongBlades = DFCareer.Proficiency.Normal,
                Luck = 50,
                Magic = DFCareer.Tolerance.Normal,
                MajorSkill1 = DFCareer.Skills.Restoration,
                MajorSkill2 = DFCareer.Skills.Archery,
                MajorSkill3 = DFCareer.Skills.ShortBlade,
                MinorSkill1 = DFCareer.Skills.Alteration,
                MinorSkill2 = DFCareer.Skills.CriticalStrike,
                MinorSkill3 = DFCareer.Skills.Medical,
                MinorSkill4 = DFCareer.Skills.Illusion,
                MinorSkill5 = DFCareer.Skills.Mysticism,
                MinorSkill6 = DFCareer.Skills.Thaumaturgy,
                MissileWeapons = DFCareer.Proficiency.Normal,
                Name = "Spellsword",
                NoRegenSpellPoints = false,
                Paralysis = DFCareer.Tolerance.Normal,
                Personality = 35,
                Poison = DFCareer.Tolerance.Normal,
                PrimarySkill1 = DFCareer.Skills.Destruction,
                PrimarySkill2 = DFCareer.Skills.Daedric,
                PrimarySkill3 = DFCareer.Skills.LongBlade,
                RapidHealing = DFCareer.RapidHealingFlags.None,
                Regeneration = DFCareer.RegenerationFlags.None,
                Shock = DFCareer.Tolerance.Normal,
                ShortBlades = DFCareer.Proficiency.Normal,
                Speed = 50,
                SpellAbsorption = DFCareer.SpellAbsorptionFlags.None,
                SpellPointMultiplier = DFCareer.SpellPointMultipliers.Times_0_50,
                SpellPointMultiplierValue = 0.5f,
                Strength = 75,
                UndeadAttackModifier = DFCareer.AttackModifier.Normal,
                Willpower = 40
            };
            return c;

        }

        public int SelectedClassIndex
        {
            get { return selectedClassIndex; }
        }

        public List<DFCareer> ClassList
        {
            get { return classList; }
        }
    }
}