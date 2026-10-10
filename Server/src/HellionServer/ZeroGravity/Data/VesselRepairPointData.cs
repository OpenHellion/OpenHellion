using ProtoBuf;

namespace ZeroGravity.Data;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class VesselRepairPointData : ISceneData
{
	public int InSceneID;

	public int RoomID;

	public RepairPointDamageType DamageType;

	public int AffectedSystemID;

	public float MalfunctionThreshold;

	public float RepairThreshold;

	public bool External;
}
