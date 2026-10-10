// QuaternionDTests.cs
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

using ZeroGravity.Math;

namespace HellionServer.Tests;

public class QuaternionDTests
{
	[Test]
	public void EulerComposesYThenXThenZ()
	{
		var euler = QuaternionD.Euler(10.0, 20.0, 30.0);
		var composed = QuaternionD.AngleAxis(20.0, Vector3D.Up) * QuaternionD.AngleAxis(10.0, Vector3D.Right) * QuaternionD.AngleAxis(30.0, Vector3D.Forward);

		Assert.That(System.Math.Abs(QuaternionD.Dot(euler, composed)), Is.EqualTo(1.0).Within(1e-12));
	}

	[Test]
	public void EulerAnglesRoundTripsMultiAxis()
	{
		var angles = QuaternionD.Euler(10.0, 20.0, 30.0).EulerAngles;

		Assert.Multiple(() =>
		{
			Assert.That(angles.X, Is.EqualTo(10.0).Within(1e-9));
			Assert.That(angles.Y, Is.EqualTo(20.0).Within(1e-9));
			Assert.That(angles.Z, Is.EqualTo(30.0).Within(1e-9));
		});
	}

	[Test]
	public void ToAngleAxisReturnsUnitAxis()
	{
		var expectedAxis = new Vector3D(1.0, 2.0, 3.0).Normalized;

		QuaternionD.AngleAxis(60.0, expectedAxis).ToAngleAxis(out double angle, out Vector3D axis);

		Assert.Multiple(() =>
		{
			Assert.That(angle, Is.EqualTo(60.0).Within(1e-9));
			Assert.That(axis.X, Is.EqualTo(expectedAxis.X).Within(1e-12));
			Assert.That(axis.Y, Is.EqualTo(expectedAxis.Y).Within(1e-12));
			Assert.That(axis.Z, Is.EqualTo(expectedAxis.Z).Within(1e-12));
		});
	}
}
