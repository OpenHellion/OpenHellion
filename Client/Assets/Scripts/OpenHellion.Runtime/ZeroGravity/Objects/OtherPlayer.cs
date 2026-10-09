using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using ZeroGravity.CharacterMovement;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;
using OpenHellion;
using OpenHellion.Net;

namespace ZeroGravity.Objects
{
	public class OtherPlayer : Player
	{
		public delegate void InteractLockDelegate();

		public InteractLockDelegate OnIteractStart;

		public InteractLockDelegate OnIteractComplete;

		public InteractLockDelegate OnLockStart;

		public InteractLockDelegate OnLockComplete;

		public OtherCharacterController tpsController;

		public Jetpack CurrentJetpack;

		public Helmet CurrentHelmet;

		public Weapon CurrentWeapon;

		public bool isDrilling;

		public AnimatorHelper AnimatorHelperHair;

		public SkinnedMeshRenderer hairMesh;

		public GameObject HairObject;

		private float movementReceivedTime = -1f;

		private Vector3 movementTargetPosition;

		private Quaternion movementTargetRotation;

		public override SpaceObjectType Type => SpaceObjectType.Player;

		public void UpdateMovement()
		{
			if (Time.time - movementReceivedTime <= 1f)
			{
				float t = Mathf.Pow(Time.time - movementReceivedTime, 0.5f);
				transform.SetLocalPositionAndRotation(
					Vector3.Lerp(transform.localPosition, movementTargetPosition, t),
					Quaternion.Slerp(transform.localRotation, movementTargetRotation, t));
			}
		}

		public void SetMovementData(Vector3 position, Quaternion rotation)
		{
			if (movementReceivedTime < 0f)
			{
				transform.localPosition = position;
			}

			movementReceivedTime = Time.time;
			movementTargetPosition = position;
			movementTargetRotation = rotation;
		}

		protected void Awake()
		{
			EventSystem.AddListener(typeof(PlayerDrillingMessage), PlayerDrillingMessageListener);
			EventSystem.AddListener(typeof(PlayerStatsMessage), PlayerStatsMessageListener);
			if (tpsController == null)
			{
				tpsController = transform.GetComponent<OtherCharacterController>();
			}

			InitInventory();
			tpsController.PlayerName = PlayerName;
		}

		private void InitInventory()
		{
			if (Inventory == null)
			{
				tpsController.RecreateRig();
				Inventory = new Inventory(this, tpsController.animHelper);
			}
		}

