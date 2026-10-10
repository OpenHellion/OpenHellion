// CharacterAnimationMessage.cs
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

using System.Collections.Generic;
using ProtoBuf;
using ZeroGravity.Network;

namespace OpenHellion.Net.Message;

/// <summary>
/// 	The player's cosmetic presentation sent to other clients
/// 	via <see cref="MovementMessage.OtherPlayerInfo"/>.
/// </summary>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class CharacterAnimationMessage : NetworkData
{
	public long Guid;

	public CharacterAnimationData AnimationData;

	public float FreeLookX;

	public float FreeLookY;

	public float MouseLook;

	public sbyte[] JetpackDirection;

	public Dictionary<byte, RagdollItemData> RagdollData;
}
