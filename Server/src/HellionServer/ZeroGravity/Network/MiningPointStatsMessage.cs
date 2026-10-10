using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class MiningPointStatsMessage : NetworkData
{
	public VesselObjectID ID;

	public float? Quantity;

	public bool? GasBurst;
}