		public static OtherPlayer Create(World world, long guid, Vector3 position, Quaternion rotation, long parentId, Gender gender, byte headType, byte hairType,
			string name, string playerId, int spawnPointID, int animationStatsMask, VesselObjectID lockedToTriggerID, DynamicObjectDetails[] dynamicObjects = null)
		{
			if (guid == MyPlayer.Instance.Guid)
			{
				Debug.LogWarning("Player attempted to spawn itself.");
				return null;
			}

			if (world.GetPlayer(guid) != null)
			{
				return null;
			}

			ArtificialBody parent;
			if (parentId == MyPlayer.Instance.Parent.Guid)
			{
				parent = MyPlayer.Instance.Parent as ArtificialBody;
			}
			else
			{
				world.TryGetSpaceObject(parentId, out parent);
			}

			GameObject gameObject =
				Instantiate(Resources.Load("Models/Units/Characters/ThirdPersonCharacter"),
					new Vector3(20000f, 20000f, 20000f), Quaternion.identity) as GameObject;
			gameObject.SetActive(value: false);
			OtherPlayer otherPlayer = gameObject.AddComponent<OtherPlayer>();
			otherPlayer.World = world;
			otherPlayer.tpsController = otherPlayer.GetComponent<OtherCharacterController>();
			GenderSettings component = gameObject.GetComponent<GenderSettings>();
			GenderSettings.GenderItem genderItem = null;
			foreach (GenderSettings.GenderItem setting in component.settings)
			{
				if (setting.Gender != gender)
				{
					Destroy(setting.Outfit.gameObject);
				}
				else
				{
					genderItem = setting;
				}
			}

			if (genderItem == null)
			{
				throw new ArgumentNullException();
			}

			otherPlayer.AnimatorHelperHair = genderItem.Outfit.GetComponent<AnimatorHelper>();
			otherPlayer.tpsController.Outfit = genderItem.Outfit;
			GameObject headObject =
				Instantiate(Resources.Load("Models/Units/Characters/Heads/" + gender + "/Head" + 1)) as GameObject;
			headObject.transform.parent = gameObject.transform;
			headObject.transform.SetLocalPositionAndRotation(new Vector3(0f, -1.34f, 0f), Quaternion.identity);
			headObject.transform.localScale = Vector3.one;
			headObject.GetComponent<SkinnedMeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
			if ((gender != 0 ? 1 : 0) != 0)
			{
				otherPlayer.HairObject =
					Instantiate(Resources.Load("Models/Units/Characters/Hairs/" + gender + "/Hair" + (gender != 0 ? 1 : 0))) as GameObject;
				otherPlayer.HairObject.transform.parent =
					otherPlayer.AnimatorHelperHair.GetBone(AnimatorHelper.HumanBones.Head);
				otherPlayer.HairObject.transform.localPosition = Vector3.zero;
				otherPlayer.HairObject.transform.localScale = Vector3.one;
				otherPlayer.hairMesh = otherPlayer.HairObject.GetComponent<SkinnedMeshRenderer>();
			}

			otherPlayer.tpsController.HeadSkin = headObject.GetComponent<SkinnedMeshRenderer>();
			otherPlayer.tpsController.HeadSkin.rootBone =
				otherPlayer.AnimatorHelperHair.GetBone(AnimatorHelper.HumanBones.Spine2);
			otherPlayer.tpsController.TPSAnimator = otherPlayer.AnimatorHelperHair.GetComponent<Animator>();
			otherPlayer.tpsController.RagdollComponent = genderItem.Outfit.GetComponent<RagdollHelper>();
			otherPlayer.tpsController.animHelper = otherPlayer.AnimatorHelperHair;
			otherPlayer.tpsController.hips = otherPlayer.AnimatorHelperHair.GetBone(AnimatorHelper.HumanBones.Hips);
			otherPlayer.tpsController.spine2 = otherPlayer.AnimatorHelperHair.GetBone(AnimatorHelper.HumanBones.Spine2);
			otherPlayer.tpsController.ReferenceHead.rootBone =
				otherPlayer.AnimatorHelperHair.GetBone(AnimatorHelper.HumanBones.Spine2);
			otherPlayer.UpdateReferenceHead();
			otherPlayer.RefreshOutfitData();
			otherPlayer.tpsController.TransitionHelperGO.transform.parent =
				otherPlayer.AnimatorHelperHair.GetBone(AnimatorHelper.HumanBones.Spine2);
			otherPlayer.tpsController.TransitionHelperGO.GetComponent<TransitionTriggerHelper>()
				.SetTransferableObject(otherPlayer);
			otherPlayer.tpsController.SetPlayer(otherPlayer);
			otherPlayer.Guid = guid;
			otherPlayer.PlayerName = name;
			otherPlayer.PlayerId = playerId;
			gameObject.name = "Character_" + otherPlayer.Guid;
			otherPlayer.Parent = parent;
			TargetingPoint[] componentsInChildren = otherPlayer.GetComponentsInChildren<TargetingPoint>();
			foreach (TargetingPoint targetingPoint in componentsInChildren)
			{
				targetingPoint.MainObject = otherPlayer;
			}

			world.AddPlayer(otherPlayer.Guid, otherPlayer);
			SceneSpawnPoint sceneSpawnPoint = spawnPointID > 0 && parent.Type == SpaceObjectType.Ship
				? (parent as Ship).GetStructureObject<SceneSpawnPoint>(spawnPointID)
				: null;

			if (sceneSpawnPoint != null)
			{
				otherPlayer.transform.SetPositionAndRotation(sceneSpawnPoint.transform.position, sceneSpawnPoint.transform.rotation);
			}
			else
			{
				otherPlayer.transform.SetLocalPositionAndRotation(position, rotation);
			}

			gameObject.SetActive(value: true);
			otherPlayer.PlayerStatsMessageListener(new PlayerStatsMessage
			{
				GUID = otherPlayer.Guid,
				AnimationStatesMask = animationStatsMask,
				LockedToTriggerID = lockedToTriggerID
			});

			if (otherPlayer.Inventory == null)
			{
				otherPlayer.InitInventory();
			}

			if (sceneSpawnPoint != null && sceneSpawnPoint.Executor != null)
			{
				if (otherPlayer.tpsController == null)
				{
					otherPlayer.tpsController = otherPlayer.transform.GetComponent<OtherCharacterController>();
				}

				sceneSpawnPoint.Executor.SetExecutorDetails(new SceneTriggerExecutorDetails
				{
					PlayerThatActivated = otherPlayer.Guid,
					InSceneID = sceneSpawnPoint.Executor.InSceneID,
					IsImmediate = true,
					IsFail = false,
					CurrentStateID = sceneSpawnPoint.Executor.CurrentStateID,
					NewStateID = sceneSpawnPoint.Executor.GetStateID(sceneSpawnPoint.ExecutorState)
				}, isInstant: false, null, checkCurrentState: false);
			}

			if (world.CharacterInteractionStatesQueue.ContainsKey(otherPlayer.Guid))
			{
				world.CharacterInteractionStatesQueue[otherPlayer.Guid].Executor
					.CharacterInteractInstant(world.CharacterInteractionStatesQueue[otherPlayer.Guid],
						otherPlayer.Guid);
				world.CharacterInteractionStatesQueue.Remove(otherPlayer.Guid);
			}

			otherPlayer.SpawnInventory(dynamicObjects);
			return otherPlayer;
		}

