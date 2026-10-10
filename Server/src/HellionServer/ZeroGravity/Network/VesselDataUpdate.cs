using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class VesselDataUpdate
{
	public long Guid;

	public string VesselRegistration;

	public string VesselName;

	public double? RadarSignature;

	public bool? IsDistressSignalActive;

	public bool? IsAlwaysVisible;
}
