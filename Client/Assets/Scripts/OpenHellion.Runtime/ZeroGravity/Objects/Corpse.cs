using System;
using System.Collections.Generic;
using System.Linq;
using OpenHellion;
using UnityEngine;
using ZeroGravity.CharacterMovement;
using ZeroGravity.LevelDesign;
using ZeroGravity.Math;
using ZeroGravity.Network;

namespace ZeroGravity.Objects
{
	public class Corpse : SpaceObjectTransferable, ISlotContainer
	{
		private class CorpsePart
		{
			public Rigidbody RBody;

			public Transform Trans;
		}

		public Inventory Inventory;

		[SerializeField] private RagdollHelper ragdollComponent;

		[SerializeField] private AnimatorHelper animHelper;

		[SerializeField] private SkinnedMeshRenderer headSkin;

		[SerializeField] private Transform outfitTransform;

		[SerializeField] private Transform basicOutfitHolder;

		[SerializeField] private Transform centerOfMass;

		private byte hipsKey = byte.MaxValue;

		private Dictionary<byte, CorpsePart> corpseParts = new Dictionary<byte, CorpsePart>();

		private float _movementReceivedTime = -1f;

		private Vector3 _movementTargetPosition;

		private Quaternion _movementTargetRotation;

		private Vector3 _movementTargetVelocity;

		private Vector3 _movementTargetAngularVelocity;

		private Gender Gender;

		public override SpaceObjectType Type => SpaceObjectType.Corpse;

		public Outfit CurrentOutfit { get; private set; }

		public Rigidbody RigidBody => corpseParts[hipsKey].RBody;

		public bool IsKinematic => RigidBody.isKinematic;

		public override SpaceObject Parent
		{
			get => base.Parent;
			set
			{
				base.Parent = value;
				SetParentTransferableObjectsRoot();
			}
		}

		public static Corpse Create(Corpse template)
		{
			GameObject gameObject = template.Gender != 0
				? Instantiate(Resources.Load("Models/Units/Characters/CharacterCorpseFemale"),
					new Vector3(20000f, 20000f, 20000f), Quaternion.identity) as GameObject
				: Instantiate(Resources.Load("Models/Units/Characters/CharacterCorpse"),
					new Vector3(20000f, 20000f, 20000f), Quaternion.identity) as GameObject;
			gameObject.SetActive(value: false);
			Corpse component = gameObject.GetComponent<Corpse>();
			component.World = template.World;
			component.Gender = template.Gender;
			component.Guid = template.Guid;
			component.animHelper.CreateRig();
			component.InitializeInventory();
			component.ReconnectAllBones();
			component.gameObject.name = "Corpse_" + component.Guid;
			template.World.AddCorpse(component.Guid, component);
			component.Parent = template.Parent;
			component.transform.SetPositionAndRotation(template.transform.position, template.transform.rotation);
			foreach (KeyValuePair<AnimatorHelper.HumanBones, Transform> bone in template.animHelper.GetBones())
			{
				if (component.corpseParts.ContainsKey((byte)bone.Key))
				{
					component.corpseParts[(byte)bone.Key].Trans.rotation = bone.Value.rotation;
				}
			}

			component.SetKinematic(toggle: false);
			gameObject.SetActive(value: true);
			component.ragdollComponent.ToggleRagdoll(enabled: true, component);
			return component;
		}

