// OrbitParametersTests.cs
//
// Copyright (C) 2026, OpenHellion contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using System.Diagnostics;
using ZeroGravity;
using ZeroGravity.Math;

namespace HellionServer.Tests;

public class OrbitParametersTests
{
	private const double EarthMass = 5.97E24;

	private const double SunMass = 1.989E30;

	private readonly System.Text.StringBuilder _assertFailures = new System.Text.StringBuilder();

	private TraceListener[] _listeners = [];

	private static readonly object[] NearParabolicOrbits =
	[
		new object[] { EarthMass, 0.9999, 1.0E8, 10.0, 20.0, 30.0 },
		new object[] { EarthMass, 1.0 - 1E-6, 1.0E8, 40.0, 50.0, 60.0 },
		new object[] { EarthMass, 1.0 - 1E-8, 4.0E7, 0.0, 0.0, 0.0 },
		new object[] { EarthMass, 1.0 - 1E-9, 4.0E7, 120.0, 200.0, 310.0 },
		new object[] { SunMass, 1.0 - 1E-8, 3.0E11, 5.0, 15.0, 25.0 }
	];

	private static readonly object[] StraightLineTrajectories =
	[
		new object[] { new Vector3D(1.0, 0.0, 0.0), 0.0, 0.0 },
		new object[] { new Vector3D(0.0, 1.0, 0.0), 0.0, 0.0 },
		new object[] { new Vector3D(0.3, 0.4, 0.866), 0.5, 0.0 },
		new object[] { new Vector3D(0.3, 0.4, 0.866), -0.5, 0.0 },
		new object[] { new Vector3D(0.6, -0.8, 0.0), 0.2, 1E-6 }
	];

	[SetUp]
	public void CaptureAssertFailures()
	{
		_listeners = Trace.Listeners.Cast<TraceListener>().ToArray();
		_assertFailures.Clear();
		Trace.Listeners.Clear();
		Trace.Listeners.Add(new TextWriterTraceListener(new StringWriter(_assertFailures)));
	}

	[TearDown]
	public void RestoreListeners()
	{
		Trace.Listeners.Clear();
		Trace.Listeners.AddRange(_listeners);
		Assert.That(_assertFailures.ToString(), Is.Empty, "unexpected assert failures");
	}

	private static readonly object[] EllipticOrbits =
	[
		new object[] { EarthMass, 0.0, 6.8E6, 51.6, 0.0, 0.0 },
		new object[] { EarthMass, 0.3, 4.0E7, 30.0, 60.0, 120.0 },
		new object[] { EarthMass, 0.6, 4.0E7, 0.0, 45.0, 0.0 },
		new object[] { EarthMass, 0.5, 4.0E7, 150.0, 270.0, 300.0 },
		new object[] { EarthMass, 0.9, 1.0E8, 63.4, 270.0, 10.0 },
		new object[] { EarthMass, 0.97, 1.0E8, 10.0, 90.0, 200.0 },
		new object[] { EarthMass, 0.995, 5.0E8, 5.0, 300.0, 80.0 },
		new object[] { SunMass, 0.0167, 1.496E11, 0.0, 102.9, 0.0 },
		new object[] { SunMass, 0.2, 4.5E11, 2.0, 100.0, 250.0 }
	];

	private static readonly object[] IntegrableOrbits = EllipticOrbits.Where(m => (double)((object[])m)[1] <= 0.9).ToArray();

	private static readonly double[] PeriodFractions = [0.0, 0.01, 0.25, 0.49, 0.5, 0.51, 0.75, 0.99];

	private static OrbitParameters CreateOrbit(double parentMass, double eccentricity, double semiMajorAxis, double inclination, double argumentOfPeriapsis, double longitudeOfAscendingNode, double timeSincePeriapsis)
	{
		OrbitParameters parent = new OrbitParameters();
		parent.InitFromElements(null, parentMass, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0);
		OrbitParameters orbit = new OrbitParameters();
		orbit.InitFromElements(parent, 1.0, 1.0, 1.0, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, timeSincePeriapsis, timeSincePeriapsis);
		return orbit;
	}

	[TestCaseSource(nameof(EllipticOrbits))]
	public void ApsidesAreAtPeriapsisAndHalfPeriod(double parentMass, double eccentricity, double semiMajorAxis, double inclination, double argumentOfPeriapsis, double longitudeOfAscendingNode)
	{
		OrbitParameters orbit = CreateOrbit(parentMass, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, 0.0);

		double periapsis = orbit.PositionAtTimeAfterPeriapsis(0.0, true).Magnitude;
		double apoapsis = orbit.PositionAtTimeAfterPeriapsis(orbit.OrbitalPeriod / 2.0, true).Magnitude;

		Assert.Multiple(() =>
		{
			Assert.That(periapsis, Is.EqualTo(semiMajorAxis * (1.0 - eccentricity)).Within(1E-9).Percent);
			Assert.That(apoapsis, Is.EqualTo(semiMajorAxis * (1.0 + eccentricity)).Within(1E-6).Percent);
		});
	}

