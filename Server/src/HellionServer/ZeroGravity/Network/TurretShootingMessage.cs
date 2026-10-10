using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class TurretShootingMessage : NetworkData
{
	public ShotData ShotData;
}
