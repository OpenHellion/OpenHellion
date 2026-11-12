// MapItemData.cs
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

using ZeroGravity.Math;
using ZeroGravity.Objects;
using ZeroGravity;
using OpenHellion.Net.Message;

namespace OpenHellion.Map
{
	/// <summary>
	/// 	Client-side map model for a single vessel or asteroid, built from a <see cref="KnownVesselsMessage.KnownVessel"/>
	/// 	snapshot sent by the server.
	/// </summary>
	public class MapItemData : IMapMainObject
	{
		public long Guid { get; private set; }

		public SpaceObjectType Type { get; private set; }

		public string Name { get; private set; }

		public string Registration { get; private set; }

		public long SpawnRuleId { get; private set; }

		public double CrossSection { get; private set; }

		public bool IsAlwaysVisible { get; private set; }

		public bool IsDistressSignalActive { get; private set; }

		public OrbitParameters Orbit { get; private set; }

		public CelestialBody ParentCelesitalBody => Orbit?.Parent?.CelestialBody;

		public double Radius => 30.0;

		public Vector3D Position => Orbit.Position;

		public string Description => string.IsNullOrEmpty(Registration) ? Type.ToString() : Registration;

		public RadarVisibilityType RadarVisibilityType
		{
			get
			{
				if (IsAlwaysVisible)
				{
					return RadarVisibilityType.AlwaysVisible;
				}

				return IsDistressSignalActive ? RadarVisibilityType.Distress : RadarVisibilityType.Visible;
			}
		}

		/// <summary>
		/// 	Refreshes this item from a fresh server snapshot, reusing the existing orbit so it keeps
		/// 	ticking and the map object keeps its identity between refreshes.
		/// </summary>
		public void UpdateFrom(World world, KnownVesselsMessage.KnownVessel data)
		{
			Guid = data.Guid;
			Type = data.Type;
			Name = data.Name;
			Registration = data.Registration;
			SpawnRuleId = data.SpawnRuleId;
			CrossSection = data.CrossSection;
			IsAlwaysVisible = data.IsAlwaysVisible;
			IsDistressSignalActive = data.IsDistressSignalActive;
			Orbit ??= new OrbitParameters();
			Orbit.ParseNetworkData(world, data.Orbit);
		}
	}
}
