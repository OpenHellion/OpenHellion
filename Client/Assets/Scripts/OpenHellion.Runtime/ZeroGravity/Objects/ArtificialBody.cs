using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroGravity.LevelDesign;
using OpenHellion;
using ZeroGravity.Network;

namespace ZeroGravity.Objects
{
	public class ArtificialBody : SpaceObject
	{
		private const float CorrectionTime = 0.15f;

		public ManeuverData Maneuver;

		public bool ManeuverExited;

		public VesselDestructionEffects DestructionEffects;

		public Rigidbody ArtificialRigidbody;

		[NonSerialized] public ArtificialBody StabilizeToTargetObj;

		[NonSerialized] public Vector3 StabilizationOffset;

		public readonly HashSet<ArtificialBody> StabilizedChildren = new HashSet<ArtificialBody>();

		public double Radius { get; protected set; }

		[NonSerialized] private Vector3 _velocity;

		[NonSerialized] private Vector3 _angularVelocity;

		public override Vector3 Velocity => _velocity;

		public Vector3 AngularVelocity => _angularVelocity;

		public virtual bool IsDistressSignalActive { get; internal set; }

		public virtual bool IsAlwaysVisible { get; internal set; }

		public virtual double RadarSignature { get; set; } = 0.0;

		public bool IsStabilized => StabilizeToTargetObj != null;

		// Called by the Ship, Asteroid, and Pivot classes by their Create methods.
		protected static ArtificialBody InitialiseArtificialBody(World world, long guid, SpaceObjectType type, Vector3 position, Quaternion rotation)
		{
			GameObject gameObject = new GameObject(type + "_" + guid);
			ArtificialBody artificialBody;
			switch (type)
			{
				case SpaceObjectType.Ship:
					artificialBody = gameObject.AddComponent<Ship>();
					break;
				case SpaceObjectType.Asteroid:
					artificialBody = gameObject.AddComponent<Asteroid>();
					break;
				case SpaceObjectType.PlayerPivot:
				case SpaceObjectType.DynamicObjectPivot:
				case SpaceObjectType.CorpsePivot:
					artificialBody = gameObject.AddComponent<Pivot>();
					break;
				case SpaceObjectType.Station:
					artificialBody = gameObject.AddComponent<Station>();
					break;
				default:
					Debug.LogError("Cannot create artificial body of invalid type:" + type);
					return null;
			}

			if (type is SpaceObjectType.Ship or SpaceObjectType.Asteroid)
			{
				artificialBody.gameObject.SetActive(false);
			}

			artificialBody.World = world;
			artificialBody.Guid = guid;
			artificialBody.Radius = 30.0;
			artificialBody.TransferableObjectsRoot = new GameObject("TransferableObjectsRoot");
			artificialBody.TransferableObjectsRoot.transform.parent = artificialBody.transform;
			artificialBody.TransferableObjectsRoot.transform.Reset();
			artificialBody.ConnectedObjectsRoot = new GameObject("ConnectedObjectsRoot");
			artificialBody.ConnectedObjectsRoot.transform.parent = artificialBody.transform;
			artificialBody.ConnectedObjectsRoot.transform.Reset();
			if (type is SpaceObjectType.Asteroid or SpaceObjectType.Ship or SpaceObjectType.Station)
			{
				artificialBody.GeometryPlaceholder = new GameObject("GeometryPlaceholder");
				artificialBody.GeometryPlaceholder.transform.parent = artificialBody.transform;
				artificialBody.GeometryPlaceholder.transform.Reset();
				artificialBody.GeometryRoot = new GameObject("GeometryRoot");
				GeometryRoot geometryRoot = artificialBody.GeometryRoot.AddComponent<GeometryRoot>();
				artificialBody.GeometryRoot.transform.parent = artificialBody.GeometryPlaceholder.transform;
				artificialBody.GeometryRoot.transform.Reset();
				geometryRoot.MainObject = artificialBody;
				artificialBody.ArtificialRigidbody = artificialBody.GeometryRoot.AddComponent<Rigidbody>();
				artificialBody.ArtificialRigidbody.isKinematic = true;
				artificialBody.ArtificialRigidbody.useGravity = false;
				artificialBody.TransferableObjectsRoot.transform.parent = artificialBody.GeometryPlaceholder.transform;
				artificialBody.TransferableObjectsRoot.transform.Reset();
			}

			artificialBody.transform.parent = world.SpaceObjectsRoot.transform;
			artificialBody.transform.localPosition = Vector3.zero;
			artificialBody.SetTargetPositionAndRotation(position, rotation, true);

			world.AddArtificialBody(artificialBody);
			return artificialBody;
		}

		public override void DestroyGeometry()
		{
			base.DestroyGeometry();
			if (GeometryRoot != null)
			{
				if (this is SpaceObjectVessel)
				{
					ZeroOcclusion.DestroyOcclusionObjectsFor(this as SpaceObjectVessel);
				}

				foreach (Transform child in GeometryRoot.transform.GetChildren())
				{
					if (child != null)
					{
						Destroy(child.gameObject);
					}
				}

				GeometryRoot.transform.parent = GeometryPlaceholder.transform;
				GeometryRoot.transform.Reset();
			}

			if (ArtificialRigidbody != null)
			{
				Destroy(ArtificialRigidbody);
			}

			ArtificialRigidbody = null;
		}

