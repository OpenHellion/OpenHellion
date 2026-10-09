using System;
using System.Collections.Generic;
using System.Linq;
using OpenHellion.IO;
using UnityEngine;
using ZeroGravity.Effects;
using ZeroGravity.Network;
using ZeroGravity.Objects;
using ZeroGravity.UI;

namespace ZeroGravity.LevelDesign
{
	public class SceneDoor : MonoBehaviour, ISceneObject, ISoundOccludable
	{
		[SerializeField] private int _inSceneID;

		[SerializeField] private bool _isSealable;

		[SerializeField] private bool _hasPower;

		[SerializeField] private bool _isLocked;

		[SerializeField] private bool _isOpen;

		public bool LockedAutoToggle;

		public float PassageArea = 2f;

		public SceneTriggerRoom Room1;

		public SceneTriggerRoom Room2;

		[Tooltip("Setujes particles tako da vazduh curi iz sobe 1")]
		public AirDoorParticleToggler DoorParticles1;

		[Tooltip("Setujes particles tako da vazduh curi iz sobe 2")]
		public AirDoorParticleToggler DoorParticles2;

		public GameObject DockingDoorPatch;

		public GameObject DoorUI;

		public GameObject Hazard;

		public GameObject Locked;

		public Collider DoorPassageTrigger;

		public AnimationCurve DoorOpenCurve = new AnimationCurve(new Keyframe
		{
			time = 0f,
			value = 0f,
			inTangent = 0f,
			outTangent = 0f
		}, new Keyframe
		{
			time = 1f,
			value = 1f,
			inTangent = 2f,
			outTangent = 2f
		});

		public float DoorOpenDuration = 1f;

		[HideInInspector] public SpaceObjectVessel ParentVessel;

		private float _pressureEqualisationTime;

		private int _airFlowDirection;

		private float _airSpeed;

		private float _doorOpenTime;

		public List<SoundEffect> DepressurisationSounds;

		private Vector3 _doorPassageTriggerSize;

		private bool _hasOverriddenSafety;

		public int InSceneID
		{
			get => _inSceneID;
			set => _inSceneID = value;
		}

		public bool IsSealable => _isSealable;

		public bool HasPower => _hasPower;

		public bool IsLocked => _isLocked;

		public bool IsOpen => _isOpen;

		private void Start()
		{
			if (ParentVessel == null)
			{
				ParentVessel = GetComponentInParent<GeometryRoot>().MainObject as SpaceObjectVessel;
			}

			if (Room1 != null)
			{
				Room1.AddBehaviourScript(this);
			}

			if (Room2 != null)
			{
				Room2.AddBehaviourScript(this);
			}

			if (TryGetComponent<SceneTriggerAnimation>(out var component))
			{
				DoorOpenDuration = component.GetActiveExecutingDuration();
			}

			_doorPassageTriggerSize = DoorPassageTrigger.GetComponent<Collider>().bounds.size;
			UpdateDoorUI();
			if (GetComponent<AnimationHelper>() != null)
			{
				DepressurisationSounds = GetComponent<AnimationHelper>().SoundEffects;
			}
		}

		public void SetDoorDetails(DoorDetails details, bool isInstant = false)
		{
			_hasPower = details.HasPower;
			_isLocked = details.IsLocked;
			_isOpen = details.IsOpen;
			_pressureEqualisationTime = (details.PressureEquilizationTime is >= 0.5f or <= 0f)
				? details.PressureEquilizationTime
				: 0.5f;
			_airFlowDirection = details.AirFlowDirection;
			_airSpeed = details.AirSpeed;
			if (details.Room1ID != null)
			{
				SpaceObjectVessel vessel = ParentVessel.World.GetVessel(details.Room1ID.VesselGUID);
				if (vessel != null)
				{
					vessel.RoomTriggers.TryGetValue(details.Room1ID.InSceneID, out Room1);
				}
			}
			else
			{
				Room1 = null;
			}

			if (details.Room2ID != null)
			{
				SpaceObjectVessel vessel2 = ParentVessel.World.GetVessel(details.Room2ID.VesselGUID);
				if (vessel2 != null)
				{
					vessel2.RoomTriggers.TryGetValue(details.Room2ID.InSceneID, out Room2);
				}
			}
			else
			{
				Room2 = null;
			}

			_doorOpenTime = Time.time;
			if (DepressurisationSounds.Count <= 0 || !(_airSpeed > 40f))
			{
				return;
			}

			foreach (SoundEffect depressurisationSound in DepressurisationSounds)
			{
				depressurisationSound.Play(2);
			}
		}

