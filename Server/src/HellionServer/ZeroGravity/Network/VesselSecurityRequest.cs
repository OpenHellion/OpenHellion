using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class VesselSecurityRequest : NetworkData
{
	public long VesselGUID;

	public string VesselName;

	public string AddPlayerId;

	public AuthorizedPersonRank? AddPlayerRank;

	public string AddPlayerName;

	public string RemovePlayerId;

	public bool? HackPanel;
}
