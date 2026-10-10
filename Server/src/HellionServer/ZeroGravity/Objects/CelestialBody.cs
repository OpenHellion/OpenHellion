using System.Collections.Generic;
using ZeroGravity.Data;
using ZeroGravity.Math;

namespace ZeroGravity.Objects;

public class CelestialBody
{
	public long GUID;

	public CelestialBody Parent;

	public OrbitParameters Orbit = new OrbitParameters();

	public double Radius;

	public float AsteroidGasBurstTimeMin;

	public float AsteroidGasBurstTimeMax;

	public List<ResourceMinMax> AsteroidResources;

	public List<CelestialBody> ChildBodies = new List<CelestialBody>();

	public Vector3D Position => Orbit.Position;

	public Vector3D Velocity => Orbit.Velocity;

	public CelestialBody(long guid)
	{
		GUID = guid;
		Orbit.SetCelestialBody(this);
	}

	public bool Set(CelestialBody parent, double mass, double radius, double rotationPeriod, double eccentricity, double semiMajorAxis, double inclination, double argumentOfPeriapsis, double longitudeOfAscendingNode, double solarSystemTime)
	{
		Parent = parent;
		if (Parent != null)
		{
			Parent.ChildBodies.Add(this);
		}
		Radius = radius;
		Orbit.InitFromElements(Parent != null ? Parent.Orbit : null, mass, radius, rotationPeriod, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, 0.0, 0.0);
		return Orbit.IsOrbitValid;
	}

	public void Update()
	{
		if (Orbit.IsOrbitValid)
		{
			Orbit.UpdateOrbit();
		}
	}
}
