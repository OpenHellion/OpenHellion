using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class SpawnPointDetails
{
	public string Name;

	public bool IsPartOfCrew;

	public SpawnSetupType SpawnSetupType;

	public long SpawnPointParentID;

	public List<string> PlayersOnShip;
}
