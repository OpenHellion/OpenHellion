using System;
using System.Collections;
using System.Linq;
using OpenHellion.IO;
using UnityEngine;
using UnityEngine.Serialization;
using ZeroGravity.LevelDesign;
using ZeroGravity.Math;
using ZeroGravity.Objects;

namespace ZeroGravity.CharacterMovement
{
	[RequireComponent(typeof(CapsuleCollider))]
	[RequireComponent(typeof(SphereCollider))]
	[RequireComponent(typeof(Rigidbody))]
	public class MyCharacterController : MonoBehaviour
	{
		[Serializable]
		public struct MovementSpeed
		{
			public float ForwardVelocity;

			public float RightVelocity;

			public float BackwardVelocity;

			public float LeanVelocity;

			public float RotateVelocity;

			public float AccelerationStep;

			public float DeaccelerationStep;
		}

		private enum MovementState
		{
			Normal = 1,
			Run,
			Crouch,
			Jump
		}

		private struct MovementAxis
		{
			public float Forward;

			public float Right;

			public float LeanRight;

			public float Up;
		}

		public delegate void TranslateDelegate();

		[FormerlySerializedAs("collider1G")] [SerializeField] private CapsuleCollider _collider1G;

		[FormerlySerializedAs("collider0G")] [SerializeField] private SphereCollider _collider0G;

		[FormerlySerializedAs("characterRoot")] [SerializeField] private Transform _characterRoot;

		[FormerlySerializedAs("rigidBody")] [SerializeField] public Rigidbody RigidBody;

		[FormerlySerializedAs("myPlayer")] [SerializeField] private MyPlayer _myPlayer;

		[FormerlySerializedAs("animatorHelper")] [SerializeField] public AnimatorHelper AnimatorHelper;

		[FormerlySerializedAs("cameraController")] [SerializeField] private CameraController _cameraController;

		[FormerlySerializedAs("mainCamera")] [SerializeField] private Camera _mainCamera;

		[FormerlySerializedAs("farCamera")] [SerializeField] private Camera _farCamera;

		[FormerlySerializedAs("nearCamera")] [SerializeField] private Camera _nearCamera;

		public Transform HeadCameraParent;

		public ParticleSystem StarDustParticle;

		[FormerlySerializedAs("normalSpeeds")] [SerializeField] private MovementSpeed _normalSpeeds;

		[FormerlySerializedAs("runSpeeds")] [SerializeField] private MovementSpeed _runSpeeds;

		[FormerlySerializedAs("crouchSpeeds")] [SerializeField] private MovementSpeed _crouchSpeeds;

		[FormerlySerializedAs("airSpeeds")] [SerializeField] private MovementSpeed _airSpeeds;

		[FormerlySerializedAs("zeroGNormalSpeeds")] [SerializeField] private MovementSpeed _zeroGNormalSpeeds;

		[FormerlySerializedAs("zeroGJetpackSpeeds")] [SerializeField] private MovementSpeed _zeroGJetpackSpeeds;

		private MovementSpeed _currSpeeds;

		[FormerlySerializedAs("animatorForwardMaxVelocity")] [SerializeField] private float _animatorForwardMaxVelocity;

		[FormerlySerializedAs("animatorBackwardMaxVelocity")] [SerializeField] private float _animatorBackwardMaxVelocity;

		[FormerlySerializedAs("animatorRightMaxVelocity")] [SerializeField] private float _animatorRightMaxVelocity;

		[FormerlySerializedAs("ragdollChestRigidbody")] [SerializeField] public Rigidbody RagdollChestRigidbody;

		[FormerlySerializedAs("centerOfMass")] [SerializeField] private Transform _centerOfMass;

		public Rigidbody CenterOfMassRigidbody;

		[FormerlySerializedAs("maxSlopeAngle")] [SerializeField] private float _maxSlopeAngle;

		private float _currSlopeAngle;

		private Vector3 _currSlopeNormal;

		private bool _isZeroG;

		private bool _isMovementEnabled = true;

		[FormerlySerializedAs("isGrounded")] [SerializeField] private bool _isGrounded;

		private const float GroundCheckDistance = 0.01f;

		private bool _isOnLadder;

		[FormerlySerializedAs("ladderVelocity")] [SerializeField] private float _ladderVelocity = 1.5f;

		[NonSerialized] public Jetpack CurrentJetpack;

		private bool _gravityChanged;

		private bool _gravityChangedRagdoll;

		private float _gravityChangeStartTime;

		private Quaternion _gravityChangeEndingRotation = Quaternion.identity;

		private float _gravityChangeLerpHelper;

		private float _gravityChangeRagdollTimer;

		private float _normalColliderHeight;

		private float _crouchLerpHelper = 1f;

		private const float GravityChangeRagdollTimeMax = 5f;

		private const float CrouchColliderHeight = 0.8f;

		private const float NormalColliderCenter = -0.44f;

		private const float CrouchColliderCenter = -0.935f;

		[FormerlySerializedAs("crouchLerpSpeed")] [SerializeField] private float _crouchLerpSpeed = 1.5f;

		[FormerlySerializedAs("zgAtmosphereAngularDrag")] [SerializeField] private float _zgAtmosphereAngularDrag = 1.5f;

		[FormerlySerializedAs("zgAtmosphereDrag")] [SerializeField] private float _zgAtmosphereDrag = 0.1f;

		[FormerlySerializedAs("zgJetpackAngularDrag")] [SerializeField] private float _zgJetpackAngularDrag = 3f;

		[FormerlySerializedAs("zgGrabDrag")] [SerializeField] private float _zgGrabDrag = 2.2f;

		[FormerlySerializedAs("zgGrabAngularDrag")] [SerializeField] private float _zgGrabAngularDrag = 1.5f;

		[FormerlySerializedAs("jumpHeightMax")] [SerializeField] private float _jumpHeightMax = 0.8f;

		private Vector3 _currMovementDirection;

		private MovementAxis _movementAxis;

		private MovementState _lastMovementState;

		private float _currForwardVelocity;

		private float _currRightVelocity;

		private float _currUpVelocity;

		private float _stanceSpeedMultiplier = 1f;

		private float _currForwardAnimationVal;

		private float _currRightAnimationVal;

		private float _currForwardFloatingAnimationVal;

		private float _currRightFloatingAnimationVal;

		private float _headBobStrength = 1f;

		private bool _canLockToPoint;

		private bool _isLockedToPoint;

		private bool _canGrabWall;

		private bool _lerpCameraBack;

		private float _lerpCameraBackStartTime;

		private float _lerpCameraBackStep = 0.3333f;

		private Quaternion _lerpCameraBackZeroRotation = Quaternion.identity;

		private Vector3 _lerpCameraBackStartPos = Vector3.zero;

		private Quaternion _lerpCameraBackStartRot = Quaternion.identity;

		[FormerlySerializedAs("grabSlowEnabled")] public bool GrabSlowEnabled;

		private float _translateLerpHelper;

		public bool HasTumbled;

		public SpaceObjectVessel NearbyVessel;

		private int _collisionLayerMask;

		[FormerlySerializedAs("playerImpactVelocityTreshold")] [SerializeField] private float _playerImpactVelocityTreshold;

		public float DefaultMaxAngularVelocity = 20f;

		public SceneTriggerLadder LockedToLadder;

		private Vector3 _triggerHelperCenter;

		private float _lastImpactTime;

		public GlassPostEffect HelmetGlassEffect;

		public SpaceObjectVessel StickToVessel;

		public Quaternion StickToVesselRotation;

		public Vector3 StickToVesselTangentialVelocity;

		[FormerlySerializedAs("legDistance")] public float LegDistance = 1.15f;

		private RaycastHit _groundRayHit = default(RaycastHit);

		private bool _doJump;

		private float _airTime;

		private float _slopeJumpTimer;

		private bool _playingStabilizeSound;

		private float _lockLerp;

		private const float LockSpeed = 0.5f;

		[FormerlySerializedAs("attachPointSaved")] public Transform AttachPointSaved;

