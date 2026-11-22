using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;
using OpenHellion.Net;

namespace ZeroGravity.Objects
{
	public class PortableTurret : Item, IVesselSystemAccessory
	{
		private static readonly int CanTargetHash = Animator.StringToHash("CanTarget");
		private static readonly int IsDestroyedHash = Animator.StringToHash("IsDestroyed");
		private static readonly int IsStunnedHash = Animator.StringToHash("IsStunned");
		private static readonly int IsActiveHash = Animator.StringToHash("IsActive");
		private Animator turretAnimator;

		[SerializeField] private PortableTurretTargettingHelper targettingHelper;

		public Transform detectionRaycastSource;

		public List<Vector3> raycastDirections = new List<Vector3>();

		[Range(0f, 1f)] public float animationPercentage_Horizontal;

		[Range(0f, 1f)] public float animationPercentage_Vertical;

		private float oldPercentage_Horizontal;

		private float oldPercentage_Vertical;

		[SerializeField] private Transform turretBaseMovable;

		[SerializeField] private Transform turretBaseStatic;

		private HashSet<TargetingPoint> Targets = new HashSet<TargetingPoint>();

		private HashSet<TargetingPoint> TargetIgnoreList = new HashSet<TargetingPoint>();

		public GameObject UnfoldedItem;

		public GameObject FoldedItem;

		public Collider[] UnfoldedColliders;

		public Collider[] FoldedColliders;

		[SerializeField] private TargetingPoint currentTarget;

		public float leftRightAngle;

		public float upDownAngle;

		public float hFrameDifference;

		public float vFrameDifference;

		[Tooltip("HP per second")] public float Damage;

		private float elapsedTime;

		private float LookAtTime = 2f;

		private bool isDestroyed;

		private bool isStunned;

		private bool rotationRightPlaying;

		private bool rotationUpPlaying;

		[SerializeField] private ParticleSystem ShootingEffect;

		public SoundEffect TurretSound;

		private float radius = 50f;

		private bool playTargetingSound;

		[SerializeField] private float turretAngularSpeed;

		private bool isShooting;

		private bool shootingSoundPlaying;

		private int shootingRaycastMask;

		private float targetTimer;

		private float shootTimer = 0.1f;

		public override EquipType EquipTo => EquipType.Hands;

		public bool IsActive => AttachPoint is ActiveSceneAttachPoint && !isStunned && !isDestroyed;

		public bool CanTarget => IsActive && BaseVesselSystem is { Status: SystemStatus.Online };

		public TargetingPoint CurrentTarget
		{
			get => currentTarget;
			set
			{
				if (!(currentTarget == value))
				{
					if (value != null && currentTarget == null)
					{
						animationPercentage_Horizontal =
							turretAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;
						oldPercentage_Horizontal = turretAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;
						animationPercentage_Vertical = 0f;
						oldPercentage_Vertical = 0.25f;
						turretAnimator.SetBool("HasTarget", value: true);
						TurretSound.Play(3);
					}
					else if ((!(value == null) || !(currentTarget != null)) && value != null && currentTarget != null &&
					         !(value.MainObject != currentTarget.MainObject))
					{
					}

					currentTarget = value;
				}
			}
		}

		public VesselSystem BaseVesselSystem
		{
			get =>
				!(AttachPoint != null) || !(AttachPoint is ActiveSceneAttachPoint)
					? null
					: (AttachPoint as ActiveSceneAttachPoint).BaseVesselSystem;
			set { }
		}

		private new void Awake()
		{
			base.Awake();
			turretAnimator = GetComponentInChildren<Animator>(includeInactive: true);
			shootingRaycastMask = (1 << LayerMask.NameToLayer("Default")) | (1 << LayerMask.NameToLayer("Player")) |
			                      (1 << LayerMask.NameToLayer("DynamicObject"));
			EventSystem.AddListener(typeof(PortableTurretShootingMessage), PortableTurretShootingMessageListener);
			radius = targettingHelper.GetComponent<SphereCollider>().radius;
		}

		public override DynamicObjectAuxData GetAuxData()
		{
			PortableTurretData baseAuxData = GetBaseAuxData<PortableTurretData>();
			baseAuxData.Damage = Damage;
			return baseAuxData;
		}

