using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class PlayerDrillingMessage : NetworkData
{
	public bool isDrilling;

	public long DrillersGUID;

	public bool dontPlayEffect;

	public VesselObjectID MiningPointID;

	public float MiningTime;
}
