using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class RecycleItemMessage : NetworkData
{
	public VesselObjectID ID;

	public long? GUID;

	public RecycleMode RecycleMode;
}
