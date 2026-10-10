// MapDataResponse.cs
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

using ProtoBuf;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace OpenHellion.Net.Message;

/// <summary>
/// 	Snapshot of every object the player sees on the navigation map, in response to
/// 	a <see cref="MapDataRequest"/>.
/// </summary>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class MapDataResponse : NetworkData
{
	/// <summary>
	/// 	A single object on the navigation map.
	/// </summary>
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public struct MapDetailsData
	{
		public long Guid;

		public SpaceObjectType Type;

		public string Name;

		public string Registration;

		public long SpawnRuleId;

		public OrbitData Orbit;

		public double RadarSignature;

		public bool IsAlwaysVisible;

		public bool IsDistressSignalActive;
	}

	public MapDetailsData[] Objects;
}