		public void ToggleOpen(bool isOpen)
		{
			bool? open = isOpen;
			ChangeStats(null, open);
		}

		public void ToggleOpenInstant(bool isOpen)
		{
			_isOpen = isOpen;
		}

		public void ToggleLock(bool isLocked)
		{
			ChangeStats(isLocked);
		}

		public void ToggleLockInstant(bool isLocked)
		{
			_isLocked = isLocked;
		}

		public void EquilizePressureBeforeOpen(bool equilize)
		{
		}

		public void ChangeStats(bool? locked = null, bool? open = null)
		{
			if (locked.HasValue || open.HasValue)
			{
				DoorDetails door = new DoorDetails
				{
					InSceneID = InSceneID,
					HasPower = _hasPower,
					IsLocked = (!locked.HasValue) ? _isLocked : locked.Value,
					IsOpen = (!open.HasValue) ? _isOpen : open.Value
				};
				ParentVessel.ChangeStats(null, null, null, null, null, null, null, door);
			}
		}

		private void FixedUpdate()
		{
			if (!IsSealable || !IsOpen || _pressureEqualisationTime <= 0f || _airFlowDirection == 0 ||
			    DoorPassageTrigger == null)
			{
				return;
			}

			float num = Time.time - _doorOpenTime;
			float num2 = !(num < DoorOpenDuration) || !(DoorOpenDuration > 0f)
				? 1f
				: DoorOpenCurve.Evaluate(num / DoorOpenDuration);
			Vector3 fLine = DoorPassageTrigger.transform.rotation * Vector3.forward;
			float fIntensity = _airSpeed * _airSpeed * 0.3311f * num2;
			_pressureEqualisationTime -= Time.deltaTime * num2;
			float num3 = !(Room1 != null) ? 0f : Room1.AirPressure;
			float num4 = !(Room2 != null) ? 0f : Room2.AirPressure;
			if (DoorParticles1 != null && DoorParticles2 != null)
			{
				if (num3 > num4)
				{
					DoorParticles1.Play();
				}
				else if (num3 < num4)
				{
					DoorParticles2.Play();
				}
			}

			AffectMyPlayer(fLine, fIntensity);
			AffectInsideDynamicObjects(fLine, fIntensity);
			AffectOutsideDynamicObjects(fIntensity);
			AffectInsideCorpses(fLine, fIntensity);
			AffectOutsideCorpses(fIntensity);
		}

		private void AffectOutsideCorpses(float fIntensity)
		{
			foreach (Corpse item in ParentVessel.World.AllCorpses.Where((Corpse m) => m.Parent is Pivot))
			{
				if (!item.IsKinematic)
				{
					Vector3 vector = item.transform.position - DoorPassageTrigger.transform.position;
					if (vector.sqrMagnitude < 25f)
					{
						Vector3 vector2 = vector.normalized * fIntensity;
						item.AddForce(vector2 * 0.005f, ForceMode.Impulse);
					}
				}
			}
		}

