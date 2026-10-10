using System;
using System.Collections.Generic;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity;

public class OrbitParameters
{
	public const double AstronomicalUnitLength = 149597870700.0;

	public const double GravitationalConstant = 6.67384E-11;

	public const double MaxObjectDistance = 897587224200.0;

	private double mass;

	private double radius;

	private double rotationPeriod = 86400.0;

	private double gravParameter;

	private double gravityInfluenceRadius;

	private double gravityInfluenceRadiusSquared;

	private OrbitParameters parent;

	private CelestialBody celestialBodyObj;

	private ArtificialBody artificialBodyObj;

	private double eccentricity;

	private double semiMajorAxis;

	private double semiMinorAxis;

	private double inclination;

	private double argumentOfPeriapsis;

	private double longitudeOfAscendingNode;

	private double orbitalPeriod;

	private double timeSincePeriapsis;

	private double solarSystemTimeAtPeriapsis;

	private double lastChangeTime;

	private Vector3D _relativePosition = Vector3D.Zero;

	private Vector3D _relativeVelocity = Vector3D.Zero;

	private double lastValidTrueAnomaly;

	private double lastValidTimeSincePeriapsis;

	public Vector3D RelativePosition
	{
		get
		{
			if (artificialBodyObj is SpaceObjectVessel vessel)
			{
				if (!vessel.IsMainVessel)
				{
					Debug.LogFormat("Vessel {0} (id: {2}) with main vessel {1}.", vessel.FullName, vessel.MainVessel.FullName, vessel.Guid);
					return vessel.MainVessel.Orbit.RelativePosition
					+ vessel.MainVessel.Rotation
					* (vessel.RelativePositionFromMainParent - vessel.MainVessel.CollidersCenterOffset.ToVector3D());
				}
			}
			return _relativePosition;
		}
		set
		{
			_relativePosition = value;
		}
	}

	public Vector3D RelativeVelocity
	{
		get
		{
			if (artificialBodyObj is SpaceObjectVessel vessel)
			{
				if (!vessel.IsMainVessel)
				{
					return vessel.MainVessel.Orbit.RelativeVelocity;
				}
			}
			return _relativeVelocity;
		}
		set
		{
			_relativeVelocity = value;
		}
	}

	public Vector3D Position => parent != null ? parent.Position + RelativePosition : RelativePosition;

	public Vector3D Velocity => parent != null ? parent.Velocity + RelativeVelocity : RelativeVelocity;

	public double OrbitalPeriod => orbitalPeriod;

	public double Radius => radius;

	public double GravParameter => gravParameter;

	public double GravityInfluenceRadius => gravityInfluenceRadius;

	public double GravityInfluenceRadiusSquared => gravityInfluenceRadiusSquared;

	public double LastChangeTime => lastChangeTime;

	public OrbitParameters Parent => parent;

	public bool IsOrbitValid => semiMajorAxis != 0.0 && semiMinorAxis != 0.0;

	public double TimeSincePeriapsis => timeSincePeriapsis;

	public double SolarSystemTimeAtPeriapsis => solarSystemTimeAtPeriapsis;

	public double Eccentricity => eccentricity;

	public CelestialBody CelestialBody => celestialBodyObj;

	public ArtificialBody ArtificialBody => artificialBodyObj;

	public double LongitudeOfAscendingNode => longitudeOfAscendingNode;

	public double ArgumentOfPeriapsis => argumentOfPeriapsis;

	public double Inclination => inclination;

	public double PeriapsisDistance => CalculatePeriapsisDistance(this);

	public double ApoapsisDistance => CalculateApoapsisDistance(this);

	public double SemiMajorAxis => semiMajorAxis;

	public double SemiMinorAxis => semiMinorAxis;

	public double Circumference => CalculateCircumference(this);

	public long GUID
	{
		get
		{
			if (artificialBodyObj != null)
			{
				return artificialBodyObj.Guid;
			}
			if (celestialBodyObj != null)
			{
				return celestialBodyObj.GUID;
			}
			return 0L;
		}
	}

