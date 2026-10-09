using System;
using System.Collections.Generic;
using UnityEngine;
using OpenHellion;
using ZeroGravity;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

public class AnimatorHelper : MonoBehaviour
{
	private static readonly int UseSwayHash = Animator.StringToHash("UseSway");
	private static readonly int BusyEquippingHash = Animator.StringToHash("BusyEquipping");
	private static readonly int GrabHandleHash = Animator.StringToHash("GrabHandle");
	private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");
	private static readonly int WeaponCheckLockHash = Animator.StringToHash("WeaponCheckLock");
	private static readonly int WeaponCheckToggleHash = Animator.StringToHash("WeaponCheckToggle");
	private static readonly int LockTypeHash = Animator.StringToHash("LockType");
	private static readonly int GravityInteractParamHash = Animator.StringToHash("GravityInteractParam");
	private static readonly int ReloadTypeHash = Animator.StringToHash("ReloadType");
	private static readonly int RotationDirectionRightHash = Animator.StringToHash("RotationDirectionRight");
	private static readonly int RotationDirectionForwardHash = Animator.StringToHash("RotationDirectionForward");
	private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
	private static readonly int WeaponBobStrengthHash = Animator.StringToHash("WeaponBobStrength");
	private static readonly int HeadBobStrengthHash = Animator.StringToHash("HeadBobStrength");
	private static readonly int HorizontalRollDirectionHash = Animator.StringToHash("HorizontalRollDirection");
	private static readonly int RotateDirectionHash = Animator.StringToHash("RotateDirection");
	private static readonly int ZeroGHandStateHash = Animator.StringToHash("ZeroGHandState");
	private static readonly int StanceSwitchesHash = Animator.StringToHash("StanceSwitches");
	private static readonly int PlayerStancePreviousHash = Animator.StringToHash("PlayerStancePrevious");
	private static readonly int RollParamHash = Animator.StringToHash("RollParam");
	private static readonly int HeadRightPosHash = Animator.StringToHash("HeadRightPos");
	private static readonly int HeadUpPosHash = Animator.StringToHash("HeadUpPos");
	private static readonly int CanTouchWallHash = Animator.StringToHash("CanTouchWall");
	private static readonly int TurningHash = Animator.StringToHash("Turning");
	private static readonly int RotateUpHash = Animator.StringToHash("RotateUp");
	private static readonly int RotateDownHash = Animator.StringToHash("RotateDown");
	private static readonly int IsMovingZeroGHash = Animator.StringToHash("isMovingZeroG");
	private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
	private static readonly int WeaponActivatedHash = Animator.StringToHash("WeaponActivated");
	private static readonly int UsingLadderHash = Animator.StringToHash("UsingLadder");
	private static readonly int EmoteHash = Animator.StringToHash("Emote");
	private static readonly int UsingToolHash = Animator.StringToHash("UsingTool");
	private static readonly int TouchingFloorHash = Animator.StringToHash("TouchingFloor");
	private static readonly int BaseDisabledHash = Animator.StringToHash("BaseDisabled");
	private static readonly int VelocityForwardHash = Animator.StringToHash("VelocityForward");
	private static readonly int VelocityRightHash = Animator.StringToHash("VelocityRight");
	private static readonly int ZeroGForwardHash = Animator.StringToHash("ZeroGForward");
	private static readonly int ZeroGRightHash = Animator.StringToHash("ZeroGRight");
	private static readonly int InteractTypeHash = Animator.StringToHash("InteractType");
	private static readonly int AirTimeHash = Animator.StringToHash("AirTime");
	private static readonly int FireModeHash = Animator.StringToHash("FireMode");
	private static readonly int GetUpTypeHash = Animator.StringToHash("GetUpType");
	private static readonly int PlayerStanceFloatHash = Animator.StringToHash("PlayerStanceFloat");
	private static readonly int LadderDirectionHash = Animator.StringToHash("LadderDirection");
	private static readonly int MeleeAttackTypeHash = Animator.StringToHash("MeleeAttackType");
	private static readonly int ReloadItemTypeHash = Animator.StringToHash("ReloadItemType");
	private static readonly int EmoteTypeHash = Animator.StringToHash("EmoteType");
	private static readonly int EquipItemIdHash = Animator.StringToHash("EquipItemId");
	private static readonly int EquipOrDeEquipHash = Animator.StringToHash("EquipOrDeEquip");
	private static readonly int TurningDirectionHash = Animator.StringToHash("TurningDirection");
	private static readonly int ReloadingHash = Animator.StringToHash("Reloading");
	private static readonly int InStanceHash = Animator.StringToHash("InStance");
	private static readonly int PlayerStanceHash = Animator.StringToHash("PlayerStance");
	private static readonly int IsZeroGHash = Animator.StringToHash("isZeroG");
	private static readonly int CrouchHash = Animator.StringToHash("Crouch");

	public class AnimationData
	{
		public float VelocityForward;

		public float VelocityRight;

		public float ZeroGForward;

		public float ZeroGRight;

		public bool IsCrouch;

		public bool IsJump;

		public bool IsZeroG;

		public bool IsMoving;

		public bool IsMovingZeroG;

		public bool Turning;

		public int TurningDirection;

		public bool CanTouchWall;

		public int ZeroGHandState;

		public int RotateDirection;

		public int HorizontalRollDirection;

		public bool isInStance;

		public bool IsHolster;

		public bool IsDraw;

		public bool IsReloading;

		public bool IsGrounded;

		public int PlayerStance;

		public float InteractType;

		public bool CancelInteract;

		public bool OldIsJump;

		public bool OldIsHolster;

		public bool OldIsDraw;

		public bool OldCancelInteract;

		public bool OldIsEquipping;

		public bool IsFalling;

		public bool isEquipping;

		public float EquipItemId;

		public float EquipOrDeEquip;

		public bool TouchingFloor;

		public bool UsingTool;

		public bool IsEmote;

		public float EmoteType;

		public float ReloadType;

		public float ReloadItemType;

		public bool IsMelee;

		public bool OldMelee;

		public float MeleeAttackType;

		public bool UsingLadder;

		public float LadderDirection;

		public float PlayerStanceFloat;

		public float GetUpType;

		public bool UseConsumable;

		public bool OldUseConsumable;

		public float FireMode;

		public float AirTime;

		public bool WeaponActivated;
	}

	public enum Parameter
	{
		Reloading = 1,
		isMoving = 2,
		InStance = 3,
		PlayerStance = 4,
		isZeroG = 5,
		WeaponCheckLock = 6,
		AirTime = 7,
		LeftFoot = 8,
		RightFoot = 9,
		VelocityForward = 10,
		VelocityRight = 11,
		BusyEquipping = 12,
		EquipOrDeEquip = 13,
		IsGrounded = 14,
		ReloadType = 15,
		Crouch = 16
	}

	public enum Triggers
	{
		Jump = 1,
		GetUpFromBelly = 2,
		GetUpFromBack = 3,
		Holster = 4,
		Draw = 5,
		Grounded = 6,
		InteractTrigger = 7,
		CancelInteract = 8,
		Shoot = 9,
		Pickup = 10,
		Drop = 11,
		Lock = 12,
		LockImmediate = 13,
		EquipItem = 14,
		UnlockImmediate = 15,
		Melee = 16,
		InstantStandUp = 17,
		UseConsumable = 18,
		WantsToSwitchStance = 19
	}

	public enum HumanBones
	{
		Hips = 1,
		Spine1 = 2,
		Spine2 = 3,
		Neck = 4,
		Head = 5,
		LeftUpLeg = 6,
		LeftLeg = 7,
		LeftFoot = 8,
		RightUpLeg = 9,
		RightLeg = 10,
		RightFoot = 11,
		LeftShoulder = 12,
		LeftArm = 13,
		LeftForearm = 14,
		LeftHand = 15,
		RightShoulder = 16,
		RightArm = 17,
		RightForearm = 18,
		RightHand = 19,
		LeftToe = 20,
		RightToe = 21,
		RightInteractBone = 22,
		LeftInteractBone = 23,
		Root = 100,
		LeftUpLegRoll = 101,
		LeftLegRoll = 102,
		LeftToe_END = 103,
		RightUpLegRoll = 104,
		RightLegRoll = 105,
		RightToe_END = 106,
		LeftArmRoll = 107,
		LeftForearmRoll = 108,
		Head_END = 109,
		RightArmRoll = 110,
		RightForearmRoll = 111
	}

	public enum InteractType
	{
		None = 0,
		ControlPanel02 = 1,
		LeverDoorHandle = 2,
		EnterCryo = 3,
		ExitCryo = 4,
		Chair_Sit = 5,
		Chair_StandUp = 6,
		Ladder_Up = 7,
		Ladder_Down = 8,
		DockingPanel_Grab = 9,
		DockingPanel_Release = 10
	}