		public override void ProcesStatsData(DynamicObjectStats data)
		{
			PortableTurretStats portableTurretStats = data as PortableTurretStats;
			if (portableTurretStats.Health.HasValue)
			{
				if (Health > 0f && portableTurretStats.Health.Value <= 0f)
				{
					DestroyTurret();
				}
				else if (Health <= MaxHealth * 0.5f &&
				         portableTurretStats.Health.Value > MaxHealth * 0.5f)
				{
					RestoreTurret();
				}
			}

			base.ProcesStatsData(portableTurretStats);
			if (portableTurretStats.IsStunned.HasValue)
			{
				isStunned = portableTurretStats.IsStunned.Value;
				ShootingEffect.Stop();
				isShooting = false;
				animationPercentage_Horizontal = 0f;
				animationPercentage_Vertical = 0f;
				oldPercentage_Horizontal = 0f;
				oldPercentage_Vertical = 0f;
			}
		}

		private void RestoreTurret()
		{
			isDestroyed = false;
			if (AttachPoint != null)
			{
				OnEnable();
			}
		}

		private void DestroyTurret()
		{
			isDestroyed = true;
			StopShooting();
		}

		public void ChangeLook(bool isFolded)
		{
		}

		public override void TakeDamage(Dictionary<TypeOfDamage, float> damages)
		{
		}

		public override void OnAttach(bool isAttached, bool isOnPlayer)
		{
			if (!(UnfoldedItem == null) && !(FoldedItem == null))
			{
				bool flag = isAttached || AttachPoint == null || !(AttachPoint is ActiveSceneAttachPoint);
				FoldedItem.SetActive(flag);
				UnfoldedItem.SetActive(!flag);
				CurrentTarget = null;
				ShootingEffect.Stop();
				if (!isOnPlayer && !isDestroyed)
				{
					targettingHelper.gameObject.SetActive(!flag);
				}

				Collider[] unfoldedColliders = UnfoldedColliders;
				foreach (Collider collider in unfoldedColliders)
				{
					collider.enabled = !isOnPlayer && !flag;
				}
			}
		}

		private void OnEnable()
		{
			if (GetComponentInParent<SceneAttachPoint>() != null)
			{
				OnAttach(isAttached: false, isOnPlayer: false);
			}
		}

		private void Update()
		{
			if (turretAnimator.isActiveAndEnabled && turretAnimator.isInitialized)
			{
				turretAnimator.SetBool(IsActiveHash, AttachPoint is ActiveSceneAttachPoint && BaseVesselSystem is { Status: SystemStatus.Online });
				turretAnimator.SetBool(IsStunnedHash, isStunned);
				turretAnimator.SetBool(IsDestroyedHash, isDestroyed);
				if (!CanTarget)
				{
					turretAnimator.SetBool(CanTargetHash, false);
				}
			}

			if (CanTarget)
			{
				DoTurretRotation();
				DoTargetting();
				if (CurrentTarget != null && CurrentTarget.MainObject is MyPlayer &&
				    Mathf.Abs(vFrameDifference) < 0.1f && Mathf.Abs(hFrameDifference) < 0.1f &&
				    animationPercentage_Horizontal.IsEpsilonEqual(oldPercentage_Horizontal, 0.01f) &&
				    animationPercentage_Vertical.IsEpsilonEqual(oldPercentage_Vertical, 0.01f))
				{
					StartShooting();
				}
				else
				{
					StopShooting();
				}

				if (CurrentTarget != null && CurrentTarget.Targetable && CurrentTarget.MainObject is DynamicObject)
				{
					targetTimer += Time.deltaTime;
					if (targetTimer >= LookAtTime)
					{
						targetTimer = 0f;
						CurrentTarget.Targetable = false;
						ResetTargetting();
					}
				}

				if (shootingSoundPlaying)
				{
					shootTimer -= Time.deltaTime;
					if (shootTimer <= 0f)
					{
						shootTimer = 0.1f;
					}
				}

				TurretSound.Play(3);
			}
			else
			{
				StopShooting();
				TurretSound.Play(5);
			}
		}

		private void StartShooting()
		{
			if (!isShooting)
			{
				OpenHellion.Diagnostics.Log($"turret {GUID} start shooting at {CurrentTarget?.MainObject?.GetType().Name}");
				NetworkController.SendAndForget(new PortableTurretShootingMessage
				{
					IsShooting = true,
					TurretGUID = GUID
				});
				isShooting = true;
				shootingSoundPlaying = true;
			}
		}

