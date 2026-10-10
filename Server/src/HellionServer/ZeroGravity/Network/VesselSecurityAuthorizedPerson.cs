using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class VesselSecurityAuthorizedPerson
{
	public AuthorizedPersonRank Rank;

	public string PlayerId;

	public string Name;
}
