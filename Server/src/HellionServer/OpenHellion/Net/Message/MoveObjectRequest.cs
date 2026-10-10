// MoveObjectRequest.cs
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
/// 	A request from the client asking the server to move an object.
/// </summary>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class MoveObjectRequest : NetworkData
{
	public long Guid;

	public long ParentGuid;

	public float[] Position;

	public float[] Rotation;

	public float[] Velocity;

	public float[] AngularVelocity;

	// When > 0, the object is velocity-coupled to this body (the player "sticking" to a vessel
	// in zero-G).
	public long StabiliseToTargetGuid;

	// TODO probably needs a more permanent solution.
	// Debris particles do not slow the player down, so the server's velocity-delta impact path cannot detect them.
	public bool HitDebrisField;
}