		private void StopShooting()
		{
			if (isShooting)
			{
				isShooting = false;
				shootingSoundPlaying = false;
				NetworkController.SendAndForget(new PortableTurretShootingMessage
				{
					IsShooting = false,
					TurretGUID = GUID
				});
			}
		}

		private void DoTurretRotation()
		{
			if (CurrentTarget == null)
			{
				return;
			}

			Vector3 vector = !(CurrentTarget.MainObject is Player)
				? CurrentTarget.transform.position
				: (CurrentTarget.MainObject as Player).AnimHelper.GetBone(AnimatorHelper.HumanBones.Spine2).position;
			Vector3 vec = vector - turretBaseMovable.position;
			upDownAngle = Mathf.Clamp(135f - MathHelper.AngleSigned(turretBaseMovable.up, vec, turretBaseMovable.right),
				0f, 135f);
			Vector3 vec2 = Vector3.ProjectOnPlane(vector - turretBaseStatic.position, turretBaseStatic.up);
			leftRightAngle = MathHelper.AngleSigned(turretBaseStatic.forward, vec2, turretBaseStatic.up);
			if (leftRightAngle <= 0f && leftRightAngle >= -180f)
			{
				animationPercentage_Horizontal = (leftRightAngle + 360f) / 360f;
			}
			else if (leftRightAngle > 0f && leftRightAngle <= 180f)
			{
				animationPercentage_Horizontal = leftRightAngle / 360f;
			}

			animationPercentage_Vertical = upDownAngle / 135f;
			if (!animationPercentage_Horizontal.IsEpsilonEqual(oldPercentage_Horizontal, 0.001f))
			{
				hFrameDifference = animationPercentage_Horizontal - oldPercentage_Horizontal;
				if (Mathf.Abs(hFrameDifference) > 0.5f)
				{
					hFrameDifference *= -1f;
				}

				if (Mathf.Abs(hFrameDifference) > turretAngularSpeed * Time.deltaTime)
				{
					if (!rotationRightPlaying && !isShooting)
					{
						rotationRightPlaying = true;
					}

					for (animationPercentage_Horizontal = oldPercentage_Horizontal +
					                                      turretAngularSpeed * Time.deltaTime *
					                                      MathHelper.Sign(hFrameDifference);
					     animationPercentage_Horizontal > 1f;
					     animationPercentage_Horizontal -= 1f)
					{
					}

					while (animationPercentage_Horizontal < 0f)
					{
						animationPercentage_Horizontal += 1f;
					}
				}
				else
				{
					animationPercentage_Horizontal = oldPercentage_Horizontal + hFrameDifference;
				}

				oldPercentage_Horizontal = animationPercentage_Horizontal;
				turretAnimator.CrossFade("Horizontal", 0f, 1, animationPercentage_Horizontal);
			}

			if (animationPercentage_Vertical.IsEpsilonEqual(oldPercentage_Vertical, 0.001f))
			{
				return;
			}

			vFrameDifference = animationPercentage_Vertical - oldPercentage_Vertical;
			if (Mathf.Abs(vFrameDifference) > turretAngularSpeed * Time.deltaTime)
			{
				if (!rotationUpPlaying && !isShooting)
				{
					rotationUpPlaying = true;
				}

				for (animationPercentage_Vertical = oldPercentage_Vertical +
				                                    turretAngularSpeed * Time.deltaTime *
				                                    MathHelper.Sign(vFrameDifference);
				     animationPercentage_Vertical > 1f;
				     animationPercentage_Vertical -= 1f)
				{
				}

				while (animationPercentage_Vertical < 0f)
				{
					animationPercentage_Vertical += 1f;
				}
			}
			else
			{
				animationPercentage_Vertical = oldPercentage_Vertical + vFrameDifference;
			}

			oldPercentage_Vertical = animationPercentage_Vertical;
			turretAnimator.CrossFade("Vertical", 0f, 2, animationPercentage_Vertical);
		}

