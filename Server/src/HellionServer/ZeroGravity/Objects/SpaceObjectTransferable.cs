using ZeroGravity.Math;

namespace ZeroGravity.Objects;

public abstract class SpaceObjectTransferable : SpaceObject
{
	private Vector3D _localPosition;

	public Vector3D LocalPosition
	{
		get
		{
			return _localPosition;
		}
		set
		{
			_localPosition = value;
		}
	}

	public QuaternionD LocalRotation;

	public override Vector3D Position
	{
		get
		{
			if (Parent == null)
			{
				Debug.LogError("SpaceObjectTransferable must have a parent!", Guid);
				return LocalPosition;
			}
			Vector3D localPosition = Parent is SpaceObjectVessel vessel ? vessel.StructureToLocalPosition(LocalPosition) : LocalPosition;
			return Parent.Position + Parent.Rotation * localPosition;
		}
	}

	public override QuaternionD Rotation
	{
		get
		{
			if (Parent == null)
			{
				Debug.LogError("SpaceObjectTransferable must have a parent!", Guid);
				return LocalRotation;
			}
			return Parent.Rotation * LocalRotation;
		}
	}

	public SpaceObjectTransferable(long guid, Vector3D localPosition, QuaternionD localRotation)
		: base(guid)
	{
		_localPosition = localPosition;
		LocalRotation = localRotation;
	}
}
