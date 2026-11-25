// RadarDetectionTests.cs
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

using OpenHellion.State;

namespace HellionServer.Tests;

public class RadarDetectionTests
{
	private static readonly double[] VanillaDamageMultipliers = [4.0, 3.897, 3.621, 3.222, 2.749, 2.251, 1.778, 1.379, 1.103, 1.0];

	[Test]
	public void DamageRaisesHullSignatureLikeVanilla()
	{
		Assert.Multiple(() =>
		{
			for (int step = 0; step < VanillaDamageMultipliers.Length; step++)
			{
				double healthFraction = step / (VanillaDamageMultipliers.Length - 1.0);
				Assert.That(RadarDetection.GetHullSignature(1.0, healthFraction, isPowered: false, isRock: false),
					Is.EqualTo(VanillaDamageMultipliers[step]).Within(0.01), $"health {healthFraction:F2}");
			}
		});
	}

	[Test]
	public void CecaHearsPoweredCecaAtVanillaDistance()
	{
		const double cecaPassiveRangePerSquareMetre = 4140.0;
		const double vanillaCecaPassivePerSignaturePoint = 9000.0;
		const double vanillaPoweredCecaSignature = 385.0;
		double poweredCecaSignature = RadarDetection.GetHullSignature(340.0, 1.0, isPowered: true, isRock: false);

		double range = RadarDetection.GetPassiveRange(cecaPassiveRangePerSquareMetre, poweredCecaSignature, 1.0);

		Assert.That(range, Is.EqualTo(vanillaCecaPassivePerSignaturePoint * vanillaPoweredCecaSignature).Within(25).Percent);
	}

	[Test]
	public void HalvingTheConeDoublesActiveRange()
	{
		double wide = RadarDetection.GetActiveRange(1000.0, 60.0, 500.0, 1.0);
		double narrow = RadarDetection.GetActiveRange(1000.0, 30.0, 500.0, 1.0);

		Assert.That(narrow, Is.EqualTo(wide * 2.0).Within(1e-6));
	}

	[Test]
	public void RadiationOnlyEverWeakensSensitivity()
	{
		Assert.Multiple(() =>
		{
			Assert.That(RadarDetection.GetSensitivityMultiplier(1.0, 0.5, 1.0, 1.0), Is.EqualTo(1.0));
			Assert.That(RadarDetection.GetSensitivityMultiplier(1.0, 50.0, 1.0, 1.0), Is.LessThan(1.0));
		});
	}
}
