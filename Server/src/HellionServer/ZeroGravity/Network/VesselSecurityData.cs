using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class VesselSecurityData
{
	public string VesselName;

	public List<VesselSecurityAuthorizedPerson> AuthorizedPersonel;
}
