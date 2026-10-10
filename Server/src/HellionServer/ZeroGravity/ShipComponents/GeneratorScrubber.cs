using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class GeneratorScrubber : Generator
{
	public float ScrubberCartridgeConsumption;

	public override GeneratorType Type => GeneratorType.AirScrubber;

	public override DistributionSystemType OutputType => DistributionSystemType.ScrubbedAir;

	public GeneratorScrubber(SpaceObjectVessel vessel, VesselObjectID id, GeneratorData genData)
		: base(vessel, id, genData)
	{
		ScrubberCartridgeConsumption = (genData.AuxData as GeneratorScrubbedAirAuxData).ScrubberCartridgeConsumption;
	}

	public override async Task Update(double duration)
	{
		await base.Update(duration);
		GetScopeMultiplier(MachineryPartSlotScope.ResourcesConsumption, out var _, out var _, out var _);
		PartsWearFactor = 0f;
	}

	public override void SetAuxData(SystemAuxData auxData)
	{
	}
}