	[TestCaseSource(nameof(EllipticOrbits))]
	public void EnergyAndAngularMomentumAreConservedAlongEllipse(double parentMass, double eccentricity, double semiMajorAxis, double inclination, double argumentOfPeriapsis, double longitudeOfAscendingNode)
	{
		OrbitParameters orbit = CreateOrbit(parentMass, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, 0.0);
		double mu = orbit.Parent.GravParameter;
		double angularMomentum = System.Math.Sqrt(mu * semiMajorAxis * (1.0 - eccentricity * eccentricity));

		Assert.Multiple(() =>
		{
			for (int i = -64; i <= 64; i++)
			{
				double time = orbit.OrbitalPeriod * i / 64.0;
				Vector3D r = orbit.PositionAtTimeAfterPeriapsis(time, true);
				Vector3D v = orbit.VelocityAtTimeAfterPeriapsis(time, true);
				double fraction = (i / 64.0 % 1.0 + 1.0) % 1.0;
				Assert.That(v.SqrMagnitude, Is.EqualTo(mu * (2.0 / r.Magnitude - 1.0 / semiMajorAxis)).Within(1E-6).Percent, $"vis-viva at {time:F1} s");
				Assert.That(Vector3D.Cross(r, v).Magnitude, Is.EqualTo(angularMomentum).Within(1E-6).Percent, $"angular momentum at {time:F1} s");
				if (eccentricity > 0.0 && fraction is > 0.001 and < 0.499)
				{
					Assert.That(Vector3D.Dot(r, v), Is.Positive, $"moving away from periapsis at {time:F1} s");
				}
				if (eccentricity > 0.0 && fraction is > 0.501 and < 0.999)
				{
					Assert.That(Vector3D.Dot(r, v), Is.Negative, $"moving towards periapsis at {time:F1} s");
				}
			}
		});
	}

	[TestCaseSource(nameof(EllipticOrbits))]
	public void PositionRepeatsEveryPeriodForwardsAndBackwards(double parentMass, double eccentricity, double semiMajorAxis, double inclination, double argumentOfPeriapsis, double longitudeOfAscendingNode)
	{
		OrbitParameters orbit = CreateOrbit(parentMass, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, 0.0);
		double period = orbit.OrbitalPeriod;

		Assert.Multiple(() =>
		{
			foreach (double fraction in PeriodFractions)
			{
				Vector3D reference = orbit.PositionAtTimeAfterPeriapsis(period * fraction, true);
				foreach (double periods in new[] { -1000.0, -10.0, -1.0, 1.0, 10.0, 1000.0, 100000.0 })
				{
					double time = period * (fraction + periods);
					double error = (orbit.PositionAtTimeAfterPeriapsis(time, true) - reference).Magnitude;
					Assert.That(error, Is.LessThan(semiMajorAxis * 1E-6), $"{periods} periods from {fraction} of period");
				}
			}
		});
	}

	[TestCaseSource(nameof(EllipticOrbits))]
	public void ResetOrbitKeepsPositionAtSameTime(double parentMass, double eccentricity, double semiMajorAxis, double inclination, double argumentOfPeriapsis, double longitudeOfAscendingNode)
	{
		double period = CreateOrbit(parentMass, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, 0.0).OrbitalPeriod;

		Assert.Multiple(() =>
		{
			foreach (double fraction in PeriodFractions)
			{
				double time = period * fraction;
				OrbitParameters orbit = CreateOrbit(parentMass, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, time);
				Vector3D initialised = orbit.Position;
				orbit.UpdateOrbit(time);
				Assert.That((orbit.Position - initialised).Magnitude, Is.LessThan(semiMajorAxis * 1E-6), $"{fraction} of period became {orbit.TimeSincePeriapsis / period:F4}");
			}
		});
	}