		public static Corpse Create(World world, long guid, Vector3 position, Quaternion rotation, long parentGUID, Gender gender,
			DynamicObjectDetails[] dynamicObjects)
		{
			if (world.GetCorpse(guid) != null)
			{
				return null;
			}

			if (!world.TryGetSpaceObject(parentGUID, out SpaceObject spaceObject))
			{
				Debug.LogErrorFormat("Cannot spawn corpse (guid: {0}) because there is no corpse parent (guid: {1}).", guid, parentGUID);
				return null;
			}

			GameObject corpseGameObject = gender == 0
				? Instantiate(Resources.Load("Models/Units/Characters/CharacterCorpse"),
					new Vector3(20000f, 20000f, 20000f), Quaternion.identity) as GameObject
				: Instantiate(Resources.Load("Models/Units/Characters/CharacterCorpseFemale"),
					new Vector3(20000f, 20000f, 20000f), Quaternion.identity) as GameObject;
			corpseGameObject.SetActive(value: false);
			Corpse corpse = corpseGameObject.GetComponent<Corpse>();
			corpse.World = world;
			corpse.Gender = gender;
			corpse.Guid = guid;
			corpse.animHelper.CreateRig();
			corpse.InitializeInventory();
			corpse.ReconnectAllBones();
			corpse.gameObject.name = "Corpse_" + corpse.Guid;
			corpse.Parent = spaceObject;
			corpse.transform.SetLocalPositionAndRotation(position, rotation);
			corpse.SetKinematic(toggle: false);
			corpseGameObject.SetActive(value: true);
			corpse.ragdollComponent.ToggleRagdoll(enabled: true, corpse);

			// Register before spawning the loot: each item's attach data names this corpse as its parent
			// and is resolved through World.GetCorpse during ProcessAttachData.
			world.AddCorpse(corpse.Guid, corpse);
			corpse.SpawnInventory(dynamicObjects);

			if (corpse.Inventory.ItemInHands != null)
			{
				Vector3 value = corpse.transform.parent.InverseTransformPoint(corpse.transform.position);
				world.SolarSystem.SendMoveCommand(corpse.Inventory.ItemInHands.DynamicObj, MyPlayer.Instance.Parent, null, value,
					Quaternion.identity, Vector3.zero, Vector3.zero, MyPlayer.Instance.rigidBody.linearVelocity);
			}

			return corpse;
		}

		private void SpawnInventory(DynamicObjectDetails[] dynamicObjects)
		{
			foreach (DynamicObjectDetails details in dynamicObjects ?? Array.Empty<DynamicObjectDetails>())
			{
				DynamicObject.CreateDynamicObject(World, details);
			}
		}

		private void Awake()
		{
			InitializeInventory();
		}

		public void InitializeInventory()
		{
			if (Inventory == null)
			{
				Inventory = new Inventory(this, animHelper);
			}
		}

		public void ReconnectAllBones()
		{
			corpseParts.Clear();
			corpseParts = new Dictionary<byte, CorpsePart>();
			foreach (KeyValuePair<AnimatorHelper.HumanBones, Transform> bone in animHelper.GetBones())
			{
				RagdollCollider component = bone.Value.GetComponent<RagdollCollider>();
				Rigidbody rigidbody = null;
				if (component != null)
				{
					if (hipsKey == byte.MaxValue && bone.Key == AnimatorHelper.HumanBones.Hips)
					{
						hipsKey = (byte)bone.Key;
					}

					component.enabled = true;
					component.CorpseObject = this;
					rigidbody = bone.Value.GetComponent<Rigidbody>();
					if (rigidbody == null)
					{
						Debug.LogError("Missing rigidbody on ragdoll colliders" + component.name + Guid);
						return;
					}

					rigidbody.useGravity = false;
					rigidbody.isKinematic = true;
					Collider component2 = bone.Value.gameObject.GetComponent<Collider>();
					component2.isTrigger = false;
				}

				corpseParts.Add((byte)bone.Key, new CorpsePart
				{
					RBody = rigidbody,
					Trans = bone.Value.transform
				});
			}

			DynamicObject[] componentsInChildren = GetComponentsInChildren<DynamicObject>(includeInactive: true);
			foreach (DynamicObject dynamicObject in componentsInChildren)
			{
				dynamicObject.ToggleKinematic(value: true);
				dynamicObject.ToggleEnabled(isEnabled: true, toggleColliders: true);
			}

			if (Gender == Gender.Female)
			{
				GameObject gameObject =
					Instantiate(Resources.Load("Models/Units/Characters/Hairs/Female/Hair1")) as
						GameObject;
				gameObject.transform.parent = animHelper.GetBone(AnimatorHelper.HumanBones.Head);
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.localScale = Vector3.one;
			}

			SetKinematic(toggle: true);
		}

		private void SetKinematic(bool toggle)
		{
			foreach (KeyValuePair<byte, CorpsePart> corpsePart in corpseParts)
			{
				if (corpsePart.Value.RBody != null)
				{
					corpsePart.Value.RBody.isKinematic = toggle;
				}
			}
		}

		private void RemoveOutfit()
		{
			if (CurrentOutfit != null)
			{
				CurrentOutfit.SetOutfitParent(outfitTransform.GetChildren(), CurrentOutfit.OutfitTrans);
				CurrentOutfit.FoldedOutfitTrans.gameObject.SetActive(value: true);
				return;
			}

			foreach (Transform child in outfitTransform.GetChildren())
			{
				child.parent = basicOutfitHolder;
				child.gameObject.SetActive(value: false);
			}
		}