		private void DoTargetting()
		{
			bool flag = true;
			List<TargetingPoint> list = Targets.OrderByDescending((TargetingPoint y) => y.Priority)
				.ThenBy((TargetingPoint x) =>
					!(x == null)
						? (x.transform.position - detectionRaycastSource.position).sqrMagnitude
						: float.MaxValue).ToList();
			List<TargetingPoint> list2 = new List<TargetingPoint>();
			foreach (TargetingPoint item in list)
			{
				if (item == null ||
				    (item.MainObject is DynamicObject && (item.MainObject as DynamicObject).IsAttached) ||
				    !item.Targetable || !item.gameObject.activeInHierarchy)
				{
					list2.Add(item);
					continue;
				}

				if ((item.MainObject is Player && !ShouldShootAtPlayer(item.MainObject as Player)) || !item.Targetable)
				{
					flag = true;
					continue;
				}

				RaycastHit[] source = Physics.RaycastAll(detectionRaycastSource.position,
					item.transform.position - detectionRaycastSource.position, radius, shootingRaycastMask,
					QueryTriggerInteraction.Ignore);
				foreach (RaycastHit item2 in source.OrderBy((RaycastHit x) => x.distance))
				{
					Player componentInParent = item2.collider.GetComponentInParent<Player>();
					if (componentInParent != null && item.MainObject is MyPlayer)
					{
						CurrentTarget = item;
						flag = false;
						break;
					}

					Item componentInParent2 = item2.collider.GetComponentInParent<Item>();
					if (componentInParent2 == this)
					{
						continue;
					}

					if (componentInParent2 != null && item.MainObject == componentInParent2.DynamicObj)
					{
						CurrentTarget = item;
						flag = false;
					}

					break;
				}
			}

			foreach (TargetingPoint item3 in list2)
			{
				Targets.Remove(item3);
				TargetIgnoreList.Remove(item3);
			}

			if (flag || CurrentTarget == null)
			{
				ResetTargetting();
			}
		}

		private void ResetTargetting()
		{
			if (!(CurrentTarget == null))
			{
				CurrentTarget = null;
				turretAnimator.SetBool("HasTarget", value: false);
				turretAnimator.CrossFade("ReconMode", 0f, 0, animationPercentage_Horizontal);
			}
		}

		public void OnTriggerEnterBehaviour(Collider coli)
		{
			if (IsActive)
			{
				TargetingPoint component = coli.GetComponent<TargetingPoint>();
				if (component != null && (component.MainObject is Player || component.MainObject is DynamicObject))
				{
					Targets.Add(component);
				}
			}
		}

		public void OnTriggerExitBehaviour(Collider coli)
		{
			if (!IsActive)
			{
				return;
			}

			TargetingPoint component = coli.GetComponent<TargetingPoint>();
			if (!(component != null) || (!(component.MainObject is Player) && !(component.MainObject is DynamicObject)))
			{
				return;
			}

			Targets.Remove(component);
			TargetIgnoreList.Remove(component);
			component.Targetable = true;
			if (Targets.Count == 0)
			{
				turretAnimator.SetBool("HasTarget", value: false);
				if (CurrentTarget != null && CurrentTarget.MainObject == component.MainObject)
				{
					turretAnimator.CrossFade("ReconMode", 0f, 0, animationPercentage_Horizontal);
				}

				ResetTargetting();
			}
		}

		private bool ShouldShootAtPlayer(Player pl)
		{
			if (DynamicObj.Parent is SpaceObjectVessel && AttachPoint != null &&
			    !(DynamicObj.Parent as SpaceObjectVessel).IsPlayerAuthorized(MyPlayer.Instance))
			{
				return true;
			}

			return false;
		}

		public override bool CanPlayerPickUp(Player pl)
		{
			return !CanTarget || !ShouldShootAtPlayer(MyPlayer.Instance);
		}

		private void PortableTurretShootingMessageListener(NetworkData data)
		{
			PortableTurretShootingMessage portableTurretShootingMessage = data as PortableTurretShootingMessage;
			if (portableTurretShootingMessage.TurretGUID == GUID)
			{
				turretAnimator.SetBool("IsShooting", portableTurretShootingMessage.IsShooting);
				if (portableTurretShootingMessage.IsShooting)
				{
					ShootingEffect.Play();
				}
				else
				{
					ShootingEffect.Stop();
				}
			}
		}

		private void OnDestroy()
		{
			EventSystem.RemoveListener(typeof(PortableTurretShootingMessage), PortableTurretShootingMessageListener);
			if (BaseVesselSystem != null)
			{
				BaseVesselSystem.Accessories.Remove(this);
			}
		}

		public void BaseVesselSystemUpdated()
		{
		}
	}
}
