using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class PlayersOnServerRequest : NetworkData
{
	public VesselObjectID SpawnPointID;

	public VesselObjectID SecuritySystemID;
}