		private void AffectInsideCorpses(Vector3 fLine, float fIntensity)
		{
			Corpse[] componentsInChildren = ParentVessel.TransferableObjectsRoot.GetComponentsInChildren<Corpse>();
			foreach (Corpse corpse in componentsInChildren)
			{
				if (corpse.IsKinematic || !(corpse.CurrentRoomTrigger != null))
				{
					continue;
				}

				Vector3 vector = corpse.transform.position - DoorPassageTrigger.transform.position;
				if (vector.sqrMagnitude < 25f)
				{
					Vector3 vector2 = Vector3.zero;
					Vector3 vector3 = Vector3.Project(vector, fLine);
					if (corpse.CurrentRoomTrigger == Room1)
					{
						vector2 = ((_airFlowDirection != 1) ? 1 : (-1)) * fIntensity * vector3.normalized;
					}
					else if (corpse.CurrentRoomTrigger == Room2)
					{
						vector2 = ((_airFlowDirection != 2) ? 1 : (-1)) * fIntensity * vector3.normalized;
					}

					Vector3 vector4 = Vector3.ProjectOnPlane(vector, vector2);
					if (vector2.sqrMagnitude > float.Epsilon && (vector4.x + 0.3f > _doorPassageTriggerSize.x / 2f ||
					                                             vector4.x - 0.3f < (0f - _doorPassageTriggerSize.x) /
					                                             2f ||
					                                             vector4.y + 0.3f > _doorPassageTriggerSize.y / 2f ||
					                                             vector4.y - 0.3f < (0f - _doorPassageTriggerSize.y) /
					                                             2f))
					{
						vector2 += 0.1f * fIntensity * -vector4;
					}

					corpse.AddForce(vector2 * 0.005f, ForceMode.Impulse);
				}
			}
		}

		private void AffectOutsideDynamicObjects(float fIntensity)
		{
			foreach (DynamicObject item in ParentVessel.World.AllDynamicObjects.Where((DynamicObject m) =>
				         m.Parent is Pivot))
			{
				if (!item.IsKinematic)
				{
					Vector3 vector = item.transform.position - DoorPassageTrigger.transform.position;
					if (vector.sqrMagnitude < 25f)
					{
						Vector3 vector2 = vector.normalized * fIntensity;
						float num = item.Mass > 0f ? item.Mass : 10f;
						item.RigidBody.linearVelocity = Vector3.ClampMagnitude(item.Velocity + vector2 / num * Time.deltaTime, 10f);
					}
				}
			}
		}

		private void AffectInsideDynamicObjects(Vector3 fLine, float fIntensity)
		{
			DynamicObject[] componentsInChildren =
				ParentVessel.TransferableObjectsRoot.GetComponentsInChildren<DynamicObject>();
			foreach (DynamicObject dynamicObject in componentsInChildren)
			{
				if (dynamicObject.IsKinematic || !(dynamicObject.CurrentRoomTrigger != null))
				{
					continue;
				}

				Vector3 vector = dynamicObject.transform.position - DoorPassageTrigger.transform.position;
				if (vector.sqrMagnitude < 25f)
				{
					Vector3 vector2 = Vector3.zero;
					Vector3 vector3 = Vector3.Project(vector, fLine);
					if (dynamicObject.CurrentRoomTrigger == Room1)
					{
						vector2 = ((_airFlowDirection != 1) ? 1 : (-1)) * fIntensity * vector3.normalized;
					}
					else if (dynamicObject.CurrentRoomTrigger == Room2)
					{
						vector2 = ((_airFlowDirection != 2) ? 1 : (-1)) * fIntensity * vector3.normalized;
					}

					Vector3 vector4 = Vector3.ProjectOnPlane(vector, vector2);
					if (vector2.sqrMagnitude > float.Epsilon && (vector4.x + 0.2f > _doorPassageTriggerSize.x / 2f ||
					                                             vector4.x - 0.2f < (0f - _doorPassageTriggerSize.x) /
					                                             2f ||
					                                             vector4.y + 0.2f > _doorPassageTriggerSize.y / 2f ||
					                                             vector4.y - 0.2f < (0f - _doorPassageTriggerSize.y) /
					                                             2f))
					{
						vector2 += 0.1f * fIntensity * -vector4;
					}

					float num = dynamicObject.Mass > 0f ? dynamicObject.Mass : 10f;
					dynamicObject.RigidBody.linearVelocity =
						Vector3.ClampMagnitude(dynamicObject.Velocity + vector2 / num * Time.deltaTime, 10f);
				}
			}
		}

