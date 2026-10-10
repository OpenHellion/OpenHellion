// ShipThrustStateMessage.cs
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
/// 	Authoritative state of the vessel a player is piloting, sent only to that player and at a
/// 	much higher rate than <see cref="MovementMessage"/>.
/// </summary>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ShipThrustStateMessage : NetworkData
{
	public long VesselGuid;

	public uint LastProcessedInputSequence;

	public double[] WorldPosition;

	public float[] Velocity;

	public float[] Rotation;

	public float[] AngularVelocity;
}
