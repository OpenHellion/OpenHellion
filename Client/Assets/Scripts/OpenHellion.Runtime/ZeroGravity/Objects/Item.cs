using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using OpenHellion;
using OpenHellion.IO;
using OpenHellion.Net;
using UnityEngine;
using UnityEngine.Serialization;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.UI;

namespace ZeroGravity.Objects
{
	[RequireComponent(typeof(DynamicObject))]
	public class Item : MonoBehaviour, IDamageable
	{
		[Serializable]
		public class ItemIngredient
		{
			public ResourceType ResourceType;

			public float Quantity;
		}

		[Serializable]
		public class ItemIngredientLists
		{
			public ItemIngredient[] Recycle;

			public ItemIngredient[] Craft;
		}

		public enum SizeType
		{
			None,
			Rifle,
			Pistol,
			SmallItem,
			Barrel,
			Box
		}

		public enum EquipType
		{
			None,
			Hands,
			EquipInventory,
			Inventory
		}

		public enum Hand
		{
			Left = 1,
			Right
		}

		public enum ItemAnimationType
		{
			Equip = 1,
			Unequip
		}

		public enum ItemSound
		{
			Reload
		}

		public bool DefaultBlueprint;

		public ItemCategory Category;

		public ItemType Type;

		public List<ItemType> ReloadWithTypes;

		[Title("General")] public string Name = string.Empty;

		[HideInInspector] public string ToolTip = string.Empty;

		public bool CanGoDirectlyToInventory = true;

		public Dictionary<short, ItemSlot> Slots = new Dictionary<short, ItemSlot>();

		public bool IsSlotContainer;

		private IItemSlot _Slot;

		[SerializeField] [Range(1f, 4f)] private int _Tier = 1;

		public float[] TierMultipliers;

		public float[] AuxValues;

		[ContextMenuItem("apply", "ApplyTierColor")]
		public Color[] TierColors;

		[ContextMenuItem("apply", "ApplyTierColor")]
		public GameObject[] TierGameObjects;

		public ItemIngredientLists[] Ingredients;

		[SerializeField] [Space(10f)] public string ShaderProperty = "_Mat1Color1";

		[HideInInspector] public DynamicObject DynamicObj;

		public float PassiveSpeedMultiplier = 1f;

		public float ActiveSpeedMultiplier = 1f;

		public float SpecialSpeedMultiplier = 1f;

		public bool HasActiveStance;

		public bool HasSpecialStance;

		public bool NeedsFullAnimOverride;

		public bool useIkForTargeting;

		public bool forceAnimOverride;

		public bool useSwayAnimations;

		public bool HasMelee;

		public float MeleeDamage;

		public float MeleeRange;

		[Title("ITEM DESTRUCTION")] public bool DestructAt0HP;

		[FormerlySerializedAs("BlastRadius")] public float ExplosionRadius;

		[FormerlySerializedAs("Damage")] public float ExplosionDamage;

		[FormerlySerializedAs("DamageType")] public TypeOfDamage ExplosionDamageType;

		[FormerlySerializedAs("BlastImpulse")] public float ExplosionImpulse;

		[FormerlySerializedAs("Effects")] public ParticleSystem ExplosionEffects;

		public SoundEffect ExplosionSound;

		[Space(10f)] public Transform ikTargetingPoint;

		[SerializeField] private float _MaxHealth = 100f;

		[SerializeField] private float _Health = 100f;

		[SerializeField] private float _Armor;

		[SerializeField] private bool _Damageable;

		[SerializeField] private bool _Expendable;

		[SerializeField] private bool _Repairable;

		[SerializeField] [Tooltip("HP per second")]
		private float _UsageWear;

		[Space(10f)] public Renderer HealthIndicator;

		public int MaterialIndex;

		public Color FullHealthColor = new Color(1f, 1f, 1f, 1f);

		public float FullHealthIntensity = 20f;

		public Color MidHealthColor = new Color(0.5f, 0.5f, 0.5f, 1f);

		public float MidHealthIntensity = 10f;

		public Color NoHealthColor = new Color(0f, 0f, 0f, 1f);

		public float NoHealthIntensity;

		public AnimationCurve HealthIndicatorCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

		[Range(0f, 1f)] public float TestHealthPercentage = 1f;

		[Title("SOUND")] public SoundEffect ThrowSound;

		public SoundEffect PickupSound;

		private Vector3? sendVelocity;

		private Vector3? sendTorque;

		private Vector3? sendThrowForce;

		[Title("ANIMATIONS")] public ItemAnimations ItemAnimations;

		[HideInInspector] public ItemAnimations.FPSAnimations fpsAnimations;

		[HideInInspector] public ItemAnimations.TPSAnimations tpsAnimations;

		private float prevQuantity;

		private bool exploded;

		private int explosionRaycastMask;

		protected World World => DynamicObj.World;

		public Sprite Icon
		{
			get
			{
				if (this is GenericItem)
				{
					return SpriteManager.Instance.GetSprite((this as GenericItem).SubType);
				}

				if (this is MachineryPart)
				{
					return SpriteManager.Instance.GetSprite((this as MachineryPart).PartType);
				}

				return SpriteManager.Instance.GetSprite(Type);
			}
		}

		public virtual float Quantity => 0f;

		public virtual float MaxQuantity => 0f;

		public IItemSlot Slot
		{
			get => _Slot;
			set
			{
				UpdateUI();
				_Slot = value;
				UpdateUI();
			}
		}

		public int Tier
		{
			get => _Tier;
			set
			{
				_Tier = value;
				ApplyTierColor();
			}
		}

		public float AuxValue
		{
			get
			{
				if (Tier < 1 || Tier > AuxValues.Length)
				{
					return 1f;
				}

				return AuxValues[Tier - 1];
			}
		}

		public float TierMultiplier
		{
			get
			{
				if (Tier < 1 || Tier > TierMultipliers.Length)
				{
					return 1f;
				}

				return TierMultipliers[Tier - 1];
			}
		}

		public long GUID => DynamicObj.Guid;

		public bool IsInsideSpaceObject => DynamicObj.IsInsideSpaceObject;

		public BaseSceneAttachPoint AttachPoint
		{
			get => !(_Slot is BaseSceneAttachPoint) ? null : _Slot as BaseSceneAttachPoint;
			private set => Slot = value;
		}

		public InventorySlot InvSlot
		{
			get => !(_Slot is InventorySlot) ? null : _Slot as InventorySlot;
			private set => Slot = value;
		}

		public short InvSlotID => (short)(InvSlot == null ? -1111 : InvSlot.SlotID);

		public virtual Transform TipOfItem => null;

		public virtual bool IsInvetoryEquipable => false;

		public virtual EquipType EquipTo => EquipType.Hands;

		public float MaxHealth
		{
			get => _MaxHealth;
			set => _MaxHealth = !(value < 0f) ? value : 0f;
		}

		public float Health
		{
			get => _Health;
			set => _Health = value > MaxHealth ? MaxHealth : !(value < 0f) ? value : 0f;
		}

		public float Armor
		{
			get => _Armor;
			set => _Armor = !(value < 0f) ? value : 0f;
		}

		public bool Damageable => _Damageable;

		public bool Expendable => _Expendable;

		public bool Repairable => _Repairable;

		public float UsageWear
		{
			get => _UsageWear;
			set => _UsageWear = !(value < 0f) ? value : 0f;
		}

		public string TypeName
		{
			get
			{
				if (this is GenericItem)
				{
					return (this as GenericItem).SubType.ToString();
				}

				if (this is MachineryPart)
				{
					return (this as MachineryPart).PartType.ToString();
				}

				return Type.ToString();
			}
		}