		public void SetOutfitParent(List<Transform> children, Transform parentTransform, bool activeGeometry)
		{
			foreach (Transform child in children)
			{
				child.parent = parentTransform;
				child.localScale = Vector3.one;
				child.localPosition = Vector3.zero;
				child.localRotation =
					Quaternion.Euler(!(child.name == "Root") ? Vector3.zero : new Vector3(0f, 90f, -90f));
				child.gameObject.SetActive(activeGeometry);
			}
		}

		public void EquipOutfit(Outfit o)
		{
			o.FoldedOutfitTrans.gameObject.SetActive(value: false);
			o.transform.parent = transform;
			RemoveOutfit();
			CurrentOutfit = o;
			SetOutfitParent(o.OutfitTrans.GetChildren(), outfitTransform, activeGeometry: true);
			RefreshOutfitData();
			Inventory.SetOutfit(o);
		}

		public void TakeOffOutfit()
		{
			ragdollComponent.ToggleRagdoll(enabled: false, this);
			RemoveOutfit();
			Destroy(gameObject);
			Create(this);
		}

		private void RefreshHeadBones()
		{
			Transform[] array = new Transform[headSkin.bones.Length];
			Transform bone = animHelper.GetBone(AnimatorHelper.HumanBones.Spine2);
			for (int i = 0; i < headSkin.bones.Length; i++)
			{
				array[i] = bone.FindChildByName(headSkin.bones[i].name);
			}

			headSkin.bones = array;
		}

		public void RefreshOutfitData()
		{
			animHelper.CreateRig();
			ragdollComponent.RefreshRagdollVariables();
			ReconnectAllBones();
			centerOfMass.transform.parent = animHelper.GetBone(AnimatorHelper.HumanBones.Spine2);
			centerOfMass.localScale = Vector3.one;
			centerOfMass.transform.localPosition = new Vector3(-0.133f, 0.014f, 0.001f);
			centerOfMass.transform.localRotation = Quaternion.Euler(97.33099f, -90f, 0.2839966f);
			RefreshHeadBones();
		}

		public InventorySlot GetInventorySlot(short attachedToID)
		{
			return Inventory.GetSlotByID(attachedToID);
		}

		public void AddForce(Vector3 force, ForceMode forceMode)
		{
			if (corpseParts.ContainsKey(hipsKey) && corpseParts[hipsKey].RBody != null)
			{
				AddForce(corpseParts[hipsKey].RBody, force, forceMode);
			}
		}

		public void AddTorque(Vector3 torque, ForceMode forceMode)
		{
			if (corpseParts.ContainsKey(hipsKey) && corpseParts[hipsKey].RBody != null)
			{
				AddTorque(corpseParts[hipsKey].RBody, torque, forceMode);
			}
		}

		public void AddForce(Rigidbody rbody, Vector3 force, ForceMode forceMode)
		{
			rbody.isKinematic = false;
			rbody.AddForce(force, forceMode);
		}

		public void AddTorque(Rigidbody rbody, Vector3 torque, ForceMode forceMode)
		{
			rbody.isKinematic = false;
			rbody.AddTorque(torque, forceMode);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			World.RemoveCorpse(Guid);
		}