	public enum LockType
	{
		None = 0,
		EnterCryo = 1,
		ExitCryo = 2,
		Chair_Sit_Idle = 3,
		Chair_StandUp_Idle = 4,
		Ladder_Up = 5,
		Ladder_Down = 6,
		DockingPanel_Grab = 7,
		DockingPanel_Release = 8
	}

	public enum ReloadType
	{
		JustLoad = 1,
		FullReload = 2,
		Unload = 3
	}

	public enum EmoteType
	{
		Signaling = 0,
		OK = 1
	}

	public enum GravityInteractParam
	{
		OneG = 0,
		ZeroG = 1
	}

	public enum AnimatorLayers_FPS
	{
		Base = 0,
		JumpLayer = 1,
		ZeroGRollLayer = 2,
		StanceLayer_1G = 3,
		StanceLayer_0G = 4,
		WeaponSwayHorizontal = 5,
		WeaponSwayVertical = 6,
		LegLayer_0G = 7,
		WeaponInteractions = 8,
		InteractionLayer = 9,
		ShootingLayer = 10,
		MeleeLayer = 11,
		GrabDropLayer = 12,
		IKGrabLayer = 13,
		EquipLayer = 14,
		ToolUseLayer = 15,
		ConsumableLayer = 16,
		EmoteLayer = 17,
		LadderLayer = 18
	}

	public enum AnimatorLayers_TPS
	{
		Base = 0,
		JumpLayer = 1,
		StanceLayer = 2,
		StanceLayerFull = 3,
		LegLayer_0G = 4,
		WeaponInteractions = 5,
		InteractionLayer = 6,
		ShootingLayer = 7,
		MeleeLayer = 8,
		GrabDropLayer = 9,
		MouseLookVertical = 10,
		FreeLookHorizontal = 11,
		FreeLookVertical = 12,
		EquipLayer = 13,
		ToolUseLayer = 14,
		ConsumableLayer = 15,
		EmoteLayer = 16,
		LadderLayer = 17
	}

	public enum EquipOrDeEquip
	{
		Equip = 0,
		DeEquip = 1
	}

	public enum UnlockAnimator
	{
		Reload = 0,
		Equip = 1
	}

	public enum ReloadStepType
	{
		ReloadStart = 1,
		ItemSwitch = 2,
		UnloadEnd = 3,
		ReloadEnd = 100
	}

	private class AnimatorStateData
	{
		public readonly float AnimationTime;

		public readonly int AnimationNameHash;

		public AnimatorStateData(Animator anim, int animHash, float animTime)
		{
			AnimationNameHash = animHash;
			AnimationTime = animTime;
		}
	}

	private class AnimatorParameterData
	{
		public readonly AnimatorControllerParameterType ParamType;

		public readonly string ParamName;

		private readonly object _savedData;

		public AnimatorParameterData(Animator anim, string paramName, AnimatorControllerParameterType paramType)
		{
			ParamType = paramType;
			ParamName = paramName;
			switch (paramType)
			{
				case AnimatorControllerParameterType.Int:
					_savedData = anim.GetInteger(paramName);
					break;
				case AnimatorControllerParameterType.Float:
					_savedData = anim.GetFloat(paramName);
					break;
				case AnimatorControllerParameterType.Bool:
					_savedData = anim.GetBool(paramName);
					break;
			}
		}

		public object GetParam()
		{
			return _savedData;
		}
	}

	public bool doneSwitchingState = true;

	private bool _consumableLock;

	[SerializeField] private Animator animMain;

	[SerializeField] private Animator animBob;

	public Action DropTask;

	public Action AfterDropTask;

	private readonly List<AnimatorParameterData> _parameters = new List<AnimatorParameterData>();

	private readonly List<AnimatorStateData> _stateData = new List<AnimatorStateData>();

	private readonly Dictionary<HumanBones, Transform> _bones = new Dictionary<HumanBones, Transform>();

	private Player _player;

	private MyPlayer.PlayerStance _currentPlayerStance = MyPlayer.PlayerStance.Passive;

	public AnimationData animationData = new AnimationData();

	private AnimatorOverrideController _animOverride;

	private ItemType _oldItemType;

	public AimIKController aimIKController;

	[SerializeField] private AnimationClip defaultPickup;

	[SerializeField] private AnimationClip defaultThrow;

	private float _velocityForwardStart;

	private float _velocityForwardEnd;

	private float _velocityRightStart;

	private float _velocityRightEnd;

	private float _velocityLerpVal;

	private bool _wasInAir;

	private bool _oldReload;

	private bool _canPlayLand;

	private float _playerStancePrevious;

	private bool _animatorIsZeroG;

	public bool CanRun => animMain.GetInteger(PlayerStanceHash) < 3;

	public bool CanSwitchState => !animMain.GetCurrentAnimatorStateInfo(12).IsName("Pickup") &&
	                              !animMain.GetCurrentAnimatorStateInfo(12).IsName("Drop");

	public bool IsSpecialStance => animMain.GetInteger(PlayerStanceHash) == 3;

	public bool IsConsumableInUse => _consumableLock;

	public bool CanShoot => !MyPlayer.Instance.InIteractLayer && !MyPlayer.Instance.InLerpingState &&
	                        !GetParameterBool(Parameter.WeaponCheckLock) && !GetParameterBool(Parameter.Reloading) &&
	                        _currentPlayerStance != MyPlayer.PlayerStance.Passive && doneSwitchingState &&
	                        !GetParameterBool(Parameter.BusyEquipping);

	public bool CanPickUp => !MyPlayer.Instance.InIteractLayer && !MyPlayer.Instance.InLerpingState &&
	                         !GetParameterBool(Parameter.Reloading) && !GetParameterBool(Parameter.BusyEquipping) &&
	                         !_consumableLock && CanSwitchState;

	public bool CanDrop => !MyPlayer.Instance.InIteractLayer && !MyPlayer.Instance.InLerpingState &&
	                       !GetParameterBool(Parameter.WeaponCheckLock) && !GetParameterBool(Parameter.Reloading) &&
	                       !GetParameterBool(Parameter.BusyEquipping) && !_consumableLock && CanSwitchState;

	public bool CanSpecial => !MyPlayer.Instance.InIteractLayer && !MyPlayer.Instance.InLerpingState &&
	                          !GetParameterBool(Parameter.Reloading) && !GetParameterBool(Parameter.WeaponCheckLock) &&
	                          !GetParameterBool(Parameter.BusyEquipping) && !_consumableLock;

	public bool CanMelee => CanDrop && !animMain.GetCurrentAnimatorStateInfo(11).IsName("MeleeItem") &&
	                        !animMain.GetCurrentAnimatorStateInfo(11).IsName("MeleeFists");

	public void ParseData(CharacterAnimationData data)
	{
		animationData.VelocityForward = MathHelper.ProportionalValue(data.VelocityForward, 0f, 255f, -1f, 1f);
		animationData.VelocityRight = MathHelper.ProportionalValue(data.VelocityRight, 0f, 255f, -1f, 1f);
		animationData.ZeroGForward = !animationData.IsZeroG
			? 0f
			: MathHelper.ProportionalValue(data.ZeroGForward, 0f, 255f, -1f, 1f);
		animationData.ZeroGRight = !animationData.IsZeroG
			? 0f
			: MathHelper.ProportionalValue(data.ZeroGRight, 0f, 255f, -1f, 1f);
		animationData.InteractType = data.InteractType;
		animationData.PlayerStance = data.PlayerStance;
		animationData.TurningDirection = data.TurningDirection;
		animationData.EquipOrDeEquip = data.EquipOrDeEquip;
		animationData.EquipItemId = data.EquipItemId;
		animationData.EmoteType = data.EmoteType;
		animationData.ReloadItemType = data.ReloadItemType;
		animationData.MeleeAttackType = data.MeleeAttackType;
		animationData.LadderDirection = data.LadderDirection;
		animationData.PlayerStanceFloat = data.PlayerStanceFloat;
		animationData.GetUpType = data.GetUpType;
		animationData.FireMode = data.FireMode;
		animationData.AirTime = data.AirTime;
		animationData.IsMoving =
			(!animationData.IsZeroG && animationData.VelocityForward.IsNotEpsilonZero(0.01f)) ||
			animationData.VelocityRight.IsNotEpsilonZero(0.01f);
		animationData.IsMovingZeroG =
			(animationData.IsZeroG && animationData.ZeroGForward.IsNotEpsilonZero(0.01f)) ||
			animationData.ZeroGRight.IsNotEpsilonZero(0.01f);
	}

