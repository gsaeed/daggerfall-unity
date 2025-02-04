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
using UnityEngine;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Utility.AssetInjection;
using Debug = UnityEngine.Debug;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    /// <summary>
    /// Implements a VID player.
    /// </summary>
    public class DaggerfallVidPlayerWindow : DaggerfallBaseWindow
    {

        DaggerfallVideo video;
        VideoPlayerDrawer customVideo;
        bool useCustomVideo = false;

        bool hideCursor = true;
        bool endOnAnyKey = true;

        public string PlayOnStart { get; set; }

        public bool HideCursor
        {
            get { return hideCursor; }
            set { hideCursor = value; }
        }

        public DaggerfallVideo Video
        {
            get { return video; }
        }

        public VideoPlayerDrawer CustomVideo
        {
            get { return customVideo; }
        }

        public bool UseCustomVideo
        {
            get { return useCustomVideo; }
        }

        public bool IsPlaying
        {
            get { return useCustomVideo ? (customVideo != null && customVideo.IsPlaying) : (video != null && video.Playing); }
        }

        public bool EndOnAnyKey
        {
            get { return endOnAnyKey; }
            set { endOnAnyKey = value; }
        }

        public DaggerfallVidPlayerWindow(IUserInterfaceManager uiManager)
            : base(uiManager)
        {
        }

        public DaggerfallVidPlayerWindow(IUserInterfaceManager uiManager, string playOnStart)
            : base(uiManager)
        {
            PlayOnStart = playOnStart;
        }

        public DaggerfallVidPlayerWindow(IUserInterfaceManager uiManager, string playOnStart, bool endOnAnyKey)
            : base(uiManager)
        {
            PlayOnStart = playOnStart;
            EndOnAnyKey = endOnAnyKey;
        }

        protected override void Setup()
        {
            if (useCustomVideo = VideoReplacement.TryImportMovie(PlayOnStart, out customVideo))
            {
                // Play custom video
                customVideo.Size = new Vector2(Screen.width, Screen.height);
                NativePanel.Components.Add(customVideo);
                DoHideCursor(hideCursor);

                customVideo.Play();
                RaiseOnVideoStartGlobalEvent();
            }
            else
            {
                const int nativeScreenWidth = 320;
                const int nativeScreenHeight = 200;

                // Add video player control
                video = new DaggerfallVideo();
                video.HorizontalAlignment = HorizontalAlignment.Center;
                video.Size = new Vector2(nativeScreenWidth, nativeScreenHeight);
                NativePanel.Components.Add(video);
                DoHideCursor(hideCursor);

                // Start playing
                if (!string.IsNullOrEmpty(PlayOnStart))
                {
                    video.Open(PlayOnStart);
                    video.Playing = true;
                    RaiseOnVideoStartGlobalEvent();
                }
            }
        }

        public override void Update()
        {
            base.Update();

            // Handle exit any key or end of video
            if (useCustomVideo)
            {
                if (endOnAnyKey && InputManager.Instance.AnyKeyDownIgnoreAxisBinds ||
                    InputManager.Instance.GetBackButtonDown() ||
                    !customVideo.IsPlaying)
                {
                    customVideo.Stop();
                    customVideo.Dispose();
                    customVideo = null;

                    DoHideCursor(false);
                    RaiseOnVideoFinishedHandler();
                    RaiseOnVideoEndGlobalEvent();
                    CloseWindow();
                }
            }
            else
            {
                if (endOnAnyKey && InputManager.Instance.AnyKeyDownIgnoreAxisBinds ||
                    InputManager.Instance.GetBackButtonDown() ||
                    video.VidFile.EndOfFile && video.Playing)
                {
                    video.Playing = false;
                    video.Dispose();
                    video = null;

                    DoHideCursor(false);
                    RaiseOnVideoFinishedHandler();
                    RaiseOnVideoEndGlobalEvent();
                    CloseWindow();
                }
            }
        }

        private void DoHideCursor(bool hide)
        {
            PlayerMouseLook mLook = GameManager.Instance.PlayerMouseLook;
            if (mLook != null)
                mLook.ForceHideCursor(hide && hideCursor);
        }

        #region Event Handlers

        // OnVideoStart (global)
        public delegate void OnVideoStartEventHandler();
        public static event OnVideoStartEventHandler OnVideoStart;
        protected virtual void RaiseOnVideoStartGlobalEvent()
        {
            if (OnVideoStart != null)
                try
                {
                    OnVideoStart();
                }
                catch (Exception e)
                {
                    var del = OnVideoStart;
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

        // OnVideoEnd (global)
        public delegate void OnVideoEndEventHandler();
        public static event OnVideoEndEventHandler OnVideoEnd;
        protected virtual void RaiseOnVideoEndGlobalEvent()
        {
            if (OnVideoEnd != null)
                try
                {
                    OnVideoEnd();
                }
                catch (Exception e)
                {
                    var del = OnVideoEnd;
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

        // OnVideoFinished
        public delegate void OnVideoFinishedHandler();
        public event OnVideoFinishedHandler OnVideoFinished;
        protected virtual void RaiseOnVideoFinishedHandler()
        {
            if (OnVideoFinished != null)
                try
                {
                    OnVideoFinished();
                }
                catch (Exception e)
                {
                    var del = OnVideoFinished;
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