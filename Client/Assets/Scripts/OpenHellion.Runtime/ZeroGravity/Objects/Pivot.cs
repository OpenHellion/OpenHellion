using OpenHellion;
using UnityEngine;
using ZeroGravity.Math;

namespace ZeroGravity.Objects
{
	public class Pivot : ArtificialBody
	{
		public Vector3D Acceleration = Vector3D.Zero;

		public Vector3 Rotation = Vector3.zero;

		public SpaceObjectType ChildType;

		private SpaceObjectType _pivotType;

		public override SpaceObjectType Type => _pivotType;

		public static Pivot Create(World world, long guid, SpaceObjectType pivotType, Vector3 position, Quaternion rotation, bool isMainObject)
		{
			Pivot pivot = InitialiseArtificialBody(world, guid, pivotType, position, rotation) as Pivot;
			pivot._pivotType = pivotType;
			switch (pivotType)
			{
				case SpaceObjectType.PlayerPivot:
					pivot.ChildType = SpaceObjectType.Player;
					break;
				case SpaceObjectType.DynamicObjectPivot:
					pivot.ChildType = SpaceObjectType.DynamicObject;
					break;
				case SpaceObjectType.CorpsePivot:
					pivot.ChildType = SpaceObjectType.Corpse;
					break;
				default:
					Debug.LogError("Unknown pivot type " + pivotType);
					break;
			}

			return pivot;
		}

		public static Pivot Create(SpaceObjectType pivotType, long guid, ArtificialBody ab, bool isMainObject)
		{
			if (ab.StabilizeToTargetObj != null)
			{
				ab.UpdateStabilizedPosition();
			}

			return Create(ab.World, guid, pivotType, ab.transform.position, Quaternion.identity, isMainObject);
		}

		protected override void FixedUpdate()
		{
			base.FixedUpdate();
			if (MyPlayer.Instance.Parent != null)
			{
				UpdatePositionAndRotation(MyPlayer.Instance.Parent != this);
			}
		}

		public override void SetTargetPositionAndRotation(Vector3? localPosition, Quaternion? localRotation,
			bool instant = false)
		{
			base.SetTargetPositionAndRotation(localPosition, localRotation, true);
		}

		private void OnDrawGizmos()
		{
			Color color = ChildType == SpaceObjectType.Player
				? new Color(1f, 0f, 0f, 0.05f)
				: ChildType != SpaceObjectType.Corpse
					? new Color(0f, 1f, 0f, 0.05f)
					: new Color(0f, 0f, 1f, 0.05f);
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.color = color;
			Gizmos.DrawSphere(transform.position, 0.5f);
			color.a = 0.3f;
			Gizmos.color = color;
			Gizmos.DrawWireSphere(transform.position, 0.5f);
		}
	}
}