		public ItemCompoundType CompoundType
		{
			get
			{
				ItemCompoundType itemCompoundType;
				if (Type == ItemType.GenericItem)
				{
					itemCompoundType = new ItemCompoundType();
					itemCompoundType.Type = Type;
					itemCompoundType.SubType = (this as GenericItem).SubType;
					itemCompoundType.PartType = MachineryPartType.None;
					itemCompoundType.Tier = Tier;
					return itemCompoundType;
				}

				if (Type == ItemType.MachineryPart)
				{
					itemCompoundType = new ItemCompoundType();
					itemCompoundType.Type = Type;
					itemCompoundType.SubType = GenericItemSubType.None;
					itemCompoundType.PartType = (this as MachineryPart).PartType;
					itemCompoundType.Tier = Tier;
					return itemCompoundType;
				}

				itemCompoundType = new ItemCompoundType();
				itemCompoundType.Type = Type;
				itemCompoundType.SubType = GenericItemSubType.None;
				itemCompoundType.PartType = MachineryPartType.None;
				itemCompoundType.Tier = Tier;
				return itemCompoundType;
			}
		}

		public virtual void UpdateUI()
		{
			if (_Slot is BaseSceneAttachPoint)
			{
				if ((_Slot as BaseSceneAttachPoint).UI != null)
				{
					(_Slot as BaseSceneAttachPoint).UI.UpdateSlot();
				}
			}
			else if (_Slot is InventorySlot && (Slot as InventorySlot).Parent is MyPlayer &&
			         World.InGameGUI.PlayerOverview.Inventory.gameObject.activeInHierarchy)
			{
				(_Slot as InventorySlot).UI.UpdateSlot();
			}
		}

		protected virtual void Awake()
		{
			Slots = GetComponentsInChildren<ItemSlot>().ToDictionary((ItemSlot k) => k.ID, (ItemSlot v) => v);
			foreach (ItemSlot value in Slots.Values)
			{
				value.Parent = DynamicObj;
			}

			if (this is IBatteryConsumer)
			{
				(this as IBatteryConsumer).BatterySlot = Slots.FirstOrDefault((KeyValuePair<short, ItemSlot> m) =>
					m.Value.ItemTypes.Contains(ItemType.AltairHandDrillBattery)).Value;
			}

			DynamicObj = GetComponent<DynamicObject>();
			if (Type == ItemType.MachineryPart)
			{
				MachineryPart machineryPart = this as MachineryPart;
				Name = machineryPart.PartName;
				ToolTip = machineryPart.PartDescription;
			}
			else if (Type == ItemType.GenericItem)
			{
				GenericItem genericItem = this as GenericItem;
				Name = genericItem.ItemName;
				ToolTip = genericItem.ItemDescription;
			}
			else
			{
				Name = Type.ToLocalizedString();
				Localization.ItemsDescriptions.TryGetValue(Type, out ToolTip);
				if (ToolTip == null)
				{
					ToolTip = Name;
				}
			}

			if (ItemAnimations != null)
			{
				tpsAnimations = ItemAnimations.TPS;
				fpsAnimations = ItemAnimations.FPS;
			}
		}

		protected virtual void Start()
		{
			explosionRaycastMask = (1 << LayerMask.NameToLayer("Default")) |
			                       (1 << LayerMask.NameToLayer("DynamicObject")) |
			                       (1 << LayerMask.NameToLayer("Player")) |
			                       (1 << LayerMask.NameToLayer("RepairTriggers"));
		}

		public virtual void AttachToObject(InventorySlot slot, bool sendAttachMessage)
		{
			InventorySlot.AttachData attachPoint = slot.GetAttachPoint(this);
			if (attachPoint != null)
			{
				AttachToObjectImpl(slot.Parent, attachPoint.Point.transform, slot, null, attachPoint.HideAttachedObject,
					sendAttachMessage);
				return;
			}

			Debug.LogErrorFormat("Cannot attach item to slot because there is no attach point for it {0}, id {1}", name, slot.SlotID);
		}

		public virtual void AttachToObject(BaseSceneAttachPoint attachPoint, bool hideObject, bool sendAttachMessage)
		{
			AttachToObjectImpl(attachPoint.ParentVessel, attachPoint.GetAttachPointTransform(this), null, attachPoint,
				hideObject, sendAttachMessage);
		}

		public virtual void AttachToObject(ItemSlot slot, bool sendAttachMessage)
		{
			AttachToObjectImpl(slot.Parent, slot.ItemPlacement, slot, null, slot.ItemPlacement == slot.transform,
				sendAttachMessage);
		}

		public virtual void AttachToObject(SpaceObject obj, bool sendAttachMessage)
		{
			if (obj != null && (obj is Pivot || obj is SpaceObjectVessel))
			{
				AttachToObjectImpl(obj, null, null, null, hideObject: false, sendAttachMessage);
				return;
			}

			Debug.LogErrorFormat("Cannot attach item to object, {0}, {1}, {2}, {3},", name, GUID, obj, obj.Guid);
		}

		public virtual void AttachToBone(Player pl, AnimatorHelper.HumanBones bone, bool resetTransform = true,
			bool hide = false)
		{
			OnAttach(isAttached: true, isOnPlayer: true);
			DynamicObj.transform.parent = pl.AnimHelper.GetBone(bone);
			DynamicObj.ToggleKinematic(value: true);
			DynamicObj.ToggleActive(!hide);
			DynamicObj.ToggleEnabled(isEnabled: false, toggleColliders: true);
			if (resetTransform)
			{
				DynamicObj.transform.Reset(resetScale: true);
			}
			else
			{
				DynamicObj.transform.localScale = Vector3.one;
			}
		}