		public float MaxAngularVelocity { get; private set; }

		public bool IsGrounded => _isGrounded;

		public float HeadBobStrength
		{
			get { return _headBobStrength; }
			set
			{
				_headBobStrength = value;
				AnimatorHelper obj = AnimatorHelper;
				float? num = _headBobStrength;
				obj.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null,
					null, null, null, null, null, null, null, null, num);
			}
		}

		public Camera MainCamera => _mainCamera;

		public Camera FarCamera => _farCamera;

		public Camera NearCamera => _nearCamera;

		public Vector3 CameraPosition => _mainCamera.transform.position;

		public Vector3 CameraForward => _mainCamera.transform.forward;

		public Vector2 FreeLookAngle
		{
			get
			{
				var freeLookForward = _cameraController.FreeLookXTransform.forward;
				var mouseLookForward = _cameraController.MouseLookXTransform.forward;
				var mouseLookUp = _cameraController.MouseLookXTransform.up;
				float rotationSigned = MathHelper.AngleSigned(Vector3.ProjectOnPlane(freeLookForward, mouseLookUp).normalized,
					mouseLookForward, mouseLookUp);
				float degreeOfRotation = (Quaternion.FromToRotation(mouseLookForward,
						freeLookForward).Inverse() * _cameraController.transform.rotation)
					.eulerAngles.x;
				if (degreeOfRotation > 180f)
				{
					degreeOfRotation -= 360f;
				}

				if (rotationSigned > 180f)
				{
					rotationSigned -= 360f;
				}

				return new Vector2((0f - degreeOfRotation) / 90f, rotationSigned / 90f);
			}
		}

		public float MouseLookXAngle
		{
			get
			{
				float num = _cameraController.MouseLookXTransform.localRotation.eulerAngles.x;
				if (num > 180f)
				{
					num -= 360f;
				}

				return 0f - num;
			}
		}

		public bool IsAttached => _cameraController.IsAttached;

		public bool IsJump => _lastMovementState == MovementState.Jump;

		public bool IsCrouch => _lastMovementState == MovementState.Crouch;

		public bool IsZeroG => _isZeroG;

		public bool IsGravityChangeRagdolled => _gravityChangedRagdoll;

		public bool IsFreeLook => _cameraController.IsFreeLook;

		public bool MeleeTriggered { get; set; }

		public bool UseConsumableTriggered { get; set; }

		public Vector3 Velocity => RigidBody.linearVelocity;

		public bool IsEquippingAnimationTriggered { get; set; }

		public float MouseUpAxis => _cameraController.MouseUpAxis;

		public float MouseRightAxis => _cameraController.MouseRightAxis;

		public float CameraMaxUpAngle => _cameraController.MaxUpAngle;

		public float CameraMaxDownAngle => _cameraController.MaxDownAngle;

		public float CameraMaxRightAngle => _cameraController.MaxRightAngle;

		public bool IsOnLadder => _isOnLadder;

		public bool IsJetpackOn
		{
			get => CurrentJetpack != null && CurrentJetpack.IsActive;
			set
			{
				if (CurrentJetpack != null)
				{
					CurrentJetpack.IsActive = value;
				}
			}
		}

		public float JetpackFuel => (!(CurrentJetpack != null)) ? 0f : CurrentJetpack.CurrentFuel;

		public bool CanGrabWall => _canGrabWall;

		public bool CanLockToPoint
		{
			get { return _canLockToPoint; }
			set { _canLockToPoint = value; }
		}

		public bool IsLockedToPoint
		{
			get { return _isLockedToPoint; }
			set { _isLockedToPoint = value; }
		}

		public MovementSpeed CurrentSpeeds => _currSpeeds;

		public Transform MouseLookXTransform => _cameraController.MouseLookXTransform;

		public CameraController CameraController => _cameraController;

		public bool IsMovementEnabled => _isMovementEnabled;

		public float AirTime => _airTime;

		private void Awake()
		{
			_collisionLayerMask = (1 << LayerMask.NameToLayer("Default")) |
			                     (1 << LayerMask.NameToLayer("PlayerCollision"));
			_currSpeeds = _normalSpeeds;
			_lastMovementState = MovementState.Normal;
			_normalColliderHeight = _collider1G.height;
			_collider0G.enabled = false;
			RefreshMaxAngularVelocity();
		}

		private void Update()
		{
			if (_isLockedToPoint)
			{
				LockPlayerToPoint(locked: true);
			}

			if (GrabSlowEnabled)
			{
				CheckVelocityForLock();
			}

			if (!_isZeroG)
			{
				Update1GInput();
			}

			if (_isZeroG)
			{
				CheckLegDistanceFromFloor();
			}

			if (_lerpCameraBack)
			{
				LerpCameraToLocalZero();
			}

			if (_myPlayer.Parent is Pivot && RigidBody.linearDamping.IsNotEpsilonZero() && NearbyVessel != null)
			{
				Pivot pivot = _myPlayer.Parent as Pivot;
				RigidBody.linearVelocity -= (pivot.Velocity - NearbyVessel.Velocity) * Time.deltaTime;
			}
		}

		private void FixedUpdate()
		{
			if (!_myPlayer.PlayerReady)
			{
				return;
			}

			if (_isZeroG)
			{
				Update0GMovement();
			}
			else
			{
				if (!_isGrounded && !HasTumbled)
				{
					float num = Vector3.Dot(Vector3.Project(RigidBody.linearVelocity, _myPlayer.GravityDirection),
						_myPlayer.GravityDirection);
					if (num > 7f)
					{
						Tumble();
					}
				}

				Update1GMovement();
			}

			_canGrabWall = Physics.CheckSphere(_centerOfMass.position, 0.8f, _collisionLayerMask,
				QueryTriggerInteraction.Ignore);
		}

