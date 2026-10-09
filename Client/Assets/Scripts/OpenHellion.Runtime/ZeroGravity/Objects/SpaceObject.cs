using System;
using OpenHellion;
using UnityEngine;

namespace ZeroGravity.Objects
{
	public abstract class SpaceObject : MonoBehaviour
	{
		[NonSerialized] public World World;

		[NonSerialized] public GameObject TransferableObjectsRoot;

		[NonSerialized] public GameObject ConnectedObjectsRoot;

		[NonSerialized] public GameObject GeometryRoot;

		[NonSerialized] public GameObject GeometryPlaceholder; // TODO remove this if possible

		public bool IsInVisibilityRange = true;

		public Quaternion? TargetRotation;

		public Vector3? TargetPosition;

		public long Guid { get; set; }

		public virtual SpaceObjectType Type => SpaceObjectType.None;

		public virtual SpaceObject Parent { get; set; }

		public virtual Vector3 Velocity => Vector3.zero;

		public bool SceneObjectsLoaded { get; protected set; }

		public bool IsMainObject => MyPlayer.Instance != null && (MyPlayer.Instance.Parent == this ||
		                                                          MyPlayer.Instance.IsInVesselHierarchy(
			                                                          this as SpaceObjectVessel));

		protected virtual bool ShouldSetLocalTransform => World == null || World.AnchorGuid != Guid;

		protected virtual bool ShouldUpdateTransform => true;

		public virtual void DestroyGeometry()
		{
		}

		private static T GetParent<T>(SpaceObject parent) where T : SpaceObject
		{
			if (parent == null)
			{
				return null;
			}

			if (parent is T spaceObject)
			{
				return spaceObject;
			}

			return GetParent<T>(parent.Parent);
		}

		public T GetParent<T>() where T : SpaceObject
		{
			return GetParent<T>(Parent);
		}

		protected virtual bool PositionAndRotationPhysicsCheck(ref Vector3? nextPos, ref Quaternion? nextRot)
		{
			return true;
		}

		public virtual void SetTargetPositionAndRotation(Vector3? localPosition, Quaternion? localRotation,
			bool instant = false)
		{
			IsInVisibilityRange = true;
			if (localPosition.HasValue && ShouldSetLocalTransform)
			{
				if (instant)
				{
					transform.localPosition = localPosition.Value;
					TargetPosition = null;
				}
				else
				{
					TargetPosition = localPosition.Value;
				}
			}

			if (!localRotation.HasValue)
			{
				return;
			}

			if (instant)
			{
				transform.localRotation = localRotation.Value;
				TargetRotation = null;
			}
			else
			{
				TargetRotation = localRotation.Value;
			}
		}

		public virtual void ModifyPositionAndRotation(Vector3? position = null, Quaternion? rotation = null)
		{
			if (!IsInVisibilityRange)
			{
				return;
			}

			if (position.HasValue && ShouldSetLocalTransform)
			{
				transform.localPosition += position.Value;
				if (TargetPosition.HasValue)
				{
					TargetPosition += position.Value;
				}
			}

			if (rotation.HasValue)
			{
				transform.localRotation *= rotation.Value;
			}
		}
	}
}
