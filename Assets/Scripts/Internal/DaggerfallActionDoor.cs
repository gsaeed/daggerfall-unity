// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2023 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    LypyL (lypyl@dfworkshop.net)
// 
// Notes:
//

using UnityEngine;
using System.Collections;
using DaggerfallConnect;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Utility;
using UnityEngine.Localization.SmartFormat.Utilities;
using System.Diagnostics;
using System;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Specialised action component for hinged doors in buildings interiors and dungeons.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(BoxCollider))]
    public class DaggerfallActionDoor : MonoBehaviour
    {
        public bool StartOpen = false;                  // Door should start in open state
        public int CurrentLockValue = 0;                // if > 0, door is locked. Can check w. IsLocked prop
        public float OpenAngle = -90f;                  // Angle to swing door on axis when opening
        public float OpenDuration = 1.5f;               // How long in seconds for door to open
        public bool IsTriggerWhenOpen = true;           // Collider is disabled when door opens
        public bool PlaySounds = true;                  // Play open and close sounds if present (OpenSound > 0, CloseSound > 0)
        public short FailedSkillLevel = 0;              // Lockpicking skill of player when they failed to pick lock (TODO: persist across save and load)

        public SoundClips OpenSound = SoundClips.NormalDoorOpen;            // Sound clip to use when door opens
        public SoundClips CloseSound = SoundClips.NormalDoorClose;          // Sound clip to use when door closes
        public SoundClips BashSound = SoundClips.PlayerDoorBash;            // Sound clip to use when bashing door
        public SoundClips PickedLockSound = SoundClips.ActivateLockUnlock;      // Sound clip to use when successfully picked a locked door

        ActionState currentState;
        bool isMeshColliderConvexAlready;
        int startingLockValue = 0;                      // if > 0, is locked.
        ulong loadID = 0;

        Quaternion startingRotation;
        public AudioSource audioSource;

        [SerializeField]
        [Tooltip("Box Collider used to define a door's collision model. An enabled Box Collider is prioritised above a Mesh Collider")]
        private BoxCollider boxCollider;

        [SerializeField]
        [Tooltip("Optional. Allows you to use a Mesh Collider instead of a Box Collider for the door's collision. To use this, disable the door's Box Collider component and add a Mesh Collider. You can use either a Box Collider or Mesh Collider, not both.")]
        private MeshCollider meshCollider;

        public int StartingLockValue                    // Use to set starting lock value, will set current lock value as well
        {
            get { return startingLockValue; }
            set { startingLockValue = CurrentLockValue = value; }
        }

        public ulong LoadID
        {
            get { return loadID; }
            set { loadID = value; }
        }

        public bool IsLocked
        {
            get { return CurrentLockValue > 0; }
        }

        public bool IsOpen
        {
            get { return (currentState == ActionState.End); }
        }

        public bool IsClosed
        {
            get { return (currentState == ActionState.Start); }
        }

        public bool IsMoving
        {
            get { return (currentState == ActionState.PlayingForward || currentState == ActionState.PlayingReverse); }
        }

        public bool IsMagicallyHeld
        {
            get { return CurrentLockValue >= 20; }
        }

        public Quaternion ClosedRotation
        {
            get { return startingRotation; }
        }

        public ActionState CurrentState
        {
            get { return currentState; }
            set { currentState = value; }
        }

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider>();
            if ((boxCollider?.enabled != true) && (meshCollider == null))
                meshCollider = GetComponent<MeshCollider>();

            // Register object as Door
            ActiveGameObjectDatabase.RegisterActionDoor(gameObject);
        }

        void Start()
        {
            currentState = ActionState.Start;
            startingRotation = transform.rotation;
            if (StartOpen)
                Open(0, true);
        }

        public void ToggleDoor(bool activatedByPlayer = false)
        {
            if (IsMoving)
                return;

            if (IsOpen)
                Close(OpenDuration, activatedByPlayer);
            else
                Open(OpenDuration, false, activatedByPlayer);
        }

        public void SetOpen(bool open, bool instant = false, bool ignoreLocks = false)
        {
            float duration = (instant) ? 0 : OpenDuration;
            if (open)
                Open(duration, ignoreLocks);
            else
                Close(duration);
        }

        public void AttemptLockpicking()
        {
            if (IsMoving)
            {
                return;
            }

            PlayerEntity player = Game.GameManager.Instance.PlayerEntity;

            // If player fails at their current lockpicking skill level, they can't try again
            if (!FormulaHelper.CanRetryLockPicking(FailedSkillLevel,player.Skills.GetLiveSkillValue(DFCareer.Skills.Lockpicking)))
            {
                return;
            }

            DaggerfallAudioSource dfAudioSource = GetComponent<DaggerfallAudioSource>();
            var lockStatus = FormulaHelper.AttemptToPickLock(this,IsMagicallyHeld, CurrentLockValue, PlaySounds,
                PickedLockSound, dfAudioSource);

            if (lockStatus)
            {
                CurrentLockValue = 0;
                ToggleDoor(true);
            }
            else
            {
                if (!IsMagicallyHeld)
                    FailedSkillLevel = player.Skills.GetLiveSkillValue(DFCareer.Skills.Lockpicking);
            }
        }

        public void AttemptBash(bool byPlayer)
        {
            FormulaHelper.AttemptBash(byPlayer, this);
 
            if (byPlayer && Game.GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeonCastle)
            {
                Game.GameManager.Instance.MakeEnemiesHostile();
            }

            if (byPlayer && Game.GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
            {
                RaiseOnDoorBashedEvent();
            }
        }

        public void SetInteriorDoorSounds()
        {
            OpenSound = SoundClips.NormalDoorOpen;
            CloseSound = SoundClips.NormalDoorClose;
            BashSound = SoundClips.PlayerDoorBash;
            PickedLockSound = SoundClips.ActivateLockUnlock;
        }

        public void SetDungeonDoorSounds()
        {
            OpenSound = SoundClips.DungeonDoorOpen;
            CloseSound = SoundClips.DungeonDoorClose;
            BashSound = SoundClips.PlayerDoorBash;
            PickedLockSound = SoundClips.ActivateLockUnlock;
        }

        /// <summary>
        /// Restarts a tween in progress. For example, if restoring from save.
        /// </summary>
        public void RestartTween(float durationScale = 1)
        {
            if (currentState == ActionState.PlayingForward)
                Open(OpenDuration, false, false, durationScale);
            else if (currentState == ActionState.PlayingReverse)
                Close(OpenDuration * durationScale);
            else if (currentState == ActionState.End)
                MakeTrigger(true);
        }

        #region Private Methods

        private void Open(float duration, bool ignoreLocks = false, bool activatedByPlayer = false, float scale = 1)
        {
            // Handle DoorText actions. On first activation, show the text but don't try to open the door.
            DaggerfallAction action = GetComponent<DaggerfallAction>();
            if (action != null
                && action.ActionFlag == DFBlock.RdbActionFlags.DoorText
                && action.Index > 0
                && (action.TriggerFlag == DFBlock.RdbTriggerFlags.Door || action.TriggerFlag == DFBlock.RdbTriggerFlags.Direct // Door to Mynisera's room has a "Direct" trigger flagged
                    || action.TriggerFlag == DFBlock.RdbTriggerFlags.MultiTrigger) // Some Castle Wayrest doors have "MultiTrigger" trigger flag
                && action.activationCount == 0
                && activatedByPlayer)
            {
                ExecuteActionOnToggle();
                if (!action.ActionEnabled) // ActionEnabled will still be false if there was valid text to display. In that case, don't open the door for this first activation.
                    return;
            }

            // Do nothing if door cannot be opened right now
            if ((IsLocked && !ignoreLocks) || IsOpen)
            {
                if(!IsOpen)
                    PlayerActivate.LookAtInteriorLock(CurrentLockValue);
                return;
            }

            if (activatedByPlayer)
                ExecuteActionOnToggle();

            // Tween rotation
            Hashtable rotateParams = __ExternalAssets.iTween.Hash(
                "amount", new Vector3(0f, OpenAngle * scale / 360f, 0f),
                "space", Space.Self,
                "time", duration * scale,
                "easetype", __ExternalAssets.iTween.EaseType.linear,
                "oncomplete", "OnCompleteOpen");
            __ExternalAssets.iTween.RotateBy(gameObject, rotateParams);

            // Set collider to trigger only
            MakeTrigger(true);

            // Play open sound if flagged and ready
            if (PlaySounds && OpenSound > 0 && duration * scale > 0 && audioSource
                && currentState != ActionState.PlayingForward)
            {
                DaggerfallAudioSource dfAudioSource = GetComponent<DaggerfallAudioSource>();
                if (dfAudioSource != null)
                    dfAudioSource.PlayOneShot(OpenSound);
            }

            currentState = ActionState.PlayingForward;

            // Set flag
            //IsMagicallyHeld = false;
            CurrentLockValue = 0;
            RaiseOnOpenDoorEvent(this);
        }

        private void Close(float duration, bool activatedByPlayer = false)
        {
            // Do nothing if door cannot be closed right now
            if (IsClosed)
                return;

            // Tween rotation
            Hashtable rotateParams = __ExternalAssets.iTween.Hash(
                "rotation", startingRotation.eulerAngles,
                "time", duration,
                "easetype", __ExternalAssets.iTween.EaseType.linear,
                "oncomplete", "OnCompleteClose",
                "oncompleteparams", duration);
            __ExternalAssets.iTween.RotateTo(gameObject, rotateParams);
            currentState = ActionState.PlayingReverse;

            // For doors that are also action objects, execute action when door opened / closed
            // Only doing so if player was the activator, to keep DoorText actions from running
            // when enemies open doors.
            if (activatedByPlayer)
                ExecuteActionOnToggle();
        }

        private void OnCompleteOpen()
        {
            currentState = ActionState.End;
        }

        private void OnCompleteClose(float duration)
        {
            // Play close sound if flagged and ready
            if (PlaySounds && CloseSound > 0 && duration > 0 && audioSource)
            {
                DaggerfallAudioSource dfAudioSource = GetComponent<DaggerfallAudioSource>();
                dfAudioSource.PlayOneShot(CloseSound);
            }

            // Set collider back to a solid object
            MakeTrigger(false);

            currentState = ActionState.Start;
        }

        private void MakeTrigger(bool isTrigger)
        {
            if ((IsTriggerWhenOpen) && (boxCollider != null) && (boxCollider?.enabled == true))
                boxCollider.isTrigger = isTrigger;
            else if ((IsTriggerWhenOpen) && (meshCollider != null) && (boxCollider?.enabled != true))
            {
                if (isTrigger == true)
                {
                    isMeshColliderConvexAlready = false;
                    if (meshCollider.convex == false)
                        meshCollider.convex = true;
                    else
                        isMeshColliderConvexAlready = true;
                    meshCollider.isTrigger = true;
                }
                else
                {
                    meshCollider.isTrigger = false;
                    if (isMeshColliderConvexAlready == true)
                        isMeshColliderConvexAlready = false;
                    else
                        meshCollider.convex = false;
                }
            }
        }

        //For Doors that are also action objects, executes action when door opened / closed
        private void ExecuteActionOnToggle()
        {
            DaggerfallAction action = GetComponent<DaggerfallAction>();
            if(action != null)
                action.Receive(gameObject, DaggerfallAction.TriggerTypes.Door);
        }

        public delegate void OnDoorBashedEventHandler();
        public static event OnDoorBashedEventHandler OnDoorBashed;

        protected virtual void RaiseOnDoorBashedEvent()
        {
            if (OnDoorBashed != null)
                try
                {
                    OnDoorBashed();
                }
                catch (Exception e)
                {
                    var del = OnDoorBashed;
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

        public delegate void OnOpenDoorEventHandler(DaggerfallActionDoor door);
        public static event OnOpenDoorEventHandler OnOpenDoor;

        protected virtual void RaiseOnOpenDoorEvent(DaggerfallActionDoor door)
        {
            if (OnOpenDoor != null)
                try
                {
                    OnOpenDoor(door);
                }
                catch (Exception e)
                {
                    var del = OnOpenDoor;
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