		private void CheckLegDistanceFromFloor()
		{
			bool? touchingFloor = Physics.Raycast(transform.position, -transform.up, LegDistance,
				_collisionLayerMask);
			AnimatorHelper.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null,
				null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
				null, null, null, null, null, null, null, null, null, touchingFloor);
		}

		private void CalculateCrouch()
		{
			if (!IsCrouch && _collider1G.height < _normalColliderHeight && Physics.SphereCast(_characterRoot.position,
				    _collider1G.radius - 0.02f, _characterRoot.up, out var _, _normalColliderHeight - _collider1G.radius,
				    _collisionLayerMask))
			{
				_lastMovementState = MovementState.Crouch;
			}

			float num = _crouchLerpHelper;
			if (IsCrouch && num > 0f)
			{
				num = Mathf.Max(num - Time.deltaTime * _crouchLerpSpeed, 0f);
			}
			else if (!IsCrouch && num < 1f)
			{
				Physics.SphereCast(_characterRoot.position, _collider1G.radius - 0.02f, _characterRoot.up,
					out var hitInfo2, _normalColliderHeight - _collider1G.radius, _collisionLayerMask);
				if (hitInfo2.transform == null)
				{
					num = Mathf.Min(num + Time.deltaTime * _crouchLerpSpeed, 1f);
				}
				else
				{
					_lastMovementState = MovementState.Crouch;
					if (num > 0f)
					{
						num = Mathf.Max(num - Time.deltaTime * _crouchLerpSpeed, 0f);
					}
				}
			}

			if (_crouchLerpHelper != num)
			{
				_crouchLerpHelper = num;
				_collider1G.height = Mathf.Lerp(CrouchColliderHeight, _normalColliderHeight, _crouchLerpHelper);
				_collider1G.center = new Vector3(0f,
					Mathf.Lerp(CrouchColliderCenter, NormalColliderCenter, _crouchLerpHelper), 0f);
				_currSpeeds.AccelerationStep = Mathf.Lerp(_crouchSpeeds.AccelerationStep, _normalSpeeds.AccelerationStep,
					_crouchLerpHelper);
				_currSpeeds.DeaccelerationStep = Mathf.Lerp(_crouchSpeeds.DeaccelerationStep,
					_normalSpeeds.DeaccelerationStep, _crouchLerpHelper);
				_currSpeeds.ForwardVelocity = Mathf.Lerp(_crouchSpeeds.ForwardVelocity, _normalSpeeds.ForwardVelocity,
					_crouchLerpHelper);
				_currSpeeds.BackwardVelocity = Mathf.Lerp(_crouchSpeeds.BackwardVelocity, _normalSpeeds.BackwardVelocity,
					_crouchLerpHelper);
				_currSpeeds.RightVelocity =
					Mathf.Lerp(_crouchSpeeds.RightVelocity, _normalSpeeds.RightVelocity, _crouchLerpHelper);
				if (_crouchLerpHelper < 0.7f && _myPlayer.MeshRenderersEnabled)
				{
					_myPlayer.ToggleMeshRendereres(enableMesh: false);
				}
				else if (_crouchLerpHelper >= 0.7f && !_myPlayer.MeshRenderersEnabled)
				{
					_myPlayer.ToggleMeshRendereres(enableMesh: true);
				}
			}

			AnimatorHelper.SetParameter(IsCrouch);
		}

		private static void RecalculateAxisValue(ref float currAxisVal, ref float currVelocity,
			ref float positiveVelocityMax, ref float negativeVelocityMax)
		{
			if (currAxisVal > 0.0001f)
			{
				currAxisVal = currVelocity / positiveVelocityMax;
			}
			else if (currAxisVal < -0.0001f)
			{
				currAxisVal = currVelocity / negativeVelocityMax;
			}
		}

		private static void SetAxisValue(ref float currAxisVal, float inputAxisVal, ref float accelerationStep,
			ref float deaccelerationStep)
		{
			if (currAxisVal > 1f)
			{
				currAxisVal -= deaccelerationStep * Time.fixedDeltaTime;
				return;
			}

			if (currAxisVal < -1f)
			{
				currAxisVal += deaccelerationStep * Time.fixedDeltaTime;
				return;
			}

			if (inputAxisVal > 0.0001f)
			{
				if (currAxisVal > -0.0001f)
				{
					currAxisVal += accelerationStep * Time.fixedDeltaTime;
				}
				else
				{
					currAxisVal += deaccelerationStep * Time.fixedDeltaTime;
				}
			}
			else if (inputAxisVal < -0.0001f)
			{
				if (currAxisVal < 0.0001f)
				{
					currAxisVal -= accelerationStep * Time.fixedDeltaTime;
				}
				else
				{
					currAxisVal -= deaccelerationStep * Time.fixedDeltaTime;
				}
			}
			else if (currAxisVal > 0.0001f)
			{
				currAxisVal = Mathf.Max(currAxisVal - deaccelerationStep * Time.fixedDeltaTime, 0f);
			}
			else if (currAxisVal < -0.0001f)
			{
				currAxisVal = Mathf.Min(currAxisVal + deaccelerationStep * Time.fixedDeltaTime, 0f);
			}
			else
			{
				currAxisVal = 0f;
			}

			currAxisVal = Mathf.Clamp(currAxisVal, -1f, 1f);
		}

		private void Calculate1GMovementData()
		{
			if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Sprint) && !IsCrouch && AnimatorHelper.CanRun)
			{
				if (_lastMovementState != MovementState.Run)
				{
					RecalculateAxisValue(ref _movementAxis.Forward, ref _currForwardVelocity,
						ref _runSpeeds.ForwardVelocity, ref _runSpeeds.BackwardVelocity);
					RecalculateAxisValue(ref _movementAxis.Right, ref _currRightVelocity, ref _runSpeeds.RightVelocity,
						ref _runSpeeds.RightVelocity);
				}

				_currSpeeds = _runSpeeds;
				_lastMovementState = MovementState.Run;
				_myPlayer.HealthSounds.Switch("WalkRun", "Run");
			}
			else if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Crouch))
			{
				if (_lastMovementState != MovementState.Crouch)
				{
					RecalculateAxisValue(ref _movementAxis.Forward, ref _currForwardVelocity,
						ref _crouchSpeeds.ForwardVelocity, ref _crouchSpeeds.BackwardVelocity);
					RecalculateAxisValue(ref _movementAxis.Right, ref _currRightVelocity, ref _crouchSpeeds.RightVelocity,
						ref _crouchSpeeds.RightVelocity);
				}

				_currSpeeds = _crouchSpeeds;
				_lastMovementState = MovementState.Crouch;
				_myPlayer.HealthSounds.Switch("WalkRun", "Walk");
			}
			else
			{
				if (_lastMovementState != MovementState.Normal)
				{
					RecalculateAxisValue(ref _movementAxis.Forward, ref _currForwardVelocity,
						ref _normalSpeeds.ForwardVelocity, ref _normalSpeeds.BackwardVelocity);
					RecalculateAxisValue(ref _movementAxis.Right, ref _currRightVelocity, ref _normalSpeeds.RightVelocity,
						ref _normalSpeeds.RightVelocity);
				}

				_currSpeeds = _normalSpeeds;
				_lastMovementState = MovementState.Normal;
				_myPlayer.HealthSounds.Switch("WalkRun", "Walk");
			}

			_movementAxis.Forward = ControlsSubsystem.GetAxisRaw(ControlsSubsystem.ConfigAction.Forward);
			_movementAxis.Right = ControlsSubsystem.GetAxisRaw(ControlsSubsystem.ConfigAction.Right);
			_movementAxis.Forward *= _stanceSpeedMultiplier;
			_movementAxis.Right *= _stanceSpeedMultiplier;
			if (_movementAxis.Forward != 0f && _movementAxis.Right != 0f)
			{
				_currSpeeds.ForwardVelocity -= _currSpeeds.ForwardVelocity * Mathf.Abs(_movementAxis.Right) * 0.25f;
				_currSpeeds.RightVelocity -= _currSpeeds.RightVelocity * Mathf.Abs(_movementAxis.Forward) * 0.25f;
				_currSpeeds.BackwardVelocity -= _currSpeeds.BackwardVelocity * Mathf.Abs(_movementAxis.Right) * 0.25f;
			}

			_currMovementDirection = _movementAxis.Forward * transform.forward;
			_currMovementDirection += _movementAxis.Right * transform.right;
			_currMovementDirection.Normalize();
		}

		private void Update1GInput()
		{
			if (_isOnLadder)
			{
				return;
			}

			if (_gravityChanged)
			{
				if (_gravityChangedRagdoll)
				{
					_isGrounded = Physics.Raycast(RagdollChestRigidbody.position, _myPlayer.GravityDirection,
						out _groundRayHit, 0.4f, _collisionLayerMask, QueryTriggerInteraction.Ignore);
				}
				else
				{
					_isGrounded = Physics.Raycast(transform.position, _myPlayer.GravityDirection, out _groundRayHit,
						1.8f, _collisionLayerMask, QueryTriggerInteraction.Ignore);
				}

				if (CurrentJetpack != null)
				{
					CurrentJetpack.StartNozzles(Vector4.zero, myJetpack: true);
				}
			}
			else
			{
				_isGrounded = Physics.SphereCast(_characterRoot.position + _characterRoot.up * (_collider1G.height / 2f),
					_collider1G.radius - 0.01f, _myPlayer.GravityDirection, out _groundRayHit,
					_collider1G.height / 2f - (_collider1G.radius - 0.01f) + GroundCheckDistance + 0.02f,
					_collisionLayerMask);
			}

			if (!_isGrounded)
			{
				_airTime += Time.deltaTime;
			}
			else if (_isGrounded)
			{
				_airTime = 0f;
			}

			AnimatorHelper obj = AnimatorHelper;
			bool? flag = _isGrounded;
			float? num = _airTime;
			obj.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
				null, null, null, null, null, null, null, null, null, flag, null, null, null, null, null, null, null,
				null, num);
			if (_isMovementEnabled && _crouchLerpHelper.IsEpsilonEqual(1f, 0.001f) && _isGrounded &&
			    ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Jump))
			{
				_doJump = true;
			}
		}

		private void Update1GMovement()
		{
			_cameraController.SetLeanRightAxis(0f);
			if (_isOnLadder)
			{
				float axisRaw = ControlsSubsystem.GetAxisRaw(ControlsSubsystem.ConfigAction.Forward);
				transform.Translate(transform.up * (axisRaw * _ladderVelocity * Time.fixedDeltaTime),
					Space.World);
				AnimatorHelper animHelper = _myPlayer.animHelper;
				float? ladderDirection = axisRaw;
				animHelper.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null,
					null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
					null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
					null, ladderDirection);
				return;
			}

			if (_gravityChanged)
			{
				if (!_gravityChangedRagdoll)
				{
					_gravityChangeLerpHelper += Time.fixedDeltaTime * 2f;
					if (_gravityChangeLerpHelper > 1f)
					{
						_gravityChangeLerpHelper = 1f;
					}

					transform.rotation = _gravityChangeEndingRotation;
					if (_collider1G.height < 1.8f)
					{
						_collider1G.height = Mathf.Lerp(1f, 1.8f, _gravityChangeLerpHelper);
						_collider1G.center = new Vector3(0f, Mathf.Lerp(0f, -0.44f, _gravityChangeLerpHelper), 0f);
					}
				}
				else
				{
					if (_gravityChangeLerpHelper < 0.01f)
					{
						_gravityChangeRagdollTimer += Time.fixedDeltaTime;
						if (_gravityChangeRagdollTimer >= GravityChangeRagdollTimeMax)
						{
							_gravityChangeLerpHelper += Time.fixedDeltaTime * 2f;
						}
					}

					if (!RagdollChestRigidbody.linearVelocity.sqrMagnitude.IsNotEpsilonZero(0.001f) ||
					    _gravityChangeLerpHelper.IsNotEpsilonZero(0.001f))
					{
						_gravityChangeLerpHelper += Time.fixedDeltaTime * 2f;
					}
				}
			}

			if (_gravityChanged)
			{
				if (!_gravityChangedRagdoll && _isGrounded && _gravityChangeLerpHelper >= 1f)
				{
					_gravityChanged = false;
					_cameraController.ToggleCameraMovement(true);
					_gravityChangeLerpHelper = 0f;
					transform.rotation = _gravityChangeEndingRotation;
					_cameraController.ResetLookAt();
					return;
				}

				if (_gravityChangedRagdoll && _gravityChangeLerpHelper >= 1f)
				{
					_myPlayer.ToggleRagdoll(false);
					_gravityChangeLerpHelper = 0f;
					_gravityChanged = false;
					HasTumbled = false;
					return;
				}
			}

			if (_isGrounded)
			{
				if (_gravityChanged || !_isMovementEnabled)
				{
					return;
				}

				_currSlopeNormal = _groundRayHit.normal;
				_currSlopeAngle = Vector3.Angle(_currSlopeNormal, transform.up);
				if (_currSlopeAngle > 5f)
				{
					bool flag = false;
					RaycastHit[] hits = Physics.SphereCastAll(
						_characterRoot.position + _characterRoot.up * (_collider1G.height / 2f), _collider1G.radius - 0.01f,
						_myPlayer.GravityDirection,
						_collider1G.height / 2f - (_collider1G.radius - 0.01f) + GroundCheckDistance + 0.02f,
						_collisionLayerMask);

					if (hits.Length > 0)
					{
						foreach (RaycastHit hit in hits)
						{
							if (!hit.collider.TryGetComponent<DynamicObject>(out _) &&
							    !hit.collider.TryGetComponent<MyPlayer>(out _) &&
							    !hit.collider.TryGetComponent<RagdollCollider>(out _))
							{
								flag = false;
								_groundRayHit = hit;
								break;
							}

							if (hit.collider.TryGetComponent<RagdollCollider>(out _))
							{
								flag = true;
							}
						}
					}

					if (flag)
					{
						_currSlopeNormal = transform.up;
						_currSlopeAngle = 0f;
					}
					else
					{
						_currSlopeNormal = _groundRayHit.normal;
						_currSlopeAngle = Vector3.Angle(_currSlopeNormal, transform.up);
					}
				}

				bool? isMoving = RigidBody.linearVelocity.magnitude > float.Epsilon;
				AnimatorHelper.SetParameter(null, isMoving);
				if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Crouch))
				{
					_lastMovementState = MovementState.Crouch;
				}

				CalculateCrouch();
				if (_currSlopeAngle < _maxSlopeAngle)
				{
					if (_slopeJumpTimer != 0f)
					{
						_slopeJumpTimer = 0f;
					}

					if (_doJump && !IsCrouch)
					{
						float num = Mathf.Sqrt(2f * _jumpHeightMax * _myPlayer.Gravity.magnitude);
						RigidBody.AddForce(transform.up * num, ForceMode.VelocityChange);
						_lastMovementState = MovementState.Jump;
						AnimatorHelper.SetParameterTrigger(AnimatorHelper.Triggers.Jump);
						_doJump = false;
						_isGrounded = false;
					}
					else
					{
						Calculate1GMovementData();
					}

					if (_currSlopeAngle > 1f)
					{
						_currMovementDirection = Vector3.ProjectOnPlane(_currMovementDirection, _currSlopeNormal);
					}

					_currForwardVelocity = _movementAxis.Forward * ((!(_movementAxis.Forward > 0f))
						? _currSpeeds.BackwardVelocity
						: _currSpeeds.ForwardVelocity);
					_currRightVelocity = _movementAxis.Right * _currSpeeds.RightVelocity;
					if (_isGrounded)
					{
						if (Mathf.Abs(_currForwardVelocity) > float.Epsilon ||
						    Mathf.Abs(_currRightVelocity) > float.Epsilon)
						{
							RigidBody.AddForce(
								_currMovementDirection * Mathf.Sqrt(_currForwardVelocity * _currForwardVelocity +
								                                   _currRightVelocity * _currRightVelocity) -
								RigidBody.linearVelocity, ForceMode.VelocityChange);
						}
						else
						{
							RigidBody.AddForce(
								_currMovementDirection * (_currForwardVelocity + _currRightVelocity) - RigidBody.linearVelocity,
								ForceMode.VelocityChange);
						}
					}

					_currForwardAnimationVal = (!(Mathf.Abs(_movementAxis.Forward) <= float.Epsilon))
						? Mathf.Clamp(
							Vector3.Project(_myPlayer.MyVelocity, transform.forward).magnitude /
							_runSpeeds.ForwardVelocity * MathHelper.Sign(_movementAxis.Forward), -1f, 1f)
						: 0f;
					_currRightAnimationVal = (!(Mathf.Abs(_movementAxis.Right) <= float.Epsilon))
						? Mathf.Clamp(
							Vector3.Project(_myPlayer.MyVelocity, transform.right).magnitude /
							_runSpeeds.RightVelocity * MathHelper.Sign(_movementAxis.Right), -1f, 1f)
						: 0f;
					float? ladderDirection = _currForwardAnimationVal;
					float? velocityRight = _currRightAnimationVal;
					AnimatorHelper.SetParameter(null, null, null, null, null, null, null, null, null, null,
						velocityRight, ladderDirection);
				}
				else
				{
					_slopeJumpTimer += Time.deltaTime;
					_movementAxis.Forward = 0f;
					_movementAxis.Right = 0f;
					_currForwardAnimationVal = 0f;
					_currRightAnimationVal = 0f;
					float? ladderDirection = _currForwardAnimationVal;
					float? velocityRight = _currRightAnimationVal;
					AnimatorHelper.SetParameter(null, null, null, null, null, null, null, null, null, null,
						velocityRight, ladderDirection);
					if (_slopeJumpTimer > 1f && _isGrounded &&
					    ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Jump))
					{
						float num2 = Mathf.Sqrt(2f * _jumpHeightMax * _myPlayer.Gravity.magnitude);
						RigidBody.AddForce(transform.up * num2, ForceMode.VelocityChange);
						_lastMovementState = MovementState.Jump;
						AnimatorHelper.SetParameterTrigger(AnimatorHelper.Triggers.Jump);
						_doJump = false;
						_isGrounded = false;
						_slopeJumpTimer = 0f;
					}
				}
			}
			else if (_isMovementEnabled)
			{
				float axisRaw2 = ControlsSubsystem.GetAxisRaw(ControlsSubsystem.ConfigAction.Forward);
				float axisRaw3 = ControlsSubsystem.GetAxisRaw(ControlsSubsystem.ConfigAction.Right);
				if (axisRaw2 != 0f && Vector3.Project(RigidBody.linearVelocity, transform.forward).sqrMagnitude <
				    ((!(_movementAxis.Forward > 0f))
					    ? (_airSpeeds.BackwardVelocity * _airSpeeds.BackwardVelocity)
					    : (_airSpeeds.ForwardVelocity * _airSpeeds.ForwardVelocity)))
				{
					Vector3 force = transform.forward *
					                ((!(axisRaw2 > 0f))
						                ? (0f - _airSpeeds.BackwardVelocity)
						                : _airSpeeds.ForwardVelocity);
					RigidBody.AddForce(force, ForceMode.VelocityChange);
				}

				if (axisRaw3 != 0f && Vector3.Project(RigidBody.linearVelocity, transform.right).sqrMagnitude <
				    _airSpeeds.RightVelocity * _airSpeeds.RightVelocity)
				{
					Vector3 force2 = _airSpeeds.RightVelocity * 0.005f * transform.right;
					RigidBody.AddForce(force2, ForceMode.VelocityChange);
				}

				_movementAxis.Forward = Mathf.Lerp(_movementAxis.Forward, 0f, 0.3f * Time.fixedDeltaTime);
				_movementAxis.Right = Mathf.Lerp(_movementAxis.Right, 0f, 0.3f * Time.fixedDeltaTime);
			}

			if (!_isGrounded || _currSlopeAngle >= _maxSlopeAngle)
			{
				if (!_isMovementEnabled && _myPlayer.IsLockedToTrigger)
				{
					RigidBody.linearVelocity = Vector3.zero;
					RigidBody.angularVelocity = Vector3.zero;
				}
				else
				{
					RigidBody.AddForce(_myPlayer.Gravity, ForceMode.Acceleration);
				}
			}
		}

		private void Check0GCurrentSpeed()
		{
			if (IsJetpackOn && JetpackFuel > float.Epsilon)
			{
				_currSpeeds = _zeroGJetpackSpeeds;
			}
			else if (_canGrabWall)
			{
				_currSpeeds = _zeroGNormalSpeeds;
			}
			else
			{
				_currSpeeds = _zeroGNormalSpeeds;
			}
		}

		/// <summary>
		/// 	Calculates movement velocities.
		/// </summary>
		private void Calculate0GMovementData()
		{
			// Get normal input.
			_movementAxis.Forward = ControlsSubsystem.GetAxis(ControlsSubsystem.ConfigAction.Forward);
			_movementAxis.Right = ControlsSubsystem.GetAxis(ControlsSubsystem.ConfigAction.Right);
			_movementAxis.LeanRight = ControlsSubsystem.GetAxis(ControlsSubsystem.ConfigAction.Lean);

			// Get vertical input.
			if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Jump))
			{
				_movementAxis.Up = Mathf.Min(_movementAxis.Up + Time.fixedDeltaTime * 4f, 1f);
			}
			else if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Crouch))
			{
				_movementAxis.Up = Mathf.Max(_movementAxis.Up - Time.fixedDeltaTime * 4f, -1f);
			}
			else if (_movementAxis.Up < 0f)
			{
				_movementAxis.Up = Mathf.Min(_movementAxis.Up + Time.fixedDeltaTime * 4f, 0f);
			}
			else
			{
				_movementAxis.Up = Mathf.Max(_movementAxis.Up - Time.fixedDeltaTime * 4f, 0f);
			}

			// Sound and VFX.
			if (CurrentJetpack != null && CurrentJetpack.IsActive && JetpackFuel > float.Epsilon)
			{
				CurrentJetpack.StartNozzles(
					new Vector4(_movementAxis.Right, _movementAxis.Up, _movementAxis.Forward, _movementAxis.LeanRight),
					myJetpack: true);
			}

			Check0GCurrentSpeed();

			// Calculate final velocity if numbers are not epsilon zero.
			if (!_movementAxis.Forward.IsNotEpsilonZero() || !_movementAxis.Right.IsNotEpsilonZero() ||
			    !_movementAxis.Up.IsNotEpsilonZero())
			{
				float a = Mathf.Abs(_movementAxis.Forward) * _currSpeeds.ForwardVelocity;
				float b = Mathf.Abs(_movementAxis.Right) * _currSpeeds.RightVelocity;
				float c = Mathf.Abs(_movementAxis.Up) * _currSpeeds.RightVelocity;
				float num2 = MathHelper.AverageMaxValue(a, b, c, _currSpeeds.ForwardVelocity, _currSpeeds.RightVelocity,
					_currSpeeds.RightVelocity);
				_currSpeeds.ForwardVelocity = num2;
				_currSpeeds.RightVelocity = num2;
				_currSpeeds.BackwardVelocity = num2;
			}

			_myPlayer.HealthSounds.Switch("WalkRun", "Walk");
		}

		private void Calculate0GAnimatorParams()
		{
			Vector3 vector = Vector3.Project(_myPlayer.MyVelocity, transform.forward);
			Vector3 vector2 = Vector3.Project(_myPlayer.MyVelocity, transform.right);
			float f = Vector3.Dot(vector2.normalized, transform.right);
			float num3 = Vector3.Dot(vector.normalized, transform.forward);
			float num = Mathf.Min(
				            MathHelper.ProportionalValue(vector.magnitude, 0f,
					            (!(num3 > 1f)) ? _animatorBackwardMaxVelocity : _animatorForwardMaxVelocity, 0f, 1f),
				            1f) *
			            Mathf.Sign(num3);
			float num2 =
				Mathf.Min(MathHelper.ProportionalValue(vector2.magnitude, 0f, _animatorRightMaxVelocity, 0f, 1f), 1f) *
				Mathf.Sign(f);
			if (!Mathf.Approximately(_currForwardAnimationVal, num))
			{
				_currForwardAnimationVal = Mathf.Lerp(_currForwardAnimationVal, num,
					Time.fixedDeltaTime * _animatorForwardMaxVelocity);
			}

			if (!Mathf.Approximately(_currRightAnimationVal, num2))
			{
				_currRightAnimationVal = Mathf.Lerp(_currRightAnimationVal, num2,
					Time.fixedDeltaTime * _animatorRightMaxVelocity);
			}

			float? velocityForward = _currForwardAnimationVal;
			float? velocityRight = _currRightAnimationVal;
			AnimatorHelper.SetParameter(null, null, null, null, null, null, null, null, null, null, velocityRight,
				velocityForward);
			float forward = _movementAxis.Forward;
			float right = _movementAxis.Right;
			AnimatorHelper animHelper = _myPlayer.animHelper;
			bool? isMovingZeroG = forward.IsNotEpsilonZero() || right.IsNotEpsilonZero();
			animHelper.SetParameter(null, null, null, isMovingZeroG);
			if (!Mathf.Approximately(_currForwardFloatingAnimationVal, forward))
			{
				_currForwardFloatingAnimationVal = Mathf.Lerp(_currForwardFloatingAnimationVal, forward,
					Time.fixedDeltaTime * _animatorForwardMaxVelocity);
			}

			if (!Mathf.Approximately(_currRightFloatingAnimationVal, right))
			{
				_currRightFloatingAnimationVal = Mathf.Lerp(_currRightFloatingAnimationVal, right,
					Time.fixedDeltaTime * _animatorRightMaxVelocity);
			}

			AnimatorHelper animHelper2 = _myPlayer.animHelper;
			velocityForward = _currForwardFloatingAnimationVal;
			velocityRight = _currRightFloatingAnimationVal;
			animHelper2.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null, null,
				velocityForward, velocityRight);
		}

		private void Update0GMovement()
		{
			if (_isOnLadder)
			{
				float axisRaw = ControlsSubsystem.GetAxisRaw(ControlsSubsystem.ConfigAction.Forward);
				transform.Translate(transform.up * (axisRaw * _ladderVelocity * Time.fixedDeltaTime),
					Space.World);
				AnimatorHelper animHelper = _myPlayer.animHelper;
				float? ladderDirection = axisRaw;
				animHelper.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null,
					null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
					null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
					null, ladderDirection);
			}
			else if (_gravityChanged && _gravityChangedRagdoll)
			{
				_gravityChangeLerpHelper += Time.fixedDeltaTime * 2f;
				if (_gravityChangedRagdoll && _gravityChangeLerpHelper >= 1f)
				{
					_myPlayer.ToggleRagdoll(false);
					_gravityChangeLerpHelper = 0f;
					_gravityChanged = false;
				}
			}
			else
			{
				if (!_isMovementEnabled)
				{
					return;
				}

				Calculate0GMovementData();
				_cameraController.SetLeanRightAxis(0f - _movementAxis.LeanRight);
				bool hasToStabilise = ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Sprint) ||
				                      (_myPlayer.IsUsingItemInHands &&
				                       _myPlayer.Inventory.CheckIfItemInHandsIsType<HandDrill>());
				if (hasToStabilise && _canGrabWall && NearbyVessel != null &&
				    (NearbyVessel.Velocity - (_myPlayer.Parent.Velocity + RigidBody.linearVelocity))
				    .sqrMagnitude < 100.0)
				{
					if (_myPlayer.CurrentRoomTrigger == null)
					{
						if (StickToVessel == null)
						{
							StickToVessel = NearbyVessel;
							StickToVesselRotation = NearbyVessel.transform.rotation;
						}

						if (StickToVessel != null)
						{
							RigidBody.linearVelocity = Vector3.Lerp(RigidBody.linearVelocity,
								StickToVessel.Velocity - _myPlayer.Parent.Velocity, 0.1f);
						}
					}
					else
					{
						StickToVessel = null;
						RigidBody.linearDamping = _zgGrabDrag;
						RigidBody.linearVelocity *= 0.9f;
					}
				}
				else if (_myPlayer.IsInsideSpaceObject && _myPlayer.CurrentRoomTrigger != null &&
				         _myPlayer.CurrentRoomTrigger.AirPressure > 0.5f)
				{
					StickToVessel = null;
					StickToVesselTangentialVelocity = Vector3.zero;
					StickToVesselRotation = Quaternion.identity;
					RigidBody.linearDamping = _zgAtmosphereDrag;
				}
				else
				{
					if (StickToVessel != null)
					{
						RigidBody.linearVelocity = StickToVesselTangentialVelocity;
					}

					StickToVessel = null;
					StickToVesselTangentialVelocity = Vector3.zero;
					StickToVesselRotation = Quaternion.identity;
					RigidBody.linearDamping = 0f;
				}

				// Handle rotation.
				if (IsJetpackOn)
				{
					RigidBody.angularDamping = _zgJetpackAngularDrag;
				}
				else if (hasToStabilise && _canGrabWall)
				{
					RigidBody.angularDamping = _zgGrabAngularDrag;
				}
				else if (_myPlayer.IsInsideSpaceObject && _myPlayer.CurrentRoomTrigger != null &&
				         _myPlayer.CurrentRoomTrigger.AirPressure > 0.5f)
				{
					RigidBody.angularDamping = _zgAtmosphereAngularDrag;
				}
				else
				{
					RigidBody.angularDamping = 0f;
				}

				if (_movementAxis.Right.IsNotEpsilonZero() || _movementAxis.Forward.IsNotEpsilonZero() ||
				    _movementAxis.Up.IsNotEpsilonZero())
				{
					Vector3 vector = _currSpeeds.ForwardVelocity * _movementAxis.Forward * _cameraController.transform.forward
					                 + _currSpeeds.RightVelocity * _movementAxis.Right * _cameraController.transform.right
					                 + _currSpeeds.RightVelocity * _movementAxis.Up * _cameraController.transform.up;

					if (!IsJetpackOn && RigidBody.linearVelocity.sqrMagnitude >=
					    _currSpeeds.ForwardVelocity * _currSpeeds.ForwardVelocity)
					{
						Vector3 vector2 = Vector3.Project(vector, RigidBody.linearVelocity);
						if (MathHelper.SameSign(RigidBody.linearVelocity.normalized, vector2.normalized))
						{
							vector -= vector2;
						}
					}

					if (IsJetpackOn || _myPlayer.IsInsideSpaceObject || _canGrabWall)
					{
						RigidBody.AddForce(vector, ForceMode.Impulse);
					}
				}

				// Set animation paramaters.
				Calculate0GAnimatorParams();
			}
		}

		private void LerpCameraToLocalZero()
		{
			float num = (Time.time - _lerpCameraBackStartTime) * _lerpCameraBackStep;
			_mainCamera.transform.localPosition = Vector3.Lerp(_lerpCameraBackStartPos, Vector3.zero, num);
			_mainCamera.transform.localRotation =
				Quaternion.Lerp(_lerpCameraBackStartRot, _lerpCameraBackZeroRotation, num);
			if (num >= 1f)
			{
				_lerpCameraBack = false;
			}
		}

		public void LockPlayerToPoint(bool locked, Transform attachPoint = null)
		{
			_isLockedToPoint = locked;
			if (locked)
			{
				if (attachPoint != null)
				{
					AttachPointSaved = attachPoint;
				}

				_myPlayer.transform.parent = AttachPointSaved;
				if (_lockLerp < 1f)
				{
					_lockLerp += LockSpeed * Time.deltaTime;
				}

				transform.position = Vector3.Lerp(transform.position, AttachPointSaved.position,
					Mathf.Clamp01(_lockLerp));
			}
			else
			{
				_lockLerp = 0f;
			}
		}

		public void LockRigidbodyZRotation(bool locked)
		{
			if (locked)
			{
				RigidBody.constraints = RigidbodyConstraints.FreezeRotationZ;
			}
			else
			{
				RigidBody.constraints = RigidbodyConstraints.None;
			}
		}

		public void SetGravity(Vector3 newGravity)
		{
			if (!isActiveAndEnabled && !_gravityChangedRagdoll)
			{
				return;
			}

			if (IsOnLadder && LockedToLadder != null)
			{
				if (!_isZeroG && _myPlayer.Gravity.IsEpsilonEqual(Vector3.zero))
				{
					_isZeroG = true;
					_isGrounded = false;
					_collider1G.enabled = false;
					_collider0G.enabled = true;
					RigidBody.freezeRotation = false;
					RigidBody.angularDamping = 1.5f;
					_cameraController.IsZeroG = true;
					AnimatorHelper.SetParameter(null, false, true, false);
					_cameraController.LerpCameraControllerBack(1f);
				}
				else if (_isZeroG && !_myPlayer.Gravity.IsEpsilonEqual(Vector3.zero))
				{
					_isZeroG = false;
					_collider0G.enabled = false;
					_collider1G.enabled = true;
					_cameraController.IsZeroG = false;
					RigidBody.linearDamping = 0f;
					RigidBody.angularDamping = 0f;
					RigidBody.freezeRotation = true;
					if (CurrentJetpack != null && CurrentJetpack.IsActive)
					{
						CurrentJetpack.OldJetpackActiveState = CurrentJetpack.IsActive;
						CurrentJetpack.IsActive = false;
						CurrentJetpack.StartNozzles(Vector4.zero, myJetpack: true);
						RefreshMaxAngularVelocity();
					}

					AnimatorHelper.SetParameter(null, false, false, false);
				}

				LockedToLadder.LadderAttach(_myPlayer, checkGravity: false);
				_myPlayer.OldGravity = _myPlayer.Gravity;
				return;
			}

			if (!_isZeroG && _myPlayer.Gravity.IsEpsilonEqual(Vector3.zero))
			{
				_isZeroG = true;
				_isGrounded = false;
				_collider1G.enabled = false;
				_collider0G.enabled = true;
				RigidBody.freezeRotation = false;
				RigidBody.angularDamping = 1.5f;
				_cameraController.IsZeroG = true;
				if (_cameraController.transform.localPosition.y.IsNotEpsilonZero())
				{
					ReparentCenterOfMass(isInChest: false);
					transform.position += transform.up * _cameraController.transform.localPosition.y;
					_lastMovementState = MovementState.Normal;
					_crouchLerpHelper = 1f;
					_collider1G.height = _normalColliderHeight;
					_collider1G.center = new Vector3(0f, NormalColliderCenter, 0f);
					AnimatorHelper.SetParameter(false);
					AnimatorHelper.SetParameterTrigger(AnimatorHelper.Triggers.InstantStandUp);
					ReparentCenterOfMass(isInChest: true);
				}

				if (!IsAttached)
				{
					_cameraController.LerpCameraControllerBack(1f);
				}

				AnimatorHelper.SetParameter(null, false, true, false);
				if (CurrentJetpack != null && CurrentJetpack.OldJetpackActiveState)
				{
					CurrentJetpack.IsActive = true;
					CurrentJetpack.OldJetpackActiveState = false;
					RefreshMaxAngularVelocity();
				}

				ToggleJetPack(true);
			}
			else if (_isZeroG && !_myPlayer.Gravity.IsEpsilonEqual(Vector3.zero))
			{
				_isZeroG = false;
				_collider0G.enabled = false;
				_collider1G.enabled = true;
				_cameraController.IsZeroG = false;
				RigidBody.linearDamping = 0f;
				RigidBody.angularDamping = 0f;
				RigidBody.freezeRotation = true;
				if (CurrentJetpack != null && CurrentJetpack.IsActive)
				{
					CurrentJetpack.OldJetpackActiveState = CurrentJetpack.IsActive;
					CurrentJetpack.IsActive = false;
					CurrentJetpack.StartNozzles(Vector4.zero, myJetpack: true);
					RefreshMaxAngularVelocity();
				}

				AnimatorHelper.SetParameter(null, false, false, false);
			}

			if (!_isZeroG && !_myPlayer.OldGravity.IsEpsilonEqual(newGravity) && !_myPlayer.InIteractLayer)
			{
				_gravityChanged = true;
				_gravityChangedRagdoll = false;
				_gravityChangeLerpHelper = 0f;
				_gravityChangeRagdollTimer = 0f;
				_cameraController.ToggleCameraMovement(false);
				Vector3 vector = Vector3.ProjectOnPlane(transform.forward, -_myPlayer.GravityDirection).normalized;
				if (!vector.IsNotEpsilonZero())
				{
					vector = Vector3.forward;
				}

				_gravityChangeEndingRotation = Quaternion.LookRotation(vector, -_myPlayer.GravityDirection);
				if (Vector3.Angle(transform.up, -_myPlayer.GravityDirection) > 40f || CheckGravityRagdollDistance())
				{
					_gravityChangedRagdoll = true;
					RagdollChestRigidbody.linearVelocity = RigidBody.linearVelocity;
					_myPlayer.ToggleRagdoll(true);
				}
				else if (_myPlayer.OldGravity.IsEpsilonEqual(Vector3.zero))
				{
					if (Physics.SphereCast(transform.position, _collider1G.radius - 0.05f,
						    _myPlayer.GravityDirection, out _, 1.34f - _collider1G.radius, _collisionLayerMask))
					{
						_gravityChanged = false;
						transform.rotation = _gravityChangeEndingRotation;
						_cameraController.ToggleCameraMovement(true);
						transform.position += transform.up * (0f - CrouchColliderCenter + 0.05f);
						_crouchLerpHelper = 0f;
						_collider1G.height = CrouchColliderHeight;
						_collider1G.center = new Vector3(0f, CrouchColliderCenter, 0f);
						_lastMovementState = MovementState.Crouch;
						AnimatorHelper.SetParameter(true);
						_myPlayer.ToggleMeshRendereres(enableMesh: false);
					}
					else
					{
						_collider1G.height = 1f;
						_collider1G.center = Vector3.zero;
					}
				}
			}

			_myPlayer.OldGravity = _myPlayer.Gravity;
		}

		private bool CheckGravityRagdollDistance()
		{
			RaycastHit[] array =
				(from h in Physics.RaycastAll(transform.position, _myPlayer.GravityDirection, 4f,
						_collisionLayerMask)
					orderby h.distance
					select h).ToArray();
			if (array != null && array.Length > 0)
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].collider.GetComponent<DynamicObject>() == null &&
					    array[i].collider.GetComponent<MyPlayer>() == null &&
					    array[i].collider.GetComponent<RagdollCollider>() == null)
					{
						if (array[i].distance < 3.34f)
						{
							return false;
						}

						break;
					}
				}
			}

			return true;
		}

		private void OnCollisionEnter(Collision coli)
		{
			if (coli.relativeVelocity.magnitude >= _playerImpactVelocityTreshold)
			{
				CameraController.cameraShakeController.CamShake(0.2f, 0.05f, 15f, 15f);
			}
		}

		public void ResetVelocity()
		{
			_movementAxis.Forward = 0f;
			_movementAxis.Right = 0f;
			if (_isZeroG)
			{
				_currSpeeds = _normalSpeeds;
			}
			else
			{
				_currSpeeds = _zeroGNormalSpeeds;
			}

			_lastMovementState = MovementState.Normal;
			if (!RigidBody.isKinematic)
			{
				RigidBody.linearVelocity = Vector3.zero;
				RigidBody.angularVelocity = Vector3.zero;
			}
			AnimatorHelper.SetParameter(false, null, null, null, null, null, null, null, null, null, 0f, 0f);
			_myPlayer.ToggleMeshRendereres(enableMesh: true);
		}

		public void ResetPlayerLock()
		{
			IsLockedToPoint = false;
			_myPlayer.SetParentTransferableObjectsRoot();
			LockRigidbodyZRotation(locked: false);
		}

		public void CheckVelocityForLock()
		{
			if (RigidBody.linearVelocity.magnitude < 0.1f)
			{
				_canLockToPoint = true;
				GrabSlowEnabled = false;
			}
		}

		public void SetStateSpeedMultiplier(float speedMultiplier)
		{
			_stanceSpeedMultiplier = Mathf.Clamp01(speedMultiplier);
		}

		public void ToggleCameraController(bool? isEnabled = null)
		{
			if (isEnabled.HasValue)
			{
				_cameraController.enabled = isEnabled.Value;
			}
			else
			{
				_cameraController.enabled = !_cameraController.enabled;
			}
		}

		public void ToggleCameraMovement(bool? isEnabled = null)
		{
			_cameraController.ToggleCameraMovement(isEnabled);
		}

		public void ToggleMovement(bool? isEnabled = null)
		{
			if (isEnabled.HasValue)
			{
				_isMovementEnabled = isEnabled.Value;
			}
			else
			{
				_isMovementEnabled = !_isMovementEnabled;
			}
		}

		public void ToggleAttached(bool? isAttached = null)
		{
			if (isAttached.HasValue)
			{
				_cameraController.IsAttached = isAttached.Value;
			}
			else
			{
				_cameraController.IsAttached = !_cameraController.IsAttached;
			}

			if (_cameraController.IsAttached)
			{
				_cameraController.ToggleFreeLook(isActive: false);
			}
			else if (_cameraController.IsAttached && _cameraController.AutoFreeLook)
			{
				_cameraController.ToggleFreeLook(isActive: true);
			}
		}

		public void ToggleOnLadder(SceneTriggerLadder ladder, bool? isOn = null)
		{
			if (isOn.HasValue)
			{
				_isOnLadder = isOn.Value;
			}
			else
			{
				_isOnLadder = !_isOnLadder;
			}

			LockedToLadder = (!_isOnLadder) ? null : ladder;
		}

		public void ToggleJetPack(bool? isOn = null)
		{
			if (IsZeroG)
			{
				if (JetpackFuel <= float.Epsilon)
				{
					isOn = false;
				}

				if (isOn.HasValue)
				{
					IsJetpackOn = isOn.Value;
				}
				else
				{
					IsJetpackOn = !IsJetpackOn;
				}

				if (CurrentJetpack != null && !IsJetpackOn && CurrentJetpack != null)
				{
					RefreshMaxAngularVelocity();
				}
			}
		}

		public void ToggleColliders(bool isEnabled)
		{
			_collider1G.enabled = !_isZeroG && isEnabled;
			_collider0G.enabled = _isZeroG && isEnabled;
		}

		public void ResetLookAt(float? duration = null)
		{
			_cameraController.ResetLookAt(duration);
		}

		public void ToggleAutoFreeLook(bool isActive)
		{
			_cameraController.ToggleAutoFreeLook(isActive);
		}

		public void LookAtPoint(Vector3 point)
		{
			_cameraController.LookAtPoint(point);
		}

		public void AddForce(Vector3 force, ForceMode foceMode)
		{
			RigidBody.AddForce(force, foceMode);
		}

		public void AddTorque(Vector3 torque, ForceMode forceMode)
		{
			RigidBody.AddTorque(torque, forceMode);
		}

		public void RotateVelocity(Vector3 axis, float angle)
		{
			if (_isGrounded && !IsJump)
			{
				RigidBody.linearVelocity = Quaternion.AngleAxis(angle, axis) * RigidBody.linearVelocity;
			}
		}

		public void RefreshOutfitData(Transform outfitTrans)
		{
			HeadCameraParent = AnimatorHelper.GetBone(AnimatorHelper.HumanBones.Head).Find("HeadCameraParent");
			RagdollChestRigidbody = AnimatorHelper.GetBone(AnimatorHelper.HumanBones.Spine2).GetComponent<Rigidbody>();
			_centerOfMass.transform.parent = RagdollChestRigidbody.transform;
			_centerOfMass.localScale = Vector3.one;
			_centerOfMass.transform.localPosition = new Vector3(-0.133f, 0.014f, 0.001f);
			_centerOfMass.transform.localRotation = Quaternion.Euler(97.33099f, -90f, 0.2839966f);
			_cameraController.RefreshOutfitData(outfitTrans);
		}

		public void AddCharacterRotation(Vector3 euler)
		{
			_cameraController.AddCharacterRotation(euler);
		}

		public IEnumerator AddCharacterRotationByTime(Vector3 euler, float time)
		{
			float x = euler.x / time;
			float y = euler.y / time;
			float z = euler.z / time;
			while (time > 0f)
			{
				_cameraController.AddCharacterRotation(new Vector3(x, y, z));
				yield return new WaitForEndOfFrame();
				time -= Time.deltaTime;
			}

			yield return null;
		}

		public void ToggleCameraAttachToHeadBone(bool? isAttached = null, Vector3? lockedAngle = null)
		{
			_cameraController.ToggleCameraAttachToHeadBone(isAttached, lockedAngle);
		}

		public void ToggleKinematic(bool? isKinematic = null)
		{
			bool flag = (!isKinematic.HasValue) ? (!RigidBody.isKinematic) : isKinematic.Value;
			_collider0G.enabled = !flag && _isZeroG;
			_collider1G.enabled = !flag && !_isZeroG;
			RigidBody.isKinematic = flag;
		}

		public void ReparentCenterOfMass(bool isInChest)
		{
			if (!isInChest)
			{
				_centerOfMass.transform.parent = null;
				return;
			}

			_centerOfMass.transform.parent = RagdollChestRigidbody.transform;
			_centerOfMass.localScale = Vector3.one;
			_centerOfMass.transform.localPosition = new Vector3(-0.133f, 0.014f, 0.001f);
			_centerOfMass.transform.localRotation = Quaternion.Euler(97.33099f, -90f, 0.2839966f);
		}

		public void LerpCameraBack(float time, Vector3? localZeroRotation = null)
		{
			_lerpCameraBack = true;
			if (localZeroRotation.HasValue)
			{
				_lerpCameraBackZeroRotation = Quaternion.Euler(localZeroRotation.Value);
			}
			else
			{
				_lerpCameraBackZeroRotation = Quaternion.identity;
			}

			_lerpCameraBackStartPos = _mainCamera.transform.localPosition;
			_lerpCameraBackStartRot = _mainCamera.transform.localRotation;
			_lerpCameraBackStartTime = Time.time;
			_lerpCameraBackStep = 1f / time;
		}

		public IEnumerator TranslateAndLookAt(Transform position, Transform lookAt, TranslateDelegate actionToCall)
		{
			ResetVelocity();
			ToggleMovement(false);
			ToggleCameraMovement(false);
			Vector3 startingPosition = transform.position;
			Quaternion startingRotation = transform.rotation;
			Vector3 startingLookAt = _mainCamera.transform.position + _mainCamera.transform.forward;
			_translateLerpHelper = 0f;
			_myPlayer.InLerpingState = true;
			if (_myPlayer.CurrentActiveItem != null)
			{
				_myPlayer.Inventory.AddToInventoryOrDrop(_myPlayer.CurrentActiveItem, _myPlayer.Inventory.HandsSlot);
			}

			while (_translateLerpHelper < 1f)
			{
				if (position != null)
				{
					transform.position = Vector3.Lerp(startingPosition, position.position,
						Mathf.SmoothStep(0f, 1f, _translateLerpHelper));
					transform.rotation = Quaternion.Lerp(startingRotation, position.rotation,
						Mathf.SmoothStep(0f, 1f, _translateLerpHelper));
				}

				if (lookAt != null)
				{
					_cameraController.LookAtPoint(Vector3.Lerp(startingLookAt, lookAt.position,
						Mathf.SmoothStep(0f, 1f, _translateLerpHelper)));
				}

				_translateLerpHelper += Time.deltaTime;
				yield return new WaitForEndOfFrame();
			}

			_myPlayer.InLerpingState = false;
			transform.position = position.position;
			transform.rotation = position.rotation;
			ToggleCameraMovement(true);
			if (lookAt != null)
			{
				_cameraController.LookAtPoint(lookAt.position);
			}

			actionToCall();
		}

		public void Tumble()
		{
			HasTumbled = true;
			_gravityChanged = true;
			_gravityChangedRagdoll = true;
			_gravityChangeLerpHelper = 0f;
			_gravityChangeRagdollTimer = 0f;
			_cameraController.ToggleCameraMovement(false);
			_myPlayer.ToggleRagdoll(true);
			RagdollChestRigidbody.linearVelocity = _myPlayer.GravityDirection * 0.1f;
		}

		public void SetHandsBoxCollider(BoxCollider collider)
		{
			CameraController.SetHandsBoxCollider(collider);
		}

		public int CheckGetUpRoom()
		{
			if (Physics.SphereCast(_characterRoot.position, _collider1G.radius - 0.02f,
				    -_myPlayer.GravityDirection * 1.34f, out var _, _normalColliderHeight - _collider1G.radius,
				    _collisionLayerMask))
			{
				_crouchLerpHelper = 0f;
				_collider1G.height = CrouchColliderHeight;
				_collider1G.center = new Vector3(0f, CrouchColliderCenter, 0f);
				_lastMovementState = MovementState.Crouch;
				AnimatorHelper.SetParameter(true);
				_myPlayer.ToggleMeshRendereres(enableMesh: false);
				return 0;
			}

			return 1;
		}

		public void RefreshMaxAngularVelocity()
		{
			MaxAngularVelocity = (float)(DefaultMaxAngularVelocity * (System.Math.PI / 180.0));
		}

		private void OnParticleCollision(GameObject other)
		{
			if ((bool)other.GetComponentInParent<DebrisFieldEffect>())
			{
				CameraController.cameraShakeController.CamShake(0.2f, 0.05f, 15f, 15f);
				_myPlayer.DebrisFieldImpactPending = true;
			}
		}

		public Collider GetCollider()
		{
			return (!_collider1G.enabled) ? _collider0G : _collider1G;
		}
	}
}
