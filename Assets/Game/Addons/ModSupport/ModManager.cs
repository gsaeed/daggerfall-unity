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
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Utility.AssetInjection;
using FullSerializer;

namespace DaggerfallWorkshop.Game.Utility.ModSupport
{
    /// <summary>
    /// Handles setup and execution of mods and provides support for features related to modding support.
    /// Mods can also use this singleton to find and interact with other mods.
    /// </summary>
    public class ModManager : MonoBehaviour
    {
        #region Fields

        public const string MODEXTENSION = ".dfmod";
        public const string MODINFOEXTENSION = ".dfmod.json";
        public const string MODCONFIGFILENAME = "Mod_Settings.json";
        public static bool SuccessfulSort = false;
        public static int ErrorsEncountered = 0;
        private static bool cyclicError = false;
        private static int VisitCnt = 0;
#if UNITY_EDITOR
        const string dataFolder = "EditorData";
#else
        const string dataFolder = "GameData";
#endif
        private static List<string> _alwaysIncludeModList = new List<string>();
        bool alreadyAtStartMenuState = false;
        static bool alreadyStartedInit = false;
        [SerializeField] public List<Mod> mods;
        [SerializeField] public List<Mod> patchMods;
        List<Mod> preSortedMods;
        public static readonly fsSerializer _serializer = new fsSerializer();

        public static string[] textExtensions = new string[]
        {
            ".txt",
            ".html",
            ".html",
            ".xml",
            ".bytes",
            ".json",
            ".csv",
            ".yaml",
            ".fnt",
        };

        public struct ActiveModListComponents
        {
            public string Filename;
            public string EnableIndicator;

            public ActiveModListComponents(string modName, string enabledIndicator)
            {
                Filename = modName;
                EnableIndicator = enabledIndicator;
            }
        };

        public struct ModAsset
        {
            public bool found;
            public string name;
            public string searchName;
            public Mod mod;
            public bool isLoose;
            public Type type;
            public object go;
        }


        public static List<ActiveModListComponents> fileActiveModList = new List<ActiveModListComponents>();
        public static List<string> fileBisectRange = new List<string>();

#if UNITY_EDITOR
        [Tooltip("Loads mods from Assets/Game/Mods in debug mode, without creating an AssetBundle.")]
        public bool LoadVirtualMods = true;
#endif

        // Returns whether the ModManager has been through Init
        // Before Initialized is true, disabled mods are not filtered out
        // Systems that run on launch should ensure this is true before touching systems involving mods
        public bool Initialized
        {
            get { return alreadyStartedInit; }
        }

        #endregion

        #region Properties

        private Dictionary<string, List<string>> modMove = new Dictionary<string, List<string>>();
        private List<string> modMoveOrder = new List<string>();

        /// <summary>
        /// The number of mods loaded by Mod Manager.
        /// </summary>
        public int LoadedModCount
        {
            get { return mods.Count; }
        }

        /// <summary>
        /// An enumeration of mods sorted by load order.
        /// See <see cref="EnumerateModsReverse()"/> for reversed order.
        /// </summary>
        public IEnumerable<Mod> Mods
        {
            get { return mods; }
        }

        /// <summary>
        /// The directory where mods are stored. It's not writable on all platforms.
        /// </summary>
        public string ModDirectory { get; set; }

        /// <summary>
        /// The writable directory that holds mods data, separated for build and editor to allow mods
        /// to be developed and tested without affecting main game installation.
        /// </summary>
        internal string ModDataDirectory
        {
            get { return Path.Combine(DaggerfallUnity.Settings.PersistentDataPath, Path.Combine("Mods", dataFolder)); }
        }

        /// <summary>
        /// The writable directory that holds mods cache, separated for build and editor to allow mods
        /// to be developed and tested without affecting main game installation.
        /// </summary>
        internal string ModCacheDirectory
        {
            get { return Path.Combine(Application.temporaryCachePath, Path.Combine("Mods", dataFolder)); }
        }

        public static ModManager Instance { get; private set; }

#if UNITY_EDITOR
        /// <summary>
        /// Path to Mods folder inside Unity Editor where source data for mods is stored.
        /// </summary>
        public static string EditorModsDirectory
        {
            get { return Application.dataPath + "/Game/Mods"; }
        }
#endif

        #endregion

        #region Unity