		private void AffectMyPlayer(Vector3 fLine, float fIntensity)
		{
			Vector3 vector = MyPlayer.Instance.transform.position - DoorPassageTrigger.transform.position;
			if (!(vector.sqrMagnitude < 25f) || MyPlayer.Instance.IsLockedToTrigger ||
			    MyPlayer.Instance.rigidBody.isKinematic)
			{
				return;
			}

			Vector3 vector2 = Vector3.zero;
			if (MyPlayer.Instance.CurrentRoomTrigger != null)
			{
				Vector3 vector3 = Vector3.Project(vector, fLine);
				if (MyPlayer.Instance.CurrentRoomTrigger == Room1)
				{
					vector2 = ((_airFlowDirection != 1) ? 1 : (-1)) * fIntensity * vector3.normalized;
				}
				else if (MyPlayer.Instance.CurrentRoomTrigger == Room2)
				{
					vector2 = ((_airFlowDirection != 2) ? 1 : (-1)) * fIntensity * vector3.normalized;
				}

				Vector3 vector4 = Vector3.ProjectOnPlane(vector, vector2);
				if (vector2.sqrMagnitude > float.Epsilon && (vector4.x + 0.3f > _doorPassageTriggerSize.x / 2f ||
				                                             vector4.x - 0.3f < (0f - _doorPassageTriggerSize.x) / 2f ||
				                                             vector4.y + 0.3f > _doorPassageTriggerSize.y / 2f ||
				                                             vector4.y - 0.3f < (0f - _doorPassageTriggerSize.y) / 2f))
				{
					vector2 += 0.1f * fIntensity * -vector4;
				}
			}
			else
			{
				vector2 = vector.normalized * fIntensity;
			}

			if (_airSpeed > 100f && !MyPlayer.Instance.FpsController.IsZeroG &&
			    !MyPlayer.Instance.FpsController.HasTumbled &&
			    (!ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Sprint) ||
			     !MyPlayer.Instance.FpsController.IsGrounded))
			{
				MyPlayer.Instance.FpsController.Tumble();
			}

			float num = (!(MyPlayer.Instance.rigidBody.mass > 0f)) ? 10f : MyPlayer.Instance.rigidBody.mass;
			if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Sprint) && MyPlayer.Instance.FpsController.CanGrabWall)
			{
				num *= 100f;
			}

			MyPlayer.Instance.rigidBody.linearVelocity =
				Vector3.ClampMagnitude(MyPlayer.Instance.rigidBody.linearVelocity + vector2 / num * Time.deltaTime, 10f);
		}

		public void OverrideSafety(bool value)
		{
			_hasOverriddenSafety = value;
		}

		public bool IsSafeToOpen()
		{
			if (_hasOverriddenSafety)
			{
				_hasOverriddenSafety = false;
				return true;
			}

			return IsSafeToOpenWithoutOverride();
		}

		public bool IsSafeToOpenWithoutOverride()
		{
			float num = Mathf.Abs(((!(Room1 == null)) ? Room1.AirPressure : 0f) -
			                      ((!(Room2 == null)) ? Room2.AirPressure : 0f));
			if (num > 0.1f)
			{
				return false;
			}

			return true;
		}

		[Obsolete("Use IsAuthorizedToVessel function instead (this function is misspelled)")]
		public bool IsSubscribedToVessel()
		{
			return IsAuthorizedToVessel();
		}

		public bool IsAuthorizedToVessel()
		{
			return ParentVessel != null && ParentVessel.IsPlayerAuthorized(MyPlayer.Instance);
		}

		[Obsolete("Use IsAuthorizedToVessel function instead (this function is misspelled)")]
		public bool IsAuthorizedToVesselStrict()
		{
			return IsAuthorizedToVessel();
		}

		public bool CanOpenDoor()
		{
			return !IsLocked || IsAuthorizedToVessel();
		}

		public void UpdateDoorUI()
		{
			if (!(MyPlayer.Instance == null) && isActiveAndEnabled && DoorUI != null)
			{
				if (Locked != null)
				{
					Locked.SetActive(IsLocked);
				}

				if (Hazard != null)
				{
					Hazard.SetActive(!IsSafeToOpenWithoutOverride());
				}
			}
		}
	}
}
