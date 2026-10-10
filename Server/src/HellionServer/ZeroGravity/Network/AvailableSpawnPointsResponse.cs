using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class AvailableSpawnPointsResponse : NetworkData
{
	public List<SpawnPointDetails> SpawnPoints;
}