        void Awake()
        {
            if (string.IsNullOrEmpty(ModDirectory))
                ModDirectory = Path.Combine(Application.streamingAssetsPath, "Mods");

            SetupSingleton();

            if (Instance == this)
                StateManager.OnStateChange += StateManager_OnStateChange;

            if (!DaggerfallUnity.Settings.LypyL_ModSystem)
            {
                Debug.Log("Mod System disabled");
                StateManager.OnStateChange -= StateManager_OnStateChange;
                Destroy(this);
            }

            mods = new List<Mod>();

            if (Directory.Exists(ModDirectory))
            {
                FindModsFromDirectory();
                LoadModSettings();
                SortMods();
            }
            else
            {
                Debug.LogWarningFormat("Mod system is enabled but directory {0} doesn't exist.", ModDirectory);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get index for mod by title
        /// </summary>
        /// <param name="modTitle">The title of a mod.</param>
        /// <returns>The index of the mod with the given title or -1 if not found.</returns>
        public int GetModIndex(string modTitle)
        {
            if (string.IsNullOrEmpty(modTitle))
                return -1;

            for (int i = 0; i < mods.Count; i++)
            {
                if (mods[i].Title == modTitle)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Get mod using index
        /// </summary>
        /// <param name="index">The index of a mod.</param>
        /// <returns>The mod at the given index or null if the index is invalid.</returns>
        public Mod GetMod(int index)
        {
            if (index < 0 || index > mods.Count)
                return null;
            else
                return mods[index];
        }

        /// <summary>
        /// Get mod using Mod Title
        /// </summary>
        /// <param name="modTitle">The title of a mod.</param>
        /// <returns>The mod with the given title or null if not found.</returns>
        public Mod GetMod(string modTitle)
        {
            int index = GetModIndex(modTitle);

            if (index >= 0)
                return mods[index];
            else
                return null;
        }

        /// <summary>
        /// Get mod from GUID
        /// </summary>
        /// <param name="modGUID">The unique identifier of a mod.</param>
        /// <returns>The mod with the given GUID or null if not found.</returns>
        public Mod GetModFromGUID(string modGUID)
        {
            if (string.IsNullOrEmpty(modGUID))
                return null;
            else if (modGUID == "invalid")
                return null;
            else
            {
                foreach (var mod in mods)
                {
                    if (mod.GUID == modGUID)
                        return mod;
                }

                return null;
            }
        }

        /// <summary>
        /// Get mod title from GUID
        /// </summary>
        /// <param name="modGUID">The unique identifier of a mod.</param>
        /// <returns>The title of the mod with the given GUID or null if not found.</returns>
        public string GetModTitleFromGUID(string modGUID)
        {
            if (string.IsNullOrEmpty(modGUID))
                return null;
            else if (modGUID == "invalid")
                return null;
            else
            {
                foreach (var mod in mods)
                {
                    if (mod.GUID == modGUID)
                        return mod.Title;
                }

                return null;
            }
        }

        /// <summary>
        /// Returns all loaded mods in array. See also <see cref="Mods"/>.
        /// </summary>
        /// <returns>A collection with all the mods.</returns>
        public Mod[] GetAllMods()
        {
            return mods.ToArray();
        }

        public int GetAllModsCount()
        {
            return mods.Count;
        }

        /// <summary>
        /// Returns all loaded mods in array. See also <see cref="Mods"/>.
        /// </summary>
        /// <param name="loadOrder">ordered by load priority if true</param>
        /// <returns>A collection with all the mods.</returns>
        [Obsolete("Mods now are always sorted by load order. Use overload without parameters or Mods property.")]
        public Mod[] GetAllMods(bool loadOrder)
        {
            return GetAllMods();
        }

        /// <summary>
        /// Enumerates all mods with reverse load order.
        /// Unlike <see cref="Enumerable.Reverse{TSource}(IEnumerable{TSource})"/>, this method doesn't allocate memory for a new collection.
        /// </summary>
        /// <returns>An enumeration of mods sorted by reverse load order.</returns>
        public IEnumerable<Mod> EnumerateModsReverse()
        {
            for (int i = mods.Count; i-- > 0;)
                yield return mods[i];
        }

        /// <summary>
        /// Get modtitle string for each loaded mod
        /// </summary>
        /// <returns>A collection with all mod titles.</returns>
        public string[] GetAllModTitles()
        {
            var selection = from modInfo in GetAllModInfo()
                select modInfo.ModTitle;
            return selection.ToArray();
        }

        /// <summary>
        /// Get mod file name string for each loaded mod
        /// </summary>
        /// <returns>A collection with all mod file names.</returns>
        public string[] GetAllModFileNames()
        {
            var selection = from mod in mods
                select mod.FileName;
            return selection.ToArray();
        }

        /// <summary>
        /// Get array of ModInfo objects for each loaded mod
        /// </summary>
        /// <returns>A collection with all mod informations.</returns>
        public ModInfo[] GetAllModInfo()
        {
            var selection = from mod in GetAllMods()
                where (mod.ModInfo != null)
                select mod.ModInfo;
            return selection.ToArray();
        }

        /// <summary>
        /// Gets all the mod GUIDs which are defined and valid.
        /// </summary>
        /// <returns>A collection of valid mod GUIDs.</returns>
        public string[] GetAllModGUID()
        {
            var selection = from mod in mods
                where (mod.ModInfo != null && mod.GUID != "invalid")
                select mod.ModInfo.GUID;
            return selection.ToArray();
        }

        /// <summary>
        /// Gets all mods wich provide contributes to save data.
        /// </summary>
        /// <returns>An enumeration of mods with save data.</returns>
        public IEnumerable<Mod> GetAllModsWithSaveData()
        {
            return from mod in mods
                where mod.SaveDataInterface != null
                select mod;
        }

        /// <summary>
        /// Gets all mods, in reverse order, that provide contributes that match the given filter.
        /// </summary>
        /// <param name="filter">A filter that accepts or rejects a mod; can be used to check if a contribute is present.</param>
        /// <returns>An enumeration of mods with contributes.</returns>
        internal IEnumerable<Mod> GetAllModsWithContributes(Predicate<ModContributes> filter = null)
        {
            return EnumerateModsReverse().Where(x =>
                x.Enabled && x.ModInfo.Contributes != null && (filter == null || filter(x.ModInfo.Contributes)));
        }

        /// <summary>
        /// Get all asset names from mod
        /// </summary>
        /// <param name="modTitle">The title of a mod.</param>
        /// <returns>A collection with the names of all assets from a mod or null if not found.</returns>
        public string[] GetModAssetNames(string modTitle)
        {
            int index = GetModIndex(modTitle);
            if (index < 0)
                return null;
            else
                return mods[index].AssetNames;
        }

        /// <summary>
        /// Get type t asset from mod using name of asset
        /// </summary>
        /// <typeparam name="T">Asset Type</typeparam>
        /// <param name="assetName">asset name</param>
        /// <param name="modTitle">title of mod</param>
        /// <param name="clone">return copy of asset</param>
        ///<param name="check">true if loaded sucessfully</param>
        /// <returns>The loaded asset or null if not found.</returns>
        public T GetAssetFromMod<T>(string assetName, string modTitle, bool clone, out bool check)
            where T : UnityEngine.Object
        {
            check = false;
            T asset = null;

            int index = GetModIndex(modTitle);

            if (index < 0)
                return null;

            asset = mods[index].GetAsset<T>(assetName, clone);
            check = asset != null;
            return asset;
        }

        public bool TryGetAssetPatch<T>(string guid, string name, bool? clone, out T asset) where T : UnityEngine.Object
        {
            if (patchMods.All(x => x.ModInfo.GUID != guid))
            {
                asset = null;
                return false;
            }

            var query = from mod in patchMods
                where mod.AssetBundle != null && mod.AssetBundle.Contains(name)
                select clone.HasValue ? mod.GetAssetPatch<T>(name, out _) : mod.LoadAssetPatch<T>(name);

            asset = query.FirstOrDefault(x => x != null);

            return asset != null;

        }

        /// <summary>
        /// Seek asset in all mods with load order.
        /// </summary>
        /// <param name="name">Name of asset to seek.</param>
        /// <param name="clone">Make a copy of asset? If null is loaded without cache.</param>
        /// <param name="asset">Loaded asset or null.</param>
        /// <remarks>
        /// If multiple mods contain an asset with given name, priority is defined by load order.
        /// </remarks>
        /// <returns>True if asset is found and loaded sucessfully.</returns>
        public bool TryGetAsset<T>(string name, bool? clone, out T asset) where T : UnityEngine.Object
        {
            Mod realMod = null;
            T patchAsset;
            var query = from mod in EnumerateEnabledModsReverse()
#if UNITY_EDITOR
                    where (mod.AssetBundle != null && mod.AssetBundle.Contains(name)) ||
                          (mod.IsVirtual && mod.HasAsset(name))
#else
                        where mod.AssetBundle != null && mod.AssetBundle.Contains(name)
#endif
                    //select clone.HasValue ? mod.GetAsset<T>(name, clone.Value) : mod.LoadAsset<T>(name),
                select new { Mod = mod, Asset = clone.HasValue ? mod.GetAsset<T>(name, clone.Value) : mod.LoadAsset<T>(name) };

            var result = query.FirstOrDefault(x => x.Asset != null);
            if (result != null)
            {
                asset = result.Asset;
                realMod = result.Mod;
            }
            else
            {
                asset = null;
            }

            if (TryGetAssetPatch(realMod?.ModInfo.GUID, name, clone, out patchAsset))
            {
                asset = patchAsset;
            }
            return asset != null;
        }

        /// <summary>
        /// Seek asset in all mods with load order.
        /// Check all names for each mod with the given priority.
        /// </summary>
        /// <param name="names">Names of asset to seek ordered by priority.</param>
        /// <param name="clone">Make a copy of asset? If null is loaded without cache.</param>
        /// <param name="asset">Loaded asset or null.</param>
        /// <remarks>
        /// If multiple mods contain an asset with any of the given names, priority is defined by load order.
        /// If chosen mod contains multiple assets, priority is defined by order of names list.
        /// </remarks>
        /// <returns>True if asset is found and loaded sucessfully.</returns>
        public bool TryGetAsset<T>(string[] names, bool? clone, out T asset) where T : UnityEngine.Object
        {
            // place logic here to find patch

            var query = from mod in EnumerateEnabledModsReverse()
#if UNITY_EDITOR
                where mod.AssetBundle != null || mod.IsVirtual
                from name in names
                where mod.IsVirtual ? mod.HasAsset(name) : mod.AssetBundle.Contains(name)
#else
                        where mod.AssetBundle != null
                        from name in names where mod.AssetBundle.Contains(name)
#endif
            //    select clone.HasValue ? mod.GetAsset<T>(name, clone.Value) : mod.LoadAsset<T>(name);
            select new { Mod = mod, Asset = clone.HasValue ? mod.GetAsset<T>(name, clone.Value) : mod.LoadAsset<T>(name) };
            Mod realMod = null;
            T patchAsset = null;

            var result = query.FirstOrDefault(x => x.Asset != null);
            if (result != null)
            {
                asset = result.Asset;
                realMod = result.Mod;
            }
            else
            {
                asset = null;
            }

            if (TryGetAssetPatch(realMod?.ModInfo.GUID, name, clone, out patchAsset))
            {
                asset = patchAsset;
            }
            return asset != null;

        }

        /// <summary>
        /// Seek asset in all mods with load order.
        /// </summary>
        /// <param name="name">Name of asset to seek.</param>
        /// <remarks>
        /// If multiple mods contain an asset with given name, priority is defined by load order.
        /// </remarks>
        /// <returns>True if asset is found and loaded sucessfully.</returns>
        public ModAsset TryGetModAsset<T>(string name) where T : UnityEngine.Object
        {
            // place logic here to find patch

            // Define the regular expression pattern for N_N-N
            string pattern = @"^(\d{1,4})_(\d{1,4})-(\d{1,4})$";
            string prefabName = string.Empty;
            int archive = 0, record = 0, frame = 0;

            // Check if the name matches the pattern
            Match match = Regex.Match(name, pattern);
            if (match.Success)
            {
                // Extract the integers as archive, record, and frame
                archive = int.Parse(match.Groups[1].Value);
                record = int.Parse(match.Groups[2].Value);
                frame = int.Parse(match.Groups[3].Value);

                // Modify the name to N_N.prefab by removing the -N part
                int lastDashIndex = name.LastIndexOf('-');
                prefabName = name.Substring(0, lastDashIndex) + ".prefab";
            }
            else
            {
                // Append ".prefab" to the name
                prefabName = name + ".prefab";
            }

            if (match.Success)
            {
                //check loose files
                Texture2D tex = null;
                var found = TextureReplacement.TryImportTextureFromLooseFiles(archive, record, frame, TextureMap.Albedo,
                    true, out tex);
                if (found)
                {
                    var types = tex.GetType();
                    ModAsset modAsset = new ModAsset();
                    modAsset.found = true;
                    modAsset.isLoose = true;
                    modAsset.name = name;
                    modAsset.searchName = name;
                    modAsset.mod = null;
                    modAsset.type = tex.GetType();
                    modAsset.go = tex;
                    return modAsset;
                }
            }

            if (!string.IsNullOrEmpty(prefabName))

            {
                var query = from mod in EnumerateEnabledModsReverse()
#if UNITY_EDITOR
                    where (mod.AssetBundle != null && mod.AssetBundle.Contains(prefabName)) ||
                          (mod.IsVirtual && mod.HasAsset(prefabName))
#else
                        where mod.AssetBundle != null && mod.AssetBundle.Contains(prefabName)
#endif
                            select new { Name = prefabName, Mod = mod, Asset = mod.LoadAsset<T>(prefabName) };

                var result = query.FirstOrDefault(x => x.Asset != null);
                var modAsset = new ModAsset();
                modAsset.found = false;
                if (result != null)
                {
                    modAsset.found = true;
                    modAsset.isLoose = false;
                    modAsset.name = result.Asset.name;
                    modAsset.searchName = result.Name;
                    modAsset.mod = result.Mod;
                    modAsset.type = result.Asset.GetType();
                    modAsset.go = result.Asset;
                    return modAsset;

                }
            }
            
            {

                var query = from mod in EnumerateEnabledModsReverse()
#if UNITY_EDITOR
                    where (mod.AssetBundle != null && mod.AssetBundle.Contains(name)) ||
                          (mod.IsVirtual && mod.HasAsset(name))
#else
                    where mod.AssetBundle != null && mod.AssetBundle.Contains(name)
#endif
                    select new { Name = name, Mod = mod, Asset = mod.LoadAsset<T>(name) };

                var result = query.FirstOrDefault(x => x.Asset != null);
                var modAsset = new ModAsset();
                modAsset.found = false;
                if (result != null)
                {
                    modAsset.found = true;
                    modAsset.isLoose = false;
                    modAsset.name = result.Asset.name;
                    modAsset.searchName = result.Name;
                    modAsset.mod = result.Mod;
                    modAsset.type = result.Asset.GetType();
                    modAsset.go = result.Asset;
                    return modAsset;
                }
            }

            return new ModAsset();
        }

        public Type TryFindModAssetType(string name)
        {
            List<Type> typesArray = new List<Type>
            {
                typeof(GameObject),
                typeof(Material),
                typeof(Texture),
                typeof(Texture2D)
            };

            // Define the regular expression pattern for N_N-N
            string pattern = @"^(\d{1,4})_(\d{1,4})-(\d{1,4})$";
            string prefabName = string.Empty;
            int archive = 0, record = 0, frame = 0;

            // Check if the name matches the pattern
            Match match = Regex.Match(name, pattern);
            if (match.Success)
            {
                // Extract the integers as archive, record, and frame
                archive = int.Parse(match.Groups[1].Value);
                record = int.Parse(match.Groups[2].Value);
                frame = int.Parse(match.Groups[3].Value);

                // Modify the name to N_N.prefab by removing the -N part
                int lastDashIndex = name.LastIndexOf('-');
                prefabName = name.Substring(0, lastDashIndex) + ".prefab";
            }
            else
            {
                // Append ".prefab" to the name
                prefabName = name + ".prefab";
            }

            if (match.Success)
            {
                // Check loose files
                Texture2D tex = null;
                var found = TextureReplacement.TryImportTextureFromLooseFiles(archive, record, frame, TextureMap.Albedo,
                    true, out tex);
                if (found)
                {
                    var type = tex.GetType();
                    if (typesArray.Contains(type))
                        return type;
                }
            }

            if (!string.IsNullOrEmpty(prefabName))
            {
                var query = from mod in EnumerateEnabledModsReverse()
#if UNITY_EDITOR
                            where (mod.AssetBundle != null && mod.AssetBundle.Contains(prefabName)) ||
                                  (mod.IsVirtual && mod.HasAsset(prefabName))
#else
                    where mod.AssetBundle != null && mod.AssetBundle.Contains(prefabName)
#endif
                            let asset = mod.LoadAsset<UnityEngine.Object>(prefabName)
                            where asset != null && typesArray.Contains(asset.GetType())
                            select new { Name = prefabName, Mod = mod, Asset = asset };

                var result = query.FirstOrDefault();
                if (result != null)
                {
                    return result.Asset.GetType();
                }
            }


            {
                var query = from mod in EnumerateEnabledModsReverse()
#if UNITY_EDITOR
                    where (mod.AssetBundle != null && mod.AssetBundle.Contains(name)) ||
                          (mod.IsVirtual && mod.HasAsset(name))
#else
                where mod.AssetBundle != null && mod.AssetBundle.Contains(name)
#endif
                    let asset = mod.LoadAsset<UnityEngine.Object>(name)
                    where asset != null && typesArray.Contains(asset.GetType())
                    select new { Name = name, Mod = mod, Asset = asset };

                var result = query.FirstOrDefault();
                if (result != null)
                {
                    return result.Asset.GetType();
                }
            }
        

            return null;
        }


        public static Texture2D GenerateModelTexture(GameObject prefab, int textureWidth = 256, int textureHeight = 256, bool isometric = false)
        {
            if (prefab == null)
                return null;
            // Create a temporary camera
            GameObject tempCameraObj = new GameObject("TempCamera");
            Camera tempCamera = tempCameraObj.AddComponent<Camera>();
            tempCamera.clearFlags = CameraClearFlags.SolidColor;
            tempCamera.backgroundColor = Color.clear;
            tempCamera.orthographic = true;

            // Create a RenderTexture
            RenderTexture renderTexture = new RenderTexture(textureWidth, textureHeight, 24);
            tempCamera.targetTexture = renderTexture;

            // Create a new layer for rendering
            int renderLayer = 31; // Use layer 31 (or any unused layer)
            tempCamera.cullingMask = 1 << renderLayer;

            // Instantiate the prefab model
            GameObject modelInstance = Instantiate(prefab);
            SetLayerRecursively(modelInstance, renderLayer);

            // Calculate the bounds of the model
            Bounds bounds = CalculateBounds(modelInstance);

            // Position the camera to fit the model
            tempCamera.transform.position = bounds.center - Vector3.back * (bounds.extents.z + 1);
            tempCamera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y);

            if (isometric)
            {
                // Calculate the isometric position for the camera
                Vector3 isometricPosition = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z) * 1.5f;
                // Position the camera to fit the model
                tempCamera.transform.position = isometricPosition;
                tempCamera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y);
            }
        

            // Rotate the camera to look at the object from an isometric angle
            tempCamera.transform.rotation = Quaternion.Euler(45, 45, 0);
            tempCamera.transform.LookAt(bounds.center);

            // Add a temporary light source
            GameObject tempLightObj = new GameObject("TempLight");
            Light tempLight = tempLightObj.AddComponent<Light>();
            tempLight.type = LightType.Directional;
            tempLight.color = Color.white;
            tempLight.intensity = 1.0f;
            tempLight.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Render the model
            tempCamera.Render();

            // Read the RenderTexture into a Texture2D
            RenderTexture.active = renderTexture;
            Texture2D resultTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
            resultTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
            resultTexture.Apply();

            // Clean up
            RenderTexture.active = null;
            tempCamera.targetTexture = null;
            DestroyImmediate(renderTexture);
            DestroyImmediate(tempCameraObj);
            DestroyImmediate(modelInstance);
            DestroyImmediate(tempLightObj);

            return resultTexture;
        }

        private static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;

            obj.layer = newLayer;

            foreach (Transform child in obj.transform)
            {
                if (child == null) continue;
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }


        private static Bounds CalculateBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);

            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }
    


