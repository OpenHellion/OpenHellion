using System.Collections.Generic;

namespace ZeroGravity.Data;

public class CelestialBodyData : ISceneData
{
	public short GUID;

	public string Name;

	public long ParentGUID;

	public double Mass;

	public double Radius;

	public double RotationPeriod;

	public double Eccentricity;

	public double SemiMajorAxis;

	public double Inclination;

	public double ArgumentOfPeriapsis;

	public double LongitudeOfAscendingNode;

	public string PlanetsPrefabPath;

	public string MainCameraPrefabPath;

	public string NavigationPrefabPath;

	public float AsteroidGasBurstTimeMin;

	public float AsteroidGasBurstTimeMax;

	public float AsteroidGasBurstDmgMultiplier;

	public List<ResourceMinMax> AsteroidResources;

	public float[] ScanningSensitivityModifierValues;

	public float[] RadarSignatureModifierValues;
}
