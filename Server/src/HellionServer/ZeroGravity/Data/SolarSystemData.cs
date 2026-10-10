using System.Collections.Generic;

namespace ZeroGravity.Data;

public class SolarSystemData : ISceneData
{
	public List<CelestialBodyData> CelestialBodies;

	public double ExposureRange;

	public float[] VesselExposureValues;

	public float[] PlayerExposureValues;

	public float[] BaseSunExposureValues;
}
