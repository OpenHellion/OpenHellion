// ItemLocation.cs
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
	public readonly struct ItemLocation : IEquatable<ItemLocation>
	{
		public readonly LocationKind Kind;

		public readonly long ParentKey;

		public readonly short SlotId;

		public ItemLocation(LocationKind kind, long parentKey, short slotId)
		{
			Kind = kind;
			ParentKey = kind is LocationKind.None or LocationKind.Floating ? 0 : parentKey;
			SlotId = kind is LocationKind.Inventory or LocationKind.ItemSlot or LocationKind.AttachPoint ? slotId : (short)0;
		}

		public bool HasParent => Kind is LocationKind.Loose or LocationKind.Inventory or LocationKind.ItemSlot or LocationKind.AttachPoint;

		public bool IsSlot => Kind is LocationKind.Inventory or LocationKind.ItemSlot or LocationKind.AttachPoint;

		public bool Equals(ItemLocation other) => Kind == other.Kind && ParentKey == other.ParentKey && SlotId == other.SlotId;

		public override bool Equals(object obj) => obj is ItemLocation other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(Kind, ParentKey, SlotId);
	}
}