	public void InitFromElements(OrbitParameters parent, double mass, double radius, double rotationPeriod, double eccentricity, double semiMajorAxis, double inclination, double argumentOfPeriapsis, double longitudeOfAscendingNode, double timeSincePeriapsis, double solarSystemTime)
	{
		FixEccentricity(ref eccentricity);
		this.parent = parent;
		if (mass > 0.0)
		{
			this.mass = mass;
		}
		if (radius > 0.0)
		{
			this.radius = radius;
		}
		if (rotationPeriod > 0.0)
		{
			this.rotationPeriod = rotationPeriod;
		}
		this.eccentricity = eccentricity;
		this.semiMajorAxis = semiMajorAxis;
		this.inclination = inclination;
		this.argumentOfPeriapsis = argumentOfPeriapsis;
		this.longitudeOfAscendingNode = longitudeOfAscendingNode;
		this.timeSincePeriapsis = timeSincePeriapsis;
		solarSystemTimeAtPeriapsis = solarSystemTime - timeSincePeriapsis;
		lastChangeTime = Server.SolarSystemTime;
		gravParameter = 6.67384E-11 * this.mass;
		if (parent != null)
		{
			if (eccentricity < 1.0)
			{
				orbitalPeriod = System.Math.PI * 2.0 * System.Math.Sqrt(System.Math.Pow(semiMajorAxis, 3.0) / parent.gravParameter);
			}
			else
			{
				orbitalPeriod = System.Math.PI * 2.0 * System.Math.Sqrt(System.Math.Pow(0.0 - semiMajorAxis, 3.0) / parent.gravParameter);
			}
			while (this.timeSincePeriapsis < 0.0 - orbitalPeriod)
			{
				this.timeSincePeriapsis += orbitalPeriod;
			}
			if (eccentricity < 1.0)
			{
				semiMinorAxis = semiMajorAxis * System.Math.Sqrt(1.0 - eccentricity * eccentricity);
			}
			else
			{
				semiMinorAxis = semiMajorAxis * System.Math.Sqrt(eccentricity * eccentricity - 1.0);
			}
			double trueAnomaly = CalculateTrueAnomaly(this, timeSincePeriapsis);
			_relativePosition = PositionAtTrueAnomaly(trueAnomaly, getRelativePosition: true);
			RelativeVelocity = VelocityAtTrueAnomaly(trueAnomaly, getRelativeVelocity: true);
			gravityInfluenceRadius = semiMajorAxis * (1.0 - eccentricity) * System.Math.Pow(mass / (3.0 * parent.mass), 1.0 / 3.0);
			gravityInfluenceRadiusSquared = gravityInfluenceRadius * gravityInfluenceRadius;
		}
		else
		{
			_relativePosition = Vector3D.Zero;
			RelativeVelocity = Vector3D.Zero;
			orbitalPeriod = 0.0;
			this.timeSincePeriapsis = 0.0;
			this.semiMajorAxis = 0.0;
			semiMinorAxis = 0.0;
			gravityInfluenceRadius = double.PositiveInfinity;
			gravityInfluenceRadiusSquared = double.PositiveInfinity;
		}
	}

	public void InitFromPeriapsis(OrbitParameters parent, double mass, double radius, double rotationPeriod, double eccentricity, double periapsisDistance, double inclination, double argumentOfPeriapsis, double longitudeOfAscendingNode, double timeSincePeriapsis, double solarSystemTime)
	{
		FixEccentricity(ref eccentricity);
		double semiMajorAxis = periapsisDistance / (1.0 - eccentricity);
		InitFromElements(parent, mass, radius, rotationPeriod, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, timeSincePeriapsis, solarSystemTime);
	}

	public void InitFromPeriapisAndApoapsis(OrbitParameters parent, double periapsisDistance, double apoapsisDistance, double inclination, double argumentOfPeriapsis, double longitudeOfAscendingNode, double trueAnomalyAngleDeg, double solarSystemTime)
	{
		this.parent = parent;
		this.inclination = inclination;
		this.argumentOfPeriapsis = argumentOfPeriapsis;
		this.longitudeOfAscendingNode = longitudeOfAscendingNode;
		semiMajorAxis = (periapsisDistance + apoapsisDistance) / 2.0;
		eccentricity = (apoapsisDistance - periapsisDistance) / (apoapsisDistance + periapsisDistance);
		if (eccentricity < 1.0)
		{
			orbitalPeriod = System.Math.PI * 2.0 * System.Math.Sqrt(System.Math.Pow(semiMajorAxis, 3.0) / parent.gravParameter);
		}
		else
		{
			orbitalPeriod = System.Math.PI * 2.0 * System.Math.Sqrt(System.Math.Pow(0.0 - semiMajorAxis, 3.0) / parent.gravParameter);
		}
		timeSincePeriapsis = CalculateTimeSincePeriapsis(this, CalculateMeanAnomalyFromTrueAnomaly(this, trueAnomalyAngleDeg * (System.Math.PI / 180.0)));
		InitFromElements(parent, 0.0, 0.0, 0.0, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, timeSincePeriapsis, solarSystemTime);
	}