		private void SpawnInventory(DynamicObjectDetails[] dynamicObjects)
		{
			foreach (DynamicObjectDetails details in dynamicObjects ?? Array.Empty<DynamicObjectDetails>())
			{
				DynamicObject.CreateDynamicObject(World, details);
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
			tpsController.CurrentOutfit = o;
			SetOutfitParent(o.OutfitTrans.GetChildren(), tpsController.Outfit, activeGeometry: true);
			RefreshOutfitData();
			tpsController.TransitionHelperGO.transform.parent =
				AnimHelper.GetBone(AnimatorHelper.HumanBones.Spine2);
			tpsController.TransitionHelperGO.transform.Reset();
			Inventory.SetOutfit(o);
			InventorySlot inventorySlot = tpsController.CurrentOutfit.GetSlotsByGroup(InventorySlot.Group.Helmet).Values
				.FirstOrDefault();
			if (inventorySlot != null && inventorySlot.Item != null)
			{
				Helmet helmet = inventorySlot.Item as Helmet;
				helmet.ChangeEquip(Item.EquipType.EquipInventory, this);
				helmet.gameObject.SetActive(value: true);
				helmet.AttachToObject(inventorySlot, sendAttachMessage: false);
			}

			InventorySlot inventorySlot2 = tpsController.CurrentOutfit.GetSlotsByGroup(InventorySlot.Group.Jetpack)
				.Values.FirstOrDefault();
			if (inventorySlot2 != null && inventorySlot2.Item != null)
			{
				Jetpack jetpack = inventorySlot2.Item as Jetpack;
				jetpack.ChangeEquip(Item.EquipType.EquipInventory, this);
				jetpack.gameObject.SetActive(value: true);
				jetpack.AttachToObject(inventorySlot2, sendAttachMessage: false);
			}

			if (HairObject != null)
			{
				HairObject.transform.parent = AnimatorHelperHair.GetBone(AnimatorHelper.HumanBones.Head);
				HairObject.transform.localPosition = Vector3.zero;
				HairObject.transform.localScale = Vector3.one;
				hairMesh = HairObject.GetComponent<SkinnedMeshRenderer>();
			}
		}

		private void RemoveOutfit()
		{
			if (tpsController.CurrentOutfit != null)
			{
				tpsController.CurrentOutfit.SetOutfitParent(tpsController.Outfit.GetChildren(),
					tpsController.CurrentOutfit.OutfitTrans);
				tpsController.CurrentOutfit.FoldedOutfitTrans.gameObject.SetActive(value: true);
				return;
			}

			foreach (Transform child in tpsController.Outfit.GetChildren())
			{
				child.parent = tpsController.BasicOutfitHolder;
				child.gameObject.SetActive(value: false);
			}
		}

		public void TakeOffOutfit()
		{
			InventorySlot inventorySlot = tpsController.CurrentOutfit.GetSlotsByGroup(InventorySlot.Group.Helmet).Values
				.FirstOrDefault();
			if (inventorySlot != null && inventorySlot.Item != null)
			{
				Helmet helmet = inventorySlot.Item as Helmet;
				helmet.ChangeEquip(Item.EquipType.Inventory, this);
				helmet.gameObject.SetActive(value: false);
				helmet.transform.parent = tpsController.CurrentOutfit.transform;
			}

			InventorySlot inventorySlot2 = tpsController.CurrentOutfit.GetSlotsByGroup(InventorySlot.Group.Jetpack)
				.Values.FirstOrDefault();
			if (inventorySlot2 != null && inventorySlot2.Item != null)
			{
				Jetpack jetpack = inventorySlot2.Item as Jetpack;
				jetpack.ChangeEquip(Item.EquipType.Inventory, this);
				jetpack.gameObject.SetActive(value: false);
				jetpack.transform.parent = tpsController.CurrentOutfit.transform;
			}

			RemoveOutfit();
			foreach (Transform child in tpsController.BasicOutfitHolder.GetChildren())
			{
				child.parent = tpsController.Outfit;
				child.localPosition = Vector3.zero;
				child.localRotation =
					Quaternion.Euler(!(child.name == "Root") ? Vector3.zero : new Vector3(0f, 90f, -90f));
				child.gameObject.SetActive(value: true);
			}

			tpsController.CurrentOutfit = null;
			RefreshOutfitData();
			Inventory.SetOutfit(null);
			tpsController.TransitionHelperGO.transform.parent =
				AnimHelper.GetBone(AnimatorHelper.HumanBones.Spine2);
			tpsController.TransitionHelperGO.transform.Reset();
			if (HairObject != null)
			{
				HairObject.transform.parent = AnimatorHelperHair.GetBone(AnimatorHelper.HumanBones.Head);
				HairObject.transform.localPosition = Vector3.zero;
				HairObject.transform.localScale = Vector3.one;
				hairMesh = HairObject.GetComponent<SkinnedMeshRenderer>();
			}
		}

		public void ProcessMovementMessage(long parentGuid, Vector3 position, Quaternion rotation, float freeLookX, float freeLookY,
			float mouseLook, Dictionary<byte, RagdollItemData> ragdollData, CharacterAnimationData animationData, sbyte[] jetpackDirection)
		{
			tpsController.animHelper.ParseData(animationData);
			if (Parent.Guid != parentGuid && World.TryGetSpaceObject(parentGuid, out ArtificialBody newParent))
			{
				Parent = newParent;
				movementReceivedTime = -1f;
			}

			if (Parent.Guid == parentGuid)
			{
				SetMovementData(position, rotation);
			}
			tpsController.TargetMouseLookUpPos = mouseLook;
			tpsController.TargetFreeLookUpPos = freeLookX;
			tpsController.TargetFreeLookRightPos = freeLookY;

			if (ragdollData != null)
			{
				tpsController.animHelper.ToggleMainAnimator(false);
				tpsController.SetRagdollData(ragdollData);
			}
			else
			{
				tpsController.animHelper.ToggleMainAnimator(true);
			}

			if (CurrentJetpack != null)
			{
				if (jetpackDirection != null)
				{
					CurrentJetpack.StartNozzles(new Vector4(jetpackDirection[0], jetpackDirection[1],
						jetpackDirection[2], jetpackDirection[3]));
				}
				else if (!Gravity.IsEpsilonEqual(Vector3.zero))
				{
					CurrentJetpack.StartNozzles(Vector4.zero);
				}
			}
		}

		private void UpdateReferenceHead()
		{
			Transform[] array = new Transform[tpsController.ReferenceHead.bones.Length];
			Transform bone = AnimHelper.GetBone(AnimatorHelper.HumanBones.Spine2);
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = bone.FindChildByName(tpsController.ReferenceHead.bones[i].name);
			}

			tpsController.ReferenceHead.bones = array;
		}

