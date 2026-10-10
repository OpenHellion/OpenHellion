using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class NameTagMessage : NetworkData
{
	public VesselObjectID ID;

	public string NameTagText;
}