	public void InitFromStateVectors(OrbitParameters parent, Vector3D position, Vector3D velocity, double solarSystemTime, bool areValuesRelative)
	{
		if (parent == null)
		{
			throw new Exception("Parent object cannot be null only sun has no parent.");
		}
		if (parent.gravParameter == 0.0)
		{
			throw new Exception("Parent object grav parameter is not set.");
		}
		this.parent = parent;
		RelativePosition = position;
		RelativeVelocity = velocity;
		if (!areValuesRelative)
		{
			RelativePosition -= parent.Position;
			RelativeVelocity -= parent.Velocity;
		}
		double rMag = RelativePosition.Magnitude;
		Vector3D h = Vector3D.Cross(RelativePosition, RelativeVelocity);
		Vector3D an = Vector3D.Right;
		if (h.SqrMagnitude.IsEpsilonEqualD(0.0))
		{
			inclination = 180.0 - System.Math.Acos(RelativePosition.Y / rMag) * (180.0 / System.Math.PI);
			an = Vector3D.Cross(RelativePosition, Vector3D.Up);
			if (an.SqrMagnitude.IsEpsilonEqualD(0.0))
			{
				an = Vector3D.Right;
			}
		}
		else
		{
			inclination = 180.0 - System.Math.Acos(h.Y / h.Magnitude) * (180.0 / System.Math.PI);
			an = Vector3D.Cross(Vector3D.Up, h);
		}
		double anMag = an.Magnitude;
		Vector3D eccentricityVec = Vector3D.Cross(RelativeVelocity, h) / parent.gravParameter - RelativePosition / rMag;
		eccentricity = eccentricityVec.Magnitude;
		FixEccentricity(ref eccentricity);
		double orbitalEnergy = RelativeVelocity.SqrMagnitude / 2.0 - parent.gravParameter / rMag;
		if (eccentricity < 1.0)
		{
			semiMajorAxis = (0.0 - parent.gravParameter) / (2.0 * orbitalEnergy);
			semiMinorAxis = semiMajorAxis * System.Math.Sqrt(1.0 - eccentricity * eccentricity);
		}
		else
		{
			semiMajorAxis = (0.0 - h.SqrMagnitude / parent.gravParameter) / (eccentricity * eccentricity - 1.0);
			semiMinorAxis = semiMajorAxis * System.Math.Sqrt(eccentricity * eccentricity - 1.0);
		}
		if (anMag.IsEpsilonEqualD(0.0))
		{
			longitudeOfAscendingNode = 0.0;
			double trueAnomalyDeg = CalculateTrueAnomaly(this, RelativePosition, RelativeVelocity) * (180.0 / System.Math.PI);
			double referenceAngle = 0.0 - MathHelper.AngleSigned(Vector3D.Right, RelativePosition, Vector3D.Up);
			if (referenceAngle < 0.0)
			{
				referenceAngle += 360.0;
			}
			argumentOfPeriapsis = referenceAngle - trueAnomalyDeg;
		}
		else
		{
			longitudeOfAscendingNode = 180.0 - System.Math.Acos(an.X / anMag) * (180.0 / System.Math.PI);
			if (an.Z > 0.0)
			{
				longitudeOfAscendingNode = 360.0 - longitudeOfAscendingNode;
			}
			if (eccentricity.IsEpsilonEqualD(0.0, 1E-10))
			{
				argumentOfPeriapsis = 0.0;
			}
			else
			{
				argumentOfPeriapsis = 180.0 - System.Math.Acos(MathHelper.Clamp(Vector3D.Dot(an, eccentricityVec) / (anMag * eccentricity), -1.0, 1.0)) * (180.0 / System.Math.PI);
			}
			if (eccentricityVec.Y > 0.0 && !argumentOfPeriapsis.IsEpsilonEqualD(0.0))
			{
				argumentOfPeriapsis = 360.0 - argumentOfPeriapsis;
			}
		}
		if (eccentricity < 1.0)
		{
			orbitalPeriod = System.Math.PI * 2.0 * System.Math.Sqrt(System.Math.Pow(semiMajorAxis, 3.0) / parent.gravParameter);
		}
		else
		{
			orbitalPeriod = System.Math.PI * 2.0 * System.Math.Sqrt(System.Math.Pow(0.0 - semiMajorAxis, 3.0) / parent.gravParameter);
		}
		timeSincePeriapsis = CalculateTimeSincePeriapsis(this, RelativePosition, RelativeVelocity);
		solarSystemTimeAtPeriapsis = solarSystemTime - timeSincePeriapsis;
		lastChangeTime = Server.SolarSystemTime;
	}

	public void InitFromCurrentStateVectors(double solarSystemTime)
	{
		InitFromStateVectors(parent, RelativePosition, RelativeVelocity, solarSystemTime, areValuesRelative: true);
	}

	public void SetCelestialBody(CelestialBody body)
	{
		celestialBodyObj = body;
	}

	public void SetArtificialBody(ArtificialBody body)
	{
		artificialBodyObj = body;
	}

	private static void FixEccentricity(ref double eccentricity)
	{
		if (eccentricity == 1.0)
		{
			eccentricity += 1E-11;
		}
	}

	private static double CalculateTrueAnomaly(OrbitParameters o, double timeSincePeriapsis)
	{
		double tsp = timeSincePeriapsis % o.orbitalPeriod;
		double trueAnomaly;
		if (o.eccentricity < 1.0)
		{
			double meanAnomaly2 = tsp / o.orbitalPeriod * 2.0 * System.Math.PI;
			double eccentricAnomaly2 = CalculateEccentricAnomaly(o, meanAnomaly2);
			trueAnomaly = System.Math.Acos((System.Math.Cos(eccentricAnomaly2) - o.eccentricity) / (1.0 - o.eccentricity * System.Math.Cos(eccentricAnomaly2)));
		}
		else
		{
			double meanAnomaly = System.Math.PI * 2.0 * System.Math.Abs(tsp) / o.orbitalPeriod;
			if (tsp < 0.0)
			{
				meanAnomaly *= -1.0;
			}
			double eccentricAnomaly = CalculateEccentricAnomaly(o, System.Math.Abs(meanAnomaly));
			trueAnomaly = System.Math.Atan2(System.Math.Sqrt(o.eccentricity * o.eccentricity - 1.0) * System.Math.Sinh(eccentricAnomaly), o.eccentricity - System.Math.Cosh(eccentricAnomaly));
		}
		if (tsp > o.orbitalPeriod / 2.0)
		{
			trueAnomaly = System.Math.PI * 2.0 - trueAnomaly;
		}
		return trueAnomaly;
	}