		public void RefreshOutfitData()
		{
			tpsController.animHelper.RebindAndReload();
			tpsController.RagdollComponent.RefreshRagdollVariables();
			Transform[] array = new Transform[tpsController.ReferenceHead.bones.Length];
			Transform bone = tpsController.animHelper.GetBone(AnimatorHelper.HumanBones.Spine2);
			for (int i = 0; i < tpsController.HeadSkin.bones.Length; i++)
			{
				array[i] = bone.FindChildByName(tpsController.ReferenceHead.bones[i].name);
			}

			tpsController.HeadSkin.bones = array;
			tpsController.animHelper.aimIKController.UpdateIKBones();
		}

		private void Update()
		{
			if (!isDrilling || Inventory.CheckIfItemInHandsIsType<HandDrill>())
			{
			}

			tpsController.animHelper.UpdateVelocities();
		}

		public void AnimInteraction_LockEnter()
		{
			if (OnLockStart != null)
			{
				OnLockStart();
			}
		}

		public void AnimInteraction_LockExit()
		{
			if (OnLockComplete != null)
			{
				OnLockComplete();
			}
		}

		public void AnimInteraction_InteractEnter()
		{
			if (OnIteractStart != null)
			{
				OnIteractStart();
			}
		}

		public void AnimInteraction_InteractExit()
		{
			if (OnIteractComplete != null)
			{
				OnIteractComplete();
			}
		}

