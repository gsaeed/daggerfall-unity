// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2023 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Hazelnut
// Contributors:    
// 
// Notes:
//

using DaggerfallWorkshop.Game.UserInterface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    public enum UIWindowType
    {
        Automap,
        Banking,
        BankPurchasePopup,
        BookReader,
        CharacterSheet,
        Controls,
        Court,
        DaedraSummoned,
        EffectSettingsEditor,
        ExteriorAutomap,
        GuildServicePopup,
        GuildServiceCureDisease = 39,   // Ensure existing values remain 
        GuildServiceDonation = 40,      //  unchanged so as not to break 
        GuildServiceTraining = 41,      //  existing pre-compiled mods.
        Inventory = 11,
        ItemMaker,
        JoystickControls,
        LoadClassicGame,
        MerchantRepairPopup,
        MerchantServicePopup,
        PauseOptions,
        PlayerHistory,
        PotionMaker,
        QuestJournal,
        QuestOffer,
        Rest,
        SpellBook,
        SpellIconPicker = 42, // Ensure existing values remain unchanged so as not to break existing pre-compiled mods.
        SpellMaker = 24,
        StartNewGameWizard,
        Start,
        Talk,
        Tavern,
        TeleportPopUp,
        Trade,
        Transport,
        TravelMap,
        TravelPopUp,
        UnityMouseControls,
        UnitySaveGame,
        UseMagicItem,
        VidPlayer,
        WitchesCovenPopup,
    }

    public static class UIWindowFactory
    {
        public static Dictionary<UIWindowType, Type> uiWindowImplementations = new Dictionary<UIWindowType, Type>()
        {
            { UIWindowType.Automap, typeof(DaggerfallAutomapWindow) },
            { UIWindowType.Banking, typeof(DaggerfallBankingWindow) },
            { UIWindowType.BankPurchasePopup, typeof(DaggerfallBankPurchasePopUp) },
            { UIWindowType.BookReader, typeof(DaggerfallBookReaderWindow) },
            { UIWindowType.CharacterSheet, typeof(DaggerfallCharacterSheetWindow) },
            { UIWindowType.Controls, typeof(DaggerfallControlsWindow) },
            { UIWindowType.Court, typeof(DaggerfallCourtWindow) },
            { UIWindowType.DaedraSummoned, typeof(DaggerfallDaedraSummonedWindow) },
            { UIWindowType.EffectSettingsEditor, typeof(DaggerfallEffectSettingsEditorWindow) },
            { UIWindowType.ExteriorAutomap, typeof(DaggerfallExteriorAutomapWindow) },
            { UIWindowType.GuildServicePopup, typeof(DaggerfallGuildServicePopupWindow) },
            { UIWindowType.GuildServiceCureDisease, typeof(DaggerfallGuildServiceCureDisease) },
            { UIWindowType.GuildServiceDonation, typeof(DaggerfallGuildServiceDonation) },
            { UIWindowType.GuildServiceTraining, typeof(DaggerfallGuildServiceTraining) },
            { UIWindowType.Inventory, typeof(DaggerfallInventoryWindow) },
            { UIWindowType.ItemMaker, typeof(DaggerfallItemMakerWindow) },
            { UIWindowType.JoystickControls, typeof(DaggerfallJoystickControlsWindow) },
            { UIWindowType.LoadClassicGame, typeof(DaggerfallLoadClassicGameWindow) },
            { UIWindowType.MerchantRepairPopup, typeof(DaggerfallMerchantRepairPopupWindow) },
            { UIWindowType.MerchantServicePopup, typeof(DaggerfallMerchantServicePopupWindow) },
            { UIWindowType.PauseOptions, typeof(DaggerfallPauseOptionsWindow) },
            { UIWindowType.PlayerHistory, typeof(DaggerfallPlayerHistoryWindow) },
            { UIWindowType.PotionMaker, typeof(DaggerfallPotionMakerWindow) },
            { UIWindowType.QuestJournal, typeof(DaggerfallQuestJournalWindow) },
            { UIWindowType.QuestOffer, typeof(DaggerfallQuestOfferWindow) },
            { UIWindowType.Rest, typeof(DaggerfallRestWindow) },
            { UIWindowType.SpellBook, typeof(DaggerfallSpellBookWindow) },
            { UIWindowType.SpellIconPicker, typeof(SpellIconPickerWindow) },
            { UIWindowType.SpellMaker, typeof(DaggerfallSpellMakerWindow) },
            { UIWindowType.StartNewGameWizard, typeof(StartNewGameWizard) },
            { UIWindowType.Start, typeof(DaggerfallStartWindow) },
            { UIWindowType.Talk, typeof(DaggerfallTalkWindow) },
            { UIWindowType.Tavern, typeof(DaggerfallTavernWindow) },
            { UIWindowType.TeleportPopUp, typeof(DaggerfallTeleportPopUp) },
            { UIWindowType.Trade, typeof(DaggerfallTradeWindow) },
            { UIWindowType.Transport, typeof(DaggerfallTransportWindow) },
            { UIWindowType.TravelMap, typeof(DaggerfallTravelMapWindow) },
            { UIWindowType.TravelPopUp, typeof(DaggerfallTravelPopUp) },
            { UIWindowType.UnityMouseControls, typeof(DaggerfallUnityMouseControlsWindow) },
            { UIWindowType.UnitySaveGame, typeof(DaggerfallUnitySaveGameWindow) },
            { UIWindowType.UseMagicItem, typeof(DaggerfallUseMagicItemWindow) },
            { UIWindowType.VidPlayer, typeof(DaggerfallVidPlayerWindow) },
            { UIWindowType.WitchesCovenPopup, typeof(DaggerfallWitchesCovenPopupWindow) },
        };

        private static Dictionary<UIWindowType, Mod> uiWindowModded = new Dictionary<UIWindowType, Mod>();


        /// <summary>
        /// Register a custom UI Window implementation class. Overwrites the previous class type.
        /// </summary>
        /// <param name="windowType">The type of ui window to be replaced</param>
        /// <param name="windowClassType">The c# class Type of the implementation class to replace with</param>
        public static void RegisterCustomUIWindow(UIWindowType windowType, Type windowClassType)
        {
            Mod curMod = null;
            Mod prevMod = null;
            var stackTrace = new StackTrace();
            var thisFrame = string.Empty;
            thisFrame = stackTrace.GetFrame(1).GetMethod()?.ReflectedType?.ToString();
            string fileName = string.Empty;

            thisFrame = thisFrame.Trim();

            fileName = thisFrame;

            if (fileName.Length > 0 && fileName.Contains('.'))
                fileName = fileName.Substring(fileName.LastIndexOf('.') + 1);

            if (fileName.Length > 0 && fileName.Contains(' '))
                fileName = fileName.Substring(fileName.LastIndexOf(' ') + 1);

            var scr = GetChildWithName(GameManager.Instance.gameObject,fileName);
            if (scr != null)
            {
                curMod = ModManager.Instance.GetMod(scr.name);
                if (curMod == null)
                    Debug.Log($"RegisterCustomUIWindow: {scr.name} could not find a mod for {thisFrame}");
            }
            else
            {
                Debug.LogError(
                    $"RegisterCustomUIWindow: {fileName} could not find a GameObject [{thisFrame}]");
                uiWindowImplementations[windowType] = windowClassType;
                DaggerfallUI.Instance.ReinstantiatePersistentWindowInstances();
                return;
            }

            if (curMod == null || !uiWindowModded.TryGetValue(windowType, out prevMod) || (prevMod != null && prevMod.LoadPriority < curMod.LoadPriority))
            {
                if (prevMod == null)
                {
                    if (curMod != null)
                        DaggerfallUnity.LogMessage($"RegisterCustomUIWindow: {windowType} from mod {curMod.Title}:{curMod.FileName}", true);
                    else
                        Debug.LogWarning($"RegisterCustomUIWindow: {windowType} was unable to find mod for {scr.name} {thisFrame}");

                    uiWindowModded[windowType] = curMod;
                    uiWindowImplementations[windowType] = windowClassType;
                    DaggerfallUI.Instance.ReinstantiatePersistentWindowInstances();
                    return;
                }
                else
                {
                    DaggerfallUnity.LogMessage($"RegisterCustomUIWindow: {windowType} from mod {curMod.Title}:[{curMod.LoadPriority}] overrides {prevMod.Title}:[{prevMod.LoadPriority}]" , true);
                    uiWindowModded[windowType] = curMod;
                    uiWindowImplementations[windowType] = windowClassType;
                    DaggerfallUI.Instance.ReinstantiatePersistentWindowInstances();
                    return;
                }
            }
            else 
            {
                DaggerfallUnity.LogMessage($"RegisterCustomUIWindow: {windowType} from mod {curMod.Title}:[{curMod.LoadPriority}] cannot override {prevMod.Title}:[{prevMod.LoadPriority}] due to load position.", true);
                return;
            }

        }

        private static GameObject GetChildWithName(GameObject obj, string name)
        {
            Transform trans = obj.transform;

            foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (go.GetComponents<MonoBehaviour>().Any(child => child.name.ToUpper() == name.ToUpper()))
                {
                    return go;
                }

                foreach (var child in go.GetComponents<MonoBehaviour>())
                {
                    var chNoSp = child.ToString().Substring(child.ToString().LastIndexOf('.') + 1);
                    if (chNoSp == null || chNoSp.Length <= 1)
                        continue;
                    chNoSp = chNoSp.Remove(chNoSp.Length - 1).ToUpper();
                    var pNoSp = Regex.Replace(name, @"\s+", "").ToUpper();
                    if ( chNoSp == pNoSp || (chNoSp.Length >= pNoSp.Length && pNoSp == chNoSp.Substring(chNoSp.Length - pNoSp.Length)))
                    {
                        return go;
                    }
                }
            }

            return null;
        }

        public static IUserInterfaceWindow GetInstance(UIWindowType windowType, IUserInterfaceManager uiManager)
        {
            object[] args = new object[] { uiManager };
            return GetInstance(windowType, args);
        }

        public static IUserInterfaceWindow GetInstance(UIWindowType windowType, IUserInterfaceManager uiManager, DaggerfallBaseWindow previous)
        {
            object[] args = new object[] { uiManager, previous };
            return GetInstance(windowType, args);
        }

        public static IUserInterfaceWindow GetInstanceWithArgs(UIWindowType windowType, object[] args)
        {

            return GetInstance(windowType, args);
        }

        public static UIWindowType? GetWindowType(Type type)
        {
            var pair = uiWindowImplementations.FirstOrDefault(x => x.Value == type);
            if (pair.Value != null)
                return pair.Key;

            return null;
        }

        private static IUserInterfaceWindow GetInstance(UIWindowType windowType, object[] args)
        {
            Type windowClassType;
            if (uiWindowImplementations.TryGetValue(windowType, out windowClassType))
            {
                return (IUserInterfaceWindow)Activator.CreateInstance(windowClassType, args);
            }
            return null;
        }
    }
}