		protected virtual void AttachToObjectImpl(SpaceObject obj, Transform attachToTrans, IItemSlot slot,
			BaseSceneAttachPoint attachPoint, bool hideObject, bool sendAttachMessage)
		{
			bool flag = DynamicObj.Parent is Player && (DynamicObj.Parent as Player).Inventory.ItemInHands == this;
			Action task = new Action(delegate
			{
				IItemSlot slot2 = Slot;
				bool flag2 = attachToTrans != null;
				if (DynamicObj.Parent is Pivot && DynamicObj.Parent != obj)
				{
					World.RemoveArtificialBody(DynamicObj.Parent as ArtificialBody, DynamicObj);
					Destroy(DynamicObj.Parent.gameObject);
				}

				if (AttachPoint != null && AttachPoint != attachPoint)
				{
					AttachPoint.DetachItem(this);
					AttachPoint = null;
				}

				if (DynamicObj.Parent is Player && (attachPoint != null || !flag2))
				{
					Player player = DynamicObj.Parent as Player;
					if (player.Inventory.ItemInHands == this)
					{
						player.Inventory.RemoveItemFromHands(resetStance: true);
					}

					ChangeEquip(EquipType.None, player);
				}

				if (InvSlot != null && InvSlot != slot)
				{
					if (InvSlot.Item == this)
					{
						if (InvSlot.Inventory != null && InvSlot.Inventory.Parent is Player &&
						    InvSlot.SlotType == InventorySlot.Type.Hands)
						{
							InvSlot.Inventory.RemoveItemFromHands(resetStance: true);
						}
						else
						{
							InvSlot.SetItem(null);
						}
					}

					InvSlot = null;
				}

				if (attachPoint != null)
				{
					AttachPoint = attachPoint;
				}

				if (slot != null)
				{
					if (slot is InventorySlot)
					{
						InvSlot = slot as InventorySlot;
						if ((slot as InventorySlot).Inventory != null &&
						    (slot as InventorySlot).Inventory.Parent is SpaceObjectTransferable)
						{
							SpaceObjectTransferable spaceObjectTransferable =
								(slot as InventorySlot).Inventory.Parent as SpaceObjectTransferable;
						}
					}
					else if (slot is ItemSlot)
					{
						(slot as ItemSlot).FitItem(this);
					}
				}

				SpaceObject parent = DynamicObj.Parent;
				DynamicObj.Parent = obj;
				DynamicObj.ResetRoomTriggers();
				DynamicObj.ToggleKinematic(flag2 || parent is OtherPlayer);
				DynamicObj.ToggleActive(!hideObject);
				DynamicObj.ToggleTriggerColliders(DynamicObj.Parent is Corpse);
				DynamicObj.ToggleEnabled(!flag2 || AttachPoint != null || DynamicObj.Parent is Corpse,
					toggleColliders: true);
				if (!flag2)
				{
					DynamicObj.CheckRoomTrigger(null);
				}

				if (InvSlot != null && InvSlot.SlotType == InventorySlot.Type.Hands)
				{
					InvSlot.Inventory.ItemAddedToHands(this);
					if (PickupSound != null)
					{
						PickupSound.Play();
					}
				}

				try
				{
					if (AttachPoint != null)
					{
						AttachPoint.AttachItem(this);
					}
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}

				if (flag2)
				{
					transform.SetParent(attachToTrans);
					transform.Reset(resetScale: true);
					OnAttach(AttachPoint == null, DynamicObj.Parent is Player);
				}
				else
				{
					OnAttach(isAttached: true, DynamicObj.Parent is Player);
					if (DynamicObj.Parent is SpaceObjectVessel)
					{
						transform.SetParent(DynamicObj.Parent.TransferableObjectsRoot.transform);
						DynamicObj.CheckRoomTrigger(null);
					}
					else if (DynamicObj.Parent is Pivot && DynamicObj.Parent.Type == SpaceObjectType.DynamicObjectPivot)
					{
						transform.SetParent(DynamicObj.Parent.TransferableObjectsRoot.transform);
					}
					else
					{
						Debug.LogError("Dynamic object cannot be attached to DynamicObjectRoot");
					}
				}

				World.InGameGUI.HelmetHud.HandsSlotUpdate();
				if (slot2 != null && slot2 is BaseSceneAttachPoint && Slot != null &&
				    Slot.Parent.GetComponentInParent<MyPlayer>() != null)
				{
					SceneQuestTrigger.OnTrigger((slot2 as BaseSceneAttachPoint).gameObject,
						SceneQuestTriggerEvent.DetachItem);
				}
				else if (slot2 != null && slot2.Parent.GetComponentInParent<MyPlayer>() != null && Slot != null &&
				         Slot is BaseSceneAttachPoint)
				{
					SceneQuestTrigger.OnTrigger((Slot as BaseSceneAttachPoint).gameObject,
						SceneQuestTriggerEvent.AttachItem);
				}
			});
			if (flag && slot == null && attachPoint == null && DynamicObj.Parent is MyPlayer)
			{
				MyPlayer.Instance.AnimHelper.SetDropTask(task);
			}
			else
			{
				task();
			}

			if (sendAttachMessage)
			{
				World.SolarSystem.SendMoveCommand(DynamicObj, DynamicObj.Parent, Slot, transform.localPosition, transform.localRotation, sendThrowForce, sendTorque, sendVelocity);
				sendVelocity = null;
				sendTorque = null;
				sendThrowForce = null;
			}
		}

		public bool AreAttachDataSame(DynamicObjectAttachData data)
		{
			SpaceObject spaceObject = Slot is InventorySlot && InvSlot.Outfit != null
				? InvSlot.Outfit.DynamicObj.Parent
				: Slot is not ItemSlot ? DynamicObj.Parent : Slot.Parent;
			return spaceObject.Type == data.ParentType && spaceObject.Guid == data.ParentGUID &&
			       (Slot is ItemSlot itemSlot ? itemSlot.ID : 0) == data.ItemSlotID &&
			       DynamicObj.IsAttached == data.IsAttached &&
			       InvSlotID == data.InventorySlotID && ((AttachPoint == null && data.APDetails == null) ||
			                                             (AttachPoint != null && data.APDetails != null &&
			                                              AttachPoint.InSceneID == data.APDetails.InSceneID));
		}

		public void ProcessAttachData(DynamicObjectAttachData data, SpaceObject prevParent = null)
		{
			SpaceObject spaceObject = World.GetObject(data.ParentGUID, data.ParentType);
			if (spaceObject == null)
			{
				Debug.LogErrorFormat("Could not find space object to attach item to. {0}, {1}, {2}", name, data.ParentGUID,
					data.ParentType);
				return;
			}

			if (spaceObject is OtherPlayer)
			{
				DynamicObj.Master = false;
				DynamicObj.ToggleKinematic(value: true);
			}

			if (Slot != null && Slot is ItemSlot && Slot.Item == this)
			{
				(Slot as ItemSlot).RemoveItem();
			}

			if (this is Outfit && (InvSlotID != data.InventorySlotID || DynamicObj.Parent != spaceObject))
			{
				if (data.InventorySlotID == -2)
				{
					if (spaceObject is MyPlayer)
					{
						(this as Outfit).EquipOutfit(MyPlayer.Instance, checkHands: false);
					}
					else if (spaceObject is OtherPlayer)
					{
						(spaceObject as OtherPlayer).EquipOutfit(this as Outfit);
					}
					else if (spaceObject is Corpse)
					{
						(spaceObject as Corpse).EquipOutfit(this as Outfit);
					}

					return;
				}

				if (InvSlotID == -2)
				{
					if (DynamicObj.Parent is MyPlayer)
					{
						(this as Outfit).TakeOffOutfit(MyPlayer.Instance, sendToServer: false);
					}
					else if (DynamicObj.Parent is OtherPlayer &&
					         (DynamicObj.Parent as OtherPlayer).Inventory.Outfit == this)
					{
						(DynamicObj.Parent as OtherPlayer).TakeOffOutfit();
					}
					else if (DynamicObj.Parent is Corpse && (DynamicObj.Parent as Corpse).Inventory.Outfit == this)
					{
						(DynamicObj.Parent as Corpse).TakeOffOutfit();
					}
				}
			}

			if (data.InventorySlotID != -1111 && data.InventorySlotID != -2)
			{
				InventorySlot inventorySlot = null;
				if (spaceObject is Player)
				{
					inventorySlot = (spaceObject as Player).Inventory.GetSlotByID(data.InventorySlotID);
				}
				else if (spaceObject is DynamicObject && (spaceObject as DynamicObject).Item is Outfit)
				{
					inventorySlot = ((spaceObject as DynamicObject).Item as Outfit).GetSlotByID(data.InventorySlotID);
				}
				else if (spaceObject is Corpse && (spaceObject as Corpse).Inventory != null)
				{
					inventorySlot = (spaceObject as Corpse).Inventory.GetSlotByID(data.InventorySlotID);
				}

				if (inventorySlot != null)
				{
					inventorySlot.SetItem(this, sendMessage: false);
					return;
				}

				Debug.LogError($"Item '{GUID}' was assigned to inventory slot {data.InventorySlotID} of '{spaceObject.Guid}', which has no such slot. It will fall through to a plain attach and end up nowhere.");
			}

			if (data.APDetails != null && spaceObject is SpaceObjectVessel)
			{
				BaseSceneAttachPoint structureObject =
					(spaceObject as SpaceObjectVessel).GetStructureObject<BaseSceneAttachPoint>(data.APDetails.InSceneID);
				if (structureObject != null)
				{
					AttachToObject(structureObject, hideObject: false, sendAttachMessage: false);
					return;
				}
			}

			if (spaceObject is DynamicObject && (spaceObject as DynamicObject).Item != null)
			{
				if ((spaceObject as DynamicObject).Item.Slots.Count > 0 &&
				    (spaceObject as DynamicObject).Item.Slots.TryGetValue(data.ItemSlotID, out var value))
				{
					AttachToObject(value, sendAttachMessage: false);
				}

				(spaceObject as DynamicObject).Item.UpdateUI();
				return;
			}

			AttachToObject(spaceObject, sendAttachMessage: false);
			bool myPlayerIsParent = DynamicObj.Parent is MyPlayer;
			Action task = new Action(delegate
			{
				if (!DynamicObj.IsAttached)
				{
					if (!myPlayerIsParent || !DynamicObj.Master)
					{
						if (data.LocalPosition != null)
						{
							transform.localPosition = data.LocalPosition.ToVector3();
						}

						if (data.LocalRotation != null)
						{
							transform.localRotation = data.LocalRotation.ToQuaternion();
						}
					}

					if (DynamicObj.Master)
					{
						if (data.Velocity != null)
						{
							DynamicObj.RigidBody.linearVelocity = data.Velocity.ToVector3();
						}

						if (data.Torque != null)
						{
							AddTorque(data.Torque.ToVector3(), ForceMode.Impulse);
						}

						if (data.ThrowForce != null)
						{
							Vector3 vector = data.ThrowForce.ToVector3();
							if ((MyPlayer.Instance.CurrentRoomTrigger == null ||
							     !MyPlayer.Instance.CurrentRoomTrigger.UseGravity ||
							     MyPlayer.Instance.CurrentRoomTrigger.GravityForce == Vector3.zero) &&
							    prevParent == MyPlayer.Instance)
							{
								float num = MyPlayer.Instance.rigidBody.mass + DynamicObj.Mass;
								AddForce(vector * (MyPlayer.Instance.rigidBody.mass / num), ForceMode.VelocityChange);
								MyPlayer.Instance.rigidBody.AddForce(-vector * (DynamicObj.Mass / num),
									ForceMode.VelocityChange);
							}
							else
							{
								AddForce(vector, ForceMode.Impulse);
							}
						}
					}

					if (ThrowSound != null)
					{
						ThrowSound.Play();
					}
				}
			});
			if (DynamicObj.Parent is MyPlayer && MyPlayer.Instance.AnimHelper.DropTask != null)
			{
				MyPlayer.Instance.AnimHelper.AfterDropTask = task;
			}
			else
			{
				task();
			}
		}

