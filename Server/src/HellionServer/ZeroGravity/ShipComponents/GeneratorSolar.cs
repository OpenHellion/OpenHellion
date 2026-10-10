using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class GeneratorSolar : Generator
{
	public float Efficiency = 1f;

	public override GeneratorType Type => GeneratorType.Solar;

	public override DistributionSystemType OutputType => DistributionSystemType.Power;

	public GeneratorSolar(SpaceObjectVessel vessel, VesselObjectID id, GeneratorData genData)
		: base(vessel, id, genData)
	{
	}

	public override async Task Update(double duration)
	{
		if (ParentVessel.IsExposedToSunlight)
		{
			secondaryPowerOutputFactor = MathHelper.Clamp(ParentVessel.BaseSunExposure * Efficiency, 0f, 1f);
		}
		else
		{
			secondaryPowerOutputFactor = 0f;
		}
		await base.Update(duration);
	}

	public override void SetAuxData(SystemAuxData auxData)
	{
		GeneratorSolarAuxData aux = auxData as GeneratorSolarAuxData;
		Efficiency = aux.Efficiency;
	}

	public override IAuxDetails GetAuxDetails()
	{
		return new GeneratorSolarAuxDetails
		{
			ExposureToSunlight = secondaryPowerOutputFactor
		};
	}
}