	[TestCaseSource(nameof(EllipticOrbits))]
	public void ResetOrbitFollowsOrbitOverLongServerUptime(double parentMass, double eccentricity, double semiMajorAxis, double inclination, double argumentOfPeriapsis, double longitudeOfAscendingNode)
	{
		OrbitParameters orbit = CreateOrbit(parentMass, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, 0.0);
		double period = orbit.OrbitalPeriod;

		Assert.Multiple(() =>
		{
			foreach (double serverTime in new[] { 86400.0, 5256000.0, 3.15E8 })
			{
				foreach (double fraction in PeriodFractions)
				{
					double time = System.Math.Ceiling(serverTime / period) * period + period * fraction;
					orbit.UpdateOrbit(time);
					Vector3D expected = orbit.PositionAtTimeAfterPeriapsis(period * fraction, true);
					Assert.That((orbit.RelativePosition - expected).Magnitude, Is.LessThan(semiMajorAxis * 1E-6), $"server time {time:F0} at {fraction} of period");
					Assert.That(orbit.TimeSincePeriapsis, Is.InRange(-period, period), $"time since periapsis at server time {time:F0}");
				}
			}
		});
	}

	[TestCaseSource(nameof(EllipticOrbits))]
	public void VelocityIsDerivativeOfPosition(double parentMass, double eccentricity, double semiMajorAxis, double inclination, double argumentOfPeriapsis, double longitudeOfAscendingNode)
	{
		OrbitParameters orbit = CreateOrbit(parentMass, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, 0.0);
		double step = orbit.OrbitalPeriod * 1E-6;

		Assert.Multiple(() =>
		{
			foreach (double fraction in PeriodFractions.Concat([-0.25, -0.75]))
			{
				double time = orbit.OrbitalPeriod * fraction + step * 10.0;
				Vector3D velocity = orbit.VelocityAtTimeAfterPeriapsis(time, true);
				Vector3D derivative = (orbit.PositionAtTimeAfterPeriapsis(time + step, true) - orbit.PositionAtTimeAfterPeriapsis(time - step, true)) / (2.0 * step);
				Assert.That((derivative - velocity).Magnitude, Is.LessThan(velocity.Magnitude * 1E-4), $"at {fraction} of period");
			}
		});
	}

	[TestCaseSource(nameof(IntegrableOrbits))]
	public void MatchesNewtonianIntegrationOverSeveralPeriods(double parentMass, double eccentricity, double semiMajorAxis, double inclination, double argumentOfPeriapsis, double longitudeOfAscendingNode)
	{
		OrbitParameters orbit = CreateOrbit(parentMass, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, 0.0);
		double mu = orbit.Parent.GravParameter;
		double start = orbit.OrbitalPeriod * 0.1;
		int stepsPerCheckpoint = (int)(2500 * System.Math.Pow(1.0 / (1.0 - eccentricity), 1.5));
		double dt = orbit.OrbitalPeriod / 8.0 / stepsPerCheckpoint;
		Vector3D r = orbit.PositionAtTimeAfterPeriapsis(start, true);
		Vector3D v = orbit.VelocityAtTimeAfterPeriapsis(start, true);

		Assert.Multiple(() =>
		{
			for (int checkpoint = 1; checkpoint <= 24; checkpoint++)
			{
				for (int step = 0; step < stepsPerCheckpoint; step++)
				{
					Vector3D a1 = r * (-mu / System.Math.Pow(r.Magnitude, 3.0));
					Vector3D r2 = r + v * (dt / 2.0);
					Vector3D v2 = v + a1 * (dt / 2.0);
					Vector3D a2 = r2 * (-mu / System.Math.Pow(r2.Magnitude, 3.0));
					Vector3D r3 = r + v2 * (dt / 2.0);
					Vector3D v3 = v + a2 * (dt / 2.0);
					Vector3D a3 = r3 * (-mu / System.Math.Pow(r3.Magnitude, 3.0));
					Vector3D r4 = r + v3 * dt;
					Vector3D v4 = v + a3 * dt;
					Vector3D a4 = r4 * (-mu / System.Math.Pow(r4.Magnitude, 3.0));
					r += (v + v2 * 2.0 + v3 * 2.0 + v4) * (dt / 6.0);
					v += (a1 + a2 * 2.0 + a3 * 2.0 + a4) * (dt / 6.0);
				}
				double time = start + checkpoint * stepsPerCheckpoint * dt;
				double error = (orbit.PositionAtTimeAfterPeriapsis(time, true) - r).Magnitude;
				Assert.That(error, Is.LessThan(semiMajorAxis * 1E-5), $"checkpoint {checkpoint} at {time / orbit.OrbitalPeriod:F3} periods");
			}
		});
	}