		public void ToggleTriggersEnabled(bool value)
		{
		}

		public void ResetRoomTriggers()
		{
			DynamicObj.ResetRoomTriggers();
		}

		public virtual void AddForce(Vector3 force, ForceMode forceMode)
		{
			DynamicObj.AddForce(force, forceMode);
		}

		public void AddTorque(Vector3 torque, ForceMode forceMode)
		{
			DynamicObj.AddTorque(torque, forceMode);
		}

		public virtual bool PrimaryFunction()
		{
			return false;
		}

		public virtual bool SecondaryFunction()
		{
			return false;
		}

		public virtual void Reload(Item newMagazine)
		{
		}

		public virtual void Special()
		{
		}

		public virtual void ChangeQuantity(float amount)
		{
		}

		public virtual void ChangeEquip(EquipType type, Player pl)
		{
		}

		public virtual void ProcesStatsData(DynamicObjectStats dos)
		{
			prevQuantity = Quantity;
			if (dos.Tier.HasValue)
			{
				Tier = dos.Tier.Value;
			}

			if (dos.Armor.HasValue)
			{
				Armor = dos.Armor.Value;
			}

			if (dos.MaxHealth.HasValue)
			{
				MaxHealth = dos.MaxHealth.Value;
			}

			if (dos.Health.HasValue)
			{
				Health = dos.Health.Value;
				if (Health <= 0f && DestructAt0HP)
				{
					Explode();
				}
			}

			if (dos.Damages != null)
			{
				TakeDamage(dos.Damages);
			}

			World.InGameGUI.HelmetHud.HandsSlotUpdate();
			if (Slot is BaseSceneAttachPoint && MyPlayer.Instance != null &&
			    MyPlayer.Instance.LockedToTrigger is SceneTriggerCargoPanel)
			{
				StartCoroutine(CheckQuantityCoroutine());
			}
		}

		private IEnumerator CheckQuantityCoroutine()
		{
			yield return null;
			if (Quantity < prevQuantity)
			{
				SceneQuestTrigger.OnTrigger((Slot as BaseSceneAttachPoint).gameObject,
					SceneQuestTriggerEvent.DecreaseQuantity);
			}
			else if (Quantity > prevQuantity)
			{
				SceneQuestTrigger.OnTrigger((Slot as BaseSceneAttachPoint).gameObject,
					SceneQuestTriggerEvent.IncreaseQuantity);
			}
		}

		public virtual bool ProcessSlotChange(Inventory inv, InventorySlot mySlot, InventorySlot nextSlot)
		{
			return false;
		}

		public virtual DynamicObjectAuxData GetAuxData()
		{
			return null;
		}

		public T GetBaseAuxData<T>() where T : DynamicObjectAuxData
		{
			List<ItemSlotData> list = new List<ItemSlotData>();
			ItemSlot[] componentsInChildren = GetComponentsInChildren<ItemSlot>(includeInactive: true);
			foreach (ItemSlot itemSlot in componentsInChildren)
			{
				list.Add(itemSlot.GetData());
			}

			DynamicObjectAuxData dynamicObjectAuxData = Activator.CreateInstance(typeof(T)) as DynamicObjectAuxData;
			dynamicObjectAuxData.Category = Category;
			dynamicObjectAuxData.ItemType = Type;
			dynamicObjectAuxData.MaxHealth = MaxHealth;
			dynamicObjectAuxData.Health = Health;
			dynamicObjectAuxData.Armor = Armor;
			dynamicObjectAuxData.Damageable = Damageable;
			dynamicObjectAuxData.Repairable = Repairable;
			dynamicObjectAuxData.UsageWear = UsageWear;
			dynamicObjectAuxData.MeleeDamage = MeleeDamage;
			dynamicObjectAuxData.Tier = Tier;
			dynamicObjectAuxData.TierMultipliers = TierMultipliers;
			dynamicObjectAuxData.AuxValues = AuxValues;
			dynamicObjectAuxData.Slots = list;
			dynamicObjectAuxData.ExplosionDamage = ExplosionDamage;
			dynamicObjectAuxData.ExplosionDamageType = ExplosionDamageType;
			dynamicObjectAuxData.ExplosionRadius = ExplosionRadius;
			return dynamicObjectAuxData as T;
		}

		public ItemIngredientsData GetIngredientsData()
		{
			ItemIngredientsData itemIngredientsData = new ItemIngredientsData();
			itemIngredientsData.SubType = GenericItemSubType.None;
			itemIngredientsData.PartType = MachineryPartType.None;
			ItemIngredientsData itemIngredientsData2 = itemIngredientsData;
			itemIngredientsData2.Type = Type;
			if (this is GenericItem)
			{
				itemIngredientsData2.SubType = (this as GenericItem).SubType;
				itemIngredientsData2.Name = itemIngredientsData2.SubType.ToString();
			}
			else if (this is MachineryPart)
			{
				itemIngredientsData2.PartType = (this as MachineryPart).PartType;
				itemIngredientsData2.Name = itemIngredientsData2.PartType.ToString();
			}
			else
			{
				itemIngredientsData2.Name = Type.ToString();
			}

			itemIngredientsData2.IngredientsTiers = new Dictionary<int, ItemIngredientsTierData>();
			for (int i = 0; i < Ingredients.Length; i++)
			{
				ItemIngredientLists itemIngredientLists = Ingredients[i];
				ItemIngredientsTierData itemIngredientsTierData = new ItemIngredientsTierData();
				itemIngredientsTierData.Craft = null;
				itemIngredientsTierData.Recycle = null;
				ItemIngredientsTierData itemIngredientsTierData2 = itemIngredientsTierData;
				if (itemIngredientLists.Recycle.Length > 0)
				{
					itemIngredientsTierData2.Recycle = new Dictionary<ResourceType, float>();
					foreach (ItemIngredient item in itemIngredientLists.Recycle.Where((ItemIngredient m) =>
						         m.Quantity > 0f && m.ResourceType != ResourceType.None))
					{
						itemIngredientsTierData2.Recycle[item.ResourceType] = item.Quantity;
					}
				}

				if (itemIngredientLists.Craft.Length > 0)
				{
					itemIngredientsTierData2.Craft = new Dictionary<ResourceType, float>();
					foreach (ItemIngredient item2 in itemIngredientLists.Craft.Where((ItemIngredient m) =>
						         m.Quantity > 0f && m.ResourceType != ResourceType.None))
					{
						itemIngredientsTierData2.Craft[item2.ResourceType] = item2.Quantity;
					}
				}

				itemIngredientsData2.IngredientsTiers[i + 1] = itemIngredientsTierData2;
			}

			return itemIngredientsData2;
		}

