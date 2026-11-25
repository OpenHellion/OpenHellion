// RadarDetection.cs
//
// Copyright (C) 2026, OpenHellion contributors
//
// SPDX-License-Identifier: GPL-3.0-or-later
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
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;

namespace OpenHellion.State
{
	/// <summary>
	/// 	How far radars detect vessels, shared by the server (which decides) and the client (which displays ranges).
	/// 	Signatures are in square metres; ranges in metres. Constants are calibrated against vanilla Hellion's balance.
	/// </summary>
	public static class RadarDetection
	{
		public const double PoweredHullSignatureMultiplier = 1.9;

		public const double FullyDamagedHullSignatureMultiplier = 4.0;

		public const double RockReflectivity = 0.32;

		public const double MachineSignatureSquareMetresPerSquareRootKilowatt = 5.65;

		public const double ActiveRangeMetresPerSquareMetrePerKilowattAtOneDegree = 414.0;

		public const double MinimumActiveConeAngleDegrees = 1.0;

		public const double FuzzyRangeMultiplier = 3.0;

		public const double WarpDetectionRangeMultiplier = 15.0;

		public const double RadiationSensitivityExponent = 0.14;

		public static double GetHullSignature(double radarCrossSection, double healthFraction, bool isPowered, bool isRock)
		{
			double health = Math.Clamp(healthFraction, 0.0, 1.0);
			double smoothedHealth = health * health * (3.0 - 2.0 * health);
			double damageMultiplier = FullyDamagedHullSignatureMultiplier - (FullyDamagedHullSignatureMultiplier - 1.0) * smoothedHealth;
			return radarCrossSection * damageMultiplier * (isPowered ? PoweredHullSignatureMultiplier : 1.0) * (isRock ? RockReflectivity : 1.0);
		}

		public static double GetMachineSignature(double kilowatts)
		{
			return kilowatts > 0.0 ? MachineSignatureSquareMetresPerSquareRootKilowatt * Math.Sqrt(kilowatts) : 0.0;
		}

		public static double GetSensitivityMultiplier(double signalAmplification, double radiation, double planetZoneSensitivity, double debrisFieldSensitivity)
		{
			double radiationSensitivity = radiation > 1.0 ? Math.Pow(radiation, -RadiationSensitivityExponent) : 1.0;
			return signalAmplification * radiationSensitivity * planetZoneSensitivity * debrisFieldSensitivity;
		}

		public static double GetPassiveRange(double passiveRangePerSquareMetre, double signature, double sensitivityMultiplier)
		{
			return passiveRangePerSquareMetre * signature * sensitivityMultiplier;
		}

		public static double GetWarpDetectionRange(double passiveRangePerSquareMetre, double signature, double sensitivityMultiplier)
		{
			return GetPassiveRange(passiveRangePerSquareMetre, signature, sensitivityMultiplier) * WarpDetectionRangeMultiplier;
		}

		public static double GetActiveRange(double radarKilowatts, double coneAngleDegrees, double signature, double sensitivityMultiplier)
		{
			return ActiveRangeMetresPerSquareMetrePerKilowattAtOneDegree * radarKilowatts * signature * sensitivityMultiplier
				/ Math.Max(coneAngleDegrees, MinimumActiveConeAngleDegrees);
		}

		public static double GetFuzzyRange(double radarKilowatts, double coneAngleDegrees, double signature, double sensitivityMultiplier)
		{
			return GetActiveRange(radarKilowatts, coneAngleDegrees, signature, sensitivityMultiplier) * FuzzyRangeMultiplier;
		}
	}
}