    /// <summary>
    /// Seeks assets inside a directory from all mods with load order. An asset is accepted if its directory ends with the given subdirectory.
    /// For example "Assets/Textures" matches "Water.png" from "Assets/Game/Mods/Example/Assets/Textures/Water.png".
    /// </summary>
    /// <param name="relativeDirectory">A relative directory with forward slashes (i.e. "Assets/Textures").</param>
    /// <param name="extension">An extension including the dots (i.e ".json") or null.</param>
    /// <returns>A list of assets or null if there are no matches.</returns>
    public List<T> FindAssets<T>(string relativeDirectory, string extension = null) where T : UnityEngine.Object
        {
            if (relativeDirectory == null)
                throw new ArgumentNullException("relativeDirectory");

            List<string> names = null;
            List<T> assets = null;

            foreach (Mod mod in EnumerateModsReverse())
            {
                if (names != null)
                    names.Clear();

                if (mod.FindAssetNames(ref names, relativeDirectory, extension) != 0)
                {
                    for (int i = 0; i < names.Count; i++)
                    {
                        var asset = mod.GetAsset<T>(names[i]);
                        if (asset)
                        {
                            if (assets == null)
                                assets = new List<T>();
                            assets.Add(asset);
                        }
                    }
                }
            }

            return assets;
        }