		public virtual bool CustomCollidereToggle(bool isEnabled)
		{
			return false;
		}

		public virtual bool CanReloadOnInteract(Item item)
		{
			return false;
		}

		public virtual void PrimaryReleased()
		{
		}

		public virtual void ReloadStepComplete(Player pl, AnimatorHelper.ReloadStepType reloadStepType,
			ref Item currentReloadItem, ref Item newReloadItem)
		{
		}

		public virtual void TriggerItemAnimation(AnimatorHelper.ReloadType rType, string triggerName,
			string blendTreeParamName)
		{
		}

		public virtual void MakeHitEffect(RaycastHit info, BulletsGoThrough.HitEffectType hitType)
		{
		}

		public virtual void GetReloadType(out ItemType type, out bool getHighestQuantity)
		{
			type = ItemType.None;
			getHighestQuantity = true;
		}

		public void DropThrow(Vector3 dropPosition, Vector3 velocity, Vector3 throwForce)
		{
			if (!(DynamicObj.Parent is MyPlayer) &&
			    (!(DynamicObj.Parent is DynamicObject) || !(DynamicObj.Parent.Parent is MyPlayer)))
			{
				Debug.LogErrorFormat("Cannot drop/throw item if parent is not player {0}, {1}, {2}", DynamicObj, DynamicObj.Parent,
					DynamicObj.Parent.Parent);
				return;
			}

			transform.position = dropPosition;
			ResetRoomTriggers();
			ToggleTriggersEnabled(value: true);
			Vector3 vector = new Vector3(UnityEngine.Random.Range(0.001f, 0.01f),
				UnityEngine.Random.Range(0.001f, 0.01f), UnityEngine.Random.Range(0.001f, 0.01f));
			sendVelocity = velocity;
			sendTorque = vector;
			if (throwForce.IsNotEpsilonZero())
			{
				sendThrowForce = throwForce;
			}

			transform.localRotation = Quaternion.identity;
			if (MyPlayer.Instance.Parent is Pivot)
			{
				DynamicObj.ExitVessel(forceExit: true);
			}
			else
			{
				AttachToObject(MyPlayer.Instance.Parent, sendAttachMessage: true);
			}

			AddForce(velocity, ForceMode.VelocityChange);
			AddTorque(vector, ForceMode.Impulse);
			if (throwForce.IsNotEpsilonZero())
			{
				AddForce(throwForce, ForceMode.Impulse);
			}

			if (ThrowSound != null)
			{
				ThrowSound.Play();
			}
		}

		public virtual void SetPositionOfActiveEffect()
		{
			if (DynamicObj.Parent is MyPlayer && TipOfItem != null && MyPlayer.Instance.CurrentActiveItem == this)
			{
				MyPlayer.Instance.MuzzleFlashTransform.position = TipOfItem.position;
				MyPlayer.Instance.MuzzleFlashTransform.rotation = TipOfItem.rotation;
			}
		}