		public virtual void UpdateArtificialBodyPosition(bool updateChildren)
		{
			if (ArtificialRigidbody != null && GeometryPlaceholder != null)
			{
				var position = GeometryPlaceholder.transform.position;
				var rotation = GeometryPlaceholder.transform.rotation;
				GeometryRoot.transform.position = position;
				GeometryRoot.transform.rotation = rotation;
				ArtificialRigidbody.position = position;
				ArtificialRigidbody.rotation = rotation;
			}
		}

		protected virtual void UpdatePositionAndRotation(bool setLocalPositionAndRotation)
		{
			if (ArtificialRigidbody != null && GeometryPlaceholder != null && transform.hasChanged)
			{
				ArtificialRigidbody.position = GeometryPlaceholder.transform.position;
				ArtificialRigidbody.rotation = GeometryPlaceholder.transform.rotation;
				transform.hasChanged = false;
			}
		}

		public override void ModifyPositionAndRotation(Vector3? position = null, Quaternion? rotation = null)
		{
			base.ModifyPositionAndRotation(position, rotation);
			if (IsInVisibilityRange && (position.HasValue || rotation.HasValue) && ArtificialRigidbody != null)
			{
				UpdateArtificialBodyPosition(updateChildren: true);
				transform.hasChanged = false;
			}
		}

		public void SetVelocity(Vector3 velocity, Vector3 angularVelocity)
		{
			_velocity = velocity;
			_angularVelocity = angularVelocity;
		}

		protected virtual void FixedUpdate()
		{
			bool moved = SmoothPosition();
			bool rotated = SmoothRotation();

			// Smoothing moves the transform every step, so the geometry and colliders have to come along
			// every step.
			if (moved || rotated)
			{
				UpdateArtificialBodyPosition(updateChildren: true);
			}
		}

		private bool SmoothPosition()
		{
			if (!TargetPosition.HasValue)
			{
				return false;
			}

			if (!ShouldSetLocalTransform)
			{
				TargetPosition = null;
				return false;
			}

			float deltaTime = Time.fixedDeltaTime;
			Vector3 step = Velocity * deltaTime;
			TargetPosition += step;

			Vector3 target = TargetPosition.Value - World.AnchorOffset;
			transform.localPosition = Vector3.Lerp(transform.localPosition + step, target, Blend(deltaTime));

			return true;
		}

		private bool SmoothRotation()
		{
			float deltaTime = Time.fixedDeltaTime;

			if (World.PilotedVesselGuid == Guid)
			{
				transform.localRotation = transform.localRotation.RotatedBy(AngularVelocity, deltaTime);
				return true;
			}

			if (!TargetRotation.HasValue)
			{
				return false;
			}

			TargetRotation = TargetRotation.Value.RotatedBy(AngularVelocity, deltaTime);
			transform.localRotation = Quaternion.Slerp(
				transform.localRotation.RotatedBy(AngularVelocity, deltaTime),
				TargetRotation.Value, Blend(deltaTime));

			return true;
		}

		/// <summary>
		/// 	Fraction of the remaining error to remove this step. Exponential, so the result does not
		/// 	depend on the step rate the way a fixed lerp factor does.
		/// </summary>
		public static float Blend(float deltaTime) => 1f - Mathf.Exp(-deltaTime / CorrectionTime);

		/// <summary>
		/// 	Repositions this body to follow the body it is stabilised to, keeping a fixed local-space
		/// 	offset.
		/// </summary>
		public void UpdateStabilizedPosition()
		{
			if (StabilizeToTargetObj == null)
			{
				return;
			}

			SetTargetPositionAndRotation(
				StabilizeToTargetObj.transform.position + StabilizationOffset, null, instant: true);
		}

		public virtual void OnStabilizationChanged(bool isStabilized)
		{
		}

		public void StabilizeToTarget(long guid, Vector3 stabilizationOffset)
		{
			if (StabilizeToTargetObj != null && StabilizeToTargetObj.Guid != guid)
			{
				StabilizeToTargetObj.StabilizedChildren.Remove(this);
			}

			StabilizeToTargetObj = null;
			if (guid > 0)
			{
				World.TryGetSpaceObject(guid, out ArtificialBody target);
				StabilizeToTargetObj = target;
			}

			if (StabilizeToTargetObj != null)
			{
				StabilizationOffset = stabilizationOffset;
				StabilizeToTargetObj.StabilizedChildren.Add(this);
				OnStabilizationChanged(true);
				UpdateStabilizedPosition();
			}
		}

		public void DisableStabilization()
		{
			if (!(StabilizeToTargetObj == null))
			{
				StabilizeToTargetObj.StabilizedChildren.Remove(this);
				StabilizeToTargetObj = null;
				OnStabilizationChanged(false);
			}
		}

		public void SendDistressCall(bool isActive)
		{
			World.SendDistressCall(this, isActive);
		}
	}
}