	[TestCaseSource(nameof(EllipticOrbits))]
	public void StateVectorsRoundTripToSameOrbit(double parentMass, double eccentricity, double semiMajorAxis, double inclination, double argumentOfPeriapsis, double longitudeOfAscendingNode)
	{
		double period = CreateOrbit(parentMass, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, 0.0).OrbitalPeriod;

		Assert.Multiple(() =>
		{
			foreach (double fraction in PeriodFractions)
			{
				double time = period * fraction;
				OrbitParameters orbit = CreateOrbit(parentMass, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, time);
				OrbitParameters copy = new OrbitParameters();
				copy.InitFromStateVectors(orbit.Parent, orbit.RelativePosition, orbit.RelativeVelocity, time, areValuesRelative: true);
				Assert.That(copy.SemiMajorAxis, Is.EqualTo(semiMajorAxis).Within(1E-6).Percent, $"semi-major axis at {fraction} of period");
				Assert.That(copy.Eccentricity, Is.EqualTo(eccentricity).Within(1E-6), $"eccentricity at {fraction} of period");
				foreach (double later in new[] { 0.0, 0.3, 0.5, 0.8, 7.25 })
				{
					double error = (copy.PositionAfterTime(period * later, true) - orbit.PositionAfterTime(period * later, true)).Magnitude;
					Assert.That(error, Is.LessThan(semiMajorAxis * 1E-6), $"{later} periods after {fraction} of period");
				}
			}
		});
	}

	[TestCaseSource(nameof(NearParabolicOrbits))]
	public void NearParabolicOrbitFollowsNewtonianPhysics(double parentMass, double eccentricity, double semiMajorAxis, double inclination, double argumentOfPeriapsis, double longitudeOfAscendingNode)
	{
		OrbitParameters orbit = CreateOrbit(parentMass, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, 0.0);
		double mu = orbit.Parent.GravParameter;
		double period = orbit.OrbitalPeriod;
		double angularMomentum = System.Math.Sqrt(mu * semiMajorAxis * (1.0 - eccentricity * eccentricity));
		double start = period * 0.1;
		int stepsPerCheckpoint = 20000;
		double dt = period * 0.8 / 16.0 / stepsPerCheckpoint;
		Vector3D r = orbit.PositionAtTimeAfterPeriapsis(start, true);
		Vector3D v = orbit.VelocityAtTimeAfterPeriapsis(start, true);

		Assert.Multiple(() =>
		{
			foreach (double fraction in new[] { -1E-6, 1E-6, 1E-3, 0.1, 0.25, 0.5, 0.75, 0.999 })
			{
				double time = period * fraction;
				Vector3D position = orbit.PositionAtTimeAfterPeriapsis(time, true);
				Vector3D velocity = orbit.VelocityAtTimeAfterPeriapsis(time, true);
				Assert.That(position.Magnitude, Is.InRange(semiMajorAxis * (1.0 - eccentricity) * (1.0 - 1E-6) - 1E-6 * semiMajorAxis, semiMajorAxis * (1.0 + eccentricity) * (1.0 + 1E-9)), $"distance at {fraction} of period");
				Assert.That(velocity.SqrMagnitude / 2.0 - mu / position.Magnitude, Is.EqualTo(-mu / (2.0 * semiMajorAxis)).Within(1E-6 * mu / position.Magnitude), $"orbital energy at {fraction} of period");
				Assert.That(Vector3D.Cross(position, velocity).Magnitude, Is.EqualTo(angularMomentum).Within(1E-3).Percent, $"angular momentum at {fraction} of period");
				Assert.That((orbit.PositionAtTimeAfterPeriapsis(time + period * 3.0, true) - position).Magnitude, Is.LessThan(semiMajorAxis * 1E-6), $"periodicity at {fraction} of period");
			}

			for (int checkpoint = 1; checkpoint <= 16; checkpoint++)
			{
				for (int step = 0; step < stepsPerCheckpoint; step++)
				{
					Vector3D a1 = r * (-mu / System.Math.Pow(r.Magnitude, 3.0));
					Vector3D r2 = r + v * (dt / 2.0);
					Vector3D v2 = v + a1 * (dt / 2.0);
					Vector3D a2 = r2 * (-mu / System.Math.Pow(r2.Magnitude, 3.0));
					Vector3D r3 = r + v2 * (dt / 2.0);
					Vector3D v3 = v + a2 * (dt / 2.0);
					Vector3D a3 = r3 * (-mu / System.Math.Pow(r3.Magnitude, 3.0));
					Vector3D r4 = r + v3 * dt;
					Vector3D v4 = v + a3 * dt;
					Vector3D a4 = r4 * (-mu / System.Math.Pow(r4.Magnitude, 3.0));
					r += (v + v2 * 2.0 + v3 * 2.0 + v4) * (dt / 6.0);
					v += (a1 + a2 * 2.0 + a3 * 2.0 + a4) * (dt / 6.0);
				}
				double time = start + checkpoint * stepsPerCheckpoint * dt;
				double error = (orbit.PositionAtTimeAfterPeriapsis(time, true) - r).Magnitude;
				Assert.That(error, Is.LessThan(semiMajorAxis * 1E-5), $"integration checkpoint at {time / period:F3} periods");
			}
		});
	}

