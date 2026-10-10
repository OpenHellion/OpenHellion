using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class PortableTurretStats : DynamicObjectStats
{
	public bool? IsActive;

	public bool? IsStunned;
}
