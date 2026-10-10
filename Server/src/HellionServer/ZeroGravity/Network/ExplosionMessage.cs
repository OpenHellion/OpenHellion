using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ExplosionMessage : NetworkData
{
	public long ItemGUID;

	public long[] AffectedGUIDs;

	public VesselObjectID[] RepairPointIDs;
}
