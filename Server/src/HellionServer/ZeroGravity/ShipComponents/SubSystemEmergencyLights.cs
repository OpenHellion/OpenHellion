using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class SubSystemEmergencyLights : SubSystem
{
	public override SubSystemType Type => SubSystemType.EmergencyLight;

	public SubSystemEmergencyLights(SpaceObjectVessel vessel, VesselObjectID id, SubSystemData ssData)
		: base(vessel, id, ssData)
	{
	}

	public override void SetAuxData(SystemAuxData auxData)
	{
	}
}
