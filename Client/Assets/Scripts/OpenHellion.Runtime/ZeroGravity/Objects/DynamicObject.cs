using OpenHellion.Net.Message;
using System;
using System.Collections.Generic;
using OpenHellion;
using OpenHellion.Net;
using UnityEngine;
using ZeroGravity.CharacterMovement;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;

namespace ZeroGravity.Objects
{
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(TransitionTriggerHelper))]
	public class DynamicObject : SpaceObjectTransferable
	{
		[NonSerialized] public Rigidbody RigidBody;

		private GameObject _collisionDetector;

		// Local physics permission.
		public bool Master = true;

		private float _velocityCheckTimer;

		private float _movementReceivedTime = -1f;

		private Vector3 _movementTargetPosition;

		private Quaternion _movementTargetRotation;

		private Vector3 _movementTargetVelocity;

		private Vector3 _movementTargetAngularVelocity;

		[HideInInspector] public Item Item;

		private readonly List<Collider> _collidersWithTriggerChanged = new List<Collider>();

		public override SpaceObjectType Type => SpaceObjectType.DynamicObject;

		public float Diameter { get; private set; }

		public float Mass => RigidBody.mass;

		public override Vector3 Velocity
		{
			get => RigidBody.linearVelocity;
		}

		public Vector3 AngularVelocity
		{
			get => RigidBody.angularVelocity;
			set
			{
				if (Master)
				{
					RigidBody.angularVelocity = value;
				}
			}
		}

		public bool IsKinematic => RigidBody.isKinematic;

		public bool IsAttached =>
			Item != null && (Item.InvSlot != null || Item.AttachPoint != null || Parent is DynamicObject);

		public override SpaceObject Parent
		{
			get => base.Parent;
			set
			{
				base.Parent = value;
				if (GetParent<MyPlayer>() != null)
				{
					Master = true;
				}
			}
		}

		private void Awake()
		{
			if (TransitionTrigger == null)
			{
				TransitionTrigger = GetComponent<TransitionTriggerHelper>();
			}

			if (TransitionTrigger == null)
			{
				Debug.LogError("Transition trigger not set for dynamic object" + name + gameObject.scene);
			}

			gameObject.SetLayerRecursively(LayerMask.NameToLayer("DynamicObject"), "FirstPerson", "Triggers");
			RigidBody = GetComponent<Rigidbody>();
			RigidBody.useGravity = false;
			RigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			Item = GetComponent<Item>();
		}

		private void Update()
		{
			if (!IsKinematic)
			{
				// We are simulating locally (a freshly dropped/thrown/bumped item).
				if (AngularVelocity.IsEpsilonEqual(Vector3.zero, 0.5f) && Velocity.IsEpsilonEqual(Vector3.zero, 0.1f))
				{
					_velocityCheckTimer += Time.deltaTime;
					if (_velocityCheckTimer > 1f)
					{
						ToggleKinematic(value: true);
					}
				}
				else
				{
					_velocityCheckTimer = 0f;
				}
			}
			else if (_movementReceivedTime > 0f)
			{
				// Server owns this object: ease toward the last streamed position and velocity.
				float num = Time.realtimeSinceStartup - _movementReceivedTime;
				if (num < 1f)
				{
					transform.SetLocalPositionAndRotation(
						Vector3.Lerp(transform.localPosition, _movementTargetPosition, Mathf.Pow(num, 0.5f)),
						Quaternion.Slerp(transform.localRotation, _movementTargetRotation, Mathf.Pow(num, 0.5f)));
					RigidBody.linearVelocity = Vector3.Lerp(RigidBody.linearVelocity,
						transform.parent.TransformDirection(_movementTargetVelocity), Mathf.Pow(num, 0.5f));
					RigidBody.angularVelocity = Vector3.Lerp(RigidBody.angularVelocity,
						transform.parent.TransformDirection(_movementTargetAngularVelocity), Mathf.Pow(num, 0.5f));
				}
			}
		}

		private void FixedUpdate()
		{
			if (IsDestroying || Guid == 0 || IsAttached)
			{
				return;
			}

			if (IsInsideSpaceObject && Gravity.IsNotEpsilonZero() && !IsKinematic)
			{
				RigidBody.linearVelocity += Gravity * Time.fixedDeltaTime;
			}
		}

		// The server is authoritative for any object not currently held in an inventory/attach slot.
		// Receiving a position hands control back to it: drop local ownership and follow the stream.
		public void ProcessMovementMessage(long parentGuid, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
		{
			if (!IsAttached && parentGuid == Parent.Guid)
			{
				Master = false;
				ToggleKinematic(value: true);
				_movementReceivedTime = Time.realtimeSinceStartup;
				_movementTargetPosition = position;
				_movementTargetRotation = rotation;
				_movementTargetVelocity = velocity;
				_movementTargetAngularVelocity = angularVelocity;
			}
		}

		private bool AreAttachDataSame(DynamicObjectAttachData data)
		{
			return Parent.Type == data.ParentType && Parent.Guid == data.ParentGUID && IsAttached == data.IsAttached;
		}

		public void ApplyState(DynamicObjectStats stats, DynamicObjectAttachData attachData)
		{
			if (stats != null && Item != null)
			{
				Item.ProcesStatsData(stats);
			}

			if (attachData == null)
			{
				return;
			}

			if ((Item != null && Item.AreAttachDataSame(attachData)) ||
			    (Item == null && AreAttachDataSame(attachData)))
			{
				return;
			}

			SpaceObject prevParent = Parent;
			if (attachData.ParentType == SpaceObjectType.DynamicObjectPivot)
			{
				ArtificialBody parent = GetParent<ArtificialBody>();
				if (parent is SpaceObjectVessel parentVessel)
				{
					parent = parentVessel.MainVessel;
				}

				if (parent == null)
				{
					Debug.LogError("Dynamic object exited vessel but we don't know from where. " + Guid + Parent +
						attachData.ParentType + attachData.ParentGUID);
					return;
				}

				if (!World.TryGetSpaceObject(Guid, out Pivot pivot))
				{
					pivot = Pivot.Create(SpaceObjectType.DynamicObjectPivot, Guid, parent, isMainObject: false);
				}

				bool myPlayerIsParent = Parent is MyPlayer;
				if (Item != null)
				{
					Item.AttachToObject(pivot, sendAttachMessage: false);
				}
				else
				{
					Parent = pivot;
					SetParentTransferableObjectsRoot();
					ResetRoomTriggers();
					ToggleActive(isActive: true);
					ToggleEnabled(isEnabled: true, toggleColliders: true);
				}

				Action task = new Action(delegate
				{
					if (!myPlayerIsParent || !Master)
					{
						if (attachData.LocalPosition != null)
						{
							transform.localPosition = attachData.LocalPosition.ToVector3();
						}

						if (attachData.LocalRotation != null)
						{
							transform.localRotation = attachData.LocalRotation.ToQuaternion();
						}
					}

					if (Master)
					{
						if (attachData.Velocity != null)
						{
							RigidBody.linearVelocity = attachData.Velocity.ToVector3();
						}

						if (attachData.Torque != null)
						{
							AddTorque(attachData.Torque.ToVector3(), ForceMode.Impulse);
						}

						if (attachData.ThrowForce != null)
						{
							Vector3 vector = attachData.ThrowForce.ToVector3();
							if ((MyPlayer.Instance.CurrentRoomTrigger == null ||
							     !MyPlayer.Instance.CurrentRoomTrigger.UseGravity ||
							     MyPlayer.Instance.CurrentRoomTrigger.GravityForce == Vector3.zero) &&
							    prevParent == MyPlayer.Instance)
							{
								float num = MyPlayer.Instance.rigidBody.mass + Mass;
								AddForce(vector * (MyPlayer.Instance.rigidBody.mass / num), ForceMode.VelocityChange);
								MyPlayer.Instance.rigidBody.AddForce(-vector * (Mass / num), ForceMode.VelocityChange);
							}
							else
							{
								AddForce(vector, ForceMode.Impulse);
							}
						}
					}
				});
				if (Parent is MyPlayer && MyPlayer.Instance.AnimHelper.DropTask != null)
				{
					MyPlayer.Instance.AnimHelper.AfterDropTask = task;
				}
				else
				{
					task();
				}
			}
			else if (Parent is Pivot && (attachData.ParentType == SpaceObjectType.Ship ||
			                             attachData.ParentType == SpaceObjectType.Station ||
			                             attachData.ParentType == SpaceObjectType.Asteroid))
			{
				if (!(Parent is Pivot))
				{
					Debug.LogError("Entered vessel but we don't know from where." + Guid + Parent +
						attachData.ParentType + attachData.ParentGUID);
					return;
				}

				World.RemoveArtificialBody(Parent as ArtificialBody, this);
				Destroy(Parent.gameObject);
				Parent = World.GetVessel(attachData.ParentGUID);
				if (Item != null)
				{
					Item.AttachToObject(Parent, sendAttachMessage: false);
					return;
				}

				transform.parent = Parent.TransferableObjectsRoot.transform;
				ResetRoomTriggers();
				ToggleActive(isActive: true);
				ToggleEnabled(isEnabled: true, toggleColliders: true);
			}
			else if (Item != null)
			{
				Item.ProcessAttachData(attachData, prevParent);
			}
			else
			{
				Debug.LogWarning($"Attach data for '{Guid}' matched no branch, nothing was applied.");
			}
		}

		public void ResetRoomTriggers()
		{
			TransitionTrigger.ResetTriggers();
		}

		public void ToggleKinematic(bool value)
		{
			if (!Master && !value)
			{
				value = true;
			}

			if (!value)
			{
				_velocityCheckTimer = 0f;
			}

			RigidBody.isKinematic = value;
		}

		public void ToggleEnabled(bool isEnabled, bool toggleColliders)
		{
			enabled = isEnabled;
			TransitionTrigger.enabled = isEnabled;
			if (toggleColliders || isEnabled)
			{
				if ((bool)OnPlatform && !isEnabled)
				{
					OnPlatform.RemoveFromPlatform(this);
				}

				if (Item != null && Item.CustomCollidereToggle(isEnabled))
				{
					return;
				}

				Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
				foreach (Collider collider in componentsInChildren)
				{
					collider.enabled = isEnabled;
				}
			}

			if (_collisionDetector != null)
			{
				_collisionDetector.SetActive(isEnabled && !IsAttached);
			}
		}

		public void ToggleTriggerColliders(bool areCollidersTrigger)
		{
			if (areCollidersTrigger)
			{
				Collider[] componentsInChildren = Item.GetComponentsInChildren<Collider>();
				foreach (Collider collider in componentsInChildren)
				{
					if (!collider.isTrigger)
					{
						if (!_collidersWithTriggerChanged.Contains(collider))
						{
							_collidersWithTriggerChanged.Add(collider);
						}

						collider.isTrigger = true;
					}
				}
			}
			else
			{
				if (_collidersWithTriggerChanged.Count <= 0)
				{
					return;
				}

				foreach (Collider item in _collidersWithTriggerChanged)
				{
					item.isTrigger = false;
				}

				_collidersWithTriggerChanged.Clear();
			}
		}

		public void ToggleActive(bool isActive)
		{
			gameObject.SetActive(isActive);
			TransitionTrigger.enabled = isActive;
			if (_collisionDetector != null)
			{
				_collisionDetector.SetActive(isActive && !IsAttached);
			}
		}

		public void AddForce(Vector3 force, ForceMode forceMode)
		{
			if (Master && !IsAttached)
			{
				if (IsKinematic)
				{
					ToggleKinematic(value: false);
				}

				RigidBody.AddForce(force, forceMode);
			}
		}

		public void AddTorque(Vector3 torque)
		{
			if (Master && !IsAttached)
			{
				if (IsKinematic)
				{
					ToggleKinematic(value: false);
				}

				RigidBody.AddTorque(torque);
			}
		}

		public void AddTorque(Vector3 torque, ForceMode forceMode)
		{
			if (Master && !IsAttached && !IsKinematic)
			{
				RigidBody.AddTorque(torque, forceMode);
			}
		}

		public static DynamicObject CreateDynamicObject(World world, DynamicObjectDetails details)
		{
			if (!StaticData.DynamicObjectsDataList.TryGetValue(details.ItemID, out DynamicObjectData data))
			{
				return null;
			}

			SpaceObject parent = world.GetObject(details.AttachData.ParentGUID, details.AttachData.ParentType);
			DynamicObject dynamicObject = world.GetObject(details.GUID, SpaceObjectType.DynamicObject) as DynamicObject;
			bool reused = dynamicObject != null;
			try
			{
				if (dynamicObject == null)
				{
					UnityEngine.Object prefab = Resources.Load(data.PrefabPath);
					if (prefab == null)
					{
						Debug.LogErrorFormat("Could not find requested prefab on path {0}", data.PrefabPath);
						return dynamicObject;
					}
					GameObject gameObject = Instantiate(prefab,
						new Vector3(20000f, 20000f, 20000f), Quaternion.identity) as GameObject;
					gameObject.SetActive(value: false);
					dynamicObject = gameObject.GetComponent<DynamicObject>();
					dynamicObject.World = world;
					dynamicObject.tag = "Untagged";
					dynamicObject.Guid = details.GUID;
					dynamicObject.name = "DynamicObject_" + details.GUID;
					gameObject.SetActive(value: true);
				}

				if (dynamicObject.Item != null)
				{
					if (details.AttachData != null)
					{
						dynamicObject.Item.ProcessAttachData(details.AttachData);
					}

					if (details.StatsData != null)
					{
						dynamicObject.Item.ProcesStatsData(details.StatsData);
					}
				}

				dynamicObject.Parent = parent;
				if (!dynamicObject.IsAttached && parent is ArtificialBody)
				{
					dynamicObject.SetParentTransferableObjectsRoot();
					dynamicObject.transform.SetLocalPositionAndRotation(details.LocalPosition.ToVector3(), details.LocalRotation.ToQuaternion());
					dynamicObject.RigidBody.linearVelocity = dynamicObject.transform.parent.TransformDirection(details.Velocity.ToVector3());
					dynamicObject.RigidBody.angularVelocity = dynamicObject.transform.parent.TransformDirection(details.AngularVelocity.ToVector3());
				}

				world.AddDynamicObject(details.GUID, dynamicObject);

				return dynamicObject;
			}
			catch (Exception ex)
			{
				Debug.LogErrorFormat("Failed to create dynamic object {0}, path {1}: {2}", details.GUID,
					data.PrefabPath, ex);
				if (reused)
				{
					return dynamicObject;
				}

				if (dynamicObject != null)
				{
					Destroy(dynamicObject.gameObject);
				}

				return null;
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (World != null)
			{
				World.RemoveDynamicObject(Guid, this);

				// Only the object leaving the hands changes what the hands slot shows.
				if (MyPlayer.Instance != null && Item != null && Item.Slot == MyPlayer.Instance.Inventory.HandsSlot)
				{
					World.InGameGUI.HelmetHud.HandsSlotUpdate();
				}
			}

			CheckNearbyObjects();
		}

		public override void EnterVessel(SpaceObjectVessel vessel)
		{
			if (!IsAttached)
			{
				bool parentChanged = Parent != vessel;
				if (Parent is Pivot && parentChanged)
				{
					World.RemoveArtificialBody(Parent as ArtificialBody, this);
					Destroy(Parent.gameObject);
				}

				Parent = vessel;
				transform.parent = vessel.TransferableObjectsRoot.transform;

				// The server only learns about vessel membership from us, the same way ExitVessel and
				// DockedVesselParentChanged report it.
				if (parentChanged)
				{
					World.SolarSystem.SendCommand(StateUpdateRequest.CommandType.Relocate, Guid, vessel.Guid, position: transform.localPosition, rotation: transform.localRotation);
				}
			}
		}

		/// <inheritdoc/>
		public override void ExitVessel(bool forceExit)
		{
			if (Parent is Pivot { Type: SpaceObjectType.DynamicObjectPivot } || (IsAttached && !forceExit))
			{
				return;
			}

			ArtificialBody artificialBody = GetParent<ArtificialBody>();
			if (artificialBody is SpaceObjectVessel parentVessel)
			{
				artificialBody = parentVessel.MainVessel;
			}

			if (artificialBody == null)
			{
				Debug.LogErrorFormat("Cannot exit vessel, cannot find parents artificial body {0}, {1}", name, Guid);
				return;
			}

			Parent = Pivot.Create(SpaceObjectType.DynamicObjectPivot, Guid, artificialBody, isMainObject: false);
			SetParentTransferableObjectsRoot();
			World.SolarSystem.SendCommand(StateUpdateRequest.CommandType.Relocate, Guid, Parent.Guid, position: transform.localPosition, rotation: transform.localRotation);
		}

		public override void DockedVesselParentChanged(SpaceObjectVessel vessel)
		{
			if (IsAttached)
			{
				Debug.LogErrorFormat("Attached object changed parent {0}, {1}, {2}, {3}", Parent.Guid, Parent.Type, vessel.Guid, vessel.Type);
			}

			Parent = vessel;
			transform.parent = vessel.TransferableObjectsRoot.transform;
			World.SolarSystem.SendCommand(StateUpdateRequest.CommandType.Relocate, Guid, vessel.Guid, position: transform.localPosition, rotation: transform.localRotation);
		}

		public override void OnGravityChanged(Vector3 oldGravity)
		{
			if (oldGravity != Vector3.zero && Gravity.IsEpsilonEqual(Vector3.zero))
			{
				AddForce(
					new Vector3(UnityEngine.Random.Range(0.001f, 0.05f), UnityEngine.Random.Range(0.001f, 0.05f),
						UnityEngine.Random.Range(0.001f, 0.05f)), ForceMode.Impulse);
				AddTorque(new Vector3(UnityEngine.Random.Range(0.001f, 0.05f), UnityEngine.Random.Range(0.001f, 0.05f),
					UnityEngine.Random.Range(0.001f, 0.05f)));
			}
		}

		private void OnCollisionEnter(Collision coli)
		{
			if (!IsAttached && IsKinematic)
			{
				ToggleKinematic(value: false);
				SpaceObjectTransferable componentInParent =
					coli.gameObject.GetComponentInParent<SpaceObjectTransferable>();
				if (componentInParent is MyPlayer)
				{
					Master = true;
				}
				else if (componentInParent is DynamicObject && (componentInParent as DynamicObject).Master)
				{
					Master = true;
				}

				AddForce(coli.relativeVelocity, ForceMode.VelocityChange);
			}
		}

		public override void RoomChanged(SceneTriggerRoom prevRoomTrigger)
		{
			base.RoomChanged(prevRoomTrigger);
		}

		public void CheckNearbyObjects(HashSet<DynamicObject> alreadyTraversed = null)
		{
			if (alreadyTraversed == null)
			{
				alreadyTraversed = new HashSet<DynamicObject>();
			}

			if (!alreadyTraversed.Add(this))
			{
				return;
			}

			Renderer componentInChildren = GetComponentInChildren<Renderer>();
			if (componentInChildren == null)
			{
				return;
			}

			Collider[] array =
				Physics.OverlapSphere(transform.position, componentInChildren.bounds.size.magnitude);
			foreach (Collider collider in array)
			{
				DynamicObject componentInParent = collider.GetComponentInParent<DynamicObject>();
				if (componentInParent != null && !componentInParent.IsAttached && componentInParent.IsKinematic)
				{
					ToggleKinematic(value: false);
					componentInParent.CheckNearbyObjects(alreadyTraversed);
				}
			}
		}
	}
}
