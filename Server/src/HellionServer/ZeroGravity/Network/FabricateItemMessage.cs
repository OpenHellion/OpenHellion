using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class FabricateItemMessage : NetworkData
{
	public VesselObjectID ID;

	public ItemCompoundType ItemType;
}
