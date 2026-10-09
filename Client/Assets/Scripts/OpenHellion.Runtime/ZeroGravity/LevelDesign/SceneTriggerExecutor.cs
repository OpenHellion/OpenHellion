using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.LevelDesign
{
	/// <summary>
	/// 	Executes actions when triggered. Uses a state machine.
	/// </summary>
	/// <remarks>
	/// 	Don't remove functions here as they might be used by Unity.
	/// </remarks>
	public class SceneTriggerExecutor : MonoBehaviour, ISceneObject
	{
		[Serializable]
		public class CustomEventAction
		{
			public int Type;

			public UnityEvent Actions;
		}

		public enum AnimatorActionType
		{
			ActivateStart,
			ActivateEnd,
			InactivateStart,
			InactivateEnd,
			FailStart,
			FailEnd,
			ActivateBeforeStart,
			ActivateAfterEnd,
			InactivateBeforeStart,
			InactivateAfterEnd,
			FailBeforeStart,
			FailAfterEnd
		}

		[Serializable]
		public class AnimatorAction
		{
			public SceneTriggerAnimation Animator;

			public AnimatorActionType Type;

			public UnityEvent Actions;
		}

		[Serializable]
		public class CharacterAction
		{
			public UnityEvent InteractStart;

			public UnityEvent InteractEnd;

			public UnityEvent LockStart;

			public UnityEvent LockEnd;
		}

		[Serializable]
		public class StateAction
		{
			[NonSerialized] public int StateID;

			public string StateName;

			public string OnCancelIteractGoToState;

			public string PlayerDisconnectReturnToState;

			public bool PlayerDisconnectToStateImmediate;

			public bool OnlyActivePlayerCanChangeState;

			public CharacterInteractionState CharacterPosition;

			[Tooltip("Dependencies determine if changing to this action state was successful. If dependencies return false, this executor will call fail actions, and pass if it returns true.")]
			[FormerlySerializedAs("Dependencies")] [Space(5f)] public DependencyDelegate Dependency;

			public UnityEvent PassActions;

			public UnityEvent FailActions;

			public UnityEvent InstantActions;

			[FormerlySerializedAs("CharacterActions")] public CharacterAction CharacterAction = new CharacterAction();

			public List<AnimatorAction> AnimatorActions;

			public List<CustomEventAction> CustomActions;

			[NonSerialized] public long TriggeredPlayerGuid;
		}

		public TagAction TagAction;

		public string Tags;

		public Transform MergePivot;

		[SerializeField] private int _inSceneID;

		[SerializeField] private string _defaultState = string.Empty;

		private int _defaultStateID;

		private StateAction _newState;

		private StateAction _currentState;

		[FormerlySerializedAs("actionStates")] [SerializeField] private List<StateAction> _actionStates = new List<StateAction>();

		private readonly Dictionary<int, StateAction> _states = new Dictionary<int, StateAction>();

		private readonly Dictionary<string, int> _stateNameID = new Dictionary<string, int>();

		private long _triggeredPlayerGuid;

		[NonSerialized] public SpaceObjectVessel ParentVessel;

		private SceneTriggerExecutor _childExecutor;

		private SceneTriggerExecutor _parentExecutor;

		private int? _proximityTriggerID;

		private bool? _proximityIsEnter;

		private CharacterInteractionState _interactionToSetAfterTranslate;

		private long _interactionToSetAfterTranslateGuid;

		public int InSceneID
		{
			get => _inSceneID;
			set => _inSceneID = value;
		}

		public int DefaultStateID => _defaultStateID;

		public StateAction CurrentStateAction => _currentState;

		public int CurrentStateID => _currentState != null ? _currentState.StateID : 0;

		public string CurrentState => _currentState == null ? string.Empty : _currentState.StateName;

		public string AdditionalData // Used by dependency delegate.
		{
			set { }
		}

		public SceneTriggerExecutor ChildExecutor => _childExecutor;

		public SceneTriggerExecutor ParentExecutor => _parentExecutor;

		public bool IsMyPlayerTriggered => MyPlayer.Instance != null && MyPlayer.Instance.Guid == _triggeredPlayerGuid; // Used by dependency delegate.

		public bool IsMyPlayerLockedToPanel => MyPlayer.Instance != null && MyPlayer.Instance.IsLockedToTrigger; // Used by dependency delegate.

		private void Awake()
		{
			ReadStates();
		}

		private void Start()
		{
			if (ParentVessel == null)
			{
				ParentVessel = GetComponentInParent<GeometryRoot>().MainObject as SpaceObjectVessel;
			}
		}

		public void ReadDefaultStates()
		{
			if (_actionStates == null || _actionStates.Count == 0)
			{
				return;
			}

			int num = 1;
			foreach (StateAction actionState in _actionStates)
			{
				actionState.StateID = num++;
				if (actionState.StateName == _defaultState)
				{
					_currentState = actionState;
					_defaultStateID = actionState.StateID;
				}
			}

			if (_currentState == null)
			{
				_currentState = _actionStates[0];
			}
		}

		public List<SceneTriggerExecutorStateData> GetExecuterStatesData()
		{
			List<SceneTriggerExecutorStateData> list = new List<SceneTriggerExecutorStateData>();
			if (_actionStates.Count > 0)
			{
				foreach (StateAction actionState in _actionStates)
				{
					list.Add(new SceneTriggerExecutorStateData
					{
						StateID = actionState.StateID,
						PlayerDisconnectToStateID = !actionState.PlayerDisconnectReturnToState.IsNullOrEmpty()
							? GetStateID(actionState.PlayerDisconnectReturnToState)
							: 0,
						PlayerDisconnectToStateImmediate =
							!actionState.PlayerDisconnectReturnToState.IsNullOrEmpty() &&
							actionState.PlayerDisconnectToStateImmediate
					});
				}

				return list;
			}

			return list;
		}

		private void ReadStates()
		{
			if (_actionStates == null || _actionStates.Count == 0)
			{
				return;
			}

			int num = 1;
			foreach (StateAction actionState in _actionStates)
			{
				actionState.StateID = num++;
				_states.Add(actionState.StateID, actionState);
				_stateNameID.Add(actionState.StateName, actionState.StateID);
				actionState.Dependency.CreateDelegates();
				foreach (AnimatorAction animatorAction in actionState.AnimatorActions)
				{
					if (!(animatorAction.Animator != null))
					{
						continue;
					}

					AnimatorAction tmpAa = animatorAction;
					int stateID = actionState.StateID;
					if (animatorAction.Type == AnimatorActionType.ActivateStart ||
					    animatorAction.Type == AnimatorActionType.FailStart ||
					    animatorAction.Type == AnimatorActionType.InactivateStart)
					{
						SceneTriggerAnimation animator = animatorAction.Animator;
						animator.OnStateEnter = (SceneTriggerAnimation.OnStateEnterExitDelegate)Delegate.Combine(
							animator.OnStateEnter,
							(SceneTriggerAnimation.OnStateEnterExitDelegate)delegate(SceneTriggerAnimation anim,
								SceneTriggerAnimation.AnimationState state)
							{
								OnAnimatorStateEnter(tmpAa, anim, state, stateID, isBefore: false);
							});
					}
					else if (animatorAction.Type == AnimatorActionType.ActivateEnd ||
					         animatorAction.Type == AnimatorActionType.FailEnd ||
					         animatorAction.Type == AnimatorActionType.InactivateEnd)
					{
						SceneTriggerAnimation animator2 = animatorAction.Animator;
						animator2.OnStateExit = (SceneTriggerAnimation.OnStateEnterExitDelegate)Delegate.Combine(
							animator2.OnStateExit,
							(SceneTriggerAnimation.OnStateEnterExitDelegate)delegate(SceneTriggerAnimation anim,
								SceneTriggerAnimation.AnimationState state)
							{
								OnAnimatorStateExit(tmpAa, anim, state, stateID, isAfter: false);
							});
					}
					else if (animatorAction.Type == AnimatorActionType.ActivateBeforeStart ||
					         animatorAction.Type == AnimatorActionType.FailBeforeStart ||
					         animatorAction.Type == AnimatorActionType.InactivateBeforeStart)
					{
						SceneTriggerAnimation animator3 = animatorAction.Animator;
						animator3.OnStateBeforeEnter = (SceneTriggerAnimation.OnStateEnterExitDelegate)Delegate.Combine(
							animator3.OnStateBeforeEnter,
							(SceneTriggerAnimation.OnStateEnterExitDelegate)delegate(SceneTriggerAnimation anim,
								SceneTriggerAnimation.AnimationState state)
							{
								OnAnimatorStateEnter(tmpAa, anim, state, stateID, isBefore: true);
							});
					}
					else if (animatorAction.Type == AnimatorActionType.ActivateAfterEnd ||
					         animatorAction.Type == AnimatorActionType.FailAfterEnd ||
					         animatorAction.Type == AnimatorActionType.InactivateAfterEnd)
					{
						SceneTriggerAnimation animator4 = animatorAction.Animator;
						animator4.OnStateAfterExit = (SceneTriggerAnimation.OnStateEnterExitDelegate)Delegate.Combine(
							animator4.OnStateAfterExit,
							(SceneTriggerAnimation.OnStateEnterExitDelegate)delegate(SceneTriggerAnimation anim,
								SceneTriggerAnimation.AnimationState state)
							{
								OnAnimatorStateExit(tmpAa, anim, state, stateID, isAfter: true);
							});
					}
				}

				if (actionState.StateName == _defaultState)
				{
					_currentState = actionState;
					_defaultStateID = actionState.StateID;
				}
			}

			if (_currentState == null)
			{
				_currentState = _actionStates[0];
			}

			_actionStates.Clear();
		}

		private bool CheckDependencies(StateAction st)
		{
			if (st.Dependency != null)
			{
				return st.Dependency.Invoke();
			}

			return true;
		}

		private void CharacterInteractStarted()
		{
			if (MyPlayer.Instance.Guid == _triggeredPlayerGuid)
			{
				MyPlayer.Instance.OnIteractStart = null;
				if (_currentState.CharacterAction.InteractStart != null)
				{
					_currentState.CharacterAction.InteractStart.Invoke();
				}

				return;
			}

			OtherPlayer player = ParentVessel.World.GetPlayer(_triggeredPlayerGuid);
			if (player != null)
			{
				player.OnIteractStart = null;
				if (_currentState.CharacterAction.InteractStart != null)
				{
					_currentState.CharacterAction.InteractStart.Invoke();
				}
			}
		}

		private void CharacterInteractCompleted()
		{
			if (MyPlayer.Instance.Guid == _triggeredPlayerGuid)
			{
				MyPlayer.Instance.OnIteractComplete = null;
				if (_currentState.CharacterAction.InteractEnd != null)
				{
					_currentState.CharacterAction.InteractEnd.Invoke();
				}

				return;
			}

			OtherPlayer player = ParentVessel.World.GetPlayer(_triggeredPlayerGuid);
			if (player != null)
			{
				player.OnIteractComplete = null;
				if (_currentState.CharacterAction.InteractEnd != null)
				{
					_currentState.CharacterAction.InteractEnd.Invoke();
				}
			}
		}

		private void CharacterLockStarted()
		{
			if (MyPlayer.Instance.Guid == _triggeredPlayerGuid)
			{
				MyPlayer.Instance.OnLockStart = null;
				if (_currentState.CharacterAction.LockStart != null)
				{
					_currentState.CharacterAction.LockStart.Invoke();
				}

				return;
			}

			OtherPlayer player = ParentVessel.World.GetPlayer(_triggeredPlayerGuid);
			if (player != null)
			{
				player.OnLockStart = null;
				if (_currentState.CharacterAction.LockStart != null)
				{
					_currentState.CharacterAction.LockStart.Invoke();
				}
			}
		}

		private void CharacterLockCompleted()
		{
			if (MyPlayer.Instance.Guid == _triggeredPlayerGuid)
			{
				MyPlayer.Instance.OnLockComplete = null;
				if (_currentState.CharacterAction.LockEnd != null)
				{
					_currentState.CharacterAction.LockEnd.Invoke();
				}

				return;
			}

			OtherPlayer player = ParentVessel.World.GetPlayer(_triggeredPlayerGuid);
			if (player != null)
			{
				player.OnLockComplete = null;
				if (_currentState.CharacterAction.LockEnd != null)
				{
					_currentState.CharacterAction.LockEnd.Invoke();
				}
			}
		}

		private void SendPackageToServer()
		{
			SendPackageToServer(isImmediate: false);
		}

		private void SendPackageToServer(bool isImmediate, bool force = false)
		{
			SceneTriggerExecutorDetails sceneTriggerExecutorDetails = new SceneTriggerExecutorDetails
			{
				InSceneID = InSceneID,
				IsImmediate = isImmediate,
				IsFail = !CheckDependencies(_newState) && !force,
				CurrentStateID = CurrentStateID,
				NewStateID = _newState.StateID
			};
			int? num = _proximityTriggerID;
			if (num.HasValue)
			{
				sceneTriggerExecutorDetails.ProximityTriggerID = _proximityTriggerID.Value;
				sceneTriggerExecutorDetails.ProximityIsEnter = _proximityIsEnter.Value;
			}

			if (ParentVessel != null)
			{
				ParentVessel.ChangeStats(sceneTriggerExecutor: sceneTriggerExecutorDetails);
			}

			_proximityTriggerID = null;
			_proximityIsEnter = null;
		}

		private void RunActions(StateAction act, bool isFail, bool isInstant)
		{
			if (MyPlayer.Instance.Guid == _triggeredPlayerGuid)
			{
				if (act.CharacterAction.InteractStart.GetPersistentEventCount() > 0)
				{
					MyPlayer.Instance.OnIteractStart = CharacterInteractStarted;
				}

				if (act.CharacterAction.InteractEnd.GetPersistentEventCount() > 0)
				{
					MyPlayer.Instance.OnIteractComplete = CharacterInteractCompleted;
				}

				if (act.CharacterAction.LockStart.GetPersistentEventCount() > 0)
				{
					MyPlayer.Instance.OnLockStart = CharacterLockStarted;
				}

				if (act.CharacterAction.LockEnd.GetPersistentEventCount() > 0)
				{
					MyPlayer.Instance.OnLockComplete = CharacterLockCompleted;
				}
			}
			else
			{
				OtherPlayer player = ParentVessel.World.GetPlayer(_triggeredPlayerGuid);
				if (player != null)
				{
					player.OnIteractStart = CharacterInteractStarted;
					player.OnIteractComplete = CharacterInteractCompleted;
					player.OnLockStart = CharacterLockStarted;
					player.OnLockComplete = CharacterLockCompleted;
				}
			}

			if (isInstant)
			{
				act.InstantActions.Invoke();
			}
			else if (isFail)
			{
				act.FailActions.Invoke();
			}
			else
			{
				act.PassActions.Invoke();
			}
		}

		public int GetStateID(string stateName)
		{
			if (_stateNameID.Count > 0)
			{
				if (_stateNameID.ContainsKey(stateName))
				{
					return _stateNameID[stateName];
				}

				return 0;
			}

			if (_actionStates.Count > 0 && !Application.isPlaying)
			{
				int num = 1;
				foreach (StateAction actionState in _actionStates)
				{
					num++;
					if (actionState.StateName == stateName)
					{
						return actionState.StateID != 0 ? actionState.StateID : num;
					}
				}
			}

			return 0;
		}

		public void ChangeState(string newState)
		{
			if (_stateNameID.ContainsKey(newState))
			{
				ChangeStateID(_stateNameID[newState], isInstantChange: false);
			}
			else
			{
				Debug.LogWarningFormat("Executor {0} tried to change state to undefined state {1}.", gameObject.name, newState);
			}
		}

		public void ChangeStateImmediate(string newState)
		{
			if (_stateNameID.ContainsKey(newState))
			{
				ChangeStateID(_stateNameID[newState], isInstantChange: true);
			}
			else
			{
				Debug.LogWarningFormat("Executor {0} tried to change state to undefined state {1}.", gameObject.name, newState);
			}
		}

		public void ChangeStateImmediateForce(string newState)
		{
			if (_stateNameID.ContainsKey(newState))
			{
				ChangeStateID(_stateNameID[newState], isInstantChange: true, force: true);
			}
			else
			{
				Debug.LogWarningFormat("Executor {0} tried to change state to undefined state {1}.", gameObject.name, newState);
			}
		}

		public void ChangeStateID(int newState)
		{
			ChangeStateID(newState, isInstantChange: false);
		}

		public void ChangeStateIDImmediate(int newState)
		{
			ChangeStateID(newState, isInstantChange: true);
		}

		public void MergeStateID(int newState, bool isInstant)
		{
			if (newState != CurrentStateID)
			{
				SetExecutorDetails(new SceneTriggerExecutorDetails
				{
					PlayerThatActivated = 0L,
					InSceneID = InSceneID,
					IsImmediate = isInstant,
					IsFail = false,
					CurrentStateID = CurrentStateID,
					NewStateID = newState
				}, isInstant, _parentExecutor);
			}
		}

		public void ChangeStateID(int newStateId, bool isInstantChange, bool force = false)
		{
			if (!_states.ContainsKey(newStateId) || (_currentState.OnlyActivePlayerCanChangeState &&
												  _currentState.TriggeredPlayerGuid != 0 &&
												  _currentState.TriggeredPlayerGuid != MyPlayer.Instance.Guid))
			{
				return;
			}

			_newState = _states[newStateId];
			if (_newState.CharacterPosition == null || _newState.CharacterPosition.InteractPosition == null)
			{
				SendPackageToServer(isInstantChange, force);
			}
			else
			{
				if (_newState.CharacterPosition.SetColliderToKinematic)
				{
					MyPlayer.Instance.FpsController.ToggleKinematic(true);
				}

				_triggeredPlayerGuid = MyPlayer.Instance.Guid;
				if (isInstantChange)
				{
					MyPlayer.Instance.transform.SetPositionAndRotation(_newState.CharacterPosition.InteractPosition.position, _newState.CharacterPosition.InteractPosition.rotation);
					SendPackageToServer(isInstantChange, force);
				}
				else
				{
					StartCoroutine(MyPlayer.Instance.FpsController.TranslateAndLookAt(
						_newState.CharacterPosition.InteractPosition, _newState.CharacterPosition.InteractLookAt,
						SendPackageToServer));
				}
			}
		}

		public void SetChild(SceneTriggerExecutor childExecutor, bool isInstant)
		{
			if (childExecutor != null)
			{
				_childExecutor = childExecutor;
				_childExecutor._parentExecutor = this;
				if (_childExecutor.CurrentStateID != CurrentStateID)
				{
					_childExecutor.MergeStateID(CurrentStateID, isInstant);
				}
			}
			else if (_childExecutor != null)
			{
				_childExecutor._parentExecutor = null;
				_childExecutor = null;
			}
		}

		public string GetExecutorDebugString()
		{
			return string.Format("{6} = {0}, {1}, PEX = {2}, {3}, CH = {4}, {5}", ParentVessel.Guid, InSceneID,
				!(_parentExecutor != null) ? 0 : _parentExecutor.ParentVessel.Guid,
				_parentExecutor != null ? _parentExecutor.InSceneID : 0,
				!(_childExecutor != null) ? 0 : _childExecutor.ParentVessel.Guid,
				_childExecutor != null ? _childExecutor.InSceneID : 0, name);
		}

		public void SetExecutorDetails(SceneTriggerExecutorDetails details, bool isInstant = false,
			SceneTriggerExecutor fromExecutor = null, bool checkCurrentState = true)
		{
			if (_parentExecutor != null && _parentExecutor != fromExecutor)
			{
				_parentExecutor.SetExecutorDetails(details, isInstant, this);
			}
			else
			{
				if (!_states.ContainsKey(details.NewStateID) ||
				    (checkCurrentState && _currentState.StateID == details.NewStateID))
				{
					return;
				}

				if (details.IsImmediate.HasValue && details.IsImmediate.Value)
				{
					isInstant = true;
				}

				_triggeredPlayerGuid = details.PlayerThatActivated;
				if (!details.IsFail)
				{
					if (MyPlayer.Instance.CancelInteractExecutor == this)
					{
						MyPlayer.Instance.CancelInteractExecutor = null;
					}

					_currentState = _states[details.NewStateID];
					_currentState.TriggeredPlayerGuid = _triggeredPlayerGuid;
					if (_currentState.TriggeredPlayerGuid == MyPlayer.Instance.Guid &&
					    !_currentState.OnCancelIteractGoToState.IsNullOrEmpty())
					{
						MyPlayer.Instance.CancelInteractExecutor = this;
					}

					RunActions(_currentState, details.IsFail, isInstant);
				}
				else
				{
					if (_currentState.TriggeredPlayerGuid == MyPlayer.Instance.Guid &&
					    !_currentState.OnCancelIteractGoToState.IsNullOrEmpty())
					{
						MyPlayer.Instance.CancelInteractExecutor = this;
					}

					RunActions(_states[details.NewStateID], details.IsFail, isInstant);
				}

				if (_childExecutor != null)
				{
					_childExecutor.SetExecutorDetails(details, isInstant, this);
				}
			}
		}

		private void OnTranslateFinished()
		{
			if (_interactionToSetAfterTranslate != null)
			{
				_triggeredPlayerGuid = _interactionToSetAfterTranslateGuid;
				CharacterInteractRunner(_interactionToSetAfterTranslate, isInstant: false);
				_interactionToSetAfterTranslate = null;
			}
		}

		private void CharacterInteractRunner(CharacterInteractionState cis, bool isInstant)
		{
			if (MyPlayer.Instance.Guid == _triggeredPlayerGuid)
			{
				if (!cis.ImmediatePositionChange && !isInstant &&
				    (!MyPlayer.Instance.transform.position.IsEpsilonEqual(cis.InteractPosition.position, 0.01f) ||
				     !MyPlayer.Instance.transform.rotation.IsEpsilonEqual(cis.InteractPosition.rotation, 0.01f)))
				{
					_interactionToSetAfterTranslate = cis;
					_interactionToSetAfterTranslateGuid = _triggeredPlayerGuid;
					StartCoroutine(MyPlayer.Instance.FpsController.TranslateAndLookAt(cis.InteractPosition,
						cis.InteractLookAt, OnTranslateFinished));
					return;
				}

				if (cis.SetColliderToKinematic)
				{
					MyPlayer.Instance.FpsController.ToggleKinematic(true);
				}

				if (cis.InteractPosition != null)
				{
					MyPlayer.Instance.transform.SetPositionAndRotation(cis.InteractPosition.position, cis.InteractPosition.rotation);
					if (cis.ImmediatePositionChange && cis.ImmediateLockType != 0)
					{
						AnimatorHelper animHelper = MyPlayer.Instance.animHelper;
						AnimatorHelper.LockType? lockType = cis.ImmediateLockType;
						animHelper.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null,
							null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
							null, null, null, lockType);
						MyPlayer.Instance.animHelper.ForceAnimationUpdate();
					}

					if (cis.InteractType != 0)
					{
						AnimatorHelper animHelper2 = MyPlayer.Instance.animHelper;
						AnimatorHelper.InteractType? interactType = cis.InteractType;
						animHelper2.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null,
							null, null, null, null, null, null, null, null, null, null, null, null, null, interactType);
					}
				}

				if (cis.LockType != 0)
				{
					AnimatorHelper animHelper3 = MyPlayer.Instance.animHelper;
					AnimatorHelper.LockType? lockType = cis.LockType;
					animHelper3.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null,
						null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
						null, null, lockType);
					if (!isInstant)
					{
						MyPlayer.Instance.animHelper.SetParameterTrigger(AnimatorHelper.Triggers.Lock);
					}
					else
					{
						MyPlayer.Instance.animHelper.SetParameterTrigger(AnimatorHelper.Triggers.LockImmediate);
						MyPlayer.Instance.animHelper.ForceAnimationUpdate();
					}
				}

				if ((!isInstant || cis.LockType == AnimatorHelper.LockType.None) && cis.InteractType != 0)
				{
					MyPlayer.Instance.animHelper.ResetParameterTrigger(AnimatorHelper.Triggers.UnlockImmediate);
					MyPlayer.Instance.animHelper.SetParameterTrigger(AnimatorHelper.Triggers.InteractTrigger);
				}

				if (cis.UseAnimationCamera)
				{
					MyPlayer.Instance.FpsController.ToggleCameraAttachToHeadBone(true);
				}

				if (cis.AutoFreeLook)
				{
					MyPlayer.Instance.FpsController.ToggleAutoFreeLook(isActive: true);
				}

				if (cis.LockType != 0 || cis.InteractType != 0)
				{
					MyPlayer.Instance.FpsController.ToggleMovement(false);
					MyPlayer.Instance.FpsController.ResetVelocity();
				}

				return;
			}

			OtherPlayer player = ParentVessel.World.GetPlayer(_triggeredPlayerGuid);
			if (player != null)
			{
				if (!cis.ImmediatePositionChange && !isInstant &&
				    (!player.transform.position.IsEpsilonEqual(cis.InteractPosition.position, 0.01f) ||
				     !player.transform.rotation.IsEpsilonEqual(cis.InteractPosition.rotation, 0.01f)))
				{
					_interactionToSetAfterTranslate = cis;
					_interactionToSetAfterTranslateGuid = _triggeredPlayerGuid;
					StartCoroutine(player.tpsController.TranslateTo(cis.InteractPosition, OnTranslateFinished));
					return;
				}

				if (cis.InteractPosition != null)
				{
					player.SetGlobalPositionAndRotation(cis.InteractPosition.position, cis.InteractPosition.rotation);
					if (cis.InteractType != 0)
					{
						AnimatorHelper animHelper4 = player.tpsController.animHelper;
						AnimatorHelper.InteractType? interactType = cis.InteractType;
						animHelper4.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null,
							null, null, null, null, null, null, null, null, null, null, null, null, null, interactType);
					}
				}

				if ((!isInstant || cis.LockType == AnimatorHelper.LockType.None) && cis.InteractType != 0)
				{
					player.tpsController.animHelper.ResetParameterTrigger(AnimatorHelper.Triggers.UnlockImmediate);
					player.tpsController.animHelper.SetParameterTrigger(AnimatorHelper.Triggers.InteractTrigger);
				}

				if (cis.LockType != 0)
				{
					AnimatorHelper animHelper5 = player.tpsController.animHelper;
					AnimatorHelper.LockType? lockType = cis.LockType;
					animHelper5.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null,
						null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
						null, null, lockType);
					if (!isInstant)
					{
						player.tpsController.animHelper.SetParameterTrigger(AnimatorHelper.Triggers.Lock);
						return;
					}

					player.tpsController.animHelper.SetParameterTrigger(AnimatorHelper.Triggers.LockImmediate);
					player.tpsController.animHelper.ForceAnimationUpdate();
				}
			}
			else if (isInstant && cis.LockType != 0)
			{
				cis.Executor = this;
				ParentVessel.World.CharacterInteractionStatesQueue[_triggeredPlayerGuid] = cis;
			}
		}

		public void LockPlayerToTrigger(GameObject trigger)
		{
			if (!(trigger == null) && _triggeredPlayerGuid == MyPlayer.Instance.Guid)
			{
				if (!trigger.activeInHierarchy)
				{
					trigger.SetActive(value: true);
				}

				BaseSceneTrigger component = trigger.GetComponent<BaseSceneTrigger>();
				if (component != null)
				{
					component.Interact(MyPlayer.Instance);
				}
			}
		}

		public void UnlockPlayerFromTrigger(GameObject trigger)
		{
			if (MyPlayer.Instance.LockedToTrigger != null && trigger != null &&
			    MyPlayer.Instance.LockedToTrigger == trigger.GetComponent<BaseSceneTrigger>())
			{
				MyPlayer.Instance.LockedToTrigger.CancelInteract(MyPlayer.Instance);
				MyPlayer.Instance.DetachFromPanel();
			}
		}

		public void CharacterInteract(CharacterInteractionState cis)
		{
			CharacterInteractRunner(cis, isInstant: false);
		}

		public void CharacterIteractInstant(CharacterInteractionState cis)
		{
			CharacterInteractRunner(cis, isInstant: true);
		}

		public void CharacterInteractInstant(CharacterInteractionState cis, long playerGUID)
		{
			_triggeredPlayerGuid = playerGUID;
			CharacterInteractRunner(cis, isInstant: true);
		}

		public void CharacterUnlock()
		{
			if (MyPlayer.Instance.Guid == _triggeredPlayerGuid)
			{
				MyPlayer.Instance.FpsController.ToggleKinematic(false);
				MyPlayer.Instance.animHelper.ResetParameterTrigger(AnimatorHelper.Triggers.LockImmediate);
				if (MyPlayer.Instance.animHelper.IsCurrentAnimState(AnimatorHelper.AnimatorLayers_FPS.InteractionLayer,
					    "Locks"))
				{
					MyPlayer.Instance.animHelper.SetParameterTrigger(AnimatorHelper.Triggers.UnlockImmediate);
					MyPlayer.Instance.animHelper.ForceAnimationUpdate();
				}

				MyPlayer.Instance.FpsController.ToggleMovement(true);
				MyPlayer.Instance.FpsController.ToggleAttached(false);
				MyPlayer.Instance.FpsController.ResetLookAt(0.15f);
				MyPlayer.Instance.FpsController.ToggleCameraAttachToHeadBone(false);
				MyPlayer.Instance.FpsController.ToggleAutoFreeLook(isActive: false);
				return;
			}

			OtherPlayer player = ParentVessel.World.GetPlayer(_triggeredPlayerGuid);
			if (player != null)
			{
				player.tpsController.animHelper.ResetParameterTrigger(AnimatorHelper.Triggers.LockImmediate);
				if (player.tpsController.animHelper.IsCurrentAnimState(
					    AnimatorHelper.AnimatorLayers_TPS.InteractionLayer, "Locks"))
				{
					player.tpsController.animHelper.SetParameterTrigger(AnimatorHelper.Triggers.UnlockImmediate);
					player.tpsController.animHelper.ForceAnimationUpdate();
				}

				player.tpsController.animHelper.SetLayerWeight(AnimatorHelper.AnimatorLayers_TPS.MouseLookVertical, 1f);
			}
			else if (ParentVessel.World.CharacterInteractionStatesQueue.ContainsKey(_triggeredPlayerGuid))
			{
				ParentVessel.World.CharacterInteractionStatesQueue.Remove(_triggeredPlayerGuid);
			}
		}

		public void LockCharacter()
		{
			if (MyPlayer.Instance.Guid == _triggeredPlayerGuid)
			{
				MyPlayer.Instance.FpsController.ToggleMovement(false);
				MyPlayer.Instance.FpsController.ToggleKinematic(true);
				MyPlayer.Instance.FpsController.ToggleAutoFreeLook(isActive: true);
				MyPlayer.Instance.animHelper.SetParameter(null, null, null, null, null, null, null, null, null, null,
					0f, 0f);
			}
		}

		public void CharacterUnlockWithTransform(Transform transform)
		{
			if (MyPlayer.Instance.Guid == _triggeredPlayerGuid)
			{
				MyPlayer.Instance.transform.position = transform.position;
				MyPlayer.Instance.transform.rotation = transform.rotation;
			}

			CharacterUnlock();
		}

		public void ExecuteCustomActions(int type)
		{
			if (MyPlayer.Instance.Guid != _triggeredPlayerGuid || _currentState.CustomActions == null ||
			    _currentState.CustomActions.Count == 0)
			{
				return;
			}

			foreach (CustomEventAction customAction in _currentState.CustomActions)
			{
				if (customAction.Type == type)
				{
					customAction.Actions.Invoke();
					break;
				}
			}
		}

		public void OnAnimatorStateEnter(AnimatorAction animAction, SceneTriggerAnimation anim,
			SceneTriggerAnimation.AnimationState state, int stateID, bool isBefore)
		{
			if (animAction.Animator == anim && stateID == CurrentStateID &&
			    ((isBefore &&
			      ((animAction.Type == AnimatorActionType.ActivateBeforeStart &&
			        state == SceneTriggerAnimation.AnimationState.Active) ||
			       (animAction.Type == AnimatorActionType.InactivateBeforeStart &&
			        state == SceneTriggerAnimation.AnimationState.Inactive) ||
			       (animAction.Type == AnimatorActionType.FailBeforeStart &&
			        state == SceneTriggerAnimation.AnimationState.Fail))) || (!isBefore &&
			                                                                  ((animAction.Type ==
				                                                                   AnimatorActionType.ActivateStart &&
				                                                                   state == SceneTriggerAnimation
					                                                                   .AnimationState.Active) ||
			                                                                   (animAction.Type ==
				                                                                   AnimatorActionType.InactivateStart &&
				                                                                   state == SceneTriggerAnimation
					                                                                   .AnimationState.Inactive) ||
			                                                                   (animAction.Type ==
				                                                                   AnimatorActionType.FailStart &&
				                                                                   state == SceneTriggerAnimation
					                                                                   .AnimationState.Fail)))))
			{
				animAction.Actions.Invoke();
			}
		}

		public void OnAnimatorStateExit(AnimatorAction animAction, SceneTriggerAnimation anim,
			SceneTriggerAnimation.AnimationState state, int stateID, bool isAfter)
		{
			if (animAction.Animator == anim && stateID == CurrentStateID &&
			    ((isAfter &&
			      ((animAction.Type == AnimatorActionType.ActivateAfterEnd &&
			        state == SceneTriggerAnimation.AnimationState.Active) ||
			       (animAction.Type == AnimatorActionType.InactivateAfterEnd &&
			        state == SceneTriggerAnimation.AnimationState.Inactive) ||
			       (animAction.Type == AnimatorActionType.FailAfterEnd &&
			        state == SceneTriggerAnimation.AnimationState.Fail))) || (!isAfter &&
			                                                                  ((animAction.Type ==
				                                                                   AnimatorActionType.ActivateEnd &&
				                                                                   state == SceneTriggerAnimation
					                                                                   .AnimationState.Active) ||
			                                                                   (animAction.Type ==
				                                                                   AnimatorActionType.InactivateEnd &&
				                                                                   state == SceneTriggerAnimation
					                                                                   .AnimationState.Inactive) ||
			                                                                   (animAction.Type ==
				                                                                   AnimatorActionType.FailEnd &&
				                                                                   state == SceneTriggerAnimation
					                                                                   .AnimationState.Fail)))))
			{
				animAction.Actions.Invoke();
			}
		}

		public bool AreStatesEqual(SceneTriggerExecutor other)
		{
			if (_states.Count != other._states.Count)
			{
				return false;
			}

			foreach (KeyValuePair<int, StateAction> state in _states)
			{
				if (!other._states.ContainsKey(state.Key) || other._states[state.Key].StateName != state.Value.StateName)
				{
					return false;
				}
			}

			return true;
		}

		public void PlayerEnterTrigger(SceneTrigger trigg, MyPlayer player, int newStateID)
		{
			_proximityTriggerID = trigg.TriggerID;
			_proximityIsEnter = true;
			ChangeStateID(newStateID);
		}

		public void PlayerExitTrigger(SceneTrigger trigg, MyPlayer player, int newStateID)
		{
			_proximityTriggerID = trigg.TriggerID;
			_proximityIsEnter = false;
			ChangeStateID(newStateID);
		}

		public void SetCancelInteractExecuter(SceneTriggerExecutor exec)
		{
			if (exec != null)
			{
				MyPlayer.Instance.CancelInteractExecutor = exec;
			}
		}

		public void CancelInteract()
		{
			ChangeStateID(GetStateID(_currentState.OnCancelIteractGoToState), isInstantChange: false);
		}

		public void CallMyPlayerItemInHandsSpecial()
		{
			if (_triggeredPlayerGuid == MyPlayer.Instance.Guid && MyPlayer.Instance.Inventory.ItemInHands != null)
			{
				MyPlayer.Instance.Inventory.ItemInHands.Special();
			}
		}

		public void StartExitCryoChamberCountdown(string stateName)
		{
			if (_triggeredPlayerGuid == MyPlayer.Instance.Guid)
			{
				MyPlayer.Instance.StartExitCryoChamberCountdown(this, stateName);
			}
		}

		public void MyPlayerInteractWithPilotChair()
		{
			if (CurrentStateAction.TriggeredPlayerGuid == MyPlayer.Instance.Guid)
			{
				MyPlayer.Instance.SittingOnPilotSeat = CurrentStateID != DefaultStateID;
			}
		}
	}
}
