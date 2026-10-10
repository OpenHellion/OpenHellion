using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class CancelFabricationMessage : NetworkData
{
	public VesselObjectID ID;

	public bool CurrentItemOnly;
}