		public void AnimInteraction_NoneEnter()
		{
		}

		public void AnimInteraction_NoneExit()
		{
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			EventSystem.RemoveListener(typeof(PlayerDrillingMessage), PlayerDrillingMessageListener);
			EventSystem.RemoveListener(typeof(PlayerStatsMessage), PlayerStatsMessageListener);
			World.RemovePlayer(Guid);
		}

		public void PlayerDrillingMessageListener(NetworkData data)
		{
			PlayerDrillingMessage playerDrillingMessage = data as PlayerDrillingMessage;
			if (playerDrillingMessage.DrillersGUID == Guid && Inventory.CheckIfItemInHandsIsType<HandDrill>())
			{
				HandDrill handDrill = Inventory.ItemInHands as HandDrill;
				isDrilling = playerDrillingMessage.isDrilling;
				handDrill.effectScript.ToggleEffect(!playerDrillingMessage.dontPlayEffect);
				handDrill.HurtTrigger.Activate(isDrilling);
				handDrill.drillAnimator.SetBool("Drilling", isDrilling);
			}
		}

		private void PlayerStatsMessageListener(NetworkData data)
		{
			PlayerStatsMessage psm = data as PlayerStatsMessage;
			if (Guid != psm.GUID)
			{
				return;
			}

			tpsController.animHelper.animationData.ReloadType = psm.ReloadType;
			tpsController.animHelper.animationData.IsCrouch = (psm.AnimationStatesMask & 1) != 0;
			tpsController.animHelper.animationData.IsJump = (psm.AnimationStatesMask & 2) != 0;
			tpsController.animHelper.animationData.IsZeroG = (psm.AnimationStatesMask & 4) != 0;
			tpsController.animHelper.animationData.isInStance = (psm.AnimationStatesMask & 8) != 0;
			tpsController.animHelper.animationData.IsReloading = (psm.AnimationStatesMask & 0x10) != 0;
			tpsController.animHelper.animationData.IsGrounded = (psm.AnimationStatesMask & 0x20) != 0;
			tpsController.animHelper.animationData.IsHolster = (psm.AnimationStatesMask & 0x40) != 0;
			tpsController.animHelper.animationData.IsDraw = (psm.AnimationStatesMask & 0x80) != 0;
			tpsController.animHelper.animationData.CancelInteract = (psm.AnimationStatesMask & 0x100) != 0;
			tpsController.animHelper.animationData.IsFalling = (psm.AnimationStatesMask & 0x400) != 0;
			tpsController.animHelper.animationData.isEquipping = (psm.AnimationStatesMask & 0x800) != 0;
			tpsController.animHelper.animationData.TouchingFloor = (psm.AnimationStatesMask & 0x1000) != 0;
			tpsController.animHelper.animationData.UsingTool = (psm.AnimationStatesMask & 0x2000) != 0;
			tpsController.animHelper.animationData.IsEmote = (psm.AnimationStatesMask & 0x4000) != 0;
			tpsController.animHelper.animationData.IsMelee = (psm.AnimationStatesMask & 0x8000) != 0;
			tpsController.animHelper.animationData.UsingLadder = (psm.AnimationStatesMask & 0x10000) != 0;
			tpsController.animHelper.animationData.UseConsumable = (psm.AnimationStatesMask & 0x20000) != 0;
			tpsController.animHelper.animationData.WeaponActivated = (psm.AnimationStatesMask & 0x40000) != 0;
			tpsController.UpdateAnimatorOneFrame();
			if (psm.LockedToTriggerID != null && Parent is SpaceObjectVessel)
			{
				LockedToTrigger = Parent.GeometryRoot.GetComponentsInChildren<BaseSceneTrigger>(includeInactive: true)
					.FirstOrDefault((BaseSceneTrigger m) => m.GetID() == psm.LockedToTriggerID);
			}
			else
			{
				LockedToTrigger = null;
			}

			float num = psm.DamageList == null
				? 0f
				: psm.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.Shot)
					.Sum((PlayerDamage m) => m.Amount);
			if (num > float.Epsilon)
			{
				tpsController.HealthSounds.Play(1);
			}

