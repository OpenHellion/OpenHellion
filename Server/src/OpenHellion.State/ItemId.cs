// ItemId.cs
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
	public readonly struct ItemId : IEquatable<ItemId>
	{
		private readonly int _value;

		public ItemId(int row)
		{
			_value = row + 1;
		}

		public int Row => _value - 1;

		public bool IsValid => _value > 0;

		public static ItemId None => default;

		public bool Equals(ItemId other) => _value == other._value;

		public override bool Equals(object obj) => obj is ItemId other && Equals(other);

		public override int GetHashCode() => _value;

		public static bool operator ==(ItemId a, ItemId b) => a._value == b._value;

		public static bool operator !=(ItemId a, ItemId b) => a._value != b._value;
	}
}
