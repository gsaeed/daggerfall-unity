
// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2021 Daggerfall Workshop
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
using FullSerializer;
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
    public class CreateCharTemplateSelect : DaggerfallListPickerWindow
    {
        const int startClassDescriptionID = 2100;
        public static readonly fsSerializer _serializer = new fsSerializer();
        List<DFCareer> classList = new List<DFCareer>();
        DFCareer selectedClass;
        int selectedClassIndex = 0;

        private DFCareerArray myDFCareers = new DFCareerArray();
        public DFCareer SelectedClass => selectedClass;

        public CharacterDocument SelectCharacterDocument => myDFCareers.DfCareers[selectedClass.Name];

        public CreateCharTemplateSelect(IUserInterfaceManager uiManager, DaggerfallBaseWindow previous = null)
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
                    classList.Add(classFile.Career);
                    listBox.AddItem(classFile.Career.Name);
                }
            }

            AddCustomClasses();

            OnItemPicked += DaggerfallTemplateSelectWindow_OnItemPicked;
        }

        void AddCustomClasses()
        {
            var filename = Application.persistentDataPath + @"/customClass.json";
            var careers = new DFCareerArray();
            if (!File.Exists(filename))
            {
                return;
            }
            else
            {

                if (_serializer.TryDeserialize(fsJsonParser.Parse(File.ReadAllText(filename)), ref careers).Failed)
                    return;

                foreach (KeyValuePair<string, CharacterDocument> c in careers.DfCareers)
                {
                    myDFCareers.DfCareers.Add(c.Key, c.Value);
                    classList.Add(c.Value.career);
                    listBox.AddItem(c.Key);
                }
                return;
            }
        }

        void DaggerfallTemplateSelectWindow_OnItemPicked(int index, string className)
        {

            CreateCharCustomClass.selectedTemplateCareer = new DFCareer();
            CreateCharCustomClass.selectedTemplateCareer = classList[index];
            DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, this);
            messageBox.SetText($"you have selected {classList[index].Name}, Do you want to overwrite existing value with this class default?");
            messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
            Button noButton = messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
            noButton.ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.ButtonClick);
            messageBox.OnButtonClick += ConfirmClassPopup_OnButtonClick;
            uiManager.PushWindow(messageBox);

            AudioClip clip = DaggerfallUnity.Instance.SoundReader.GetAudioClip(SoundClips.SelectClassDrums);
            DaggerfallUI.Instance.AudioSource.PlayOneShot(clip, DaggerfallUnity.Settings.SoundVolume);

        }

        void ConfirmClassPopup_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            CreateCharCustomClass.templateSelected = false;
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                CreateCharCustomClass.templateSelected = true;
                sender.CloseWindow();
                CloseWindow();
            }
            else if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.No)
            {
                CreateCharCustomClass.templateSelected = false;
                CreateCharCustomClass.selectedTemplateCareer = null;
                selectedClass = null;
                sender.CancelWindow();
                CloseWindow();
            }
        }

        public int SelectedClassIndex => selectedClassIndex;

        public List<DFCareer> ClassList => classList;
    }
}