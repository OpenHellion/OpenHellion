// ScanForObjectsRequest.cs
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

namespace OpenHellion.Net.Message;

/// <summary>
/// 	Tells the server the player scanned for vessels from their ship's radar.
///
/// 	When <see cref="ScanDirection"/> is set the server performs a directional active scan using that
/// 	cone (the radar's longer-range sensitivity); when it is null the scan is passive.
/// </summary>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ScanForObjectsRequest : NetworkData
{
	/// <summary>
	/// Forward direction of the active-scan cone, or null for a passive scan.
	/// </summary>
	public float[] ScanDirection;

	/// <summary>
	/// Full opening angle of the active-scan cone, in degrees.
	/// </summary>
	public float ScanAngle;
}