	public CharacterAnimationData GetAnimationData(bool isJump, bool isDraw, bool isHolster, bool cancelInteract,
		float airTime, bool isEquippingItem, bool isMelee, bool useConsumable, out int AnimationStatsMask)
	{
		int num = 0;
		if (animMain.GetBool(CrouchHash))
		{
			num |= 1;
		}

		if (isJump)
		{
			num |= 2;
		}

		if (animMain.GetBool(IsZeroGHash))
		{
			num |= 4;
		}

		if (animMain.GetBool(InStanceHash))
		{
			num |= 8;
		}

		if (animMain.GetBool(ReloadingHash))
		{
			num |= 0x10;
		}

		if (animMain.GetBool("IsGrounded"))
		{
			num |= 0x20;
		}

		if (isHolster)
		{
			num |= 0x40;
		}

		if (isDraw)
		{
			num |= 0x80;
		}

		if (cancelInteract)
		{
			num |= 0x100;
		}

		if (airTime > 0.4f)
		{
			num |= 0x400;
		}

		if (isEquippingItem)
		{
			num |= 0x800;
		}

		if (animMain.GetBool(TouchingFloorHash))
		{
			num |= 0x1000;
		}

		if (animMain.GetBool(UsingToolHash))
		{
			num |= 0x2000;
		}

		if (animMain.GetBool(EmoteHash))
		{
			num |= 0x4000;
		}

		if (animMain.GetBool(UsingLadderHash))
		{
			num |= 0x10000;
		}

		if (isMelee)
		{
			num |= 0x8000;
		}

		if (useConsumable)
		{
			num |= 0x20000;
		}

		if (animMain.GetBool(WeaponActivatedHash))
		{
			num |= 0x40000;
		}

		AnimationStatsMask = num;
		return new CharacterAnimationData()
		{
			VelocityForward = (byte)MathHelper.ProportionalValue(animMain.GetFloat(VelocityForwardHash), -1f, 1f, 0f, 255f),
			VelocityRight = (byte)MathHelper.ProportionalValue(animMain.GetFloat(VelocityRightHash), -1f, 1f, 0f, 255f),
			ZeroGForward = (byte)MathHelper.ProportionalValue(animMain.GetFloat(ZeroGForwardHash), -1f, 1f, 0f, 255f),
			ZeroGRight = (byte)MathHelper.ProportionalValue(animMain.GetFloat(ZeroGRightHash), -1f, 1f, 0f, 255f),
			InteractType = (byte)animMain.GetFloat(InteractTypeHash),
			PlayerStance = (byte)animMain.GetInteger(PlayerStanceHash),
			TurningDirection = (byte)animMain.GetInteger(TurningDirectionHash),
			EquipOrDeEquip = (byte)animMain.GetFloat(EquipOrDeEquipHash),
			EquipItemId = (byte)animMain.GetFloat(EquipItemIdHash),
			EmoteType = (byte)animMain.GetFloat(EmoteTypeHash),
			ReloadItemType = (byte)animMain.GetFloat(ReloadItemTypeHash),
			MeleeAttackType = (byte)animMain.GetFloat(MeleeAttackTypeHash),
			LadderDirection = (sbyte)animMain.GetFloat(LadderDirectionHash),
			PlayerStanceFloat = (byte)animMain.GetFloat(PlayerStanceFloatHash),
			GetUpType = (byte)animMain.GetFloat(GetUpTypeHash),
			FireMode = (byte)animMain.GetFloat(FireModeHash),
			AirTime = animMain.GetFloat(AirTimeHash),
		};
	}

	private void Awake()
	{
		CreateRig();
		_animOverride = new AnimatorOverrideController
		{
			runtimeAnimatorController = animMain.runtimeAnimatorController
		};
	}

	private void Start()
	{
		_player = GetComponentInParent<Player>();
		if (_player is MyPlayer && (animMain.layerCount > Enum.GetNames(typeof(AnimatorLayers_FPS)).Length ||
		                            animMain.layerCount < Enum.GetNames(typeof(AnimatorLayers_FPS)).Length))
		{
			Debug.LogError("FPS animator layer enum is out of date (layerCount != EnumCount)");
		}
		else if ((_player is OtherPlayer && animMain.layerCount > Enum.GetNames(typeof(AnimatorLayers_TPS)).Length) ||
		         animMain.layerCount < Enum.GetNames(typeof(AnimatorLayers_TPS)).Length)
		{
			Debug.LogError("TPS animator layer enum is out of date (layerCount != EnumCount)");
		}
	}

	public Dictionary<HumanBones, Transform> GetBones()
	{
		return _bones;
	}

	public void CreateRig()
	{
		_bones[HumanBones.Hips] = transform.Find("Root/Hips");
		_bones[HumanBones.Spine1] = transform.Find("Root/Hips/Spine1");
		_bones[HumanBones.Spine2] = transform.Find("Root/Hips/Spine1/Spine2");
		_bones[HumanBones.Neck] = transform.Find("Root/Hips/Spine1/Spine2/Neck");
		_bones[HumanBones.Head] = transform.Find("Root/Hips/Spine1/Spine2/Neck/Head");
		_bones[HumanBones.LeftUpLeg] = transform.Find("Root/Hips/LeftUpLeg");
		_bones[HumanBones.LeftLeg] = transform.Find("Root/Hips/LeftUpLeg/LeftUpLegRoll/LeftLeg");
		_bones[HumanBones.LeftFoot] =
			transform.Find("Root/Hips/LeftUpLeg/LeftUpLegRoll/LeftLeg/LeftLegRoll/LeftFoot");
		_bones[HumanBones.RightUpLeg] = transform.Find("Root/Hips/RightUpLeg");
		_bones[HumanBones.RightLeg] = transform.Find("Root/Hips/RightUpLeg/RightUpLegRoll/RightLeg");
		_bones[HumanBones.RightFoot] =
			transform.Find("Root/Hips/RightUpLeg/RightUpLegRoll/RightLeg/RightLegRoll/RightFoot");
		_bones[HumanBones.LeftShoulder] = transform.Find("Root/Hips/Spine1/Spine2/LeftShoulder");
		_bones[HumanBones.LeftArm] = transform.Find("Root/Hips/Spine1/Spine2/LeftShoulder/LeftArm");
		_bones[HumanBones.LeftForearm] =
			transform.Find("Root/Hips/Spine1/Spine2/LeftShoulder/LeftArm/LeftArmRoll/LeftForearm");
		_bones[HumanBones.LeftHand] =
			transform.Find(
				"Root/Hips/Spine1/Spine2/LeftShoulder/LeftArm/LeftArmRoll/LeftForearm/LeftForearmRoll/LeftHand");
		_bones[HumanBones.RightShoulder] = transform.Find("Root/Hips/Spine1/Spine2/RightShoulder");
		_bones[HumanBones.RightArm] = transform.Find("Root/Hips/Spine1/Spine2/RightShoulder/RightArm");
		_bones[HumanBones.RightForearm] =
			transform.Find("Root/Hips/Spine1/Spine2/RightShoulder/RightArm/RightArmRoll/RightForearm");
		_bones[HumanBones.RightHand] =
			transform.Find(
				"Root/Hips/Spine1/Spine2/RightShoulder/RightArm/RightArmRoll/RightForearm/RightForearmRoll/RightHand");
		_bones[HumanBones.LeftToe] =
			transform.Find("Root/Hips/LeftUpLeg/LeftUpLegRoll/LeftLeg/LeftLegRoll/LeftFoot/LeftToe");
		_bones[HumanBones.RightToe] =
			transform.Find("Root/Hips/RightUpLeg/RightUpLegRoll/RightLeg/RightLegRoll/RightFoot/RightToe");
		_bones[HumanBones.RightInteractBone] = transform.Find(
			"Root/Hips/Spine1/Spine2/RightShoulder/RightArm/RightArmRoll/RightForearm/RightForearmRoll/RightHand/RIGHT INTERACT");
		_bones[HumanBones.LeftInteractBone] = transform.Find(
			"Root/Hips/Spine1/Spine2/LeftShoulder/LeftArm/LeftArmRoll/LeftForearm/LeftForearmRoll/LeftHand/LEFT INTERACT");
		_bones[HumanBones.Root] = transform.Find("Root");
		_bones[HumanBones.LeftUpLegRoll] = transform.Find("Root/Hips/LeftUpLeg/LeftUpLegRoll");
		_bones[HumanBones.LeftLegRoll] = transform.Find("Root/Hips/LeftUpLeg/LeftUpLegRoll/LeftLeg/LeftLegRoll");
		_bones[HumanBones.LeftToe_END] =
			transform.Find("Root/Hips/LeftUpLeg/LeftUpLegRoll/LeftLeg/LeftLegRoll/LeftFoot/LeftToe/LeftToe_END");
		_bones[HumanBones.RightUpLegRoll] = transform.Find("Root/Hips/RightUpLeg/RightUpLegRoll");
		_bones[HumanBones.RightLegRoll] =
			transform.Find("Root/Hips/RightUpLeg/RightUpLegRoll/RightLeg/RightLegRoll");
		_bones[HumanBones.RightToe_END] =
			transform.Find(
				"Root/Hips/RightUpLeg/RightUpLegRoll/RightLeg/RightLegRoll/RightFoot/RightToe/RightToe_END");
		_bones[HumanBones.LeftArmRoll] =
			transform.Find("Root/Hips/Spine1/Spine2/LeftShoulder/LeftArm/LeftArmRoll");
		_bones[HumanBones.LeftForearmRoll] =
			transform.Find("Root/Hips/Spine1/Spine2/LeftShoulder/LeftArm/LeftArmRoll/LeftForearm/LeftForearmRoll");
		_bones[HumanBones.Head_END] = transform.Find("Root/Hips/Spine1/Spine2/Neck/Head/Head_END");
		_bones[HumanBones.RightArmRoll] =
			transform.Find("Root/Hips/Spine1/Spine2/RightShoulder/RightArm/RightArmRoll");
		_bones[HumanBones.RightForearmRoll] =
			transform.Find(
				"Root/Hips/Spine1/Spine2/RightShoulder/RightArm/RightArmRoll/RightForearm/RightForearmRoll");
	}