	private static double CalculateTrueAnomaly(OrbitParameters o, Vector3D position, Vector3D velocity)
	{
		if (o.eccentricity.IsEpsilonEqualD(0.0, 1E-10))
		{
			Vector3D ascendingNodeAxis = QuaternionD.AngleAxis(0.0 - o.longitudeOfAscendingNode, Vector3D.Up) * Vector3D.Right;
			double trueAno = MathHelper.AngleSigned(ascendingNodeAxis, position, Vector3D.Cross(position, velocity).Normalized);
			if (trueAno < 0.0)
			{
				trueAno += 360.0;
			}
			return trueAno * (System.Math.PI / 180.0);
		}
		Vector3D eccentricityVec = Vector3D.Cross(velocity, Vector3D.Cross(position, velocity)) / o.parent.gravParameter - position / position.Magnitude;
		double trueAnomaly = System.Math.Acos(MathHelper.Clamp(Vector3D.Dot(eccentricityVec, position) / (o.eccentricity * position.Magnitude), -1.0, 1.0));
		if (Vector3D.Dot(position, velocity) < 0.0)
		{
			trueAnomaly = System.Math.PI * 2.0 - trueAnomaly;
		}
		if (double.IsNaN(trueAnomaly))
		{
			trueAnomaly = System.Math.PI;
		}
		return trueAnomaly;
	}

	private static double CalculateTrueAnomalyFromEccentricAnomaly(OrbitParameters o, double eccentricAnomaly)
	{
		double trueAnomaly;
		if (o.eccentricity < 1.0)
		{
			trueAnomaly = System.Math.Acos((System.Math.Cos(eccentricAnomaly) - o.eccentricity) / (1.0 - o.eccentricity * System.Math.Cos(eccentricAnomaly)));
			if (eccentricAnomaly > System.Math.PI)
			{
				trueAnomaly = System.Math.PI * 2.0 - trueAnomaly;
			}
		}
		else
		{
			trueAnomaly = System.Math.Atan2(System.Math.Sqrt(o.eccentricity * o.eccentricity - 1.0) * System.Math.Sinh(eccentricAnomaly), o.eccentricity - System.Math.Cosh(eccentricAnomaly));
			if (eccentricAnomaly < 0.0)
			{
				trueAnomaly = System.Math.PI * 2.0 - trueAnomaly;
			}
		}
		return trueAnomaly;
	}

	private static double CalculateEccentricAnomaly(OrbitParameters o, double meanAnomaly, double maxDeltaDiff = 1E-06, double maxCalculations = 50.0, double maxCalculationsExtremeEcc = 10.0)
	{
		if (o.eccentricity < 1.0)
		{
			if (o.eccentricity < 0.9)
			{
				double Et = 1.0;
				double E = meanAnomaly + o.eccentricity * System.Math.Sin(meanAnomaly) + 0.5 * o.eccentricity * o.eccentricity * System.Math.Sin(2.0 * meanAnomaly);
				int calcIndex2 = 0;
				while (System.Math.Abs(Et) > maxDeltaDiff && calcIndex2 < maxCalculations)
				{
					Et = (meanAnomaly - (E - o.eccentricity * System.Math.Sin(E))) / (1.0 - o.eccentricity * System.Math.Cos(E));
					E += Et;
					calcIndex2++;
				}
				return E;
			}
			double E3 = meanAnomaly + 0.85 * o.eccentricity * System.Math.Sign(System.Math.Sin(meanAnomaly));
			for (int index = 0; index < maxCalculationsExtremeEcc; index++)
			{
				double eccSinE = o.eccentricity * System.Math.Sin(E3);
				double EeccSinEM = E3 - eccSinE - meanAnomaly;
				double eccCosE1 = 1.0 - o.eccentricity * System.Math.Cos(E3);
				E3 += -5.0 * EeccSinEM / (eccCosE1 + System.Math.Sign(eccCosE1) * System.Math.Sqrt(System.Math.Abs(16.0 * eccCosE1 * eccCosE1 - 20.0 * EeccSinEM * eccSinE)));
			}
			return E3;
		}
		if (double.IsInfinity(meanAnomaly))
		{
			return meanAnomaly;
		}
		double Et2 = 1.0;
		double E2 = System.Math.Log(2.0 * meanAnomaly / o.eccentricity + 1.8);
		int calcIndex = 0;
		while (System.Math.Abs(Et2) > maxDeltaDiff && calcIndex < maxCalculations)
		{
			Et2 = (o.eccentricity * System.Math.Sinh(E2) - E2 - meanAnomaly) / (o.eccentricity * System.Math.Cosh(E2) - 1.0);
			E2 -= Et2;
			calcIndex++;
		}
		return E2;
	}

	private static double CalculateEccentricAnomalyFromTrueAnomaly(OrbitParameters o, double trueAnomaly)
	{
		double taCos = System.Math.Cos(trueAnomaly);
		double eccentricAnomaly;
		if (!(o.eccentricity < 1.0))
		{
			eccentricAnomaly = System.Math.Abs(o.eccentricity * taCos + 1.0) >= 1E-05 ? !(o.eccentricity * taCos >= -1.0) ? double.NaN : MathHelper.Acosh((o.eccentricity + taCos) / (1.0 + o.eccentricity * taCos)) : !(trueAnomaly >= System.Math.PI) ? double.PositiveInfinity : double.NegativeInfinity;
		}
		else
		{
			eccentricAnomaly = System.Math.Acos((o.eccentricity + taCos) / (1.0 + o.eccentricity * taCos));
			if (trueAnomaly > System.Math.PI)
			{
				eccentricAnomaly = System.Math.PI * 2.0 - eccentricAnomaly;
			}
		}
		return eccentricAnomaly;
	}

