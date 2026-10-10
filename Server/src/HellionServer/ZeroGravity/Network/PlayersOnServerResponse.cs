using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class PlayersOnServerResponse : NetworkData
{
	public VesselObjectID SpawnPointID;

	public VesselObjectID SecuritySystemID;

	public List<PlayerOnServerData> PlayersOnServer;
}
