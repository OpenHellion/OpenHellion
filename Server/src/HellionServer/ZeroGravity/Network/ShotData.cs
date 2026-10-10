using ProtoBuf;
using ZeroGravity.Objects;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ShotData
{
	public float[] Position;

	public float[] Orientation;

	public long parentGUID;

	public SpaceObjectType parentType;

	public float Range;

	public byte colliderType;

	public bool IsMeleeAttack;
}