	private static double CalculateMeanAnomalyFromTrueAnomaly(OrbitParameters o, double trueAnomaly)
	{
		double eccentricAnomaly = CalculateEccentricAnomalyFromTrueAnomaly(o, trueAnomaly);
		return CalculateMeanAnomaly(o, trueAnomaly, eccentricAnomaly);
	}

	private static double CalculateMeanAnomaly(OrbitParameters o, double trueAnomaly, double eccentricAnomaly)
	{
		double meanAnomaly = eccentricAnomaly;
		if (o.eccentricity < 1.0)
		{
			meanAnomaly = eccentricAnomaly - o.eccentricity * System.Math.Sin(eccentricAnomaly);
		}
		else if (!double.IsInfinity(eccentricAnomaly))
		{
			meanAnomaly = (o.eccentricity * System.Math.Sinh(eccentricAnomaly) - eccentricAnomaly) * (trueAnomaly >= System.Math.PI ? -1.0 : 1.0);
		}
		return meanAnomaly;
	}

	private static double CalculateDistanceAtTrueAnomaly(OrbitParameters o, double trueAnomaly)
	{
		if (o.eccentricity < 1.0)
		{
			return o.semiMajorAxis * (1.0 - o.eccentricity * o.eccentricity) / (1.0 + o.eccentricity * System.Math.Cos(trueAnomaly));
		}
		return (0.0 - o.semiMajorAxis) * (o.eccentricity * o.eccentricity - 1.0) / (1.0 + o.eccentricity * System.Math.Cos(trueAnomaly));
	}

	private static double CalculateTimeSincePeriapsis(OrbitParameters o, double meanAnomaly)
	{
		if (o.eccentricity < 1.0)
		{
			return meanAnomaly / (System.Math.PI * 2.0) * o.orbitalPeriod;
		}
		return System.Math.Sqrt(System.Math.Pow(0.0 - o.semiMajorAxis, 3.0) / o.parent.gravParameter) * meanAnomaly;
	}

	private static double CalculateTimeSincePeriapsis(OrbitParameters o, Vector3D relPosition, Vector3D relVelocity)
	{
		double trueAnomaly = CalculateTrueAnomaly(o, relPosition, relVelocity);
		double eccentricAnomaly = CalculateEccentricAnomalyFromTrueAnomaly(o, trueAnomaly);
		double meanAnomaly = CalculateMeanAnomaly(o, trueAnomaly, eccentricAnomaly);
		return CalculateTimeSincePeriapsis(o, meanAnomaly);
	}

	private static double CalculatePeriapsisDistance(OrbitParameters o)
	{
		return o.semiMajorAxis * (1.0 - o.eccentricity);
	}

	private static double CalculateApoapsisDistance(OrbitParameters o)
	{
		return o.semiMajorAxis * (1.0 + o.eccentricity);
	}

	private static double CalculateCircumference(OrbitParameters o)
	{
		if (o.eccentricity.IsEpsilonEqualD(0.0))
		{
			return 2.0 * o.semiMajorAxis * System.Math.PI;
		}
		return System.Math.PI * (3.0 * (o.semiMajorAxis + o.semiMinorAxis) - System.Math.Sqrt((3.0 * o.semiMajorAxis + o.semiMinorAxis) * (o.semiMajorAxis + 3.0 * o.semiMinorAxis)));
	}

	public Vector3D PositionAtTrueAnomaly(double angleRad, bool getRelativePosition)
	{
		double distance = CalculateDistanceAtTrueAnomaly(this, angleRad);
		Vector3D ascendingNodeAxis = QuaternionD.AngleAxis(0.0 - longitudeOfAscendingNode, Vector3D.Up) * Vector3D.Right;
		Vector3D inclinationAxis = QuaternionD.AngleAxis(inclination, ascendingNodeAxis) * Vector3D.Up;
		Vector3D pos = QuaternionD.AngleAxis(0.0 - argumentOfPeriapsis - angleRad * (180.0 / System.Math.PI), inclinationAxis) * ascendingNodeAxis * distance;
		if (getRelativePosition)
		{
			return pos;
		}
		return pos + parent.Position;
	}

	public Vector3D PositionAtTimeAfterPeriapsis(double timeAfterPeriapsis, bool getRelativePosition)
	{
		double trueAnomaly = CalculateTrueAnomaly(this, timeAfterPeriapsis);
		return PositionAtTrueAnomaly(trueAnomaly, getRelativePosition);
	}

	public Vector3D PositionAfterTime(double time, bool getRelativePosition)
	{
		if (!IsOrbitValid)
		{
			return Vector3D.Zero;
		}
		double trueAnomaly = CalculateTrueAnomaly(this, timeSincePeriapsis + time);
		if (getRelativePosition)
		{
			return PositionAtTrueAnomaly(trueAnomaly, getRelativePosition: true);
		}
		return parent.PositionAfterTime(time, getRelativePosition: false) + PositionAtTrueAnomaly(trueAnomaly, getRelativePosition: true);
	}

	public Vector3D PositionAtEccentricAnomaly(double angleRad, bool getRelativePosition)
	{
		return PositionAtTrueAnomaly(CalculateTrueAnomalyFromEccentricAnomaly(this, angleRad), getRelativePosition);
	}

