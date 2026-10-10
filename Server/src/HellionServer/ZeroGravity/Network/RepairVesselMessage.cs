using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class RepairVesselMessage : NetworkData
{
	public VesselObjectID ID;
}