        /// <summary>
        /// Converts an asset path to only the asset name, preserving the extension, converted to invariant lower case.
        /// The parameter <paramref name="assetPath"/> must be a Unity Editor asset path, using only the <c>"/"</c> separator, or an asset name.
        /// For example <c>"Assets/Texture.png"</c> to <c>"texture.png"</c>, <c>"Assets/Texture"</c> to <c>"texture"</c> and <c>"Texture"</c> to <c>"texture"</c>.
        /// </summary>
        /// <param name="assetPath">An asset path or asset name itself.</param>
        /// <returns>Asset name converted to lower case.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="assetPath"/> is null.</exception>
        /// <exception cref="FormatException"><paramref name="assetPath"/> is not a valid asset path.</exception>
        public static string GetAssetName(string assetPath)
        {
            if (assetPath == null)
                throw new ArgumentNullException(nameof(assetPath));

            int separatorIndex = assetPath.LastIndexOf('/');
            if (separatorIndex != -1)
            {
                int startIndex = separatorIndex + 1;
                if (startIndex == assetPath.Length)
                    throw new FormatException("Path ends with '/' separator.");

                assetPath = assetPath.Substring(startIndex);
            }

            if (string.IsNullOrWhiteSpace(assetPath))
                throw new FormatException("Asset name is empty.");

            return assetPath.ToLowerInvariant();
        }

        /// <summary>
        /// Utility method for gettings lines of text from a text asset
        /// </summary>
        public static List<string> GetTextAssetLines(TextAsset asset)
        {
            List<string> lines = new List<string>();
            string line;
            using (StringReader reader = new StringReader(asset.text))
            {
                while ((line = reader.ReadLine()) != null)
                    lines.Add(line);
            }
            return lines;
        }
        
        /// <summary>
        /// Goes through all mods and checks if any of them contain a quest with a given name.
        /// </summary>
        /// <param name="questName">Name of the quest</param>
        public bool AnyModContainsQuest(string questName)
        {
            return GetAllModsWithContributes(x => x.LooseQuestsList != null)
                .Any(mod => mod.ModInfo.Contributes.LooseQuestsList
                    .Any(looseQuest => looseQuest == questName));
        }

        #endregion

        #region Mod Loading & setup

        /// <summary>
        /// Look for changes in mod directory before the compiling / loading process has begun.
        /// </summary>
        public void Refresh()
        {
            if (!alreadyAtStartMenuState)
                FindModsFromDirectory(true);
        }

        /// <summary>
        /// Locates all the .dfmod files in the mod path
        /// </summary>
        /// <param name="refresh">Checks for mods to unload.</param>
        private void FindModsFromDirectory(bool refresh = false)
        {
            if (!Directory.Exists(ModDirectory))
            {
                Debug.Log("invalid mod directory: " + ModDirectory);
                return;
            }

            var modFiles = Directory.GetFiles(ModDirectory, "*" + MODEXTENSION, SearchOption.AllDirectories);
            var modFileNames = new string[modFiles.Length];
            var loadedModNames = GetAllModFileNames();

            for (int i = 0; i < modFiles.Length; i++)
            {
                string modFilePath = modFiles[i];

                string DirPath = modFilePath.Substring(0, modFilePath.LastIndexOf(Path.DirectorySeparatorChar));
                modFileNames[i] = GetModNameFromPath(modFilePath);

                if (string.IsNullOrEmpty(modFileNames[i]))
                {
                    Debug.Log($"failed to get name of mod for {modFilePath}");
                    continue;
                }

                //prevent trying to re-load same asset bundles on refresh
                if (loadedModNames.Length > 0)
                {
                    if (loadedModNames.Contains(modFileNames[i]))
                        continue;
                }

                AssetBundle ab;
                if (!LoadModAssetBundle(modFilePath, out ab))
                    continue;
                Mod mod = new Mod(modFileNames[i], DirPath, ab);

                mod.LoadPriority = i;
                int index = GetModIndex(mod.Title);
                if (index < 0)
                {
                    if(mod.ModInfo.ModPatch)
                    {
                        if (patchMods == null)
                            patchMods = new List<Mod>();
                        if (patchMods.All(x => x.ModInfo.GUID != mod.ModInfo.GUID))
                            patchMods.Add(mod);
                    }
                    else if (DaggerfallUnity.Settings.AllowModsWithSharedGuid && mods.Any(x => x.ModInfo.GUID == mod.ModInfo.GUID))
                    {
                        var oldMod = mods.First(x => x.ModInfo.GUID == mod.ModInfo.GUID);
                        Debug.LogErrorFormat(" mod {0} has same GUID as mod {1}, changing GUID of Mod {0}", mod.Title, oldMod.Title);
                        mod.ModInfo.GUID = Guid.NewGuid().ToString();
                    }
                    if (!mod.ModInfo.ModPatch)
                        mods.Add(mod);
                }
            }

            if (refresh)
            {
                for (int j = 0; j < loadedModNames.Length; j++)
                {
                    if (!modFileNames.Contains(loadedModNames[j]))
                    {
                        Debug.Log(string.Format("mod {0} no longer loaded", loadedModNames[j]));
                        UnloadMod(loadedModNames[j], true);
                    }
                }
            }



#if UNITY_EDITOR
            if (LoadVirtualMods)
            {
                foreach (string manifestPath in Directory.GetFiles(EditorModsDirectory, "*" + MODINFOEXTENSION, SearchOption.AllDirectories))
                {
                    ModInfo modInfo = null;
                    if (ModManager._serializer.TryDeserialize(fsJsonParser.Parse(File.ReadAllText(manifestPath)), ref modInfo).Failed)
                    {
                        Debug.LogErrorFormat("Failed to deserialize manifest file {0}", manifestPath);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(modInfo.ModTitle))
                    {
                        Debug.LogError($"Discarded {manifestPath} because it doesn't have a valid title.");
                        continue;
                    }

                    if (mods.Any(x => x.ModInfo.GUID == modInfo.GUID))
                    {
                        if (modInfo.ModPatch && patchMods.All(x => x.ModInfo.GUID != modInfo.GUID))
                        {
                            var patchMod = new Mod(manifestPath, modInfo);
                            if (patchMods == null)
                                patchMods = new List<Mod>();
                            patchMods.Add(patchMod);
                        }
                        else
                        {
                            int index = mods.FindIndex(x => x.ModInfo.GUID == modInfo.GUID);
                            Debug.LogError($"Ignoring virtual mod {modInfo.ModTitle} because release mod is already loaded at index {index}");
                        }
                        continue;
                    }
                    if (modInfo.ModPatch)
                    {
                        if (patchMods == null)
                            patchMods = new List<Mod>();
                        patchMods.Add(new Mod(manifestPath, modInfo));
                    }
                    else
                        mods.Add(new Mod(manifestPath, modInfo));
                }
            }
#endif
            foreach (var sourceMod in mods.Where(x => x.AssetBundle != null))
                sourceMod.LoadSourceCodeFromModBundle();
        }

        // Loads Asset bundle and adds to ModLookUp dictionary
        private static bool LoadModAssetBundle(string modFilePath, out AssetBundle ab)
        {
            ab = null;
            if (!File.Exists(modFilePath))
            {
                Debug.Log(string.Format("Asset Bundle not found: {0}", modFilePath));
                return false;
            }

            try
            {
                ab = AssetBundle.LoadFromFile(modFilePath);
                return ab != null;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                return false;
            }
        }

        // Unload mod and related asset bundle
        private bool UnloadMod(string modTitle, bool unloadAllAssets)
        {
            try
            {
                int index = GetModIndex(modTitle);
                if (index < 0)
                {
                    Debug.Log("Failed to unload mod as mod title wasn't found: " + modTitle);
                    return false;
                }

                Mod mod = mods[index];
                if (mod.AssetBundle)
                    mod.AssetBundle.Unload(unloadAllAssets);

                mods.RemoveAt(index);
                OnUnloadMod(modTitle);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                return false;
            }
        }

        //begin setting up mods
        private void Init()
        {
            if (alreadyStartedInit)
                return;

            alreadyStartedInit = true;
            WriteModSettings();

            Mod[] mods = GetAllMods();

            for (int i = 0; i < mods.Length; i++)
            {
                Mod mod = mods[i];

                if (mod == null || !mod.Enabled)
                {
                    Debug.Log("removing mod at index: " + i);
                    UnloadMod(mod.Title, true);
                    continue;
                }
                Debug.Log("ModManager - started loading mod: " + mod.Title);
                mod.CompileSourceToAssemblies();
                Debug.Log("ModManager - compiled Assemblies for " + mod.Title);
            }
            Debug.Log("ModManager - init finished.  Mod Count: " + LoadedModCount);
        }