	public Transform GetBone(HumanBones bone)
	{
		return _bones[bone];
	}

	private void SaveAnimatorData()
	{
		for (int i = 0; i < animMain.parameters.Length; i++)
		{
			AnimatorControllerParameter animatorControllerParameter = animMain.parameters[i];
			if (animatorControllerParameter.type != AnimatorControllerParameterType.Trigger &&
			    !animMain.IsParameterControlledByCurve(animatorControllerParameter.nameHash))
			{
				AnimatorParameterData item = new AnimatorParameterData(animMain, animatorControllerParameter.name,
					animatorControllerParameter.type);
				_parameters.Add(item);
			}
		}

		for (int j = 0; j < animMain.layerCount; j++)
		{
			AnimatorStateData item2 = new AnimatorStateData(animMain,
				animMain.GetCurrentAnimatorStateInfo(j).fullPathHash,
				animMain.GetCurrentAnimatorStateInfo(j).normalizedTime);
			_stateData.Add(item2);
		}
	}

	private void ReloadAnimatorData()
	{
		foreach (AnimatorParameterData parameter in _parameters)
		{
			switch (parameter.ParamType)
			{
				case AnimatorControllerParameterType.Int:
					animMain.SetInteger(parameter.ParamName, (int)parameter.GetParam());
					break;
				case AnimatorControllerParameterType.Float:
					animMain.SetFloat(parameter.ParamName, (float)parameter.GetParam());
					break;
				case AnimatorControllerParameterType.Bool:
					animMain.SetBool(parameter.ParamName, (bool)parameter.GetParam());
					break;
			}
		}

		for (int i = 0; i < animMain.layerCount; i++)
		{
			animMain.Play(_stateData[i].AnimationNameHash, i, _stateData[i].AnimationTime);
		}

		_parameters.Clear();
		_stateData.Clear();
	}

	public void OverrideItemAnimations(ItemAnimations.FPSAnimations itemAnims, ItemType type, bool needsFullOverride,
		Item item, bool overrideForPickup = false)
	{
		if (itemAnims is ItemAnimations.TPSAnimations)
		{
			if (needsFullOverride)
			{
				animMain.SetInteger(BaseDisabledHash, 1);
			}
			else
			{
				animMain.SetInteger(BaseDisabledHash, 0);
			}

			if (item is Weapon)
			{
				animMain.SetLayerWeight(10, (!item.useIkForTargeting) ? 1 : 0);
				if (item.useIkForTargeting)
				{
					aimIKController.aimIK.solver.transform = item.ikTargetingPoint;
				}
				else
				{
					aimIKController.aimIK.solver.transform = null;
				}
			}
		}

		if (_player is MyPlayer)
		{
			bool? useSway = item.useSwayAnimations;
			SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
				null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
				null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
				useSway);
		}

		if (_oldItemType == type && !overrideForPickup && !item.forceAnimOverride)
		{
			return;
		}

