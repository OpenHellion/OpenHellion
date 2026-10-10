using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ShipStatsMessage : NetworkData
{
	public long Guid;

	public VesselObjects VesselObjects;

	public float? Temperature;

	public float? Health;

	public float? Armor;

	public float? SelfDestructTime;

	public long? TargetStabilizationGuid;
}
