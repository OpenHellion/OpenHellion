using System;
using System.Collections.Generic;
using OpenHellion;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity
{
	public class OrbitParameters
	{
		private double _mass;

		private double _radius;

		private double _rotationPeriod = 86400.0;

		private double _gravParameter;

		private double _gravityInfluenceRadius;

		private double _gravityInfluenceRadiusSquared;

		private OrbitParameters _parent;

		private CelestialBody _celestialBodyObj;

		private double _eccentricity;

		private double _semiMajorAxis;

		private double _semiMinorAxis;

		private double _inclination;

		private double _argumentOfPeriapsis;

		private double _longitudeOfAscendingNode;

		private double _orbitalPeriod;

		private double _timeSincePeriapsis;

		public Vector3D RelativePosition = Vector3D.Zero;

		public Vector3D RelativeVelocity = Vector3D.Zero;

		private double _lastValidTrueAnomaly;

		private double _lastValidTimeSincePeriapsis;

		public Vector3D Position => (_parent == null) ? RelativePosition : (_parent.Position + RelativePosition);

		public Vector3D Velocity => (_parent == null) ? RelativeVelocity : (_parent.Velocity + RelativeVelocity);

		public double OrbitalPeriod => _orbitalPeriod;

		public double Radius => _radius;

		public double GravityInfluenceRadius => _gravityInfluenceRadius;

		public double GravityInfluenceRadiusSquared => _gravityInfluenceRadiusSquared;

		public OrbitParameters Parent => _parent;

		public bool IsOrbitValid => _semiMajorAxis != 0.0 && _semiMinorAxis != 0.0;

		public double TimeSincePeriapsis => _timeSincePeriapsis;

		public double SolarSystemTimeAtPeriapsis { get; private set; }

		public double Eccentricity => _eccentricity;

		public CelestialBody CelestialBody => _celestialBodyObj;

		public double LongitudeOfAscendingNode => _longitudeOfAscendingNode;

		public double ArgumentOfPeriapsis => _argumentOfPeriapsis;

		public double Inclination => _inclination;

		public double PeriapsisDistance => CalculatePeriapsisDistance(this);

		public double ApoapsisDistance => CalculateApoapsisDistance(this);

		public double SemiMajorAxis => _semiMajorAxis;

		public double SemiMinorAxis => _semiMinorAxis;

		public double Circumference => CalculateCircumference(this);

		public double DistanceAtTrueAnomaly =>
			CalculateDistanceAtTrueAnomaly(this, CalculateTrueAnomaly(this, _timeSincePeriapsis));

		public long GUID => _celestialBodyObj != null ? _celestialBodyObj.Guid : 0L;

		public void InitFromElements(OrbitParameters parent, double mass, double radius, double rotationPeriod,
			double eccentricity, double semiMajorAxis, double inclination, double argumentOfPeriapsis,
			double longitudeOfAscendingNode, double timeSincePeriapsis, double solarSystemTime)
		{
			FixEccentricity(ref eccentricity);
			_parent = parent;
			if (mass > 0.0)
			{
				_mass = mass;
			}

			if (radius > 0.0)
			{
				_radius = radius;
			}

			if (rotationPeriod > 0.0)
			{
				_rotationPeriod = rotationPeriod;
			}

			_eccentricity = eccentricity;
			_semiMajorAxis = semiMajorAxis;
			_inclination = inclination;
			_argumentOfPeriapsis = argumentOfPeriapsis;
			_longitudeOfAscendingNode = longitudeOfAscendingNode;
			SolarSystemTimeAtPeriapsis = solarSystemTime - timeSincePeriapsis;
			_gravParameter = 6.67384E-11 * _mass;
			if (parent != null)
			{
				if (eccentricity < 1.0)
				{
					_orbitalPeriod = System.Math.PI * 2.0 *
					                 System.Math.Sqrt(System.Math.Pow(semiMajorAxis, 3.0) / parent._gravParameter);
				}
				else
				{
					_orbitalPeriod = System.Math.PI * 2.0 *
					                 System.Math.Sqrt(System.Math.Pow(0.0 - semiMajorAxis, 3.0) /
					                                  parent._gravParameter);
				}

				if (eccentricity < 1.0)
				{
					_semiMinorAxis = semiMajorAxis * System.Math.Sqrt(1.0 - eccentricity * eccentricity);
				}
				else
				{
					_semiMinorAxis = semiMajorAxis * System.Math.Sqrt(eccentricity * eccentricity - 1.0);
				}

				UpdateOrbit(solarSystemTime);
				_gravityInfluenceRadius = semiMajorAxis * (1.0 - eccentricity) *
				                          System.Math.Pow(mass / (3.0 * parent._mass), 1.0 / 3.0);
				_gravityInfluenceRadiusSquared = _gravityInfluenceRadius * _gravityInfluenceRadius;
			}
			else
			{
				RelativePosition = Vector3D.Zero;
				RelativeVelocity = Vector3D.Zero;
				_orbitalPeriod = 0.0;
				_timeSincePeriapsis = 0.0;
				_semiMajorAxis = 0.0;
				_semiMinorAxis = 0.0;
				_gravityInfluenceRadius = double.PositiveInfinity;
				_gravityInfluenceRadiusSquared = double.PositiveInfinity;
			}
		}

		public void InitFromPeriapisAndApoapsis(OrbitParameters parent, double periapsisDistance,
			double apoapsisDistance, double inclination, double argumentOfPeriapsis, double longitudeOfAscendingNode,
			double trueAnomalyAngleDeg, double solarSystemTime)
		{
			_parent = parent;
			_inclination = inclination;
			_argumentOfPeriapsis = argumentOfPeriapsis;
			_longitudeOfAscendingNode = longitudeOfAscendingNode;
			_semiMajorAxis = (periapsisDistance + apoapsisDistance) / 2.0;
			_eccentricity = (apoapsisDistance - periapsisDistance) / (apoapsisDistance + periapsisDistance);
			if (_eccentricity < 1.0)
			{
				_orbitalPeriod = System.Math.PI * 2.0 *
				                 System.Math.Sqrt(System.Math.Pow(_semiMajorAxis, 3.0) / parent._gravParameter);
			}
			else
			{
				_orbitalPeriod = System.Math.PI * 2.0 *
				                 System.Math.Sqrt(System.Math.Pow(0.0 - _semiMajorAxis, 3.0) / parent._gravParameter);
			}

			_timeSincePeriapsis = CalculateTimeSincePeriapsis(this,
				CalculateMeanAnomalyFromTrueAnomaly(this, trueAnomalyAngleDeg * (System.Math.PI / 180.0)));
			InitFromElements(parent, 0.0, 0.0, 0.0, _eccentricity, _semiMajorAxis, inclination, argumentOfPeriapsis,
				longitudeOfAscendingNode, _timeSincePeriapsis, solarSystemTime);
		}

		public void InitFromStateVectors(OrbitParameters parent, Vector3D position, Vector3D velocity,
			double solarSystemTime, bool areValuesRelative)
		{
			if (parent == null)
			{
				throw new Exception("Parent object cannot be null only sun has no parent.");
			}

			if (parent._gravParameter == 0.0)
			{
				throw new Exception("Parent object grav parameter is not set.");
			}

			_parent = parent;
			RelativePosition = position;
			RelativeVelocity = velocity;
			if (!areValuesRelative)
			{
				RelativePosition -= parent.Position;
				RelativeVelocity -= parent.Velocity;
			}

			double magnitude = RelativePosition.Magnitude;
			if (magnitude < 1.0)
			{
				Diagnostics.Assert(false, $"orbit created at the centre of its parent {parent.GUID}");
				return;
			}

			double minimumTangentialSpeed = 1E-4 * System.Math.Sqrt(parent._gravParameter / magnitude);
			Vector3D rhs = Vector3D.Cross(RelativePosition, RelativeVelocity);
			if (rhs.Magnitude / magnitude < minimumTangentialSpeed)
			{
				Diagnostics.Assert(false, $"orbit created as degenerate straight-line trajectory at speed {RelativeVelocity.Magnitude:F1} m/s, distance {magnitude / 1000.0:F0} km");
				Vector3D axis = rhs.SqrMagnitude > 0.0 ? rhs : Vector3D.Cross(RelativePosition, System.Math.Abs(RelativePosition.Y / magnitude) < 0.9 ? Vector3D.Up : Vector3D.Right);
				RelativeVelocity += Vector3D.Cross(axis, RelativePosition).Normalized * (minimumTangentialSpeed - rhs.Magnitude / magnitude);
				rhs = Vector3D.Cross(RelativePosition, RelativeVelocity);
			}

			_inclination = 180.0 - System.Math.Acos(rhs.Y / rhs.Magnitude) * (180.0 / System.Math.PI);
			Vector3D right = Vector3D.Cross(Vector3D.Up, rhs);

			double magnitude2 = right.Magnitude;
			Vector3D rhs2 = Vector3D.Cross(RelativeVelocity, rhs) / parent._gravParameter -
			                RelativePosition / magnitude;
			_eccentricity = rhs2.Magnitude;
			FixEccentricity(ref _eccentricity);
			double num = RelativeVelocity.SqrMagnitude / 2.0 - parent._gravParameter / magnitude;
			if (_eccentricity < 1.0)
			{
				_semiMajorAxis = (0.0 - parent._gravParameter) / (2.0 * num);
				_semiMinorAxis = _semiMajorAxis * System.Math.Sqrt(1.0 - _eccentricity * _eccentricity);
			}
			else
			{
				_semiMajorAxis = (0.0 - rhs.SqrMagnitude / parent._gravParameter) /
				                 (_eccentricity * _eccentricity - 1.0);
				_semiMinorAxis = _semiMajorAxis * System.Math.Sqrt(_eccentricity * _eccentricity - 1.0);
			}

			if (magnitude2.IsEpsilonEqualD(0.0))
			{
				_longitudeOfAscendingNode = 0.0;
				double num2 = CalculateTrueAnomaly(this, RelativePosition, RelativeVelocity) * (180.0 / System.Math.PI);
				double num3 = 0.0 - MathHelper.AngleSigned(Vector3D.Right, RelativePosition, Vector3D.Up);
				if (num3 < 0.0)
				{
					num3 += 360.0;
				}

				_argumentOfPeriapsis = num3 - num2;
			}
			else
			{
				_longitudeOfAscendingNode = 180.0 - System.Math.Acos(right.X / magnitude2) * (180.0 / System.Math.PI);
				if (right.Z > 0.0)
				{
					_longitudeOfAscendingNode = 360.0 - _longitudeOfAscendingNode;
				}

				if (_eccentricity.IsEpsilonEqualD(0.0, 1E-10))
				{
					_argumentOfPeriapsis = 0.0;
				}
				else
				{
					_argumentOfPeriapsis = 180.0 -
					                       System.Math.Acos(MathHelper.Clamp(
						                       Vector3D.Dot(right, rhs2) / (magnitude2 * _eccentricity), -1.0, 1.0)) *
					                       (180.0 / System.Math.PI);
				}

				if (rhs2.Y > 0.0 && !_argumentOfPeriapsis.IsEpsilonEqualD(0.0))
				{
					_argumentOfPeriapsis = 360.0 - _argumentOfPeriapsis;
				}
			}

			if (_eccentricity < 1.0)
			{
				_orbitalPeriod = System.Math.PI * 2.0 *
				                 System.Math.Sqrt(System.Math.Pow(_semiMajorAxis, 3.0) / parent._gravParameter);
			}
			else
			{
				_orbitalPeriod = System.Math.PI * 2.0 *
				                 System.Math.Sqrt(System.Math.Pow(0.0 - _semiMajorAxis, 3.0) / parent._gravParameter);
			}

			_timeSincePeriapsis = CalculateTimeSincePeriapsis(this, RelativePosition, RelativeVelocity);
			SolarSystemTimeAtPeriapsis = solarSystemTime - _timeSincePeriapsis;
		}

		public void SetCelestialBody(CelestialBody body)
		{
			_celestialBodyObj = body;
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
			double num = timeSincePeriapsis % o._orbitalPeriod;
			double num2;
			if (o._eccentricity < 1.0)
			{
				if (num < 0.0)
				{
					num += o._orbitalPeriod;
				}

				double meanAnomaly = num / o._orbitalPeriod * 2.0 * System.Math.PI;
				double d = CalculateEccentricAnomaly(o, meanAnomaly);
				num2 = System.Math.Acos((System.Math.Cos(d) - o._eccentricity) /
				                        (1.0 - o._eccentricity * System.Math.Cos(d)));
			}
			else
			{
				double num3 = System.Math.PI * 2.0 * System.Math.Abs(num) / o._orbitalPeriod;
				if (num < 0.0)
				{
					num3 *= -1.0;
				}

				double value = CalculateEccentricAnomaly(o, System.Math.Abs(num3));
				num2 = System.Math.Atan2(
					System.Math.Sqrt(o._eccentricity * o._eccentricity - 1.0) * System.Math.Sinh(value),
					o._eccentricity - System.Math.Cosh(value));
			}

			if (num > o._orbitalPeriod / 2.0)
			{
				num2 = System.Math.PI * 2.0 - num2;
			}

			return num2;
		}

		private static double CalculateTrueAnomaly(OrbitParameters o, Vector3D position, Vector3D velocity)
		{
			if (o._eccentricity.IsEpsilonEqualD(0.0, 1E-10))
			{
				Vector3D vec = QuaternionD.AngleAxis(0.0 - o._longitudeOfAscendingNode, Vector3D.Up) * Vector3D.Right;
				double num = MathHelper.AngleSigned(vec, position, Vector3D.Cross(position, velocity).Normalized);
				if (num < 0.0)
				{
					num += 360.0;
				}

				return num * (System.Math.PI / 180.0);
			}

			Vector3D lhs = Vector3D.Cross(velocity, Vector3D.Cross(position, velocity)) / o._parent._gravParameter -
			               position / position.Magnitude;
			double num2 =
				System.Math.Acos(MathHelper.Clamp(Vector3D.Dot(lhs, position) / (o._eccentricity * position.Magnitude),
					-1.0, 1.0));
			if (Vector3D.Dot(position, velocity) < 0.0)
			{
				num2 = System.Math.PI * 2.0 - num2;
			}

			if (double.IsNaN(num2))
			{
				num2 = System.Math.PI;
			}

			return num2;
		}

		private static double CalculateTrueAnomalyFromEccentricAnomaly(OrbitParameters o, double eccentricAnomaly)
		{
			double num;
			if (o._eccentricity < 1.0)
			{
				num = System.Math.Acos((System.Math.Cos(eccentricAnomaly) - o._eccentricity) /
				                       (1.0 - o._eccentricity * System.Math.Cos(eccentricAnomaly)));
				if (eccentricAnomaly > System.Math.PI)
				{
					num = System.Math.PI * 2.0 - num;
				}
			}
			else
			{
				num = System.Math.Atan2(
					System.Math.Sqrt(o._eccentricity * o._eccentricity - 1.0) * System.Math.Sinh(eccentricAnomaly),
					o._eccentricity - System.Math.Cosh(eccentricAnomaly));
				if (eccentricAnomaly < 0.0)
				{
					num = System.Math.PI * 2.0 - num;
				}
			}

			return num;
		}

		private static double CalculateEccentricAnomaly(OrbitParameters o, double meanAnomaly,
			double maxDeltaDiff = 1E-06, double maxCalculations = 50.0, double maxCalculationsExtremeEcc = 10.0)
		{
			if (o._eccentricity < 1.0)
			{
				if (o._eccentricity < 0.9)
				{
					double num = 1.0;
					double num2 = meanAnomaly + o._eccentricity * System.Math.Sin(meanAnomaly) +
					              0.5 * o._eccentricity * o._eccentricity * System.Math.Sin(2.0 * meanAnomaly);
					int num3 = 0;
					while (System.Math.Abs(num) > maxDeltaDiff && num3 < maxCalculations)
					{
						num = (meanAnomaly - (num2 - o._eccentricity * System.Math.Sin(num2))) /
						      (1.0 - o._eccentricity * System.Math.Cos(num2));
						num2 += num;
						num3++;
					}

					return num2;
				}

				double num4 = meanAnomaly +
				              0.85 * o._eccentricity * System.Math.Sign(System.Math.Sin(meanAnomaly));
				for (int i = 0; i < maxCalculationsExtremeEcc; i++)
				{
					double num5 = o._eccentricity * System.Math.Sin(num4);
					double num6 = num4 - num5 - meanAnomaly;
					double num7 = 1.0 - o._eccentricity * System.Math.Cos(num4);
					num4 += -5.0 * num6 / (num7 + System.Math.Sign(num7) *
						System.Math.Sqrt(System.Math.Abs(16.0 * num7 * num7 - 20.0 * num6 * num5)));
				}

				return num4;
			}

			if (double.IsInfinity(meanAnomaly))
			{
				return meanAnomaly;
			}

			double num8 = 1.0;
			double num9 = System.Math.Log(2.0 * meanAnomaly / o._eccentricity + 1.8);
			int num10 = 0;
			while (System.Math.Abs(num8) > maxDeltaDiff && num10 < maxCalculations)
			{
				num8 = (o._eccentricity * System.Math.Sinh(num9) - num9 - meanAnomaly) /
				       (o._eccentricity * System.Math.Cosh(num9) - 1.0);
				num9 -= num8;
				num10++;
			}

			return num9;
		}

		private static double CalculateEccentricAnomalyFromTrueAnomaly(OrbitParameters o, double trueAnomaly)
		{
			double num = System.Math.Cos(trueAnomaly);
			double num2;
			if (!(o._eccentricity < 1.0))
			{
				num2 = ((System.Math.Abs(o._eccentricity * num + 1.0) >= 1E-05)
					? ((!(o._eccentricity * num >= -1.0))
						? double.NaN
						: MathHelper.Acosh((o._eccentricity + num) / (1.0 + o._eccentricity * num)))
					: ((!(trueAnomaly >= System.Math.PI)) ? double.PositiveInfinity : double.NegativeInfinity));
			}
			else
			{
				num2 = System.Math.Acos((o._eccentricity + num) / (1.0 + o._eccentricity * num));
				if (trueAnomaly > System.Math.PI)
				{
					num2 = System.Math.PI * 2.0 - num2;
				}
			}

			return num2;
		}

		private static double CalculateMeanAnomalyFromTrueAnomaly(OrbitParameters o, double trueAnomaly)
		{
			double eccentricAnomaly = CalculateEccentricAnomalyFromTrueAnomaly(o, trueAnomaly);
			return CalculateMeanAnomaly(o, trueAnomaly, eccentricAnomaly);
		}

		private static double CalculateMeanAnomaly(OrbitParameters o, double trueAnomaly, double eccentricAnomaly)
		{
			double result = eccentricAnomaly;
			if (o._eccentricity < 1.0)
			{
				result = eccentricAnomaly - o._eccentricity * System.Math.Sin(eccentricAnomaly);
			}
			else if (!double.IsInfinity(eccentricAnomaly))
			{
				result = (o._eccentricity * System.Math.Sinh(eccentricAnomaly) - eccentricAnomaly) *
				         ((!(trueAnomaly >= System.Math.PI)) ? 1.0 : (-1.0));
			}

			return result;
		}

		private static double CalculateDistanceAtTrueAnomaly(OrbitParameters o, double trueAnomaly)
		{
			if (o._eccentricity < 1.0)
			{
				return o._semiMajorAxis * (1.0 - o._eccentricity * o._eccentricity) /
				       (1.0 + o._eccentricity * System.Math.Cos(trueAnomaly));
			}

			return (0.0 - o._semiMajorAxis) * (o._eccentricity * o._eccentricity - 1.0) /
			       (1.0 + o._eccentricity * System.Math.Cos(trueAnomaly));
		}

		private static double CalculateTimeSincePeriapsis(OrbitParameters o, double meanAnomaly)
		{
			if (o._eccentricity < 1.0)
			{
				return meanAnomaly / (System.Math.PI * 2.0) * o._orbitalPeriod;
			}

			return System.Math.Sqrt(System.Math.Pow(0.0 - o._semiMajorAxis, 3.0) / o._parent._gravParameter) *
			       meanAnomaly;
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
			return o._semiMajorAxis * (1.0 - o._eccentricity);
		}

		private static double CalculateApoapsisDistance(OrbitParameters o)
		{
			return o._semiMajorAxis * (1.0 + o._eccentricity);
		}

		private static double CalculateCircumference(OrbitParameters o)
		{
			if (o._eccentricity.IsEpsilonEqualD(0.0))
			{
				return 2.0 * o._semiMajorAxis * System.Math.PI;
			}

			return System.Math.PI * (3.0 * (o._semiMajorAxis + o._semiMinorAxis) -
			                         System.Math.Sqrt((3.0 * o._semiMajorAxis + o._semiMinorAxis) *
			                                          (o._semiMajorAxis + 3.0 * o._semiMinorAxis)));
		}

		public Vector3D PositionAtTrueAnomaly(double angleRad, bool getRelativePosition)
		{
			double num = CalculateDistanceAtTrueAnomaly(this, angleRad);
			Vector3D vector3D = QuaternionD.AngleAxis(0.0 - _longitudeOfAscendingNode, Vector3D.Up) * Vector3D.Right;
			Vector3D axis = QuaternionD.AngleAxis(_inclination, vector3D) * Vector3D.Up;
			Vector3D vector3D2 =
				QuaternionD.AngleAxis(0.0 - _argumentOfPeriapsis - angleRad * (180.0 / System.Math.PI), axis) *
				vector3D * num;
			if (getRelativePosition)
			{
				return vector3D2;
			}

			return vector3D2 + _parent.Position;
		}

		public Vector3D PositionAtTimeAfterPeriapsis(double timeAfterPeriapsis, bool getRelativePosition)
		{
			double angleRad = CalculateTrueAnomaly(this, timeAfterPeriapsis);
			return PositionAtTrueAnomaly(angleRad, getRelativePosition);
		}

		public Vector3D PositionAfterTime(double time, bool getRelativePosition)
		{
			if (!IsOrbitValid)
			{
				return Vector3D.Zero;
			}

			double angleRad = CalculateTrueAnomaly(this, _timeSincePeriapsis + time);
			if (getRelativePosition)
			{
				return PositionAtTrueAnomaly(angleRad, true);
			}

			return _parent.PositionAfterTime(time, false) + PositionAtTrueAnomaly(angleRad, true);
		}

		public Vector3D PositionAtEccentricAnomaly(double angleRad, bool getRelativePosition)
		{
			return PositionAtTrueAnomaly(CalculateTrueAnomalyFromEccentricAnomaly(this, angleRad), getRelativePosition);
		}

		public Vector3D VelocityAtTrueAnomaly(double trueAnomaly, bool getRelativeVelocity)
		{
			double num = System.Math.Cos(trueAnomaly);
			double num2 = System.Math.Sin(trueAnomaly);
			double num3 =
				System.Math.Sqrt(_parent._gravParameter / (_semiMajorAxis * (1.0 - _eccentricity * _eccentricity)));
			Vector3D vector3D = QuaternionD.AngleAxis(0.0 - _longitudeOfAscendingNode, Vector3D.Up) * Vector3D.Right;
			Vector3D axis = QuaternionD.AngleAxis(_inclination, vector3D) * Vector3D.Up;
			Vector3D vector3D2 = QuaternionD.AngleAxis(0.0 - _argumentOfPeriapsis, axis) * vector3D;
			Vector3D vector3D3 = QuaternionD.AngleAxis(0.0 - _argumentOfPeriapsis - 90.0, axis) * vector3D;
			Vector3D vector3D4 = vector3D2 * ((0.0 - num2) * num3) + vector3D3 * ((_eccentricity + num) * num3);
			if (getRelativeVelocity)
			{
				return vector3D4;
			}

			return vector3D4 + _parent.Velocity;
		}

		public Vector3D VelocityAfterTime(double time, bool getRelativeVelocity)
		{
			double trueAnomaly = CalculateTrueAnomaly(this, _timeSincePeriapsis + time);
			if (!IsOrbitValid)
			{
				return Vector3D.Zero;
			}

			if (getRelativeVelocity)
			{
				return VelocityAtTrueAnomaly(trueAnomaly, getRelativeVelocity);
			}

			return _parent.VelocityAfterTime(time, false) + VelocityAtTrueAnomaly(trueAnomaly, true);
		}

		public Vector3D VelocityAtEccentricAnomaly(double angleRad, bool getRelativePosition)
		{
			return VelocityAtTrueAnomaly(CalculateTrueAnomalyFromEccentricAnomaly(this, angleRad), getRelativePosition);
		}

		public double GetRotationAngle(double solarSystemTime)
		{
			if (_rotationPeriod == 0.0)
			{
				return 0.0;
			}

			return 360.0 * (solarSystemTime % _rotationPeriod / _rotationPeriod);
		}

		public double CalculatesMeanAnomalyFromTrueAnomaly(double trueAnomaly)
		{
			return CalculateMeanAnomalyFromTrueAnomaly(this, trueAnomaly);
		}

		public void UpdateOrbit(double solarSystemTime)
		{
			if (_parent == null)
			{
				return;
			}

			Diagnostics.Assert(IsOrbitValid, $"updating degenerate orbit of {GUID} with semi-major axis {_semiMajorAxis} and semi-minor axis {_semiMinorAxis}");
			if (IsOrbitValid)
			{
				_timeSincePeriapsis = solarSystemTime - SolarSystemTimeAtPeriapsis;
				if (_eccentricity < 1.0)
				{
					_timeSincePeriapsis -= System.Math.Floor(_timeSincePeriapsis / _orbitalPeriod) * _orbitalPeriod;
					double num = CalculateTrueAnomaly(this, _timeSincePeriapsis);
					RelativePosition = PositionAtTrueAnomaly(num, true);
					RelativeVelocity = VelocityAtTrueAnomaly(num, true);
					return;
				}

				double num2 = CalculateTrueAnomaly(this, _timeSincePeriapsis);
				RelativePosition = PositionAtTrueAnomaly(num2, true);
				RelativeVelocity = VelocityAtTrueAnomaly(num2, true);
				if (RelativePosition.IsInfinity() || RelativePosition.IsNaN())
				{
					Vector3D vector3D = PositionAtTrueAnomaly(_lastValidTrueAnomaly, true);
					Vector3D vector3D2 = VelocityAtTrueAnomaly(_lastValidTrueAnomaly, true);
					RelativePosition = vector3D + vector3D2 * (_timeSincePeriapsis - _lastValidTimeSincePeriapsis);
					RelativeVelocity = vector3D2;
				}
				else
				{
					_lastValidTrueAnomaly = num2;
					_lastValidTimeSincePeriapsis = _timeSincePeriapsis;
				}
			}
		}

		public List<Vector3D> GetOrbitPositions(int numberOfPositions, double timeStep)
		{
			if (!IsOrbitValid)
			{
				return new List<Vector3D>();
			}

			List<Vector3D> list = new List<Vector3D>();
			if (_eccentricity < 1.0)
			{
				double num = System.Math.PI * 2.0 / numberOfPositions;
				for (int i = 0; i < numberOfPositions; i++)
				{
					list.Add(PositionAtEccentricAnomaly(i * num, true));
				}
			}
			else
			{
				for (int j = 0; j < numberOfPositions; j++)
				{
					list.Add(PositionAtTimeAfterPeriapsis(_timeSincePeriapsis + j * timeStep, true));
				}
			}

			return list;
		}

		public void GetOrbitPlaneData(out QuaternionD rotation, out Vector3D centerPosition)
		{
			Vector3D vector3D = QuaternionD.AngleAxis(0.0 - _longitudeOfAscendingNode, Vector3D.Up) * Vector3D.Right;
			Vector3D axis = QuaternionD.AngleAxis(_inclination, vector3D) * Vector3D.Up;
			Vector3D normalized = (QuaternionD.AngleAxis(0.0 - _argumentOfPeriapsis, axis) * vector3D).Normalized;
			rotation = QuaternionD.LookRotation(normalized,
				Vector3D.Cross(-RelativePosition, RelativeVelocity).Normalized);
			centerPosition = normalized * (CalculatePeriapsisDistance(this) - _semiMajorAxis);
		}

		public double TrueAnomalyAtZeroTime()
		{
			return CalculateTrueAnomaly(this, _orbitalPeriod - SolarSystemTimeAtPeriapsis % _orbitalPeriod);
		}

		public double TrueAnomalyAtZeroTimePlusTime(double extraTime)
		{
			return CalculateTrueAnomaly(this, extraTime);
		}

		public double TrueAnomalyAtZeroTimeFromCurrent(double extraTime)
		{
			return CalculateTrueAnomaly(this, _timeSincePeriapsis + extraTime);
		}

		public double TrueAnomalyAtCurrentTime()
		{
			return CalculateTrueAnomaly(this, _timeSincePeriapsis);
		}

		public void FillOrbitData(ref OrbitData data, long targetGuid = 0L, SpaceObjectType targetType = SpaceObjectType.None)
		{
			data.ParentGUID = _parent._celestialBodyObj.Guid;
			if (targetGuid != 0L)
			{
				data.GUID = targetGuid;
				data.ObjectType = targetType;
			}

			data.Eccentricity = _eccentricity;
			data.SemiMajorAxis = _semiMajorAxis;
			data.Inclination = _inclination;
			data.ArgumentOfPeriapsis = _argumentOfPeriapsis;
			data.LongitudeOfAscendingNode = _longitudeOfAscendingNode;
			data.SolarSystemPeriapsisTime = SolarSystemTimeAtPeriapsis;
		}

		private void CheckParent(World world, long parentGUID)
		{
			if (_parent == null || _parent._celestialBodyObj == null || _parent._celestialBodyObj.Guid != parentGUID)
			{
				_parent = world.SolarSystem.FindCelestialBody(parentGUID).Orbit;
			}
		}

		public void ParseNetworkData(World world, OrbitData data)
		{
			CheckParent(world, data.ParentGUID);
			double currentTime = world.SolarSystem.CurrentTime;
			InitFromElements(_parent, -1.0, -1.0, -1.0, data.Eccentricity, data.SemiMajorAxis, data.Inclination,
				data.ArgumentOfPeriapsis, data.LongitudeOfAscendingNode, currentTime - data.SolarSystemPeriapsisTime, currentTime);
		}

		public void ParseNetworkData(World world, ManeuverData data)
		{
			CheckParent(world, data.ParentGUID);
			RelativePosition = data.RelPosition.ToVector3D();
			RelativeVelocity = data.RelVelocity.ToVector3D();
		}

		public List<Vector3D> GetFlightPathPositions(World world, int numberOfPositions, double timeStep, out bool parentChanged)
		{
			parentChanged = false;
			List<Vector3D> posList = new List<Vector3D>();
			CalculateFlightPathPositions(world, _parent, this, numberOfPositions, timeStep, 0.0, ref posList,
				ref parentChanged);
			return posList;
		}

		private static OrbitParameters CheckGravityInfluenceRadius(World world, OrbitParameters o, double trueAnomaly,
			double timePassed, Vector3D position, Vector3D parentPos, ref List<OrbitParameters> celBodies)
		{
			if (!double.IsInfinity(o._parent._gravityInfluenceRadiusSquared) &&
			    position.DistanceSquared(parentPos) > o._parent._gravityInfluenceRadiusSquared)
			{
				Vector3D vector3D = o.VelocityAtTrueAnomaly(trueAnomaly, true) +
				                    o._parent.VelocityAfterTime(timePassed, false);
				Vector3D vector3D2 = o._parent._parent.PositionAfterTime(timePassed, false);
				Vector3D vector3D3 = o._parent._parent.VelocityAfterTime(timePassed, false);
				OrbitParameters orbitParameters = new OrbitParameters();
				orbitParameters.InitFromStateVectors(o._parent._parent, position - vector3D2, vector3D - vector3D3,
					world.SolarSystem.CurrentTime, true);
				return orbitParameters;
			}

			if (celBodies.Count > 0)
			{
				foreach (OrbitParameters celBody in celBodies)
				{
					if (position.DistanceSquared(celBody.PositionAfterTime(timePassed, false)) <
					    celBody._gravityInfluenceRadiusSquared)
					{
						Vector3D vector3D4 = o.VelocityAtTrueAnomaly(trueAnomaly, true) +
						                     o._parent.VelocityAfterTime(timePassed, false);
						Vector3D vector3D5 = celBody.PositionAfterTime(timePassed, false);
						Vector3D vector3D6 = celBody.VelocityAfterTime(timePassed, false);
						OrbitParameters orbitParameters2 = new OrbitParameters();
						orbitParameters2.InitFromStateVectors(celBody, position - vector3D5, vector3D4 - vector3D6,
							world.SolarSystem.CurrentTime, true);
						return orbitParameters2;
					}
				}
			}

			return null;
		}

		private static void CalculateFlightPathPositions(World world, OrbitParameters positionsParent, OrbitParameters o,
			int numberOfPositions, double timeStep, double timePassed, ref List<Vector3D> posList,
			ref bool parentChanged)
		{
			if (posList == null)
			{
				posList = new List<Vector3D>();
			}

			List<OrbitParameters> celBodies = new List<OrbitParameters>();
			foreach (CelestialBody celestialBody in world.SolarSystem.GetCelestialBodies())
			{
				if (celestialBody.Orbit._parent == o._parent)
				{
					celBodies.Add(celestialBody.Orbit);
				}
			}

			if (o._eccentricity < 1.0)
			{
				double meanAnomaly = o._timeSincePeriapsis / o._orbitalPeriod * 2.0 * System.Math.PI;
				double num = CalculateEccentricAnomaly(o, meanAnomaly);
				double num2 = o._timeSincePeriapsis;
				double num3 = o._timeSincePeriapsis;
				double num4 = System.Math.PI * 2.0 / numberOfPositions;
				for (int i = 0; i < numberOfPositions; i++)
				{
					num += num4;
					if (num > System.Math.PI * 2.0)
					{
						num -= System.Math.PI * 2.0;
					}

					double num5 = System.Math.Acos((System.Math.Cos(num) - o._eccentricity) /
					                               (1.0 - o._eccentricity * System.Math.Cos(num)));
					if (num3 > o._orbitalPeriod / 2.0)
					{
						num5 = System.Math.PI * 2.0 - num5;
					}

					meanAnomaly = CalculateMeanAnomaly(o, num5, num);
					num3 = CalculateTimeSincePeriapsis(o, meanAnomaly);
					timePassed = ((!(num3 < num2))
						? (timePassed + (num3 - num2))
						: (timePassed + (o._orbitalPeriod - num2 + num3)));
					num2 = num3;
					Vector3D vector3D = o._parent.PositionAfterTime(timePassed, false);
					Vector3D vector3D2 = o.PositionAtTrueAnomaly(num5, true) + vector3D;
					posList.Add(vector3D2 - positionsParent.PositionAfterTime(timePassed, false));
					OrbitParameters orbitParameters =
						CheckGravityInfluenceRadius(world, o, num5, timePassed, vector3D2, vector3D, ref celBodies);
					if (orbitParameters != null)
					{
						parentChanged = true;
						CalculateFlightPathPositions(world, positionsParent, orbitParameters, numberOfPositions - i - 1,
							timeStep, timePassed, ref posList, ref parentChanged);
						break;
					}
				}

				return;
			}

			double num6 = o._timeSincePeriapsis;
			double num7 = o._timeSincePeriapsis;
			double num8 = 0.0;
			double num9 = 0.0;
			for (int j = 0; j < numberOfPositions; j++)
			{
				num7 = o._timeSincePeriapsis + j * timeStep;
				double num10 = CalculateTrueAnomaly(o, num7);
				timePassed += num7 - num6;
				num6 = num7;
				Vector3D vector3D3 = o.PositionAtTrueAnomaly(num10, true);
				if (vector3D3.IsInfinity() || vector3D3.IsNaN())
				{
					Vector3D vector3D4 = o.PositionAtTrueAnomaly(num8, true);
					Vector3D vector3D5 = o.VelocityAtTrueAnomaly(num8, true);
					vector3D3 = vector3D4 + vector3D5 * (num7 - num9);
				}
				else
				{
					num8 = num10;
					num9 = num7;
				}

				Vector3D vector3D6 = o._parent.PositionAfterTime(timePassed, false);
				vector3D3 += vector3D6;
				posList.Add(vector3D3 - positionsParent.PositionAfterTime(timePassed, false));
				OrbitParameters orbitParameters2 =
					CheckGravityInfluenceRadius(world, o, num10, timePassed, vector3D3, vector3D6, ref celBodies);
				if (orbitParameters2 != null)
				{
					parentChanged = true;
					CalculateFlightPathPositions(world, positionsParent, orbitParameters2, numberOfPositions - j - 1, timeStep,
						timePassed, ref posList, ref parentChanged);
					break;
				}
			}
		}

	}
}