		public virtual void StartItemAnimation(ItemAnimationType type, InventorySlot destinationSlot, bool isDrop)
		{
			if (DynamicObj.Parent is MyPlayer)
			{
				MyPlayer myPlayer = DynamicObj.Parent as MyPlayer;
				if (type == ItemAnimationType.Equip && destinationSlot != null &&
				    destinationSlot.SlotType == InventorySlot.Type.Equip)
				{
					myPlayer.Inventory.SetAnimationItem(this, InvSlot, destinationSlot, isDrop);
					myPlayer.animHelper.SetParameterTrigger(AnimatorHelper.Triggers.EquipItem);
					myPlayer.FpsController.IsEquippingAnimationTriggered = true;
					myPlayer.animHelper.SetParameter(null, null, null, null, null, null, null, null, null, null, null,
						null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
						null, null, null, null, null, null, null, null, null, null, null,
						AnimatorHelper.EquipOrDeEquip.Equip);
					AnimatorHelper animHelper = myPlayer.animHelper;
					ItemType? equipItemId = Type;
					animHelper.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null,
						null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
						null, null, null, null, null, null, null, null, equipItemId);
				}
				else if (type == ItemAnimationType.Unequip)
				{
					myPlayer.Inventory.SetAnimationItem(this, InvSlot, destinationSlot, isDrop);
					myPlayer.animHelper.SetParameterTrigger(AnimatorHelper.Triggers.EquipItem);
					myPlayer.FpsController.IsEquippingAnimationTriggered = true;
					myPlayer.animHelper.SetParameter(null, null, null, null, null, null, null, null, null, null, null,
						null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
						null, null, null, null, null, null, null, null, null, null, null,
						AnimatorHelper.EquipOrDeEquip.DeEquip);
					AnimatorHelper animHelper2 = myPlayer.animHelper;
					ItemType? equipItemId = Type;
					animHelper2.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null,
						null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
						null, null, null, null, null, null, null, null, equipItemId);
				}
			}
		}

		public virtual void TakeDamage(Dictionary<TypeOfDamage, float> damages)
		{
		}

		public virtual void AttackWithItem()
		{
			SpaceObject parentSpaceObject = MyPlayer.Instance.Parent is not SpaceObjectVessel
				? MyPlayer.Instance.Parent
				: (MyPlayer.Instance.Parent as SpaceObjectVessel).MainVessel;
			ShotData shotData = new ShotData
			{
				Position = (parentSpaceObject.transform.rotation *
									 MyPlayer.Instance.FpsController.MainCamera.transform.position).ToArray(),
				Orientation = (parentSpaceObject.transform.rotation *
										MyPlayer.Instance.FpsController.MainCamera.transform.forward.normalized).ToArray(),
				parentGUID = parentSpaceObject.Guid,
				parentType = parentSpaceObject.Type,
				Range = MeleeRange,
				IsMeleeAttack = true
			};
			MyPlayer.Instance.Attack(shotData, this, 0f, 0f);
		}

		public virtual void OnAttach(bool isAttached, bool isOnPlayer)
		{
		}

		public virtual bool CanPlayerPickUp(Player pl)
		{
			return true;
		}

		public void UpdateHealthIndicator(float value, float maxValue)
		{
			if (HealthIndicator != null && HealthIndicator.materials.Length >= MaterialIndex + 1)
			{
				float num = HealthIndicatorCurve.Evaluate(!(maxValue > 0f) ? 0f : value / maxValue);
				float num2;
				Color color;
				if (num < 0.5f)
				{
					num2 = Mathf.Lerp(NoHealthIntensity, MidHealthIntensity, num * 2f);
					color = Color.Lerp(NoHealthColor, MidHealthColor, num * 2f);
				}
				else
				{
					num2 = Mathf.Lerp(MidHealthIntensity, FullHealthIntensity, (num - 0.5f) * 2f);
					color = Color.Lerp(MidHealthColor, FullHealthColor, (num - 0.5f) * 2f);
				}

				Material material = HealthIndicator.materials[MaterialIndex];
				if (material.shader.name.StartsWith("Standard"))
				{
					material.SetColor("_EmissionColor", color * num2);
					return;
				}

				material.SetFloat("_EmissionAmount", num2);
				material.SetColor("_EmColor", color);
			}
		}

		public void ApplyTierColor()
		{
			int? num = TierColors != null ? new int?(TierColors.Length) : null;
			if (num.HasValue && num.GetValueOrDefault() > 0)
			{
				Color value = Color.white;
				if (Tier > 0 && Tier <= TierColors.Length)
				{
					value = TierColors[Tier - 1];
				}

				List<Renderer> list = (from m in GetComponentsInChildren<Renderer>(includeInactive: true)
					where m.GetComponentInParent<Item>() == this
					select m).ToList();
				if (this is Outfit)
				{
					if (DynamicObj.Parent is MyPlayer && MyPlayer.Instance.CurrentOutfit == this)
					{
						list.AddRange(
							(from m in MyPlayer.Instance.Outfit.GetComponentsInChildren<Renderer>(includeInactive: true)
								where m.GetComponentInParent<Item>() == null
								select m).ToList());
						list.AddRange(
							(from m in MyPlayer.Instance.CurrentOutfit.FoldedOutfitTrans
									.GetComponentsInChildren<Renderer>(includeInactive: true)
								where m.GetComponentInParent<Item>() == null
								select m).ToList());
						list.AddRange(
							(from m in MyPlayer.Instance.CurrentOutfit.OutfitTrans.GetComponentsInChildren<Renderer>(
									includeInactive: true)
								where m.GetComponentInParent<Item>() == null
								select m).ToList());
					}
					else if (DynamicObj.Parent is OtherPlayer &&
					         (DynamicObj.Parent as OtherPlayer).tpsController.CurrentOutfit == this)
					{
						list.AddRange(
							(from m in (DynamicObj.Parent as OtherPlayer).tpsController.Outfit
									.GetComponentsInChildren<Renderer>(includeInactive: true)
								where m.GetComponentInParent<Item>() == null
								select m).ToList());
						list.AddRange(
							(from m in (DynamicObj.Parent as OtherPlayer).tpsController.CurrentOutfit.FoldedOutfitTrans
									.GetComponentsInChildren<Renderer>(includeInactive: true)
								where m.GetComponentInParent<Item>() == null
								select m).ToList());
						list.AddRange(
							(from m in (DynamicObj.Parent as OtherPlayer).tpsController.CurrentOutfit.OutfitTrans
									.GetComponentsInChildren<Renderer>(includeInactive: true)
								where m.GetComponentInParent<Item>() == null
								select m).ToList());
					}
				}

				foreach (Renderer item in list.Distinct())
				{
					Material material = item.material;
					if (material.shader.name.StartsWith("Standard"))
					{
						material.SetColor("_Color", value);
					}
					else
					{
						material.SetColor(ShaderProperty, value);
					}
				}
			}

			int? num2 = TierGameObjects != null ? new int?(TierGameObjects.Length) : null;
			if (!num2.HasValue || num2.GetValueOrDefault() <= 0)
			{
				return;
			}

			for (int i = 0; i < TierGameObjects.Length; i++)
			{
				GameObject gameObject = TierGameObjects[i];
				if (gameObject != null)
				{
					if (i == Tier - 1)
					{
						gameObject.Activate(value: true);
					}
					else
					{
						Destroy(gameObject);
					}
				}
			}
		}

		private void OnDrawGizmos()
		{
			Gizmos.DrawIcon(transform.position, "Item");
		}

		public static Dictionary<ResourceType, float> GetRecycleResources(Item item)
		{
			if (item is GenericItem)
			{
				return GetRecycleResources(item.World, item.Type, (item as GenericItem).SubType, MachineryPartType.None, item.Tier);
			}

			if (item is MachineryPart)
			{
				return GetRecycleResources(item.World, item.Type, GenericItemSubType.None, (item as MachineryPart).PartType,
					item.Tier);
			}

			return GetRecycleResources(item.World, item.Type, GenericItemSubType.None, MachineryPartType.None, item.Tier);
		}

		public static Dictionary<ResourceType, float> GetRecycleResources(World world, ItemType itemType, GenericItemSubType subType,
			MachineryPartType partType, int tier)
		{
			ItemIngredientsData itemIngredientsData = world.ItemsIngredients.FirstOrDefault(
				(ItemIngredientsData m) => m.Type == itemType && m.SubType == subType && m.PartType == partType);
			if (itemIngredientsData != null)
			{
				KeyValuePair<int, ItemIngredientsTierData>? keyValuePair = itemIngredientsData.IngredientsTiers
					.OrderBy((KeyValuePair<int, ItemIngredientsTierData> m) => m.Key).Reverse()
					.FirstOrDefault((KeyValuePair<int, ItemIngredientsTierData> m) => m.Key <= tier);
				if (keyValuePair.HasValue && keyValuePair.Value.Value.Recycle != null &&
				    keyValuePair.Value.Value.Recycle.Count > 0)
				{
					return keyValuePair.Value.Value.Recycle
						.Where((KeyValuePair<ResourceType, float> m) => m.Key != 0 && m.Value > 0f)
						.ToDictionary((KeyValuePair<ResourceType, float> k) => k.Key,
							(KeyValuePair<ResourceType, float> v) => v.Value);
				}
			}

			return null;
		}

		public static Dictionary<ResourceType, float> GetCraftingResources(World world, ItemCompoundType item)
		{
			return GetCraftingResources(world, item.Type, item.SubType, item.PartType, item.Tier);
		}

		public static Dictionary<ResourceType, float> GetCraftingResources(World world, ItemType itemType,
			GenericItemSubType subType, MachineryPartType partType, int tier)
		{
			ItemIngredientsData itemIngredientsData = world.ItemsIngredients.FirstOrDefault(
				(ItemIngredientsData m) => m.Type == itemType && m.SubType == subType && m.PartType == partType);
			if (itemIngredientsData != null)
			{
				KeyValuePair<int, ItemIngredientsTierData>? keyValuePair = itemIngredientsData.IngredientsTiers
					.OrderBy((KeyValuePair<int, ItemIngredientsTierData> m) => m.Key).Reverse()
					.FirstOrDefault((KeyValuePair<int, ItemIngredientsTierData> m) => m.Key <= tier);
				if (keyValuePair.HasValue && keyValuePair.Value.Value.Craft != null &&
				    keyValuePair.Value.Value.Craft.Count > 0)
				{
					return keyValuePair.Value.Value.Craft
						.Where((KeyValuePair<ResourceType, float> m) => m.Key != 0 && m.Value > 0f)
						.ToDictionary((KeyValuePair<ResourceType, float> k) => k.Key,
							(KeyValuePair<ResourceType, float> v) => v.Value);
				}
			}

			return null;
		}

		public static string GetName(ItemCompoundType item)
		{
			return GetName(item.Type, item.SubType, item.PartType);
		}

		public static string GetName(ItemType itemType, GenericItemSubType subType, MachineryPartType partType,
			bool localized = true)
		{
			switch (itemType)
			{
				case ItemType.GenericItem:
					if (localized)
					{
						return subType.ToLocalizedString();
					}

					return subType.ToString();
				case ItemType.MachineryPart:
					if (localized)
					{
						return partType.ToLocalizedString();
					}

					return partType.ToString();
				default:
					if (localized)
					{
						return itemType.ToLocalizedString();
					}

					return itemType.ToString();
			}
		}

		public static string GetDescription(ItemIngredientsData item)
		{
			return GetDescription(item.Type, item.SubType, item.PartType);
		}

		public static string GetDescription(ItemCompoundType compoundType)
		{
			return GetDescription(compoundType.Type, compoundType.SubType, compoundType.PartType);
		}

		public static string GetDescription(ItemType itemType, GenericItemSubType subType, MachineryPartType partType)
		{
			string value = string.Empty;
			switch (itemType)
			{
				case ItemType.GenericItem:
					Localization.GenericItemsDescriptions.TryGetValue(subType, out value);
					break;
				case ItemType.MachineryPart:
					Localization.MachineryPartsDescriptions.TryGetValue(partType, out value);
					break;
				default:
					Localization.ItemsDescriptions.TryGetValue(itemType, out value);
					break;
			}

			return value ?? string.Empty;
		}

		public void RequestPickUp(bool handsFirst = false)
		{
			if (Slot is BaseSceneAttachPoint && (Slot as BaseSceneAttachPoint).SceneQuestTriggers != null)
			{
				SceneQuestTrigger sceneQuestTrigger =
					(Slot as BaseSceneAttachPoint).SceneQuestTriggers.FirstOrDefault((SceneQuestTrigger m) =>
						m.TriggerEvent == SceneQuestTriggerEvent.DetachItem);
				if (sceneQuestTrigger != null && sceneQuestTrigger.QuestTrigger.Status != QuestStatus.Active &&
				    sceneQuestTrigger.QuestTrigger.Status != QuestStatus.Completed)
				{
					return;
				}
			}

			InventorySlot inventorySlot = null;
			InventorySlot slotByID = MyPlayer.Instance.Inventory.GetSlotByID(-2);
			inventorySlot =
				!(this is Outfit) || slotByID == null || !(slotByID.Item == null) ||
				!(MyPlayer.Instance.Inventory.HandsSlot.Item == null)
					? MyPlayer.Instance.Inventory.FindEmptyOutfitSlot(this)
					: slotByID;
			if (MyPlayer.Instance.Inventory.HandsSlot.Item == null && (handsFirst || inventorySlot == null))
			{
				inventorySlot = MyPlayer.Instance.Inventory.HandsSlot;
			}

			if (inventorySlot != null)
			{
				World.SolarSystem.SendMoveCommand(DynamicObj, MyPlayer.Instance, inventorySlot);
				if (inventorySlot.SlotGroup == InventorySlot.Group.Hands)
				{
					MyPlayer.Instance.ItemAddedToHands(this);
				}
			}
		}

		public void RequestDrop(float throwStrength = 0f)
		{
			if (Slot is BaseSceneAttachPoint && (Slot as BaseSceneAttachPoint).SceneQuestTriggers != null)
			{
				SceneQuestTrigger sceneQuestTrigger =
					(Slot as BaseSceneAttachPoint).SceneQuestTriggers.FirstOrDefault((SceneQuestTrigger m) =>
						m.TriggerEvent == SceneQuestTriggerEvent.DetachItem);
				if (sceneQuestTrigger != null && sceneQuestTrigger.QuestTrigger.Status != QuestStatus.Active &&
				    sceneQuestTrigger.QuestTrigger.Status != QuestStatus.Completed)
				{
					return;
				}
			}

			Vector3 value = MyPlayer.Instance.transform.parent.InverseTransformPoint(
				MyPlayer.Instance.FpsController.MainCamera.transform.position +
				MyPlayer.Instance.FpsController.MainCamera.transform.forward);
			Vector3 vector = MyPlayer.Instance.FpsController.CameraForward * MathHelper.ProportionalValue(throwStrength,
				0f, World.DROP_MAX_TIME, 0f, World.DROP_MAX_FORCE);
			Vector3 value2 = !(vector == Vector3.zero)
				? new Vector3(UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(-0.1f, 0.1f),
					UnityEngine.Random.Range(-0.1f, 0.1f))
				: Vector3.zero;
			World.SolarSystem.SendMoveCommand(DynamicObj, MyPlayer.Instance.Parent, null, value, Quaternion.identity, vector, value2,
				MyPlayer.Instance.rigidBody.linearVelocity);
			if (DynamicObj.Parent is MyPlayer)
			{
				MyPlayer.Instance.AnimHelper.SetParameterTrigger(AnimatorHelper.Triggers.Drop);
			}
		}

		public void RequestAttach(IItemSlot slot)
		{
			if (slot is InventorySlot && (slot as InventorySlot).SlotType == InventorySlot.Type.Hands &&
			    (slot as InventorySlot).Inventory.Parent is MyPlayer)
			{
				((slot as InventorySlot).Inventory.Parent as MyPlayer).ChangeStance(MyPlayer.PlayerStance.Passive, 1f);
			}

			if (Slot is BaseSceneAttachPoint && (Slot as BaseSceneAttachPoint).SceneQuestTriggers != null)
			{
				SceneQuestTrigger sceneQuestTrigger =
					(Slot as BaseSceneAttachPoint).SceneQuestTriggers.FirstOrDefault((SceneQuestTrigger m) =>
						m.TriggerEvent == SceneQuestTriggerEvent.DetachItem);
				if (sceneQuestTrigger != null && sceneQuestTrigger.QuestTrigger.Status != QuestStatus.Active &&
				    sceneQuestTrigger.QuestTrigger.Status != QuestStatus.Completed)
				{
					return;
				}
			}

			Item swapItem = slot.Item;
			IItemSlot slot2 = Slot;
			if (slot.Item != null && slot2 != null)
			{
				if (!slot2.CanFitItem(swapItem) && slot2.Parent is DynamicObject)
				{
					ItemSlot itemSlot =
						(slot2.Parent as DynamicObject).Item.Slots.Values.FirstOrDefault((ItemSlot m) =>
							m.CanFitItem(swapItem));
					if (itemSlot == null)
					{
						if (MyPlayer.Instance.Inventory.CanAddToInventory(swapItem))
						{
							swapItem.RequestPickUp();
						}
						else
						{
							swapItem.RequestDrop();
						}
					}
				}
				else
				{
					World.SolarSystem.SendMoveCommand(swapItem.DynamicObj, slot2.Parent, slot2);
				}
			}
			else if (swapItem != null && slot2 == null)
			{
				if (MyPlayer.Instance.Inventory.CanAddToInventory(swapItem))
				{
					swapItem.RequestPickUp();
				}
				else
				{
					swapItem.RequestDrop();
				}
			}

			World.SolarSystem.SendMoveCommand(DynamicObj, slot.Parent, slot);
		}

		public virtual string GetInfo()
		{
			return string.Empty;
		}

		public virtual string QuantityCheck()
		{
			return null;
		}

		public void CalculateRecycle()
		{
			for (int i = 0; i < (TierMultipliers.Length < 1 ? 1 : TierMultipliers.Length); i++)
			{
				Ingredients[i].Recycle = ObjectCopier.DeepCopy(Ingredients[i].Craft);
				ItemIngredient[] recycle = Ingredients[i].Recycle;
				foreach (ItemIngredient ii in recycle)
				{
					ApplyRecMulty(ii);
				}
			}
		}

		private static void ApplyRecMulty(ItemIngredient ii)
		{
			if (ii.ResourceType == ResourceType.NickelIron)
			{
				ii.Quantity *= 0.25f;
			}
			else if (ii.ResourceType == ResourceType.Silicates)
			{
				ii.Quantity *= 0.2f;
			}
			else if (ii.ResourceType == ResourceType.Titanium)
			{
				ii.Quantity *= 0.15f;
			}
			else if (ii.ResourceType == ResourceType.RareElements)
			{
				ii.Quantity *= 0.1f;
			}
			else if (ii.ResourceType == ResourceType.CarbonFibers)
			{
				ii.Quantity *= 0.25f;
			}
			else if (ii.ResourceType == ResourceType.Alloys)
			{
				ii.Quantity *= 0.2f;
			}
			else if (ii.ResourceType == ResourceType.Circuits)
			{
				ii.Quantity *= 0.15f;
			}
			else if (ii.ResourceType == ResourceType.Superconductors)
			{
				ii.Quantity *= 0.1f;
			}
		}

		public virtual bool Explode()
		{
			if (exploded)
			{
				return false;
			}

			HashSet<long> hashSet = new HashSet<long>();
			HashSet<VesselObjectID> hashSet2 = new HashSet<VesselObjectID>();
			if (ExplosionRadius > 0f)
			{
				Collider[] array = Physics.OverlapSphere(transform.position, ExplosionRadius);
				Collider[] array2 = array;
				foreach (Collider colliderOverlaped in array2)
				{
					if (!CheckDamageables(hashSet, colliderOverlaped) && !CheckPlayers(hashSet, colliderOverlaped))
					{
						CheckRepairPoints(hashSet2, colliderOverlaped);
					}
				}

				if (DynamicObj.Parent is SpaceObjectVessel)
				{
					foreach (VesselRepairPoint item in from m in (DynamicObj.Parent as SpaceObjectVessel).RepairPoints
					         where (transform.position - m.Value.transform.position).magnitude <= ExplosionRadius
					         orderby (transform.position - m.Value.transform.position).magnitude
					         select m
					         into kvp
					         select kvp.Value)
					{
						hashSet2.Add(new VesselObjectID(item.ParentVessel.Guid, item.InSceneID));
					}
				}
			}

			NetworkController.SendAndForget(new ExplosionMessage
			{
				AffectedGUIDs = hashSet.ToArray(),
				ItemGUID = GUID,
				RepairPointIDs = hashSet2.ToArray()
			});
			if (ExplosionEffects != null)
			{
				ExplosionEffects.Play();
				ExplosionEffects.transform.parent = null;
			}

			if (ExplosionSound != null)
			{
				ExplosionSound.Play();
			}

			gameObject.Activate(value: false);
			exploded = true;
			if (Slot != null)
			{
				if (Slot is BaseSceneAttachPoint attachPoint)
				{
					attachPoint.DetachItem(this);
				}
				else if (Slot is InventorySlot inventorySlot)
				{
					inventorySlot.SetItem(null);
					if (inventorySlot.SlotGroup == InventorySlot.Group.Hands)
					{
						inventorySlot.Inventory.ExitCombatStance();
					}
				}
				else if (Slot is ItemSlot itemSlot)
				{
					itemSlot.RemoveItem();
				}
			}

			return true;
		}

		private bool CheckDamageables(HashSet<long> hitGUIDs, Collider colliderOverlaped)
		{
			Item componentInParent = colliderOverlaped.GetComponentInParent<Item>();
			if (componentInParent != null && componentInParent != this &&
			    (componentInParent.AttachPoint == null || componentInParent.Damageable))
			{
				foreach (Vector3 target in GetTargets(colliderOverlaped.transform.position, 0.1f))
				{
					Debug.DrawRay(transform.position, target - transform.position, Color.green, 100f);
					RaycastHit[] array = (from m in Physics.RaycastAll(transform.position,
							target - transform.position, ExplosionRadius, explosionRaycastMask,
							QueryTriggerInteraction.Collide)
						orderby m.distance
						select m).ToArray();
					RaycastHit[] array2 = array;
					for (int i = 0; i < array2.Length; i++)
					{
						RaycastHit raycastHit = array2[i];
						Item componentInParent2 = raycastHit.collider.GetComponentInParent<Item>();
						if (componentInParent2 == componentInParent)
						{
							if (componentInParent.Damageable)
							{
								hitGUIDs.Add(componentInParent.GUID);
							}

							Vector3 vector = transform.position - target;
							colliderOverlaped.GetComponentInParent<DynamicObject>().AddForce(
								vector.normalized * (1f - ExplosionRadius / vector.magnitude) * ExplosionImpulse,
								ForceMode.Impulse);
							return true;
						}

						if (!raycastHit.collider.isTrigger)
						{
							break;
						}
					}
				}
			}

			return false;
		}

		private bool CheckPlayers(HashSet<long> hitGUIDs, Collider colliderOverlaped)
		{
			Player componentInParent = colliderOverlaped.GetComponentInParent<Player>();
			if (componentInParent != null)
			{
				TargetingPoint[] componentsInChildren = componentInParent.GetComponentsInChildren<TargetingPoint>();
				TargetingPoint[] array = componentsInChildren;
				foreach (TargetingPoint targetingPoint in array)
				{
					RaycastHit[] array2 = (from m in Physics.RaycastAll(transform.position,
							targetingPoint.transform.position - transform.position, ExplosionRadius,
							explosionRaycastMask, QueryTriggerInteraction.Collide)
						orderby m.distance
						select m).ToArray();
					RaycastHit[] array3 = array2;
					for (int j = 0; j < array3.Length; j++)
					{
						RaycastHit raycastHit = array3[j];
						if (raycastHit.collider.GetComponentInParent<Player>() != null)
						{
							hitGUIDs.Add(componentInParent.Guid);
							if (componentInParent is MyPlayer && ExplosionDamageType == TypeOfDamage.Impact &&
							    !MyPlayer.Instance.IsLockedToTrigger)
							{
								if (!MyPlayer.Instance.FpsController.IsZeroG &&
								    !MyPlayer.Instance.FpsController.HasTumbled &&
								    (!ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Sprint) ||
								     !MyPlayer.Instance.FpsController.IsGrounded))
								{
									MyPlayer.Instance.FpsController.Tumble();
								}

								Vector3 vector = transform.position - MyPlayer.Instance.transform.position;
								MyPlayer.Instance.FpsController.AddForce(
									vector.normalized * (1f - ExplosionRadius / vector.magnitude) * ExplosionImpulse,
									ForceMode.Impulse);
							}

							return true;
						}

						if (!raycastHit.collider.isTrigger)
						{
							break;
						}
					}
				}
			}

			return false;
		}

		private void CheckRepairPoints(HashSet<VesselObjectID> rpts, Collider colliderOverlaped)
		{
			VesselRepairPoint componentInParent = colliderOverlaped.GetComponentInParent<VesselRepairPoint>();
			if (!(componentInParent != null) || !(componentInParent.Health > float.Epsilon))
			{
				return;
			}

			foreach (Vector3 target in GetTargets(colliderOverlaped.transform.position, 0.35f))
			{
				RaycastHit[] array = (from m in Physics.RaycastAll(transform.position,
						target - transform.position, ExplosionRadius, explosionRaycastMask,
						QueryTriggerInteraction.Collide)
					orderby m.distance
					select m).ToArray();
				RaycastHit[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					RaycastHit raycastHit = array2[i];
					if (raycastHit.collider.GetComponentInParent<VesselRepairPoint>() == componentInParent)
					{
						rpts.Add(new VesselObjectID(componentInParent.ParentVessel.Guid, componentInParent.InSceneID));
					}
					else if (!raycastHit.collider.isTrigger)
					{
						break;
					}
				}
			}
		}

		private List<Vector3> GetTargets(Vector3 targetPos, float radius)
		{
			List<Vector3> list = new List<Vector3>();
			Quaternion quaternion =
				Quaternion.FromToRotation(Vector3.forward, (targetPos - transform.position).normalized);
			Quaternion quaternion2 = Quaternion.Euler(0f, 0f, MathHelper.RandomRange(0, 90));
			list.Add(targetPos);
			list.Add(targetPos + quaternion * quaternion2 * new Vector3(radius, radius, 0f));
			list.Add(targetPos + quaternion * quaternion2 * new Vector3(radius, 0f - radius, 0f));
			list.Add(targetPos + quaternion * quaternion2 * new Vector3(0f - radius, radius, 0f));
			list.Add(targetPos + quaternion * quaternion2 * new Vector3(0f - radius, 0f - radius, 0f));
			return list;
		}
	}
}
