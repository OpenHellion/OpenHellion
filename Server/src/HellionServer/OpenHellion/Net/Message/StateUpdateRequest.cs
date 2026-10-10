// StateUpdateRequest.cs
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

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class StateUpdateRequest : NetworkData
{
	public CommandType Type;

	public long Subject;

	public long Target;

	public short Slot;

	public float[] Position;

	public float[] Rotation;

	public float[] Velocity;

	public float[] Torque;

	public float[] ThrowForce;

	public bool Value;

	public int Number;

	public enum CommandType : byte
	{
		None,
		MoveToInventory,
		MoveToItemSlot,
		MoveToAttachPoint,
		Drop,
		Relocate,
		SetHelmetLight,
		SetHelmetVisor,
		SetWeaponMod,
		SetGrenadeActive,
		SetRepairToolActive,
		UseMedpack,
		UseHackingTool,
		UseCanister
	}
}
