using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class PlayerSpawnRequest : NetworkData
{
	public SpawnSetupType SpawnSetupType;

	public long SpawnPointParentId;
}