	public Vector3D VelocityAtTrueAnomaly(double trueAnomaly, bool getRelativeVelocity)
	{
		double cosTa = System.Math.Cos(trueAnomaly);
		double sinTa = System.Math.Sin(trueAnomaly);
		double sqGMp = System.Math.Sqrt(parent.gravParameter / (semiMajorAxis * (1.0 - eccentricity * eccentricity)));
		Vector3D ascendingNodeAxis = QuaternionD.AngleAxis(0.0 - longitudeOfAscendingNode, Vector3D.Up) * Vector3D.Right;
		Vector3D inclinationAxis = QuaternionD.AngleAxis(inclination, ascendingNodeAxis) * Vector3D.Up;
		Vector3D P = QuaternionD.AngleAxis(0.0 - argumentOfPeriapsis, inclinationAxis) * ascendingNodeAxis;
		Vector3D Q = QuaternionD.AngleAxis(0.0 - argumentOfPeriapsis - 90.0, inclinationAxis) * ascendingNodeAxis;
		Vector3D vel = P * ((0.0 - sinTa) * sqGMp) + Q * ((eccentricity + cosTa) * sqGMp);
		if (getRelativeVelocity)
		{
			return vel;
		}
		return vel + parent.Velocity;
	}

	public Vector3D VelocityAtTimeAfterPeriapsis(double timeAfterPeriapsis, bool getRelativeVelocity)
	{
		double trueAnomaly = CalculateTrueAnomaly(this, timeAfterPeriapsis);
		return VelocityAtTrueAnomaly(trueAnomaly, getRelativeVelocity);
	}

	public Vector3D VelocityAfterTime(double time, bool getRelativeVelocity)
	{
		double trueAnomaly = CalculateTrueAnomaly(this, timeSincePeriapsis + time);
		if (!IsOrbitValid)
		{
			return Vector3D.Zero;
		}
		if (getRelativeVelocity)
		{
			return VelocityAtTrueAnomaly(trueAnomaly, getRelativeVelocity);
		}
		return parent.VelocityAfterTime(time, getRelativeVelocity: false) + VelocityAtTrueAnomaly(trueAnomaly, getRelativeVelocity: true);
	}

	public Vector3D VelocityAtEccentricAnomaly(double angleRad, bool getRelativePosition)
	{
		return VelocityAtTrueAnomaly(CalculateTrueAnomalyFromEccentricAnomaly(this, angleRad), getRelativePosition);
	}

	public void FillPositionAndVelocityAtTrueAnomaly(double angleRad, bool fillRelativeData, ref Vector3D position, ref Vector3D velocity)
	{
		position = PositionAtTrueAnomaly(angleRad, fillRelativeData);
		velocity = VelocityAtTrueAnomaly(angleRad, fillRelativeData);
	}

	public void FillPositionAndVelocityAfterTime(double time, bool fillRelativeData, ref Vector3D position, ref Vector3D velocity)
	{
		position = PositionAfterTime(time, fillRelativeData);
		velocity = VelocityAfterTime(time, fillRelativeData);
	}

	public double GetRotationAngle(double solarSystemTime)
	{
		if (rotationPeriod == 0.0)
		{
			return 0.0;
		}
		return 360.0 * (solarSystemTime % rotationPeriod / rotationPeriod);
	}

	public void UpdateOrbit()
	{
		if (parent == null)
		{
			return;
		}
		if (IsOrbitValid)
		{
			timeSincePeriapsis = Server.SolarSystemTime - solarSystemTimeAtPeriapsis;
			if (eccentricity < 1.0 && timeSincePeriapsis > orbitalPeriod)
			{
				solarSystemTimeAtPeriapsis += orbitalPeriod;
				timeSincePeriapsis %= orbitalPeriod;
			}
			if (eccentricity < 1.0)
			{
				double trueAnomaly = CalculateTrueAnomaly(this, timeSincePeriapsis);
				RelativePosition = PositionAtTrueAnomaly(trueAnomaly, getRelativePosition: true);
				RelativeVelocity = VelocityAtTrueAnomaly(trueAnomaly, getRelativeVelocity: true);
				return;
			}
			double trueAnomaly2 = CalculateTrueAnomaly(this, timeSincePeriapsis);
			RelativePosition = PositionAtTrueAnomaly(trueAnomaly2, getRelativePosition: true);
			RelativeVelocity = VelocityAtTrueAnomaly(trueAnomaly2, getRelativeVelocity: true);
			if (RelativePosition.IsInfinity() || RelativePosition.IsNaN())
			{
				Vector3D oldPos = PositionAtTrueAnomaly(lastValidTrueAnomaly, getRelativePosition: true);
				Vector3D oldVel = VelocityAtTrueAnomaly(lastValidTrueAnomaly, getRelativeVelocity: true);
				RelativePosition = oldPos + oldVel * (timeSincePeriapsis - lastValidTimeSincePeriapsis);
				RelativeVelocity = oldVel;
			}
			else
			{
				lastValidTrueAnomaly = trueAnomaly2;
				lastValidTimeSincePeriapsis = timeSincePeriapsis;
			}
		}
		else
		{
			Debug.LogWarning("ATTEMPTED TO UPDATE INVALID ORBIT");
		}
	}