        private void InvokeModLoaders(StateManager.StateTypes state)
        {
#if DEBUG
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
#endif
            if (alreadyAtStartMenuState)
            {
                Mod[] mods = GetAllMods();

                for (int i = 0; i < mods.Length; i++)
                {
                    var curMod = mods[i];
#if UNITY_EDITOR
                    if (patchMods.Any(x => x.ModInfo.GUID == mods[i].ModInfo.GUID && x.IsVirtual))
                    {
                        curMod = patchMods.First(x => x.ModInfo.GUID == mods[i].ModInfo.GUID);
                    }
#endif
                    List<SetupOptions> setupOptions = curMod.FindModLoaders(state);

                    if (setupOptions == null)
                    {
                        Debug.Log("No mod loaders found for mod: " + mods[i].Title);
                        continue;
                    }

                    for (int j = 0; j < setupOptions.Count; j++)
                    {
                        SetupOptions options = setupOptions[j];
                        MethodInfo mi = options.mi;
                        if (mi == null)
                            continue;
                        InitParams initParams = new InitParams(options.mod, ModManager.Instance.GetModIndex(options.mod.Title), LoadedModCount);

                        try
                        {
                            mi.Invoke(null, new object[] { initParams });
                        }
                        catch (TargetInvocationException e)
                        {
                            Debug.LogError($"Exception has been thrown by entry point \"{mi.Name}\" of mod \"{mods[i].Title}\":\n{e.InnerException}");
                        }
                    }
                }
#if DEBUG
                timer.Stop();
                Debug.Log("InvokeModLoaders() finished...time: " + timer.ElapsedMilliseconds);
#endif
            }
        }

        #endregion

        #region Mod Source Loading/Compiling

