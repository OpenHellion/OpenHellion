using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class PlayerRoomMessage : NetworkData
{
	public VesselObjectID ID;

	public bool? IsOutsideRoom;
}
