using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class GeneratorAir : Generator
{
	public override GeneratorType Type => GeneratorType.Air;

	public override DistributionSystemType OutputType => DistributionSystemType.Air;

	public GeneratorAir(SpaceObjectVessel vessel, VesselObjectID id, GeneratorData genData)
		: base(vessel, id, genData)
	{
	}

	public override void SetAuxData(SystemAuxData auxData)
	{
	}
}