	[TestCaseSource(nameof(StraightLineTrajectories))]
	public void StraightLineTrajectoryBecomesNearlyStraightOrbit(Vector3D direction, double radialFraction, double tangentialFraction)
	{
		OrbitParameters parent = new OrbitParameters();
		parent.InitFromElements(null, EarthMass, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0);
		double mu = parent.GravParameter;
		double distance = 4.0E7;
		double circularSpeed = System.Math.Sqrt(mu / distance);
		double timeScale = distance / circularSpeed;
		direction = direction.Normalized;
		Vector3D tangent = Vector3D.Cross(direction, System.Math.Abs(direction.Y) < 0.9 ? Vector3D.Up : Vector3D.Right).Normalized;
		Vector3D r = direction * distance;
		Vector3D v = direction * (radialFraction * circularSpeed) + tangent * (tangentialFraction * circularSpeed);
		OrbitParameters orbit = new OrbitParameters();

		orbit.InitFromStateVectors(parent, r, v, 0.0, areValuesRelative: true);

		Assert.That(_assertFailures.ToString(), Does.Contain("degenerate straight-line trajectory"));
		_assertFailures.Clear();
		Assert.Multiple(() =>
		{
			Assert.That(orbit.IsOrbitValid, "orbit is valid");
			Assert.That(orbit.Eccentricity, Is.LessThan(1.0), "orbit is bound");
			Assert.That((orbit.RelativePosition - r).Magnitude, Is.LessThan(1E-9 * distance), "position is kept");
			Assert.That((orbit.RelativeVelocity - v).Magnitude, Is.LessThanOrEqualTo(1.0001E-4 * circularSpeed), "velocity changes by at most the minimum tangential speed");

			double dt = timeScale / 20000.0;
			for (int checkpoint = 1; checkpoint <= 300; checkpoint++)
			{
				for (int step = 0; step < 200; step++)
				{
					Vector3D a1 = r * (-mu / System.Math.Pow(r.Magnitude, 3.0));
					Vector3D r2 = r + v * (dt / 2.0);
					Vector3D v2 = v + a1 * (dt / 2.0);
					Vector3D a2 = r2 * (-mu / System.Math.Pow(r2.Magnitude, 3.0));
					Vector3D r3 = r + v2 * (dt / 2.0);
					Vector3D v3 = v + a2 * (dt / 2.0);
					Vector3D a3 = r3 * (-mu / System.Math.Pow(r3.Magnitude, 3.0));
					Vector3D r4 = r + v3 * dt;
					Vector3D v4 = v + a3 * dt;
					Vector3D a4 = r4 * (-mu / System.Math.Pow(r4.Magnitude, 3.0));
					r += (v + v2 * 2.0 + v3 * 2.0 + v4) * (dt / 6.0);
					v += (a1 + a2 * 2.0 + a3 * 2.0 + a4) * (dt / 6.0);
				}
				if (r.Magnitude < 0.2 * distance)
				{
					break;
				}
				double time = checkpoint * 200 * dt;
				double error = (orbit.PositionAfterTime(time, true) - r).Magnitude;
				Assert.That(error, Is.LessThan(1E-3 * distance), $"straight-line fall diverged at {time / timeScale:F2} time scales, {r.Magnitude / distance:F2} of start distance");
			}
		});
	}

	[Test]
	public void OrbitAtCentreOfParentIsRefused()
	{
		OrbitParameters parent = new OrbitParameters();
		parent.InitFromElements(null, EarthMass, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0);
		OrbitParameters orbit = new OrbitParameters();

		orbit.InitFromStateVectors(parent, Vector3D.Zero, Vector3D.One, 0.0, areValuesRelative: true);
		string creation = _assertFailures.ToString();
		_assertFailures.Clear();
		orbit.UpdateOrbit(100.0);
		string update = _assertFailures.ToString();
		_assertFailures.Clear();

		Assert.Multiple(() =>
		{
			Assert.That(creation, Does.Contain("centre of its parent"));
			Assert.That(orbit.IsOrbitValid, Is.False);
			Assert.That(update, Does.Contain("degenerate orbit"));
			Assert.That(orbit.RelativePosition.IsNaN(), Is.False);
		});
	}
}