		public void ProcessMovementMessage(long parentGuid, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
		{
			if (parentGuid != Parent.Guid)
			{
				return;
			}

			ToggleKinematic(value: true);
			_movementReceivedTime = Time.time;
			_movementTargetPosition = position;
			_movementTargetRotation = rotation;
			_movementTargetVelocity = velocity;
			_movementTargetAngularVelocity = angularVelocity;
		}

		public override void DockedVesselParentChanged(SpaceObjectVessel vessel)
		{
			Parent = vessel;
		}

		public override void OnGravityChanged(Vector3 oldGravity)
		{
		}

		public override void RoomChanged(SceneTriggerRoom prevRoomTrigger)
		{
			base.RoomChanged(prevRoomTrigger);
		}

		public override void EnterVessel(SpaceObjectVessel vessel)
		{
			if (!IsKinematic && !(vessel == null))
			{
				if (Parent is Pivot && Parent != vessel)
				{
					World.RemoveArtificialBody(Parent as ArtificialBody, this);
					Destroy(Parent.gameObject);
				}

				Parent = vessel;
			}
		}

		/// <inheritdoc/>
		public override void ExitVessel(bool forceExit)
		{
			if (!IsKinematic)
			{
				ArtificialBody artificialBody = Parent is not SpaceObjectVessel
					? Parent as ArtificialBody
					: (Parent as SpaceObjectVessel).MainVessel;
				if (artificialBody == null)
				{
					Debug.LogError("Corpse cannot exit vessel, cannot find parents artificial body" + name + Guid);
				}
				else
				{
					Parent = Pivot.Create(SpaceObjectType.CorpsePivot, Guid, artificialBody, isMainObject: false);
				}
			}
		}

		public List<Item> AllItems()
		{
			List<Item> list = new List<Item>();
			if (Inventory != null)
			{
				if (Inventory.Outfit != null)
				{
					foreach (KeyValuePair<short, InventorySlot> inventorySlot in Inventory.Outfit.InventorySlots)
					{
						if (inventorySlot.Value.Item != null)
						{
							list.Add(inventorySlot.Value.Item);
						}
					}
				}

				if (Inventory.HandsSlot != null && Inventory.HandsSlot.Item != null)
				{
					list.Add(Inventory.HandsSlot.Item);
				}
			}

			if (list.Count == 0)
			{
				return null;
			}

			return list;
		}

		private void Update()
		{
			Transform parent = corpseParts[hipsKey].Trans.parent;
			corpseParts[hipsKey].Trans.parent = null;
			transform.position = corpseParts[hipsKey].Trans.position;
			transform.rotation = corpseParts[hipsKey].Trans.rotation;
			corpseParts[hipsKey].Trans.parent = parent;

			float num = Time.time - _movementReceivedTime;
			if (_movementReceivedTime > 0f && num < 1f)
			{
				transform.localPosition = Vector3.Lerp(transform.localPosition, _movementTargetPosition,
					Mathf.Pow(num, 0.5f));
				transform.localRotation = Quaternion.Slerp(transform.localRotation,
					_movementTargetRotation, Mathf.Pow(num, 0.5f));
				RigidBody.linearVelocity = Vector3.Lerp(RigidBody.linearVelocity,
					transform.parent.TransformDirection(_movementTargetVelocity), Mathf.Pow(num, 0.5f));
				RigidBody.angularVelocity = Vector3.Lerp(RigidBody.angularVelocity,
					transform.parent.TransformDirection(_movementTargetAngularVelocity), Mathf.Pow(num, 0.5f));
			}
		}

		public void ToggleKinematic(bool value)
		{
			RigidBody.isKinematic = value;
		}

		public void PushedByMyPlayer(Vector3 relativeVelocity)
		{
			if (IsKinematic)
			{
				AddForce(relativeVelocity, ForceMode.VelocityChange);
			}
		}

		public InventorySlot GetSlotByID(short id)
		{
			switch (id)
			{
				case -1:
					return Inventory.HandsSlot;
				case -2:
					return Inventory.OutfitSlot;
				default:
					if (CurrentOutfit != null)
					{
						return CurrentOutfit.GetSlotByID(id);
					}

					return null;
			}
		}

		public Dictionary<short, InventorySlot> GetAllSlots()
		{
			InventorySlot inventorySlot =
				CurrentOutfit.InventorySlots.Values.FirstOrDefault((InventorySlot m) =>
					m.SlotGroup == InventorySlot.Group.Jetpack);
			InventorySlot inventorySlot2 =
				CurrentOutfit.InventorySlots.Values.FirstOrDefault((InventorySlot m) =>
					m.SlotGroup == InventorySlot.Group.Helmet);
			Dictionary<short, InventorySlot> dictionary = new Dictionary<short, InventorySlot>
			{
				{ -1, Inventory.HandsSlot },
				{ -2, Inventory.OutfitSlot },
				{ inventorySlot.SlotID, inventorySlot },
				{ inventorySlot2.SlotID, inventorySlot2 }
			};
			return dictionary;
		}

		public Dictionary<short, InventorySlot> GetSlotsByGroup(InventorySlot.Group group)
		{
			return (from m in GetAllSlots()
				where m.Value.SlotGroup == @group
				select m).ToDictionary((KeyValuePair<short, InventorySlot> k) => k.Key,
				(KeyValuePair<short, InventorySlot> v) => v.Value);
		}
	}
}