	public void ResetOrbit(double solarSystemTime)
	{
		if (!IsOrbitValid)
		{
			return;
		}
		if (eccentricity < 1.0)
		{
			timeSincePeriapsis = solarSystemTime - solarSystemTimeAtPeriapsis;
			if (orbitalPeriod > 0)
			{
				double n = System.Math.Ceiling(( -orbitalPeriod - timeSincePeriapsis) / orbitalPeriod);
				timeSincePeriapsis += n * orbitalPeriod;
			}
			if (timeSincePeriapsis > orbitalPeriod)
			{
				timeSincePeriapsis %= orbitalPeriod;
				solarSystemTimeAtPeriapsis = solarSystemTime - timeSincePeriapsis;
			}
		}
		else
		{
			timeSincePeriapsis = solarSystemTime - solarSystemTimeAtPeriapsis;
		}
		double trueAnomaly = CalculateTrueAnomaly(this, timeSincePeriapsis);
		RelativePosition = PositionAtTrueAnomaly(trueAnomaly, getRelativePosition: true);
		RelativeVelocity = VelocityAtTrueAnomaly(trueAnomaly, getRelativeVelocity: true);
		lastChangeTime = Server.SolarSystemTime;
	}

	public List<Vector3D> GetOrbitPositions(int numberOfPositions, double timeStep)
	{
		if (!IsOrbitValid)
		{
			return new List<Vector3D>();
		}
		List<Vector3D> retVal = new List<Vector3D>();
		if (eccentricity < 1.0)
		{
			double theta = System.Math.PI * 2.0 / numberOfPositions;
			for (int j = 0; j < numberOfPositions; j++)
			{
				retVal.Add(PositionAtEccentricAnomaly(j * theta, getRelativePosition: true));
			}
		}
		else
		{
			for (int i = 0; i < numberOfPositions; i++)
			{
				retVal.Add(PositionAtTimeAfterPeriapsis(timeSincePeriapsis + i * timeStep, getRelativePosition: true));
			}
		}
		return retVal;
	}

	public List<Vector3D> GetOrbitVelocities(int numberOfPositions, bool getRelativeVelocities, double timeStep)
	{
		if (!IsOrbitValid)
		{
			return new List<Vector3D>();
		}
		List<Vector3D> retVal = new List<Vector3D>();
		if (eccentricity < 1.0)
		{
			double theta = System.Math.PI * 2.0 / numberOfPositions;
			for (int j = 0; j < numberOfPositions; j++)
			{
				retVal.Add(VelocityAtEccentricAnomaly(j * theta, getRelativeVelocities));
			}
		}
		else
		{
			for (int i = 0; i < numberOfPositions; i++)
			{
				retVal.Add(VelocityAtTimeAfterPeriapsis(timeSincePeriapsis + i * timeStep, getRelativeVelocities));
			}
		}
		return retVal;
	}

	public double GetTimeAfterPeriapsis(Vector3D position, Vector3D velocity, bool areValuesRelative)
	{
		if (!areValuesRelative)
		{
			position -= parent.Position;
			velocity -= parent.Velocity;
		}
		return CalculateTimeSincePeriapsis(this, position, velocity);
	}

	public void ChangeOrbitParent(OrbitParameters newParent)
	{
		RelativePosition = Position - newParent.Position;
		RelativeVelocity = Velocity - newParent.Velocity;
		parent = newParent;
	}

	public void GetOrbitPlaneData(out QuaternionD rotation, out Vector3D centerPosition)
	{
		Vector3D ascendingNodeAxis = QuaternionD.AngleAxis(0.0 - longitudeOfAscendingNode, Vector3D.Up) * Vector3D.Right;
		Vector3D inclinationAxis = QuaternionD.AngleAxis(inclination, ascendingNodeAxis) * Vector3D.Up;
		Vector3D pos = (QuaternionD.AngleAxis(0.0 - argumentOfPeriapsis, inclinationAxis) * ascendingNodeAxis).Normalized;
		rotation = QuaternionD.LookRotation(pos, Vector3D.Cross(-RelativePosition, RelativeVelocity).Normalized);
		centerPosition = pos * (CalculatePeriapsisDistance(this) - semiMajorAxis);
	}

	public double TrueAnomalyAtZeroTime()
	{
		return CalculateTrueAnomaly(this, orbitalPeriod - solarSystemTimeAtPeriapsis % orbitalPeriod);
	}

	public double TrueAnomalyAtZeroTimeFromCurrent(double extraTime)
	{
		return CalculateTrueAnomaly(this, timeSincePeriapsis + extraTime);
	}

	public void FillOrbitData(ref OrbitData data, SpaceObjectVessel targetVessel = null)
	{
		data.ParentGUID = parent.celestialBodyObj.GUID;
		if (targetVessel != null)
		{
			data.GUID = targetVessel.Guid;
			if (targetVessel is Ship)
			{
				data.ObjectType = SpaceObjectType.Ship;
			}
			else if (targetVessel is Asteroid)
			{
				data.ObjectType = SpaceObjectType.Asteroid;
			}
		}
		data.Eccentricity = eccentricity;
		data.SemiMajorAxis = semiMajorAxis;
		data.Inclination = inclination;
		data.ArgumentOfPeriapsis = argumentOfPeriapsis;
		data.LongitudeOfAscendingNode = longitudeOfAscendingNode;
		data.TimeSincePeriapsis = timeSincePeriapsis;
		data.SolarSystemPeriapsisTime = solarSystemTimeAtPeriapsis;
	}

	private void CheckParent(long parentGUID)
	{
		if (parent == null || parent.celestialBodyObj == null || parent.celestialBodyObj.GUID != parentGUID)
		{
			parent = Server.Instance.SolarSystem.GetCelestialBody(parentGUID).Orbit;
		}
	}

