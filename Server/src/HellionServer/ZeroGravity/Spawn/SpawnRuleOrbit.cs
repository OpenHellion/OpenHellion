using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Objects;

namespace ZeroGravity.Spawn;

public class SpawnRuleOrbit
{
	public CelestialBodyGUID CelestialBody;

	public SpawnRange<double> PeriapsisDistance;

	public SpawnRange<double> ApoapsisDistance;

	public SpawnRange<double> Inclination;

	public SpawnRange<double> ArgumentOfPeriapsis;

	public SpawnRange<double> LongitudeOfAscendingNode;

	public SpawnRange<double> TrueAnomaly;

	public bool UseCurrentSolarSystemTime;

	public OrbitParameters GenerateRandomOrbit(CelestialBody parentBody = null)
	{
		if (parentBody == null)
		{
			parentBody = Server.Instance.SolarSystem.GetCelestialBody((long)CelestialBody);
		}
		double per = MathHelper.RandomRange(PeriapsisDistance.Min, PeriapsisDistance.Max);
		double apo = MathHelper.RandomRange(ApoapsisDistance.Min, ApoapsisDistance.Max);
		if (per > apo)
		{
			double tmp = apo;
			apo = per;
			per = apo;
		}
		OrbitParameters retVal = new OrbitParameters();
		retVal.InitFromPeriapisAndApoapsis(parentBody.Orbit, per, apo, MathHelper.RandomRange(Inclination.Min, Inclination.Max), MathHelper.RandomRange(ArgumentOfPeriapsis.Min, ArgumentOfPeriapsis.Max), MathHelper.RandomRange(LongitudeOfAscendingNode.Min, LongitudeOfAscendingNode.Max), MathHelper.RandomRange(TrueAnomaly.Min, TrueAnomaly.Max), UseCurrentSolarSystemTime ? Server.SolarSystemTime : 0.0);
		return retVal;
	}
}
