using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class PortableTurretShootingMessage : NetworkData
{
	public long TurretGUID;

	public bool IsShooting;
}
