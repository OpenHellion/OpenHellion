using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class LockToTriggerMessage : NetworkData
{
	public VesselObjectID TriggerID;

	public bool IsPilotingVessel;
}