	public void ParseNetworkData(OrbitData data, bool resetOrbit = false)
	{
		CheckParent(data.ParentGUID);
		solarSystemTimeAtPeriapsis = data.SolarSystemPeriapsisTime;
		double currTime = Server.Instance.SolarSystem.CurrentTime;
		timeSincePeriapsis = currTime - solarSystemTimeAtPeriapsis;
		InitFromElements(parent, -1.0, -1.0, -1.0, data.Eccentricity, data.SemiMajorAxis, data.Inclination, data.ArgumentOfPeriapsis, data.LongitudeOfAscendingNode, timeSincePeriapsis, currTime);
		if (resetOrbit)
		{
			ResetOrbit(currTime);
		}
	}

	private static bool AreAnglesEqualDeg(double angle1, double angle2, double anglePrecissionDeg)
	{
		angle1 %= 360.0;
		angle2 %= 360.0;
		if (angle1 < 0.0)
		{
			angle1 += 360.0;
		}
		if (angle2 < 0.0)
		{
			angle2 += 360.0;
		}
		return angle1.IsEpsilonEqualD(angle2, anglePrecissionDeg) || (angle1 >= 360.0 - anglePrecissionDeg && angle2 <= anglePrecissionDeg - 360.0 + angle1) || (angle2 >= 360.0 - anglePrecissionDeg && angle1 <= anglePrecissionDeg - 360.0 + angle2);
	}

	public bool AreOrbitsOverlapping(OrbitParameters orbit, double axisPrecision = 1.0, double eccentricityPrecision = 1E-08, double anglePrecissionDeg = 1.0, double eccentricityZero = 0.001)
	{
		if (parent != orbit.parent || !eccentricity.IsEpsilonEqualD(orbit.eccentricity, eccentricityPrecision) || !semiMajorAxis.IsEpsilonEqualD(orbit.semiMajorAxis, axisPrecision))
		{
			return false;
		}
		if (eccentricity.IsEpsilonEqualD(0.0, eccentricityZero))
		{
			bool loanEq = AreAnglesEqualDeg(longitudeOfAscendingNode, orbit.longitudeOfAscendingNode, anglePrecissionDeg);
			bool incEq = AreAnglesEqualDeg(inclination, orbit.inclination, anglePrecissionDeg);
			if (incEq && (loanEq || inclination.IsEpsilonEqualD(0.0, eccentricityZero)))
			{
				return true;
			}
			if (loanEq && (incEq || AreAnglesEqualDeg(inclination, 180.0 - orbit.inclination, anglePrecissionDeg)))
			{
				return true;
			}
			return false;
		}
		return AreAnglesEqualDeg(longitudeOfAscendingNode, orbit.longitudeOfAscendingNode, anglePrecissionDeg) && AreAnglesEqualDeg(argumentOfPeriapsis, orbit.argumentOfPeriapsis, anglePrecissionDeg) && AreAnglesEqualDeg(inclination, orbit.inclination, anglePrecissionDeg);
	}

	public void CopyDataFrom(OrbitParameters orbit, double solarSystemTime, bool exactCopy = false)
	{
		parent = orbit.parent;
		eccentricity = orbit.eccentricity;
		semiMajorAxis = orbit.semiMajorAxis;
		semiMinorAxis = orbit.semiMinorAxis;
		inclination = orbit.inclination;
		argumentOfPeriapsis = orbit.argumentOfPeriapsis;
		longitudeOfAscendingNode = orbit.longitudeOfAscendingNode;
		solarSystemTimeAtPeriapsis = orbit.solarSystemTimeAtPeriapsis;
		orbitalPeriod = orbit.orbitalPeriod;
		if (exactCopy)
		{
			timeSincePeriapsis = orbit.timeSincePeriapsis;
			if (lastChangeTime < orbit.lastChangeTime)
			{
				lastChangeTime = orbit.lastChangeTime;
			}
			RelativePosition = orbit.RelativePosition;
			RelativeVelocity = orbit.RelativeVelocity;
		}
		else
		{
			ResetOrbit(solarSystemTime);
		}
	}

	public void SetLastChangeTime(double time)
	{
		lastChangeTime = time;
	}

	public double CircularOrbitVelocityMagnitudeAtDistance(double distance)
	{
		return System.Math.Sqrt(gravParameter / distance);
	}

	public double RandomOrbitVelocityMagnitudeAtDistance(double distance)
	{
		double e = MathHelper.RandomRange(0.0, 0.8);
		double a = distance / (1.0 - e);
		if (a + a - distance > gravityInfluenceRadius)
		{
			a = gravityInfluenceRadius * 0.8 / 2.0;
			e = 1.0 - distance / a;
		}
		if (a + a - distance > 897587224200.0)
		{
			a = 359034889680.0;
			e = 1.0 - distance / a;
		}
		if (e < 0.0)
		{
			e = 0.0;
		}
		return System.Math.Sqrt((e + 1.0) / distance * gravParameter);
	}

	public string DebugString()
	{
		return $"P {(parent != null ? parent.GUID : -1)}, ECC {eccentricity}, SMA {semiMajorAxis}, INC {inclination}, AOP {argumentOfPeriapsis}, LOAN {longitudeOfAscendingNode}, SSTAP {solarSystemTimeAtPeriapsis}, TSP {timeSincePeriapsis}";
	}
}
