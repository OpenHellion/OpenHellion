using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class SubSystemGeneric : SubSystem
{
	public override SubSystemType Type => SubSystemType.Generic;

	public SubSystemGeneric(SpaceObjectVessel vessel, VesselObjectID id, SubSystemData ssData)
		: base(vessel, id, ssData)
	{
	}

	public override void SetAuxData(SystemAuxData auxData)
	{
	}
}
