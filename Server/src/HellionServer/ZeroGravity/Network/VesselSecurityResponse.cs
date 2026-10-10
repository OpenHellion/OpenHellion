using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class VesselSecurityResponse : NetworkData
{
	public long VesselGUID;

	public VesselSecurityData Data;
}