		SaveAnimatorData();
		if (!overrideForPickup)
		{
			if (itemAnims is ItemAnimations.TPSAnimations tpsAnimations)
			{
				_animOverride.AddIfExists("Male_Stance_1", itemAnims.Passive_Idle);
				_animOverride.AddIfExists("Male_Stance1_Walk_Forward", itemAnims.Passive_WalkForward);
				_animOverride.AddIfExists("Male_Stance1_Walk_Backward", itemAnims.Passive_WalkBackward);
				_animOverride.AddIfExists("Male_Stance1_Walk_Right", itemAnims.Passive_WalkRight);
				_animOverride.AddIfExists("Male_Stance1_Walk_Left", itemAnims.Passive_WalkLeft);
				_animOverride.AddIfExists("Male_Stance1_RunForward", itemAnims.Passive_Run);
				_animOverride.AddIfExists("Male_Stance1_Walk_Forward_Right", tpsAnimations.Passive_Walk_Forward_Right);
				_animOverride.AddIfExists("Male_Stance1_Walk_Forward_Left", tpsAnimations.Passive_Walk_Forward_Left);
				_animOverride.AddIfExists("Male_Stance1_Walk_Backward_Right",
					tpsAnimations.Passive_Walk_Backward_Right);
				_animOverride.AddIfExists("Male_Stance1_Walk_Backward_Left", tpsAnimations.Passive_Walk_Backward_Left);
				_animOverride.AddIfExists("Male_Stance1_RunForwardRight", tpsAnimations.Passive_Run_Forward_Right);
				_animOverride.AddIfExists("Male_Stance1_RunForwardLeft", tpsAnimations.Passive_Run_Forward_Left);
				_animOverride.AddIfExists("Male_Stance1_Run_Right", tpsAnimations.Passive_Run_Right);
				_animOverride.AddIfExists("Male_Stance1_Run_Left", tpsAnimations.Passive_Run_Left);
				_animOverride.AddIfExists("Male_Stance_1_ZeroG", tpsAnimations.Passive_0G_Idle);
				_animOverride.AddIfExists("Male_Stance_01_Jump_InAir", tpsAnimations.Passive_InAir);
				_animOverride.AddIfExists("Male_Stance_2", itemAnims.Active_Idle);
				_animOverride.AddIfExists("Male_Stance2_Walk_Forward", itemAnims.Active_WalkForward);
				_animOverride.AddIfExists("Male_Stance2_Walk_Backward", itemAnims.Active_WalkBackward);
				_animOverride.AddIfExists("Male_Stance2_Walk_Right", itemAnims.Active_WalkRight);
				_animOverride.AddIfExists("Male_Stance2_Walk_Left", itemAnims.Active_WalkLeft);
				_animOverride.AddIfExists("Male_Stance2_RunForward", itemAnims.Active_Run);
				_animOverride.AddIfExists("Male_Stance2_Walk_Forward_Right", tpsAnimations.Active_Walk_Forward_Right);
				_animOverride.AddIfExists("Male_Stance2_Walk_Forward_Left", tpsAnimations.Active_Walk_Forward_Left);
				_animOverride.AddIfExists("Male_Stance2_Walk_Backward_Right", tpsAnimations.Active_Walk_Backward_Right);
				_animOverride.AddIfExists("Male_Stance2_Walk_Backward_Left", tpsAnimations.Active_Walk_Backward_Left);
				_animOverride.AddIfExists("Male_Stance2_RunForwardRight", tpsAnimations.Active_Run_Forward_Right);
				_animOverride.AddIfExists("Male_Stance2_RunForwardLeft", tpsAnimations.Active_Run_Forward_Left);
				_animOverride.AddIfExists("Male_Stance2_Run_Right", tpsAnimations.Active_Run_Right);
				_animOverride.AddIfExists("Male_Stance2_Run_Left", tpsAnimations.Active_Run_Left);
				_animOverride.AddIfExists("Male_Stance_2_ZeroG", tpsAnimations.Active_0G_Idle);
				_animOverride.AddIfExists("Male_Stance_02_Jump_InAir", tpsAnimations.Active_InAir);
				_animOverride.AddIfExists("Male_Stance_3", itemAnims.Special_Idle);
				_animOverride.AddIfExists("Male_Stance3_Walk_Forward", itemAnims.Special_WalkForward);
				_animOverride.AddIfExists("Male_Stance3_Walk_Backward", tpsAnimations.Special_Walk_Backward);
				_animOverride.AddIfExists("Male_Stance3_Walk_Right", tpsAnimations.Special_Walk_Right);
				_animOverride.AddIfExists("Male_Stance3_Walk_Left", tpsAnimations.Special_Walk_Left);
				_animOverride.AddIfExists("Male_Stance3_Walk_Forward_Right", tpsAnimations.Special_Walk_Forward_Right);
				_animOverride.AddIfExists("Male_Stance3_Walk_Forward_Left", tpsAnimations.Special_Walk_Forward_Left);
				_animOverride.AddIfExists("Male_Stance3_Walk_Backward_Right",
					tpsAnimations.Special_Walk_Backward_Right);
				_animOverride.AddIfExists("Male_Stance3_Walk_Backward_Left", tpsAnimations.Special_Walk_Backward_Left);
				_animOverride.AddIfExists("Male_Stance_3_ZeroG", tpsAnimations.Special_0G_Idle);
				_animOverride.AddIfExists("Male_Stance_2_Shoot", itemAnims.Stance2_Shooting_Standard);
				_animOverride.AddIfExists("Male_Stance_3_Shoot", itemAnims.Stance3_Shooting_Standard);
				_animOverride.AddIfExists("Male_Medkit_Use", itemAnims.Consumable_Use);
				if (GetFloatFromItemType(item.Type) == 1f)
				{
					_animOverride.AddIfExists("Male_Stance_1_Load", tpsAnimations.Stance1_Load);
					_animOverride.AddIfExists("Male_Stance_1_Reload", tpsAnimations.Stance1_Reload);
					_animOverride.AddIfExists("Male_Stance_1_UnLoad", tpsAnimations.Stance1_Unload);
					_animOverride.AddIfExists("Male_Stance_2_Load", tpsAnimations.Stance2_Load);
					_animOverride.AddIfExists("Male_Stance_2_Reload", tpsAnimations.Stance2_Reload);
					_animOverride.AddIfExists("Male_Stance_2_UnLoad", tpsAnimations.Stance2_Unload);
				}

				_animOverride.AddIfExists("Male_Stance1_CrouchIdle", tpsAnimations.Stance1_CrouchWalk_Idle);
				_animOverride.AddIfExists("Male_Stance1_CrouchWalk_Forward", tpsAnimations.Stance1_CrouchWalk_Forward);
				_animOverride.AddIfExists("Male_Stance1_CrouchWalk_Backward",
					tpsAnimations.Stance1_CrouchWalk_Backward);
				_animOverride.AddIfExists("Male_Stance1_CrouchWalk_Right", tpsAnimations.Stance1_CrouchWalk_Right);
				_animOverride.AddIfExists("Male_Stance1_CrouchWalk_Left", tpsAnimations.Stance1_CrouchWalk_Left);
				_animOverride.AddIfExists("Male_Stance1_CrouchWalk_Forward_Right",
					tpsAnimations.Stance1_CrouchWalk_Forward_Right);
				_animOverride.AddIfExists("Male_Stance1_CrouchWalk_Forward_Left",
					tpsAnimations.Stance1_CrouchWalk_Forward_Left);
				_animOverride.AddIfExists("Male_Stance1_CrouchWalk_Backward_Right",
					tpsAnimations.Stance1_CrouchWalk_Backward_Right);
				_animOverride.AddIfExists("Male_Stance1_CrouchWalk_Backward_Left",
					tpsAnimations.Stance1_CrouchWalk_Backward_Left);
				_animOverride.AddIfExists("Male_Stance2_CrouchIdle", tpsAnimations.Stance2_CrouchWalk_Idle);
				_animOverride.AddIfExists("Male_Stance2_CrouchWalk_Forward", tpsAnimations.Stance2_CrouchWalk_Forward);
				_animOverride.AddIfExists("Male_Stance2_CrouchWalk_Backward",
					tpsAnimations.Stance2_CrouchWalk_Backward);
				_animOverride.AddIfExists("Male_Stance2_CrouchWalk_Right", tpsAnimations.Stance2_CrouchWalk_Right);
				_animOverride.AddIfExists("Male_Stance2_CrouchWalk_Left", tpsAnimations.Stance2_CrouchWalk_Left);
				_animOverride.AddIfExists("Male_Stance2_CrouchWalk_Forward_Right",
					tpsAnimations.Stance2_CrouchWalk_Forward_Right);
				_animOverride.AddIfExists("Male_Stance2_CrouchWalk_Forward_Left",
					tpsAnimations.Stance2_CrouchWalk_Forward_Left);
				_animOverride.AddIfExists("Male_Stance2_CrouchWalk_Backward_Right",
					tpsAnimations.Stance2_CrouchWalk_Backward_Right);
				_animOverride.AddIfExists("Male_Stance2_CrouchWalk_Backward_Left",
					tpsAnimations.Stance2_CrouchWalk_Backward_Left);
				_animOverride.AddIfExists("Male_Stance3_CrouchIdle", tpsAnimations.Stance3_CrouchWalk_Idle);
				_animOverride.AddIfExists("Male_Stance3_CrouchWalk_Forward", tpsAnimations.Stance3_CrouchWalk_Forward);
				_animOverride.AddIfExists("Male_Stance3_CrouchWalk_Backward",
					tpsAnimations.Stance3_CrouchWalk_Backward);
				_animOverride.AddIfExists("Male_Stance3_CrouchWalk_Right", tpsAnimations.Stance3_CrouchWalk_Right);
				_animOverride.AddIfExists("Male_Stance3_CrouchWalk_Left", tpsAnimations.Stance3_CrouchWalk_Left);
				_animOverride.AddIfExists("Male_Stance3_CrouchWalk_Forward_Right",
					tpsAnimations.Stance3_CrouchWalk_Forward_Right);
				_animOverride.AddIfExists("Male_Stance3_CrouchWalk_Forward_Left",
					tpsAnimations.Stance3_CrouchWalk_Forward_Left);
				_animOverride.AddIfExists("Male_Stance3_CrouchWalk_Backward_Right",
					tpsAnimations.Stance3_CrouchWalk_Backward_Right);
				_animOverride.AddIfExists("Male_Stance3_CrouchWalk_Backward_Left",
					tpsAnimations.Stance3_CrouchWalk_Backward_Left);
				_animOverride.AddIfExists("Male_Helmet_On", tpsAnimations.Item_Equip);
				_animOverride.AddIfExists("Male_Helmet_Off", tpsAnimations.Item_DeEquip);
				_animOverride.AddIfExists("Male_Grenade_Cock", itemAnims.Weapon_Activation);
				_animOverride.AddIfExists("Male_Grenade_Cocked_Idle", itemAnims.Weapon_ActivatedIdle);
				_animOverride.AddIfExists("Male_Grenade_Uncock", itemAnims.Weapon_ActivationCancel);
				_animOverride.AddIfExists("Male_Crowbar_Swing", itemAnims.Melee_Passive);
				_animOverride.AddIfExists("Male_Crowbar_Swing2", itemAnims.Melee_Passive2);
			}
			else
			{
				_animOverride.AddIfExists("Male_FPS_Stance_1_Idle", itemAnims.Passive_Idle);
				_animOverride.AddIfExists("Male_FPS_Stance_1_WalkForward", itemAnims.Passive_WalkForward);
				_animOverride.AddIfExists("Male_FPS_Stance_1_WalkBack", itemAnims.Passive_WalkBackward);
				_animOverride.AddIfExists("Male_FPS_Stance_1_WalkRight", itemAnims.Passive_WalkRight);
				_animOverride.AddIfExists("Male_FPS_Stance_1_WalkLeft", itemAnims.Passive_WalkLeft);
				_animOverride.AddIfExists("Male_FPS_Stance_1_Run", itemAnims.Passive_Run);
				_animOverride.AddIfExists("Male_FPS_Stance_2_Idle", itemAnims.Active_Idle);
				_animOverride.AddIfExists("Male_FPS_Stance_2_WalkForward", itemAnims.Active_WalkForward);
				_animOverride.AddIfExists("Male_FPS_Stance_2_WalkBack", itemAnims.Active_WalkBackward);
				_animOverride.AddIfExists("Male_FPS_Stance_2_WalkRight", itemAnims.Active_WalkRight);
				_animOverride.AddIfExists("Male_FPS_Stance_2_WalkLeft", itemAnims.Active_WalkLeft);
				_animOverride.AddIfExists("Male_FPS_Stance_2_Run", itemAnims.Active_Run);
				_animOverride.AddIfExists("Male_FPS_Stance_3_Idle", itemAnims.Special_Idle);
				_animOverride.AddIfExists("Male_FPS_Stance_3_WalkForward", itemAnims.Special_WalkForward);
				_animOverride.AddIfExists("Male_FPS_Stance1ToStance2", itemAnims.Stance1ToStance2);
				_animOverride.AddIfExists("Male_FPS_Stance1ToStance3", itemAnims.Stance1ToStance3);
				_animOverride.AddIfExists("Male_FPS_Stance2ToStance1", itemAnims.Stance2ToStance1);
				_animOverride.AddIfExists("Male_FPS_Stance2ToStance3", itemAnims.Stance2ToStance3);
				_animOverride.AddIfExists("Male_FPS_Stance3ToStance1", itemAnims.Stance3ToStance1);
				_animOverride.AddIfExists("Male_FPS_Stance3ToStance2", itemAnims.Stance3ToStance2);
				_animOverride.AddIfExists("Male_FPS_Stance_1_Jump", itemAnims.Passive_Jump);
				_animOverride.AddIfExists("Male_FPS_Stance_1_Land", itemAnims.Passive_Land);
				_animOverride.AddIfExists("Male_FPS_Stance_2_Jump", itemAnims.Active_Jump);
				_animOverride.AddIfExists("Male_FPS_Stance_2_Land", itemAnims.Active_Land);
				_animOverride.AddIfExists("Male_FPS_Stance_3_Jump", itemAnims.Special_Jump);
				_animOverride.AddIfExists("Male_FPS_Stance_3_Land", itemAnims.Special_Land);
				_animOverride.AddIfExists("Sway_Down", itemAnims.Sway_Down);
				_animOverride.AddIfExists("Sway_Up", itemAnims.Sway_Up);
				_animOverride.AddIfExists("Sway_Left", itemAnims.Sway_Left);
				_animOverride.AddIfExists("Sway_Right", itemAnims.Sway_Right);
				_animOverride.AddIfExists("Sway_Idle", itemAnims.Sway_Idle);
				_animOverride.AddIfExists("Male_FPS_AmmoCheck_Idle", itemAnims.WeaponCheck_Idle);
				_animOverride.AddIfExists("Male_FPS_Stance1_to_AmmoCheck", itemAnims.Stance1_WeaponCheck_IdleToCheck);
				_animOverride.AddIfExists("Male_FPS_AmmoCheck_to_Stance1", itemAnims.Stance1_WeaponCheck_CheckToIdle);
				_animOverride.AddIfExists("Male_FPS_Stance2_To_AmmoCheck", itemAnims.Stance2_WeaponCheck_IdleToCheck);
				_animOverride.AddIfExists("Male_FPS_AmmoCheck_To_Stance2", itemAnims.Stance2_WeaponCheck_CheckToIdle);
				_animOverride.AddIfExists("Male_FPS_Stance_2_Shoot", itemAnims.Stance2_Shooting_Standard);
				_animOverride.AddIfExists("Male_FPS_Stance_3_Shoot", itemAnims.Stance3_Shooting_Standard);
				_animOverride.AddIfExists("Male_Medkit_Use_FPS", itemAnims.Consumable_Use);
				_animOverride.AddIfExists("Male_Grenade_Cock_FPS", itemAnims.Weapon_Activation);
				_animOverride.AddIfExists("Male_Grenade_Cocked_Idle_FPS", itemAnims.Weapon_ActivatedIdle);
				_animOverride.AddIfExists("Male_Grenade_Uncock_FPS", itemAnims.Weapon_ActivationCancel);
				_animOverride.AddIfExists("Male_Crowbar_Swing_FPS", itemAnims.Melee_Passive);
				_animOverride.AddIfExists("Male_Crowbar_Swing_2_FPS", itemAnims.Melee_Passive2);
				if (GetFloatFromItemType(item.Type) == 1f)
				{
					_animOverride.AddIfExists("Male_FPS_Stance_1_Load", itemAnims.Stance1_Load);
					_animOverride.AddIfExists("Male_FPS_Stance_1_Reload", itemAnims.Stance1_Reload);
					_animOverride.AddIfExists("Male_FPS_Stance_1_UnLoad", itemAnims.Stance1_Unload);
					_animOverride.AddIfExists("Male_FPS_Stance_2_Load", itemAnims.Stance2_Load);
					_animOverride.AddIfExists("Male_FPS_Stance_2_Reload", itemAnims.Stance2_Reload);
					_animOverride.AddIfExists("Male_FPS_Stance_2_UnLoad", itemAnims.Stance2_Unload);
				}

				_animOverride.AddIfExists("NoviThrow_Stance1",
					(!(itemAnims.Drop_Stance1 != null)) ? defaultThrow : itemAnims.Drop_Stance1);
				_animOverride.AddIfExists("NoviThrow_Stance2",
					(!(itemAnims.Drop_Stance2 != null)) ? defaultThrow : itemAnims.Drop_Stance2);
				_animOverride.AddIfExists("Male_Helmet_On", itemAnims.Item_Equip);
				_animOverride.AddIfExists("Male_Helmet_Off", itemAnims.Item_DeEquip);
			}
		}
		else
		{
			_animOverride.AddIfExists("NoviGrab",
				(!(itemAnims.Pickup_ToHands != null)) ? defaultPickup : itemAnims.Pickup_ToHands);
		}

