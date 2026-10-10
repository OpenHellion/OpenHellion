// MovementMessage.cs
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
/// 	Message to the client that tells the game to move objects in the world.
/// 	Celestial bodies (planets and sun) are not included. Map is updated separately.
/// </summary>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class MovementMessage : NetworkData
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public struct TransformInfo
	{
		public long Guid;

		// When > 0, the this transform will be relative to this object. Anchor when 0.
		public long ParentGuid;

		public float[] Position;

		public float[] Rotation;

		public float[] Velocity;

		public float[] AngularVelocity;

		// When > 0, this object is coupled (stabilised) to another body. StabilisationOffset then
		// gives its fixed local-space offset from that body instead of an absolute Position.
		public long StabiliseToTargetGuid;

		public float[] StabilisationOffset;
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public struct OtherPlayerInfo
	{
		public long Guid;

		public long ParentGuid;

		public float[] Position;

		public float[] Rotation;

		public float FreeLookX;

		public float FreeLookY;

		public float MouseLook;

		public Dictionary<byte, RagdollItemData> RagdollData;

		public CharacterAnimationData AnimationData;

		public sbyte[] JetpackDirection;
	}

	public long AnchorGuid;

	public long ParentGuid;

	public float[] PlayerPosition;

	public float[] PlayerRotation;

	public float[] PlayerVelocity;

	public CharacterAnimationData? PlayerAnimationData;

	public double[] OriginWorldPosition;

	public long[] VisibleObjects;

	public List<TransformInfo> ArtificialBodiesMovement;

	public List<OtherPlayerInfo> OtherPlayersMovement;

	public List<TransformInfo> DynamicObjectsMovement;

	public List<TransformInfo> CorpsesMovement;
}
