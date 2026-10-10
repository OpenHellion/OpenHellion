using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ShipCollisionMessage : NetworkData
{
	public float CollisionVelocity;

	public long ShipOne;

	public long ShipTwo;
}