		animMain.runtimeAnimatorController = _animOverride;
		animMain.Rebind();
		if (itemAnims is ItemAnimations.TPSAnimations)
		{
			animMain.SetLayerWeight(3, needsFullOverride ? 1 : 0);
			animMain.SetLayerWeight(2, (!needsFullOverride) ? 1 : 0);
		}

		ReloadAnimatorData();
		if (!overrideForPickup)
		{
			_oldItemType = type;
		}
	}

	public void RebindAndReload()
	{
		SaveAnimatorData();
		animMain.Rebind();
		ReloadAnimatorData();
		CreateRig();
	}

	public void Reset()
	{
		animMain.Rebind();
		CreateRig();
	}

	public void UpdateVelocities()
	{
		_velocityLerpVal = Mathf.Clamp01(_velocityLerpVal + Time.deltaTime * 10f);
		float? velocityForward = Mathf.Lerp(_velocityForwardStart, _velocityForwardEnd, _velocityLerpVal);
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, velocityForward);
		velocityForward = Mathf.Lerp(_velocityRightStart, _velocityRightEnd, _velocityLerpVal);
		SetParameter(null, null, null, null, null, null, null, null, null, null, velocityForward);
	}

	public void UpdateTPSAnimatorOneFrame()
	{
		ReloadType? reloadType = (ReloadType)animationData.ReloadType;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, reloadType);
		bool? reload = animationData.IsReloading;
		SetParameter(null, null, null, null, null, null, null, null, null, reload);
		SetParameter(animationData.IsCrouch);
		reload = animationData.IsZeroG;
		SetParameter(null, null, reload);
		reload = animationData.isInStance;
		SetParameter(null, null, null, null, null, null, null, null, reload);
		reload = animationData.IsGrounded;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, reload);
		reload = animationData.TouchingFloor;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, reload);
		reload = animationData.UsingTool;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, reload);
		reload = animationData.IsEmote;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, reload);
		reload = animationData.UsingLadder;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, reload);
		if (animationData.IsJump != animationData.OldIsJump)
		{
			if (animationData.IsJump)
			{
				SetParameterTrigger(Triggers.Jump);
			}

			animationData.OldIsJump = animationData.IsJump;
		}

		if (animationData.IsHolster != animationData.OldIsHolster)
		{
			if (animationData.IsHolster)
			{
				SetParameterTrigger(Triggers.Holster);
			}

			animationData.OldIsHolster = animationData.IsHolster;
		}

		if (animationData.IsDraw != animationData.OldIsDraw)
		{
			if (animationData.IsDraw)
			{
				SetParameterTrigger(Triggers.Draw);
			}

			animationData.OldIsDraw = animationData.IsDraw;
		}

		if (animationData.CancelInteract != animationData.OldCancelInteract)
		{
			if (animationData.CancelInteract)
			{
				SetParameterTrigger(Triggers.CancelInteract);
			}

			animationData.OldCancelInteract = animationData.CancelInteract;
		}

		if (animationData.isEquipping != animationData.OldIsEquipping)
		{
			if (animationData.isEquipping)
			{
				SetParameterTrigger(Triggers.EquipItem);
			}

			animationData.OldIsEquipping = animationData.isEquipping;
		}

		if (animationData.IsMelee != animationData.OldMelee)
		{
			if (animationData.IsMelee)
			{
				SetParameterTrigger(Triggers.Melee);
			}

			animationData.OldMelee = animationData.IsMelee;
		}

		if (animationData.UseConsumable != animationData.OldUseConsumable)
		{
			if (animationData.UseConsumable)
			{
				SetParameterTrigger(Triggers.UseConsumable);
			}

			animationData.OldUseConsumable = animationData.UseConsumable;
		}
	}

	public void UpdateTPSAnimatorConstant()
	{
		_velocityForwardStart = Mathf.Lerp(_velocityForwardStart, _velocityForwardEnd, _velocityLerpVal);
		_velocityRightStart = Mathf.Lerp(_velocityRightStart, _velocityRightEnd, _velocityLerpVal);
		_velocityForwardEnd = animationData.VelocityForward;
		_velocityRightEnd = animationData.VelocityRight;
		_velocityLerpVal = 0f;
		float? zeroGForward = animationData.ZeroGForward;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, zeroGForward);
		zeroGForward = animationData.ZeroGRight;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			zeroGForward);
		InteractType? interactType = (InteractType)animationData.InteractType;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, interactType);
		MyPlayer.PlayerStance? playerStance = (MyPlayer.PlayerStance)animationData.PlayerStance;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, playerStance);
		int? turningDirection = animationData.TurningDirection;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, turningDirection);
		bool? isMoving = animationData.IsMoving;
		SetParameter(null, isMoving);
		isMoving = animationData.IsMovingZeroG;
		SetParameter(null, null, null, isMoving);
		ItemType? equipItemId = (ItemType)animationData.EquipItemId;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, equipItemId);
		EquipOrDeEquip? equipOrDeEquip = (EquipOrDeEquip)animationData.EquipOrDeEquip;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, equipOrDeEquip);
		EmoteType? emoteType = (EmoteType)animationData.EmoteType;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, emoteType);
		equipItemId = (ItemType)animationData.ReloadItemType;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, equipItemId);
		zeroGForward = animationData.MeleeAttackType;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, zeroGForward);
		zeroGForward = animationData.LadderDirection;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, zeroGForward);
		zeroGForward = animationData.PlayerStanceFloat;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, zeroGForward);
		zeroGForward = animationData.GetUpType;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, zeroGForward);
		zeroGForward = animationData.FireMode;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, zeroGForward);
		zeroGForward = animationData.AirTime;
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			zeroGForward);
	}

	public void SetParameter(bool? isCrouch = null, bool? isMoving = null, bool? isZeroG = null,
		bool? isMovingZeroG = null, bool? rotateUp = null, bool? rotateDown = null, bool? isTurning = null,
		bool? canTouchWall = null, bool? inStance = null, bool? reload = null, float? velocityRight = null,
		float? velocityForward = null, float? headUpPos = null, float? headRightPos = null, float? zeroGForward = null,
		float? zeroGRight = null, float? rollParam = null, MyPlayer.PlayerStance? playerStance = null,
		int? turningDirection = null, int? zeroGHandState = null, int? rotateDirection = null,
		int? horizontalRollDirection = null, float? headBobStrength = null, float? weaponBobStrength = null,
		bool? isGrounded = null, InteractType? interactType = null, float? rotationDirectionForward = null,
		float? rotationDirectionRight = null, ReloadType? reloadType = null, GravityInteractParam? gravityParam = null,
		LockType? lockType = null, bool? weaponCheckToggle = null, bool? weaponCheckLock = null, float? airTime = null,
		bool? isFalling = null, bool? grabHandle = null, ItemType? equipItemId = null, bool? busyEquipping = null,
		EquipOrDeEquip? equipOrDeEquip = null, bool? touchingFloor = null, bool? usingTool = null,
		EmoteType? emoteType = null, bool? emote = null, ItemType? reloadItemType = null, float? meleeAttackType = null,
		bool? usingLadder = null, float? ladderDirection = null, float? playerStanceFloat = null,
		float? getUpType = null, float? fireMode = null, bool? useSway = null, bool? weaponActivated = null)
	{
		if (isCrouch.HasValue)
		{
			animMain.SetBool(CrouchHash, isCrouch.Value);
			if (animBob != null)
			{
				animBob.SetBool(CrouchHash, isCrouch.Value);
			}
		}

		if (isMoving.HasValue)
		{
			animMain.SetBool(IsMovingHash, isMoving.Value);
			if (animBob != null)
			{
				animBob.SetBool(IsMovingHash, isMoving.Value);
			}
		}

		if (isZeroG.HasValue)
		{
			animMain.SetBool(IsZeroGHash, isZeroG.Value);
			_animatorIsZeroG = isZeroG.Value;
			if (animBob != null)
			{
				animBob.SetBool(IsZeroGHash, isZeroG.Value);
			}
		}

		if (isMovingZeroG.HasValue)
		{
			animMain.SetBool(IsMovingZeroGHash, isMovingZeroG.Value);
			if (animBob != null)
			{
				animBob.SetBool(IsMovingZeroGHash, isMovingZeroG.Value);
			}
		}

		if (rotateUp.HasValue)
		{
			animMain.SetBool(RotateUpHash, rotateUp.Value);
		}

		if (rotateDown.HasValue)
		{
			animMain.SetBool(RotateDownHash, rotateDown.Value);
		}

		if (isTurning.HasValue)
		{
			animMain.SetBool(TurningHash, isTurning.Value);
			if (animBob != null)
			{
				animBob.SetBool(TurningHash, isTurning.Value);
			}
		}

		if (canTouchWall.HasValue)
		{
			animMain.SetBool(CanTouchWallHash, canTouchWall.Value);
		}

		if (inStance.HasValue)
		{
			animMain.SetBool(InStanceHash, inStance.Value);
		}

		if (reload.HasValue)
		{
			animMain.SetBool(ReloadingHash, reload.Value);
			if (_player is MyPlayer && !reload.Value && _currentPlayerStance == MyPlayer.PlayerStance.Special)
			{
				(_player as MyPlayer).ChangeCamerasFov(Globals.Instance.SpecialCameraFov);
			}

			if (_oldReload == reload.Value)
			{
				return;
			}

			_oldReload = reload.Value;
			if (!_oldReload)
			{
			}
		}

		if (velocityRight.HasValue)
		{
			animMain.SetFloat(VelocityRightHash, velocityRight.Value);
			if (animBob != null)
			{
				animBob.SetFloat(VelocityRightHash, velocityRight.Value);
			}
		}

		if (velocityForward.HasValue)
		{
			animMain.SetFloat(VelocityForwardHash, velocityForward.Value);
			if (animBob != null)
			{
				animBob.SetFloat(VelocityForwardHash, velocityForward.Value);
			}
		}

		if (velocityForward.HasValue && _player is MyPlayer && _player.World != null && _player.World.InGameGUI.HelmetOverlayModel != null)
		{
			if (!_animatorIsZeroG)
			{
				_player.World.InGameGUI.HelmetOverlayModel.SetMovement(velocityForward.Value, velocityRight.Value);
			}
			else
			{
				_player.World.InGameGUI.HelmetOverlayModel.SetMovement(0f, 0f);
			}
		}

		if (headUpPos.HasValue)
		{
			animMain.SetFloat(HeadUpPosHash, headUpPos.Value);
		}

		if (headRightPos.HasValue)
		{
			animMain.SetFloat(HeadRightPosHash, headRightPos.Value);
		}

		if (zeroGForward.HasValue)
		{
			animMain.SetFloat(ZeroGForwardHash, zeroGForward.Value);
		}

		if (zeroGRight.HasValue)
		{
			animMain.SetFloat(ZeroGRightHash, zeroGRight.Value);
		}

		if (rollParam.HasValue)
		{
			animMain.SetFloat(RollParamHash, rollParam.Value);
		}

		if (playerStance.HasValue && _currentPlayerStance != playerStance.Value)
		{
			if (_currentPlayerStance == MyPlayer.PlayerStance.Passive)
			{
				doneSwitchingState = false;
			}

			if (_player is MyPlayer)
			{
				if (animMain.GetCurrentAnimatorStateInfo(4).IsName("StanceSwitches") ||
				    animMain.GetCurrentAnimatorStateInfo(3).IsName("StanceSwitches"))
				{
					float num = ((!_animatorIsZeroG)
						? animMain.GetCurrentAnimatorStateInfo(3).normalizedTime
						: animMain.GetCurrentAnimatorStateInfo(4).normalizedTime);
					animMain.CrossFade(StanceSwitchesHash, 0f, (!_animatorIsZeroG) ? 3 : 4, 1f - num % 1f);
				}
				else
				{
					SetParameterTrigger(Triggers.WantsToSwitchStance);
				}

				animMain.SetFloat(PlayerStancePreviousHash, (float)_currentPlayerStance);
			}

			_currentPlayerStance = playerStance.Value;
			animMain.SetFloat(PlayerStanceFloatHash, (float)_currentPlayerStance);
			animMain.SetInteger(PlayerStanceHash, (int)_currentPlayerStance);
		}

		if (turningDirection.HasValue)
		{
			animMain.SetInteger(TurningDirectionHash, turningDirection.Value);
		}

		if (zeroGHandState.HasValue)
		{
			animMain.SetInteger(ZeroGHandStateHash, zeroGHandState.Value);
		}

		if (rotateDirection.HasValue)
		{
			animMain.SetInteger(RotateDirectionHash, rotateDirection.Value);
		}

		if (horizontalRollDirection.HasValue)
		{
			animMain.SetInteger(HorizontalRollDirectionHash, horizontalRollDirection.Value);
		}

		if (animBob != null && headBobStrength.HasValue)
		{
			animBob.SetFloat(HeadBobStrengthHash, headBobStrength.Value);
		}

		if (animBob != null && weaponBobStrength.HasValue)
		{
			animBob.SetFloat(WeaponBobStrengthHash, weaponBobStrength.Value);
		}

		if (isGrounded.HasValue)
		{
			animMain.SetBool(IsGroundedHash, isGrounded.Value);
			if (_wasInAir && isGrounded.Value)
			{
				if (_canPlayLand)
				{
					_canPlayLand = false;
				}

				_wasInAir = false;
			}

			if (animBob != null)
			{
				animBob.SetBool(IsGroundedHash, isGrounded.Value);
			}
		}

		if (interactType.HasValue)
		{
			animMain.SetFloat(InteractTypeHash, (float)interactType.Value);
		}

		if (rotationDirectionForward.HasValue)
		{
			animMain.SetFloat(RotationDirectionForwardHash, rotationDirectionForward.Value);
		}

		if (rotationDirectionRight.HasValue)
		{
			animMain.SetFloat(RotationDirectionRightHash, rotationDirectionRight.Value);
		}

		if (reloadType.HasValue)
		{
			animMain.SetFloat(ReloadTypeHash, (float)reloadType.Value);
		}

		if (gravityParam.HasValue)
		{
			animMain.SetFloat(GravityInteractParamHash, (float)gravityParam.Value);
		}

		if (lockType.HasValue)
		{
			animMain.SetFloat(LockTypeHash, (float)lockType.Value);
		}

		if (weaponCheckToggle.HasValue)
		{
			animMain.SetBool(WeaponCheckToggleHash, weaponCheckToggle.Value);
		}

		if (weaponCheckLock.HasValue)
		{
			animMain.SetBool(WeaponCheckLockHash, weaponCheckLock.Value);
		}

		if (airTime.HasValue)
		{
			animMain.SetFloat(AirTimeHash, airTime.Value);
			if (airTime.Value > 0.3f)
			{
				_wasInAir = true;
			}

			if (animBob != null)
			{
				animBob.SetFloat(AirTimeHash, airTime.Value);
			}
		}

		if (isFalling.HasValue)
		{
			animMain.SetBool(IsFallingHash, isFalling.Value);
		}

		if (grabHandle.HasValue)
		{
			animMain.SetBool(GrabHandleHash, grabHandle.Value);
		}

		if (equipItemId.HasValue)
		{
			if (_player is MyPlayer)
			{
				animMain.SetFloat(EquipItemIdHash, GetFloatFromItemType(equipItemId.Value));
			}
			else
			{
				animMain.SetFloat(EquipItemIdHash, (float)equipItemId.Value);
			}
		}

		if (busyEquipping.HasValue)
		{
			animMain.SetBool(BusyEquippingHash, busyEquipping.Value);
		}

		if (equipOrDeEquip.HasValue)
		{
			animMain.SetFloat(EquipOrDeEquipHash, (float)equipOrDeEquip.Value);
		}

		if (touchingFloor.HasValue)
		{
			animMain.SetBool(TouchingFloorHash, touchingFloor.Value);
		}

		if (usingTool.HasValue)
		{
			animMain.SetBool(UsingToolHash, usingTool.Value);
		}

		if (reloadItemType.HasValue)
		{
			if (_player is MyPlayer)
			{
				animMain.SetFloat(ReloadItemTypeHash, GetFloatFromItemType(reloadItemType.Value));
			}
			else
			{
				animMain.SetFloat(ReloadItemTypeHash, (float)reloadItemType.Value);
			}
		}

		if (meleeAttackType.HasValue)
		{
			animMain.SetFloat(MeleeAttackTypeHash, meleeAttackType.Value);
		}

		if (usingLadder.HasValue)
		{
			animMain.SetBool(UsingLadderHash, usingLadder.Value);
		}

		if (ladderDirection.HasValue)
		{
			animMain.SetFloat(LadderDirectionHash, ladderDirection.Value);
		}

		if (getUpType.HasValue)
		{
			animMain.SetFloat(GetUpTypeHash, getUpType.Value);
		}

		if (fireMode.HasValue)
		{
			animMain.SetFloat(FireModeHash, fireMode.Value);
		}

		if (useSway.HasValue)
		{
			animMain.SetBool(UseSwayHash, useSway.Value);
		}

		if (weaponActivated.HasValue)
		{
			animMain.SetBool(WeaponActivatedHash, weaponActivated.Value);
		}
	}

	private float GetFloatFromItemType(ItemType type)
	{
		if (ItemTypeRange.IsWeapon(type))
		{
			return 1f;
		}

		if (ItemTypeRange.IsAmmo(type))
		{
			return 4f;
		}

		if (ItemTypeRange.IsOutfit(type))
		{
			return 6f;
		}

		if (ItemTypeRange.IsHelmet(type))
		{
			return 7f;
		}

		if (ItemTypeRange.IsBattery(type))
		{
			return 11f;
		}

		if (ItemTypeRange.IsCanister(type))
		{
			return 12f;
		}

		return -1f;
	}

	public void SetParameterTrigger(Triggers param)
	{
		if (param == Triggers.InteractTrigger || param == Triggers.Lock || param == Triggers.LockImmediate)
		{
			if (GetParameterBool(Parameter.isZeroG))
			{
				SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
					null, null, null, null, null, null, null, null, null, null, null, null, null, null,
					GravityInteractParam.ZeroG);
			}
			else
			{
				SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
					null, null, null, null, null, null, null, null, null, null, null, null, null, null,
					GravityInteractParam.OneG);
			}
		}

		if (param != Triggers.InstantStandUp)
		{
			animMain.SetTrigger(param.ToString());
		}

		if (param == Triggers.Jump)
		{
			_canPlayLand = true;
		}

		if (animBob != null && (param == Triggers.Jump || param == Triggers.InstantStandUp))
		{
			animBob.SetTrigger(param.ToString());
		}

		if (_player is MyPlayer)
		{
			if (param == Triggers.UseConsumable)
			{
				MyPlayer.Instance.FpsController.UseConsumableTriggered = true;
			}

			if (param == Triggers.Melee)
			{
				MyPlayer.Instance.FpsController.MeleeTriggered = true;
			}
		}
	}

	public void ResetParameterTrigger(Triggers param)
	{
		animMain.ResetTrigger(param.ToString());
	}

	public bool GetParameterBool(Parameter param)
	{
		return animMain.GetBool(param.ToString());
	}

	public float GetParameterFloat(Parameter param)
	{
		return animMain.GetFloat(param.ToString());
	}

	public void ToggleMainAnimator(bool? isEnabled)
	{
		if (isEnabled.HasValue)
		{
			if (animMain.enabled != isEnabled.Value)
			{
				animMain.enabled = isEnabled.Value;
			}
		}
		else
		{
			animMain.enabled = !animMain.enabled;
		}
	}

	public void SetLayerWeight(AnimatorLayers_TPS layer, float weight)
	{
		animMain.SetLayerWeight((int)layer, weight);
	}

	public void SetLayerWeight(AnimatorLayers_FPS layer, float weight)
	{
		animMain.SetLayerWeight((int)layer, weight);
	}

	public float GetLayerWeight(AnimatorLayers_FPS layer)
	{
		return animMain.GetLayerWeight((int)layer);
	}

	public float GetLayerWeight(AnimatorLayers_TPS layer)
	{
		return animMain.GetLayerWeight((int)layer);
	}

	public bool IsCurrentAnimState(AnimatorLayers_TPS layer, string name)
	{
		return animMain.GetCurrentAnimatorStateInfo((int)layer).IsName(name);
	}

	public bool IsCurrentAnimState(AnimatorLayers_FPS layer, string name)
	{
		return animMain.GetCurrentAnimatorStateInfo((int)layer).IsName(name);
	}

	private void ReloadStart()
	{
		if (_player is MyPlayer)
		{
			(_player as MyPlayer).ReloadStepComplete(ReloadStepType.ReloadStart);
		}
	}

	private void MagazineSwitch()
	{
		if (_player is MyPlayer)
		{
			(_player as MyPlayer).ReloadStepComplete(ReloadStepType.ItemSwitch);
		}
	}

	private void ReloadEnd(int unload)
	{
		if (_player is MyPlayer)
		{
			(_player as MyPlayer).ReloadStepComplete(
				(unload != 0) ? ReloadStepType.UnloadEnd : ReloadStepType.ReloadEnd);
		}
	}

	private void PickupEvent(int type)
	{
	}

	private void DropEvent()
	{
		if (DropTask != null && _player is MyPlayer)
		{
			this.CancelInvoke(DropEvent);
			DropTask();
			DropTask = null;
			if (AfterDropTask != null)
			{
				AfterDropTask();
				AfterDropTask = null;
			}
		}
	}

	public void SetDropTask(Action task)
	{
		DropTask = task;
		this.Invoke(DropEvent, 1f);
	}

	private void WeaponCheckStart()
	{
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, true);
	}

	private void WeaponCheckEnd()
	{
		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, false);
	}

	private void HolsterFinished()
	{
	}

	private void DrawFinished()
	{
	}

	private void EquipStart()
	{
		if (_player is MyPlayer)
		{
			(_player as MyPlayer).EquipAnimStart();
		}

		SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
			null, null, null, true);
	}

	private void EquipEnd(int equipping)
	{
		if (_player is MyPlayer player)
		{
			player.EquipAnimationEnd(equipping);
		}
	}

	private void HelmetToggleCanvas(int state)
	{
	}

	public void Footstep(AnimationEvent animationEvent)
	{
	}

	public void ToggleWeaponStanceSwitch(bool status)
	{
		doneSwitchingState = status;
		animMain.SetBool("DoneSwitchingState", status);
	}

	public void ToggleConsumableLock(bool status)
	{
		_consumableLock = status;
	}

	public void ForceAnimationUpdate()
	{
		animMain.Update(0f);
	}

	public void MeleeAttackEvent()
	{
		if (_player.CurrentActiveItem != null)
		{
			_player.CurrentActiveItem.AttackWithItem();
		}
	}

	public void MeleeAttackEventFists()
	{
		if (_player is MyPlayer player)
		{
			player.MeleeAttack();
		}
	}
}
