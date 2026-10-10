using ProtoBuf;
using ZeroGravity.Objects;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class DynamicObjectAttachData
{
	public bool IsAttached;

	public long ParentGUID = -1L;

	public SpaceObjectType ParentType = SpaceObjectType.None;

	public short ItemSlotID;

	public short InventorySlotID;

	public AttachPointDetails APDetails;

	public float[] LocalPosition;

	public float[] LocalRotation;

	public float[] Velocity;

	public float[] Torque;

	public float[] ThrowForce;
}
