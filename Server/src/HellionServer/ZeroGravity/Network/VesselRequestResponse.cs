using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class VesselRequestResponse : NetworkData
{
	public long GUID;

	public bool Active;

	public RescueShipMessages Message;

	public float Time;
}