			float num2 = psm.DamageList == null
				? 0f
				: psm.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.Pressure)
					.Sum((PlayerDamage m) => m.Amount);
			if (num2 > float.Epsilon)
			{
				tpsController.HealthSounds.Play(0);
			}

			float num3 = psm.DamageList == null
				? 0f
				: psm.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.Impact)
					.Sum((PlayerDamage m) => m.Amount);
			if (num3 > float.Epsilon)
			{
				tpsController.HealthSounds.Play(1);
			}

			float num4 = psm.DamageList == null
				? 0f
				: psm.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.Suffocate)
					.Sum((PlayerDamage m) => m.Amount);
			if (num4 > float.Epsilon)
			{
				tpsController.HealthSounds.Play(0);
			}

			float num5 = psm.DamageList == null
				? 0f
				: psm.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.Frost)
					.Sum((PlayerDamage m) => m.Amount);
			if (num5 > float.Epsilon)
			{
			}

			float num6 = psm.DamageList == null
				? 0f
				: psm.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.Heat)
					.Sum((PlayerDamage m) => m.Amount);
			if (num6 > float.Epsilon)
			{
				tpsController.HealthSounds.Play(1);
			}

			float num7 = psm.DamageList == null
				? 0f
				: psm.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.Shred)
					.Sum((PlayerDamage m) => m.Amount);
			if (num7 > float.Epsilon)
			{
				tpsController.HealthSounds.Play(1);
			}

			float num8 = psm.DamageList == null
				? 0f
				: psm.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.SpaceExposure)
					.Sum((PlayerDamage m) => m.Amount);
			if (num8 > float.Epsilon)
			{
				tpsController.HealthSounds.Play(1);
			}
		}

		public void SetGlobalPositionAndRotation(Vector3 position, Quaternion rotation)
		{
			transform.SetPositionAndRotation(position, rotation);
		}

		public override void EnterVessel(SpaceObjectVessel vessel)
		{
		}

		/// <inheritdoc/>
		public override void ExitVessel(bool forceExit)
		{
		}

		public override void ModifyPositionAndRotation(Vector3? position = null, Quaternion? rotation = null)
		{
			tpsController.ModifyPositionAndRotation(position, rotation);
		}

		public override void DockedVesselParentChanged(SpaceObjectVessel vessel)
		{
		}

		public override void OnGravityChanged(Vector3 oldGravity)
		{
		}

		public override void RoomChanged(SceneTriggerRoom prevRoomTrigger)
		{
			base.RoomChanged(prevRoomTrigger);
		}
	}
}
