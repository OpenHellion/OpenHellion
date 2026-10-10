using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class PairedDoorsDetails
{
	public VesselObjectID DoorID;

	public VesselObjectID PairedDoorID;
}
