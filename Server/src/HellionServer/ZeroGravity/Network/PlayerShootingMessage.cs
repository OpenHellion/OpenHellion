using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class PlayerShootingMessage : NetworkData
{
	public long HitGUID;

	public int HitIndentifier;

	public ShotData ShotData;

	public long GUID;
}
