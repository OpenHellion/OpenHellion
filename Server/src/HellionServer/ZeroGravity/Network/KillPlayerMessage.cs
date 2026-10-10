using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class KillPlayerMessage : NetworkData
{
	public long Guid;

	public HurtType CauseOfDeath;

	public VesselDamageType VesselDamageType;
}
