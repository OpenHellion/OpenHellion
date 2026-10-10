using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class RoomPressureMessage : NetworkData
{
	public VesselObjectID ID;

	public float? TargetPressure;

	public VesselObjectID TargetRoomID;
}