        /// <summary>
        /// Compiles source files in mod bundle to assembly.
        /// </summary>
        /// <param name="source">The content of source files.</param>
        /// <returns>The compiled assembly or null.</returns>
        public static Assembly CompileFromSourceAssets(string[] source, string modName = "(no mod name)")
        {
            if (source == null || source.Length < 1)
            {
                Debug.Log($"{modName} has nothing to compile");
                return null;
            }

            Assembly assembly;

            try
            {
                assembly = Compiler.CompileSource(source, true);
                return assembly;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{modName}] {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Public Helpers

        /// <summary>
        /// Writes mod settings (title, priority, enabled) to file.
        /// </summary>
        /// <returns>True if settings written successfully.</returns>
        public static bool WriteModSettings()
        {
            try
            {
                if (ModManager.Instance.mods == null || ModManager.Instance.mods.Count <= 0)
                {
                    return false;
                }

                fsData sdata = null;
                var result = _serializer.TrySerialize<List<Mod>>(ModManager.Instance.mods, out sdata);

                if (result.Failed)
                {
                    return false;
                }

                File.WriteAllText(Path.Combine(ModManager.Instance.ModDataDirectory, "Mods.json"), fsJsonPrinter.PrettyJson(sdata));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("Failed to write mod settings: {0}", ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Attempts to load saved mod settings from file and updates loaded mods.
        /// </summary>
        /// <returns>True if settings loaded successfully.</returns>
        public static bool LoadModSettings()
        {
           fsResult result = new fsResult();

            try
            {
                string oldFilepath = Path.Combine(ModManager.Instance.ModDirectory, MODCONFIGFILENAME);
                string filePath = Path.Combine(ModManager.Instance.ModDataDirectory, "Mods.json");

                Directory.CreateDirectory(ModManager.Instance.ModDataDirectory);

                if (File.Exists(oldFilepath))
                    MoveOldConfigFile(oldFilepath, filePath);

                if (!File.Exists(filePath))
                    return false;

                var serializedData = File.ReadAllText(filePath);
                if (string.IsNullOrEmpty(serializedData))
                    return false;

                List<Mod> temp = new List<Mod>();
                fsData data = fsJsonParser.Parse(serializedData);
                result = _serializer.TryDeserialize<List<Mod>>(data, ref temp);

                if (result.Failed || temp.Count <= 0)
                    return false;

                foreach (Mod _mod in temp)
                {
                    if (ModManager.Instance.GetModIndex(_mod.Title) >= 0)
                    {
                        Mod mod = ModManager.Instance.GetMod(_mod.Title);
                        if (mod == null)
                            continue;
                        mod.Enabled = _mod.Enabled;
                        mod.LoadPriority = _mod.LoadPriority;
                        ModManager.Instance.mods[ModManager.Instance.GetModIndex(_mod.Title)] = mod;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("Error trying to load mod settings: {0}", ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Helper function to assist with serializing and deserializing prefabs.
        /// </summary>
        /// <param name="trans">Parent transform.</param>
        /// <param name="transforms">A list already containing data or null to request a new list.</param>
        /// <returns>The list of transforms.</returns>
        public static List<Transform> GetAllChildren(Transform trans, ref List<Transform> transforms)
        {
            if (transforms == null)
                transforms = new List<Transform>() { trans };
            else
                transforms.Add(trans);

            for (int i = 0; i < trans.childCount; i++)
            {
                GetAllChildren(trans.GetChild(i), ref transforms);
            }

            return transforms;
        }

        /// <summary>
        /// Send data to a mod that has a valid <see cref="DFModMessageReceiver"/> delegate.
        /// </summary>
        /// <param name="modTitle">The title of the target mod.</param>
        /// <param name="message">A string to be sent to the target mod.</param>
        /// <param name="data">Data to send with the message.</param>
        /// <param name="callback">An optional message callback.</param>
        public void SendModMessage(string modTitle, string message, object data = null, DFModMessageCallback callback = null)
        {
            if (mods == null || mods.Count < 1)
                return;
            var mod = GetMod(modTitle);
            if (mod == null || mod.MessageReceiver == null)
                return;
            else
                mod.MessageReceiver(message, data, callback);
        }

        /// <summary>
        /// Combines an array of strings into a path.
        /// This is a substitute of <see cref="Path.Combine(string[])"/>, which was not available with previously used .NET version.
        /// </summary>
        /// <param name="paths">An array of parts of the path.</param>
        /// <returns>The combined paths.</returns>
        [Obsolete("Use string Path.Combine(params string[] paths")]
        public static string CombinePaths(params string[] paths)
        {
            return Path.Combine(paths);
        }

        /// <summary>
        /// Determines the difference between two paths.
        /// </summary>
        /// <param name="path0">The main path that contains <paramref name="path1"/>.</param>
        /// <param name="path1">A path to a subdirectory or file inside <paramref name="path0"/>.</param>
        /// <returns><paramref name="path1"/> converted to a relative path of <paramref name="path0"/> or null.</returns>
        public static string MakeRelativePath(string path0, string path1)
        {
            var uri0 = new Uri(path0);
            var uri1 = new Uri(path1);
            if (uri0.IsBaseOf(uri1))
                return uri0.MakeRelativeUri(uri1).ToString();

            return null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Seeks asset contributes for the target mod, reading the folder name of each asset.
        /// </summary>
        /// <param name="modInfo">Manifest data for a mod, which will be filled with retrieved contributes.</param>
        /// <param name="automaticallyRegisterQuestLists">Optional parameter that triggers Quest Lists declaration</param>
        /// <remarks>
        /// Assets are imported from loose files according to folder name,
        /// for example all textures inside `SpellIcons` are considered icon atlases.
        /// This method replicates the same behaviour for mods, doing all the hard work at build time.
        /// Results are stored to json manifest file for performant queries at runtime.
        /// </remarks>
        public static void SeekModContributes(ModInfo modInfo, bool automaticallyRegisterQuestLists = false)
        {
            // Reset contributions before rebuilding it
            modInfo.Contributes = null;

            List<string> spellIcons = null;
            List<string> booksMapping = null;
            List<string> questLists = null;
            List<string> looseQuestsList = null;

            foreach (var file in modInfo.Files)
            {
                var directory = Path.GetDirectoryName(file);

                if (!string.IsNullOrEmpty(directory) && directory.EndsWith("SpellIcons"))
                {
                    AddNameToList(ref spellIcons, file);
                    continue;
                }

                if (!string.IsNullOrEmpty(directory) && directory.EndsWith("Mapping"))
                {
                    var parentDirectory = Path.GetDirectoryName(directory);
                    if (!string.IsNullOrEmpty(parentDirectory) && parentDirectory.EndsWith("Books"))
                    {
                        AddNameToList(ref booksMapping, file);
                        continue;
                    }
                }

                if (automaticallyRegisterQuestLists)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    if (string.IsNullOrEmpty(name) || !name.StartsWith("QuestList-"))
                    {
                        continue;
                    }

                    AddNameToList(ref questLists, name.Substring(10));

                    var questListPath = Path.GetDirectoryName(file);

                    foreach (var looseQuest in modInfo.Files.Where(f => Path.GetDirectoryName(f) == questListPath && f != file))
                    {
                        AddNameToList(ref looseQuestsList, looseQuest);
                    }
                }
            }

            if (spellIcons == null && booksMapping == null && questLists == null && looseQuestsList == null)
                return;

            modInfo.Contributes = new ModContributes
            {
                SpellIcons = spellIcons?.ToArray(),
                BooksMapping = booksMapping?.ToArray(),
                QuestLists = questLists?.ToArray(),
                LooseQuestsList = looseQuestsList?.ToArray()
            };
        }

        private static void AddNameToList(ref List<string> names, string path)
        {
            if (names == null)
                names = new List<string>();

            string name = Path.GetFileNameWithoutExtension(path);
            if (!names.Contains(name))
                names.Add(name);
        }

        /// <summary>
        /// Import asset in editor from full path. This is an helper over <see cref="AssetDatabase.ImportAsset(string, ImportAssetOptions)"/> which takes a relative path.
        /// </summary>
        /// <param name="path">Full path to asset.</param>
        /// <param name="importAssetOptions">Import asset options.</param>
        public static void ImportAsset(string path, ImportAssetOptions importAssetOptions = ImportAssetOptions.Default)
        {
            string relPath = MakeRelativePath(Application.dataPath, path);
            if (relPath != null)
                AssetDatabase.ImportAsset(relPath, importAssetOptions);
        }
#endif

        #endregion

        #region Internal methods

        /// <summary>
        /// Sorts mods by load order.
        /// </summary>
        internal void SortMods()
        {
            mods.Sort((x, y) => x.LoadPriority - y.LoadPriority);
        }

        /// <summary>
        /// Automatically assigns load priority from relationships defined by <see cref="ModInfo.Dependencies"/>.
        /// </summary>
        public int GetLoadPriority(string filename)
        {
            for(int i = 0; i < mods.Count; i++)
                if (mods[i].FileName == filename)
                   // return mods[i].LoadPriority;
                    return i;
            return 0;
        }
        internal void AutoSortMods()
        {
            SuccessfulSort = false;
            ErrorsEncountered = 0;
            while(!SuccessfulSort && ErrorsEncountered < 25)
            {
                SuccessfulSort = true;
                for (int n = modMoveOrder.Count - 1; n >= 0; n--)
                {
                    var key = GetLoadPriority(modMoveOrder[n]);

                    var value = modMove[modMoveOrder[n]];
                    {
                        int newPos = key;
                        foreach (var v in value)
                        {

                            var a = newPos;
                            var b = GetLoadPriority(v);
                            if (a < b)
                            {
                                Debug.Log(
                                    $"Sending {Instance.mods[a].FileName}:{Instance.mods[a].LoadPriority} below {Instance.mods[b].FileName}:{Instance.mods[b].LoadPriority}");
                                for (int i = a; i < b; i++)
                                {
                                    if (i + 1 >= Instance.mods.Count)
                                        break;

                                    var m1 = Instance.mods[i];
                                    var m2 = Instance.mods[i + 1];
                                    m1.LoadPriority += 1;
                                    newPos = m1.LoadPriority;
                                    m2.LoadPriority -= 1;
                                    Instance.mods[i] = m2;
                                    Instance.mods[i + 1] = m1;
                                }
                            }
                        }
                    }
                }

                try
                {
                   preSortedMods = mods;
                    mods = TopologicalSort(mods, mod =>
                    {
                        if (mod.ModInfo.Dependencies == null)
                            return Enumerable.Empty<Mod>();

                        var query = from dependency in mod.ModInfo.Dependencies
                            where !dependency.IsPeer
                            select GetModFromName(dependency.Name);

                        return query.Where(x => x != null);
                    });
                    if (cyclicError)
                    {
                        mods = preSortedMods;
                        ErrorsEncountered++;
                        continue;
                    }

                    for (int i = 0; i < mods.Count; i++)
                        mods[i].LoadPriority = i;


                }
                catch (Exception e)
                {
                    SuccessfulSort = false;
                    mods = preSortedMods;
                    if (!e.ToString().ToLower().Contains("cyclic"))
                        Debug.LogErrorFormat("Failed to auto sort mods: Error Cnt: {0} {1}", ErrorsEncountered, e.ToString());
                }
                ErrorsEncountered++;
            }
        }

        /// <summary>
        /// Checks if all conditions defined in Dependency section of a mod are satisfied.
        /// </summary>
        /// <param name="mod">A mod that should be validated.</param>
        /// <returns>A readable error message or null.</returns>
        internal void CheckModDependencies(Mod mod, List<string> errorMessages, ref bool hasSortIssues)
        {
            if (mod.ModInfo.Dependencies != null)
            {
                foreach (ModDependency dependency in mod.ModInfo.Dependencies)
                {
                    // Check if dependency is available
                    Mod target = GetModFromName(dependency.Name);
                    if (target == null)
                    {
                        if (dependency.IsOptional)
                            continue;

                        errorMessages.Add($"{mod.FileName} needs {dependency.Name} because optional = {dependency.IsOptional.ToString()}");

                        if (DaggerfallUnity.Settings.BinarySearch > 0)
                            continue;

                        if (ModLoaderInterfaceWindow.DisableModWhenDependenciesNotAvailable)
                        {
                            mod.Enabled = false;
                            errorMessages.Add($"{mod.FileName} was disabled.");

                        }
                        continue;
                    }

                    if (!target.Enabled && !dependency.IsOptional)
                    {
                        var newError = string.Format(GetText("dependencyNotEnabled"), dependency.Name);
                        if (!errorMessages.Contains(newError))
                            errorMessages.Add(newError);

                        if (DaggerfallUnity.Settings.BinarySearch > 0)
                            continue;

                        if (ModLoaderInterfaceWindow.DisableModWhenDependenciesNotAvailable)
                        {
                            mod.Enabled = false;
                            errorMessages.Add($"{mod.FileName} was disabled.");

                        }
                        continue;
                    }

                    // Check load order priority
                    if (target.Enabled && !dependency.IsPeer && mod.LoadPriority < target.LoadPriority)
                    {
                        var newError = string.Format(GetText("dependencyWithIncorrectPosition"), target.Title);
                        if (!errorMessages.Contains(newError))
                            errorMessages.Add(newError);

                        if (modMove.ContainsKey(mod.FileName))
                        {
                            if (!modMove[mod.FileName].Contains(target.FileName))
                                modMove[mod.FileName].Add(target.FileName);
                        }
                        else
                        {
                            var nt = new List<string> { target.FileName };
                            modMove.Add(mod.FileName, nt);
                            modMoveOrder.Add(mod.FileName);
                        }
                        hasSortIssues = true;
                    }

                    // Check minimum version (ignore pre-release identifiers after hyphen).
                    if (target.Enabled && dependency.Version != null && dependency.Version.Trim().Length > 0)
                    {
                        if (target.ModInfo.ModVersion == null)
                        {
                            errorMessages.Add(string.Format(GetText("dependencyWithIncompatibleVersion"), target.Title, "<undefined>", dependency.Version));
                        }
                        else
                        {
                            int index = target.ModInfo.ModVersion.IndexOf('-');
                            string referenceVersion = index != -1 ? target.ModInfo.ModVersion.Remove(index) : target.ModInfo.ModVersion;
                            if (IsVersionLowerOrEqual(dependency.Version, referenceVersion) != true)
                                errorMessages.Add(string.Format(GetText("dependencyWithIncompatibleVersion"), target.Title, target.ModInfo.ModVersion, dependency.Version));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates all enabled mods with reverse load order, for internal use only. Mods are always launched
        /// at a later time (after disabled mods are unloaded), so they can use <see cref="EnumerateModsReverse()"/> instead.
        /// </summary>
        /// <returns>An enumeration of enabled mods sorted by reverse load order.</returns>
        internal IEnumerable<Mod> EnumerateEnabledModsReverse()
        {
            IEnumerable<Mod> query = EnumerateModsReverse();
            if (alreadyAtStartMenuState)
                return query;

            return query.Where(x => x.Enabled);
        }

        internal Mod GetModFromName(string name)
        {
            return mods.FirstOrDefault(x => x.FileName.Equals(name, StringComparison.Ordinal));
        }

        internal void PruneCache(float time, float threshold)
        {
            foreach (Mod mod in mods)
                mod.PruneCache(time, threshold);
        }

        /// <summary>
        /// Gets a localized string for a mod system text.
        /// </summary>
        internal static string GetText(string key)
        {
            return TextManager.Instance.GetText("ModSystem", key);
        }

        /// <summary>
        /// An helper for moving mod config data from StreamingAssets to PersistentDataPath.
        /// </summary>
        internal static void MoveOldConfigFile(string sourceFileName, string destFileName)
        {
            try
            {
                if (File.Exists(destFileName))
                    File.Delete(destFileName);

                File.Move(sourceFileName, destFileName);
                Debug.LogFormat("Moved {0} to {1}.", sourceFileName, destFileName);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Checks if a given version is lower or equal to another version.
        /// For example <c>"9.0.8"</c> is lower than <c>"10.0.2"</c>.
        /// </summary>
        /// <param name="first">A version with format x.y.z, x.y or just x that is expected to be lower or equal.</param>
        /// <param name="second">A version with format x.y.z, x.y or just x that is expected to be higher or equal.</param>
        /// <returns>true if first is lower or equal to second, false is first is higher than second, null if parse failed.</returns>
        internal static bool? IsVersionLowerOrEqual(string first, string second)
        {
            if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second))
                return null;

            string[] firstParts = first.Split('.');
            if (firstParts.Length < 1 || firstParts.Length > 3)
                return null;

            string[] secondParts = second.Split('.');
            if (secondParts.Length < 1 || secondParts.Length > 3)
                return null;

            for (int i = 0; i < firstParts.Length || i < secondParts.Length; i++)
            {
                int firstPart = 0;
                int secondPart = 0;

                if ((i < firstParts.Length && !int.TryParse(firstParts[i], out firstPart)) ||
                    (i < secondParts.Length && !int.TryParse(secondParts[i], out secondPart)))
                    return null;

                if (firstPart > secondPart)
                    return false;

                if (firstPart < secondPart)
                    break;
            }

            return true;
        }

        #endregion

        #region Private Methods

        private static string GetModNameFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            return path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1).Replace(MODEXTENSION, "");
        }

        private void SetupSingleton()
        {
            if (Instance == null)
            {
                DontDestroyOnLoad(this.gameObject);
                Instance = this;
            }
            else if (Instance != this)
            {
                if (Application.isPlaying)
                {
                    DaggerfallUnity.LogMessage("Multiple ModManager instances detected in scene!", true);
                    Destroy(this);
                }
            }
        }

        // Adapted from https://stackoverflow.com/questions/4106862/how-to-sort-depended-objects-by-dependency
        private static List<T> TopologicalSort<T>(IEnumerable<T> source, Func<T, IEnumerable<T>> dependencies)
        {
            var sorted = new List<T>();
            var visited = new HashSet<T>();
            var beingVisited = new HashSet<T>(); // New set to track nodes currently being visited
            cyclicError = false;
            foreach (var item in source)
            {
                VisitCnt = 0;
                Visit(item, visited, beingVisited, sorted, dependencies);
            }

            return sorted;
        }

        private static void Visit<T>(T item, HashSet<T> visited, HashSet<T> beingVisited, List<T> sorted, Func<T, IEnumerable<T>> dependencies)
        {
            if (cyclicError || VisitCnt > 100)
                return;

            if (!visited.Contains(item))
            {
                beingVisited.Add(item); // Mark the node as being visited

                foreach (var dependency in dependencies(item))
                {
                    if (beingVisited.Contains(dependency))
                    {
                        SuccessfulSort = false;
                        if (VisitCnt > 10)
                        {
                            throw new Exception(
                                $"Error Cnt {ErrorsEncountered} Cyclic dependency found between item {(item as Mod)?.ModInfo.ModTitle} and dependency {(dependency as Mod)?.ModInfo.ModTitle}");
                        }
                        cyclicError = true;
                        return;
                    }

                    VisitCnt++;
                    Visit(dependency, visited, beingVisited, sorted, dependencies);
                }

                visited.Add(item); // Mark the node as visited
                sorted.Add(item);
                beingVisited.Remove(item); // Remove the node from the beingVisited set
            }
        }

        #endregion
        #region Bisect
        public static void RunBisect(IUserInterfaceManager uiManager, bool startOfFirstBisect = false)
        {
            if (startOfFirstBisect)
            {
                ReadAlwaysIncludeModList();
                //create bisect.txt file
                //in mod order list modname, tab, X for active and O for inactive
                var activeModList = ModManager.Instance.mods.Where(x => x.Enabled).ToList();
                if (activeModList.Count < 2)
                {
                    DaggerfallUnity.Settings.BinarySearch = 0;
                    DaggerfallUnity.Settings.SaveSettings();
                    DaggerfallUI.MessageBox("There are not enough active mods to run binary search.  Cancelled.");
                    return;
                }

                DaggerfallUnity.Settings.BinarySearch = 1;
                DaggerfallUnity.Settings.SaveSettings();
                
                var str = string.Empty;
                var halfWay = 0;
                for (int i = 0; i < activeModList.Count; i++)
                {
                    halfWay = UnityEngine.Mathf.RoundToInt(activeModList.Count / 2f);
                    if (i < halfWay || _alwaysIncludeModList.Contains(activeModList[i].FileName.ToLower()))
                    {
                        str += $"{activeModList[i].FileName}\tX\n";
                        var mod = ModLoaderInterfaceWindow.GetModFromName(activeModList[i].FileName);
                        if (mod != null)
                            mod.Enabled = true;
                    }
                    else
                    {
                        str += $"{activeModList[i].FileName}\tO\n";
                        var mod = ModLoaderInterfaceWindow.GetModFromName(activeModList[i].FileName);
                        if (mod != null)
                            mod.Enabled = false;
                    }
                }
                File.WriteAllText(Application.persistentDataPath + @"/bisect.txt", str);
                File.WriteAllText(Application.persistentDataPath + @"/bisectOriginal.txt", str);
                File.WriteAllText(Application.persistentDataPath + @"/bisectLastFail.txt", str);
                str = $"{activeModList.First().FileName}\n";
                str += $"{activeModList.Last().FileName}\n";
                File.WriteAllText(Application.persistentDataPath + @"/bisectRange.txt", str);
                var estSteps = Mathf.Clamp(
                    UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Log(activeModList.Count) / UnityEngine.Mathf.Log(2)),
                    1, activeModList.Count);
                DaggerfallUI.MessageBox(
                    $"Mods have been set for first Binary search test, There will be about \r{estSteps} tests.  Press Play to begin.");
            }
            else
            {
                ReadActiveModList();
                if (fileActiveModList.Count <= 1)
                {
                    ProcessLastEntry(uiManager);
                    ResetBisect();
                    return;
                }
                var messageBox = new DaggerfallMessageBox(uiManager);
                messageBox.EnableVerticalScrolling(80);
                messageBox.SetText("Was the last test successful?");
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Cancel);
                messageBox.OnButtonClick += (box, button) =>
                {

                    bool allSameIndicator = fileActiveModList.All(x => x.EnableIndicator == fileActiveModList[0].EnableIndicator);

                    if (button == DaggerfallMessageBox.MessageBoxButtons.Cancel)
                    {
                        messageBox.CloseWindow();
                        ResetBisect();
                        return;
                    }
                    if (button == DaggerfallMessageBox.MessageBoxButtons.Yes)
                    {
                        messageBox.CloseWindow();
                        if (!allSameIndicator)
                        {
                            BisectSuccessXO();
                        }
                        else
                        {
                            BisectSuccessX();
                        }

                    }
                    else
                    {
                        messageBox.CloseWindow();
                        File.Copy(Application.persistentDataPath + @"/bisect.txt",
                            Application.persistentDataPath + @"/bisectLastFail.txt", overwrite:true);
                        if (!allSameIndicator)
                        {
                            BisectFailXO();
                        }
                        else
                        {
                            BisectFailX();
                        }
                    }
                };
                messageBox.Show();

            }
        }

        private static void ReadAlwaysIncludeModList()
        {
            string filePath = Application.persistentDataPath + @"/bisectAlwaysInclude.txt";

            if (File.Exists(filePath))
            {
                _alwaysIncludeModList = File.ReadAllLines(filePath)
                    .Select(line => line.Trim().ToLower())
                    .Where(line => !line.StartsWith("#"))
                    .ToList();
            }
            else
            {
                _alwaysIncludeModList = new List<string>();
            }
        }

        private static void ProcessLastEntry(IUserInterfaceManager uiManager)
        {
            var lastFileName = fileActiveModList[0].Filename;
            var messageBox = new DaggerfallMessageBox(uiManager);
            messageBox.EnableVerticalScrolling(80);
            messageBox.SetText("Was the last test successful?");
            messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
            messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
            messageBox.OnButtonClick += (box, button) =>
            {
                if (button == DaggerfallMessageBox.MessageBoxButtons.Yes)
                {
                    messageBox.CloseWindow();
                    DaggerfallUI.MessageBox(
                        "Bisect didn't find the culprit.\rThe error must be caused by a combination of mods."
                        + "\rCheck bisectLastFail.txt for a short list of mods that combined for the error.");
                }
                else
                {
                    messageBox.CloseWindow();
                    DaggerfallUI.MessageBox($"The culprit is {lastFileName}");
                }
            };
            messageBox.Show();
        }

        private static void BisectSuccessXO()
        {
            int firstIndex = fileActiveModList.FindIndex(x => x.EnableIndicator == "O");
            int lastIndex = fileActiveModList.FindLastIndex(x => x.EnableIndicator == "O");
            int halfway = (lastIndex + firstIndex) / 2;
            for (int i = 0; i < fileActiveModList.Count; i++)
            {
                var activeModListComponents = fileActiveModList[i];

                activeModListComponents.EnableIndicator = (i >= firstIndex && i <= halfway) || _alwaysIncludeModList.Contains(activeModListComponents.Filename.ToLower()) ? "X" : "O";
                fileActiveModList[i] = activeModListComponents;
                var mod = ModLoaderInterfaceWindow.GetModFromName(fileActiveModList[i].Filename);
                if (mod != null)
                    mod.Enabled = activeModListComponents.EnableIndicator == "X";
            }
            CreateBisectFile(firstIndex, lastIndex);

        }

        private static void CreateBisectFile(int firstIndex, int lastIndex)
        {
            string sep = "\t";

            string str = string.Empty;
            
           // for(int i = firstIndex; i <= lastIndex; i++)
           for (int i = 0; i < fileActiveModList.Count; i++)
            {
                if ((i >= firstIndex && i <= lastIndex) || _alwaysIncludeModList.Contains(fileActiveModList[i].Filename.ToLower()))
                    str += $"{fileActiveModList[i].Filename}\t{fileActiveModList[i].EnableIndicator}\n";
            }

            File.WriteAllText(Application.persistentDataPath + @"/bisect.txt", str);

            str = string.Empty;
            
            str = $"{fileActiveModList[firstIndex].Filename}\n";
            str += $"{fileActiveModList[lastIndex].Filename}\n";
            File.WriteAllText(Application.persistentDataPath + @"/bisectRange.txt", str);


            var estSteps = Mathf.Clamp(
                UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Log(lastIndex - firstIndex + 1) / UnityEngine.Mathf.Log(2)),
                1, lastIndex - firstIndex + 1);

            DaggerfallUI.MessageBox(
                $"Mods have been set for the next Binary search test, There will be about \r{estSteps} tests.  Press Play to begin.");
        }

        private static void BisectSuccessX()
        {
            DaggerfallUI.MessageBox(
                "Bisect didn't find the culprit.\rThe error must be caused by a combination of mods.");
            ResetBisect();
        }

        private static void ResetBisect()
        {
            ReadActiveModList(true);
            foreach (var mod in ModManager.Instance.mods)
            {
                var result = fileActiveModList.FirstOrDefault(x => x.Filename == mod.FileName);
                if (result.Filename != null && result.Filename == mod.FileName)
                    mod.Enabled = true;
                else
                    mod.Enabled = false;
            }

            DaggerfallUnity.Settings.BinarySearch = 0;
            DaggerfallUnity.Settings.SaveSettings();
            DaggerfallUnitySetupGameWizard.bisectLabel.Enabled = false;
        }

        private static void BisectFailXO()
        {
            int firstIndex = fileActiveModList.FindIndex(x => x.EnableIndicator == "X");
            int lastIndex = fileActiveModList.FindLastIndex(x => x.EnableIndicator == "X");
            int halfway = (lastIndex + firstIndex) / 2;

            for (int i = 0; i < fileActiveModList.Count; i++)
            {
                var activeModListComponents = fileActiveModList[i];
                activeModListComponents.EnableIndicator = (i >= firstIndex && i <= halfway) || _alwaysIncludeModList.Contains(activeModListComponents.Filename.ToLower()) ? "X" : "O";
                fileActiveModList[i] = activeModListComponents;
                var mod = ModLoaderInterfaceWindow.GetModFromName(fileActiveModList[i].Filename);
                if (mod != null)
                    mod.Enabled = activeModListComponents.EnableIndicator == "X";

            }
            CreateBisectFile(firstIndex, lastIndex);
        }
        
        private static void BisectFailX()
        {
            int firstIndex = 0;
            int lastIndex = fileActiveModList.Count - 1;
            int halfWay = lastIndex / 2;

            for (int i = 0; i < fileActiveModList.Count; i++)
            {
                var activeModListComponents = fileActiveModList[i];
                activeModListComponents.EnableIndicator = (i >= firstIndex && i <= halfWay) || _alwaysIncludeModList.Contains(activeModListComponents.Filename.ToLower()) ? "X" : "O";
                fileActiveModList[i] = activeModListComponents;
                var mod = ModLoaderInterfaceWindow.GetModFromName(fileActiveModList[i].Filename);
                if (mod != null)
                    mod.Enabled = activeModListComponents.EnableIndicator == "X";

            }
            CreateBisectFile(firstIndex, lastIndex);
        }

        private static void ReadActiveModList(bool reset = false)
        {
            fileActiveModList.Clear();
            fileBisectRange.Clear();
            string sep = "\t";

            string filename;

            if (reset)
                filename = Application.persistentDataPath + @"/bisectOriginal.txt";
            else
                filename = Application.persistentDataPath + @"/bisect.txt";
            
            foreach (string line in System.IO.File.ReadLines(filename))
            {
                var fields = line.Split(sep.ToCharArray());
                var modName = fields[0];
                var enabledIndicator = fields[1];
                fileActiveModList.Add(new ActiveModListComponents(modName, enabledIndicator));
            }

            filename = Application.persistentDataPath + @"/bisectRange.txt";
            foreach (string line in System.IO.File.ReadLines(filename))
            {
                var fields = line.Split(sep.ToCharArray());
                var modName = fields[0];
                
                fileBisectRange.Add(modName);
            }

            return;
        }


        #endregion
        #region Events

        //public delegate void NewObjectCreatedHandler(object obj, SetupOptions options);
        //public static event NewObjectCreatedHandler OnNewObjectCreated;

        //public delegate void NewModRegistered(IModController newModController);
        //public static event NewModRegistered OnNewModControllerRegistered;

        /// <summary>
        /// Signature for an event related to an asset from a mod.
        /// </summary>
        /// <param name="ModTitle">The title of a mod.</param>
        /// <param name="AssetName">The name of the asset without relative path, as a lowercase text.</param>
        /// <param name="assetType">The Type of target asset.</param>
        public delegate void AssetUpdate(string ModTitle, string AssetName, Type assetType);

        /// <summary>
        /// An event that is raised when an asset is loaded from an <see cref="AssetBundle"/> and is cached.
        /// It's not raised again if the asset is cloned and instantiated multiple times.
        /// </summary>
        public static event AssetUpdate OnLoadAssetEvent;

        /// <summary>
        /// Signature for an event related to a mod.
        /// </summary>
        /// <param name="ModTitle">The title of target mod.</param>
        public delegate void ModUpdate(string ModTitle);

        /// <summary>
        /// An event that is raised when a mod is removed and its AssetBundle is unloaded.
        /// </summary>
        public static event ModUpdate OnUnloadModEvent;

        private void OnUnloadMod(string ModTitle)
        {
            if (OnUnloadModEvent != null)
                OnUnloadModEvent(ModTitle);
        }

        public bool LoadSourceCodeFromModBundle(string guid, out List<Source> sources)
        {
            try
            {
                sources = new List<Source>();

                if (patchMods.All(x => x.ModInfo.GUID != guid))
                {
                    sources = null;
                    return false;
                }


                var query = from mod in patchMods
                    where mod.ModInfo.GUID == guid
                            select mod;

                var sourceMod = query.FirstOrDefault(x => x != null);
#if UNITY_EDITOR
                if (sourceMod == null || sourceMod.IsVirtual)
#endif
#if !UNITY_EDITOR
                if (sourceMod == null)
#endif
                {
                    sources = null;
                    return false;
                }

                var list = sourceMod.AssetBundle.GetAllAssetNames();
                foreach (string assetName in sourceMod.AssetBundle.GetAllAssetNames())
                {
                    bool isSource = false;
                    bool isPrecompiled = false;

                    if (assetName.EndsWith(".cs.txt", StringComparison.Ordinal))
                    {
                        isSource = true;
                        isPrecompiled = false;
                    }
                    else if (assetName.EndsWith(".dll.bytes", StringComparison.Ordinal))
                    {
                        isSource = true;
                        isPrecompiled = true;
                    }

                    if (isSource)
                    {
                        var newSource = sourceMod.GetAsset<TextAsset>(assetName);
                        if (newSource)
                        {
                            sources.Add(new Source()
                            {
                                sourceTxt = newSource,
                                isPreCompiled = isPrecompiled
                            });
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                sources = null;
                return false;
            }
        }
        internal static void OnLoadAsset(string ModTitle, string assetName, Type assetType)
        {
            if (OnLoadAssetEvent != null)
                OnLoadAssetEvent(ModTitle, assetName, assetType);
        }

        public void StateManager_OnStateChange(StateManager.StateTypes state)
        {
            if (state == StateManager.StateTypes.Start)
            {
                alreadyAtStartMenuState = true;
                Init();
                InvokeModLoaders(state);
            }
            else if (state == StateManager.StateTypes.Game)
            {
                alreadyAtStartMenuState = true;
                InvokeModLoaders(state);
                StateManager.OnStateChange -= StateManager_OnStateChange;
            }
        }

#endregion
    }
}
