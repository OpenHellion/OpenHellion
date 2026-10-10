// SolarSystemState.cs
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
using System.Collections.Generic;
using System.Numerics;

namespace OpenHellion.State
{
	/// <summary>
	/// 	The authoritative state of the solar system, stored as columns indexed by row. Every write goes through a
	/// 	function on this class, which keeps the relationship indexes consistent and records what changed.
	/// 	All writes must happen on the simulation loop; reads are free.
	/// </summary>
	public sealed class SolarSystemState
	{
		private int _itemCapacity;
		private int _itemRows;
		private int[] _freeItems = new int[16];
		private int _freeItemCount;

		private bool[] _alive;
		private long[] _guid;
		private short[] _dataId;
		private ItemLocation[] _location;
		private int[] _nextSibling;
		private int[] _prevSibling;
		private float[] _health;
		private float[] _maxHealth;
		private float[] _armor;
		private int[] _tier;
		private float[] _power;
		private float[] _maxPower;
		private int[] _bullets;
		private bool[] _lightOn;
		private bool[] _visorOn;
		private int[] _weaponMod;
		private bool[] _active;
		private bool[] _stunned;
		private bool[] _detonated;
		private Vector3[] _velocity;
		private Vector3[] _torque;
		private Vector3[] _throwForce;
		private int[] _firstResource;
		private ItemChanges[] _changes;
		private bool[] _listed;

		private ItemId[] _changed = new ItemId[64];
		private int _changedCount;

		private readonly Dictionary<long, int> _rowByGuid = new();
		private readonly Dictionary<long, int> _firstChild = new();
		private readonly Dictionary<(long Parent, LocationKind Kind, short Slot), int> _slots = new();

		private int _resourceCapacity;
		private int _resourceRows;
		private int[] _freeResources = new int[16];
		private int _freeResourceCount;
		private int[] _resourceItem;
		private short[] _resourceCompartment;
		private short[] _resourceType;
		private float[] _resourceQuantity;
		private int[] _nextResource;

		public SolarSystemState()
		{
			GrowItems(256);
			GrowResources(64);
		}

		public ReadOnlySpan<ItemId> ChangedItems => new ReadOnlySpan<ItemId>(_changed, 0, _changedCount);

		public bool IsAlive(ItemId item) => item.IsValid && item.Row < _itemRows && _alive[item.Row];

		public bool TryGetItem(long guid, out ItemId item)
		{
			if (_rowByGuid.TryGetValue(guid, out int row))
			{
				item = new ItemId(row);
				return true;
			}
			item = ItemId.None;
			return false;
		}

		public long Guid(ItemId item) => _guid[item.Row];

		public short DataId(ItemId item) => _dataId[item.Row];

		public ItemLocation Location(ItemId item) => _location[item.Row];

		public ItemId ItemInSlot(long parentKey, LocationKind kind, short slotId)
		{
			return _slots.TryGetValue((parentKey, kind, slotId), out int row) ? new ItemId(row) : ItemId.None;
		}

		public ItemId FirstChild(long parentKey)
		{
			return _firstChild.TryGetValue(parentKey, out int row) ? new ItemId(row) : ItemId.None;
		}

		public ItemId NextSibling(ItemId item)
		{
			int next = _nextSibling[item.Row];
			return next < 0 ? ItemId.None : new ItemId(next);
		}

		/// <summary>
		/// 	Appends every item below the parent to the list, parents before their contents.
		/// </summary>
		public void AddDescendants(long parentKey, List<ItemId> result)
		{
			ItemId item = FirstChild(parentKey);
			while (item.IsValid)
			{
				result.Add(item);
				ItemId child = FirstChild(_guid[item.Row]);
				if (child.IsValid)
				{
					item = child;
					continue;
				}

				while (item.IsValid)
				{
					ItemId next = NextSibling(item);
					if (next.IsValid)
					{
						item = next;
						break;
					}

					long parent = _location[item.Row].ParentKey;
					if (parent == parentKey || !TryGetItem(parent, out item))
					{
						item = ItemId.None;
					}
				}
			}
		}

		public ItemChanges Changes(ItemId item) => _changes[item.Row];

		public float Health(ItemId item) => _health[item.Row];

		public float MaxHealth(ItemId item) => _maxHealth[item.Row];

		public float Armor(ItemId item) => _armor[item.Row];

		public int Tier(ItemId item) => _tier[item.Row];

		public float Power(ItemId item) => _power[item.Row];

		public float MaxPower(ItemId item) => _maxPower[item.Row];

		public int Bullets(ItemId item) => _bullets[item.Row];

		public bool LightOn(ItemId item) => _lightOn[item.Row];

		public bool VisorOn(ItemId item) => _visorOn[item.Row];

		public int WeaponMod(ItemId item) => _weaponMod[item.Row];

		public bool Active(ItemId item) => _active[item.Row];

		public bool Stunned(ItemId item) => _stunned[item.Row];

		public bool Detonated(ItemId item) => _detonated[item.Row];

		public Vector3 Velocity(ItemId item) => _velocity[item.Row];

		public Vector3 Torque(ItemId item) => _torque[item.Row];

		public Vector3 ThrowForce(ItemId item) => _throwForce[item.Row];

		public int FirstResource(ItemId item) => _firstResource[item.Row];

		public int NextResource(int resource) => _nextResource[resource];

		public short ResourceCompartment(int resource) => _resourceCompartment[resource];

		public short ResourceType(int resource) => _resourceType[resource];

		public float ResourceQuantity(int resource) => _resourceQuantity[resource];

		public float ResourceQuantity(ItemId item, short compartment, short type)
		{
			int resource = FindResource(item.Row, compartment, type);
			return resource < 0 ? 0f : _resourceQuantity[resource];
		}

		public ItemId CreateItem(long guid, short dataId)
		{
			if (_rowByGuid.ContainsKey(guid))
			{
				throw new InvalidOperationException($"An item with guid {guid} already exists.");
			}

			int row;
			if (_freeItemCount > 0)
			{
				row = _freeItems[--_freeItemCount];
			}
			else
			{
				if (_itemRows == _itemCapacity)
				{
					GrowItems(_itemCapacity * 2);
				}
				row = _itemRows++;
			}

			_alive[row] = true;
			_guid[row] = guid;
			_dataId[row] = dataId;
			_location[row] = default;
			_nextSibling[row] = -1;
			_prevSibling[row] = -1;
			_health[row] = 0f;
			_maxHealth[row] = 0f;
			_armor[row] = 0f;
			_tier[row] = 1;
			_power[row] = 0f;
			_maxPower[row] = 0f;
			_bullets[row] = 0;
			_lightOn[row] = false;
			_visorOn[row] = false;
			_weaponMod[row] = 0;
			_active[row] = false;
			_stunned[row] = false;
			_detonated[row] = false;
			_velocity[row] = Vector3.Zero;
			_torque[row] = Vector3.Zero;
			_throwForce[row] = Vector3.Zero;
			_firstResource[row] = -1;
			_changes[row] = ItemChanges.None;
			_rowByGuid.Add(guid, row);
			return new ItemId(row);
		}

		public void DestroyItem(ItemId item)
		{
			int row = item.Row;
			if (_firstChild.ContainsKey(_guid[row]))
			{
				throw new InvalidOperationException($"Item {_guid[row]} is destroyed while it still holds items.");
			}

			Unlink(row);
			for (int resource = _firstResource[row]; resource >= 0;)
			{
				int next = _nextResource[resource];
				FreeResource(resource);
				resource = next;
			}
			_firstResource[row] = -1;
			_rowByGuid.Remove(_guid[row]);
			_alive[row] = false;
			_changes[row] = ItemChanges.None;
			if (_freeItemCount == _freeItems.Length)
			{
				Array.Resize(ref _freeItems, _freeItems.Length * 2);
			}
			_freeItems[_freeItemCount++] = row;
		}

		public bool CanMoveItem(ItemId item, ItemLocation location)
		{
			int row = item.Row;
			if (_location[row].Equals(location))
			{
				return true;
			}

			if (location.IsSlot && _slots.ContainsKey((location.ParentKey, location.Kind, location.SlotId)))
			{
				return false;
			}

			if (location.HasParent)
			{
				long ancestor = location.ParentKey;
				while (_rowByGuid.TryGetValue(ancestor, out int ancestorRow))
				{
					if (ancestorRow == row)
					{
						return false;
					}
					if (!_location[ancestorRow].HasParent)
					{
						break;
					}
					ancestor = _location[ancestorRow].ParentKey;
				}
			}
			return true;
		}

		public bool TryMoveItem(ItemId item, ItemLocation location)
		{
			int row = item.Row;
			if (_location[row].Equals(location))
			{
				return true;
			}
			if (!CanMoveItem(item, location))
			{
				return false;
			}

			Unlink(row);
			_location[row] = location;
			if (location.IsSlot)
			{
				_slots.Add((location.ParentKey, location.Kind, location.SlotId), row);
			}
			if (location.HasParent)
			{
				int head = _firstChild.TryGetValue(location.ParentKey, out int first) ? first : -1;
				_nextSibling[row] = head;
				if (head >= 0)
				{
					_prevSibling[head] = row;
				}
				_firstChild[location.ParentKey] = row;
			}
			MarkChanged(row, ItemChanges.Location);
			return true;
		}

		public void SetImpulse(ItemId item, Vector3 velocity, Vector3 torque, Vector3 throwForce)
		{
			int row = item.Row;
			_velocity[row] = velocity;
			_torque[row] = torque;
			_throwForce[row] = throwForce;
			MarkChanged(row, ItemChanges.Impulse);
		}

		public void SetHealth(ItemId item, float value)
		{
			if (_health[item.Row] != value)
			{
				_health[item.Row] = value;
				MarkChanged(item.Row, ItemChanges.Health);
			}
		}

		public void SetMaxHealth(ItemId item, float value)
		{
			if (_maxHealth[item.Row] != value)
			{
				_maxHealth[item.Row] = value;
				MarkChanged(item.Row, ItemChanges.MaxHealth);
			}
		}

		public void SetArmor(ItemId item, float value)
		{
			if (_armor[item.Row] != value)
			{
				_armor[item.Row] = value;
				MarkChanged(item.Row, ItemChanges.Armor);
			}
		}

		public void SetTier(ItemId item, int value)
		{
			if (_tier[item.Row] != value)
			{
				_tier[item.Row] = value;
				MarkChanged(item.Row, ItemChanges.Tier);
			}
		}

		public void SetPower(ItemId item, float value)
		{
			if (_power[item.Row] != value)
			{
				_power[item.Row] = value;
				MarkChanged(item.Row, ItemChanges.Power);
			}
		}

		public void SetMaxPower(ItemId item, float value)
		{
			if (_maxPower[item.Row] != value)
			{
				_maxPower[item.Row] = value;
				MarkChanged(item.Row, ItemChanges.MaxPower);
			}
		}

		public void SetBullets(ItemId item, int value)
		{
			if (_bullets[item.Row] != value)
			{
				_bullets[item.Row] = value;
				MarkChanged(item.Row, ItemChanges.Bullets);
			}
		}

		public void SetLightOn(ItemId item, bool value)
		{
			if (_lightOn[item.Row] != value)
			{
				_lightOn[item.Row] = value;
				MarkChanged(item.Row, ItemChanges.LightOn);
			}
		}

		public void SetVisorOn(ItemId item, bool value)
		{
			if (_visorOn[item.Row] != value)
			{
				_visorOn[item.Row] = value;
				MarkChanged(item.Row, ItemChanges.VisorOn);
			}
		}

		public void SetWeaponMod(ItemId item, int value)
		{
			if (_weaponMod[item.Row] != value)
			{
				_weaponMod[item.Row] = value;
				MarkChanged(item.Row, ItemChanges.WeaponMod);
			}
		}

		public void SetActive(ItemId item, bool value)
		{
			if (_active[item.Row] != value)
			{
				_active[item.Row] = value;
				MarkChanged(item.Row, ItemChanges.Active);
			}
		}

		public void SetStunned(ItemId item, bool value)
		{
			if (_stunned[item.Row] != value)
			{
				_stunned[item.Row] = value;
				MarkChanged(item.Row, ItemChanges.Stunned);
			}
		}

		public void SetDetonated(ItemId item, bool value)
		{
			if (_detonated[item.Row] != value)
			{
				_detonated[item.Row] = value;
				MarkChanged(item.Row, ItemChanges.Detonated);
			}
		}

		public void SetResource(ItemId item, short compartment, short type, float quantity)
		{
			int row = item.Row;
			int resource = FindResource(row, compartment, type);
			if (resource >= 0)
			{
				if (_resourceQuantity[resource] == quantity)
				{
					return;
				}
				_resourceQuantity[resource] = quantity;
				MarkChanged(row, ItemChanges.Resources);
				return;
			}

			if (_freeResourceCount > 0)
			{
				resource = _freeResources[--_freeResourceCount];
			}
			else
			{
				if (_resourceRows == _resourceCapacity)
				{
					GrowResources(_resourceCapacity * 2);
				}
				resource = _resourceRows++;
			}
			_resourceItem[resource] = row;
			_resourceCompartment[resource] = compartment;
			_resourceType[resource] = type;
			_resourceQuantity[resource] = quantity;
			_nextResource[resource] = _firstResource[row];
			_firstResource[row] = resource;
			MarkChanged(row, ItemChanges.Resources);
		}

		public void RemoveResource(ItemId item, short compartment, short type)
		{
			int row = item.Row;
			int previous = -1;
			for (int resource = _firstResource[row]; resource >= 0; resource = _nextResource[resource])
			{
				if (_resourceCompartment[resource] == compartment && _resourceType[resource] == type)
				{
					if (previous < 0)
					{
						_firstResource[row] = _nextResource[resource];
					}
					else
					{
						_nextResource[previous] = _nextResource[resource];
					}
					FreeResource(resource);
					MarkChanged(row, ItemChanges.Resources);
					return;
				}
				previous = resource;
			}
		}

		public void ClearChanges()
		{
			for (int i = 0; i < _changedCount; i++)
			{
				int row = _changed[i].Row;
				_changes[row] = ItemChanges.None;
				_listed[row] = false;
				_velocity[row] = Vector3.Zero;
				_torque[row] = Vector3.Zero;
				_throwForce[row] = Vector3.Zero;
			}
			_changedCount = 0;
		}

		/// <summary>
		/// 	Checks every relationship index against the columns. Returns a description of the first broken
		/// 	invariant, or null when the state is consistent.
		/// </summary>
		public string ValidateInvariants()
		{
			foreach (KeyValuePair<long, int> entry in _rowByGuid)
			{
				if (!_alive[entry.Value] || _guid[entry.Value] != entry.Key)
				{
					return $"Guid index entry {entry.Key} points to row {entry.Value}, which holds {_guid[entry.Value]}.";
				}
			}

			foreach (KeyValuePair<(long Parent, LocationKind Kind, short Slot), int> entry in _slots)
			{
				ItemLocation location = _location[entry.Value];
				if (!_alive[entry.Value] || location.ParentKey != entry.Key.Parent || location.Kind != entry.Key.Kind || location.SlotId != entry.Key.Slot)
				{
					return $"Slot {entry.Key} points to item {_guid[entry.Value]}, which is elsewhere.";
				}
			}

			foreach (KeyValuePair<long, int> entry in _firstChild)
			{
				int previous = -1;
				for (int row = entry.Value; row >= 0; row = _nextSibling[row])
				{
					if (!_alive[row] || !_location[row].HasParent || _location[row].ParentKey != entry.Key || _prevSibling[row] != previous)
					{
						return $"Child list of {entry.Key} is broken at item {_guid[row]}.";
					}
					previous = row;
				}
			}

			for (int row = 0; row < _itemRows; row++)
			{
				if (!_alive[row])
				{
					continue;
				}

				ItemLocation location = _location[row];
				if (location.IsSlot && (!_slots.TryGetValue((location.ParentKey, location.Kind, location.SlotId), out int occupant) || occupant != row))
				{
					return $"Item {_guid[row]} is in slot {location.Kind} {location.SlotId} of {location.ParentKey}, but the slot index disagrees.";
				}

				if (location.HasParent)
				{
					bool found = false;
					if (_firstChild.TryGetValue(location.ParentKey, out int child))
					{
						for (; child >= 0; child = _nextSibling[child])
						{
							if (child == row)
							{
								found = true;
								break;
							}
						}
					}
					if (!found)
					{
						return $"Item {_guid[row]} is missing from the child list of {location.ParentKey}.";
					}
				}
				else if (_nextSibling[row] >= 0 || _prevSibling[row] >= 0)
				{
					return $"Item {_guid[row]} has no parent but is linked to siblings.";
				}

				for (int resource = _firstResource[row]; resource >= 0; resource = _nextResource[resource])
				{
					if (_resourceItem[resource] != row)
					{
						return $"Resource {resource} is listed under item {_guid[row]} but belongs to row {_resourceItem[resource]}.";
					}
				}

				if (_changes[row] != ItemChanges.None && !_listed[row])
				{
					return $"Item {_guid[row]} has changes {_changes[row]} but is not in the changed list.";
				}
			}

			return null;
		}

		private void MarkChanged(int row, ItemChanges change)
		{
			_changes[row] |= change;
			if (_listed[row])
			{
				return;
			}
			_listed[row] = true;
			if (_changedCount == _changed.Length)
			{
				Array.Resize(ref _changed, _changed.Length * 2);
			}
			_changed[_changedCount++] = new ItemId(row);
		}

		private void Unlink(int row)
		{
			ItemLocation location = _location[row];
			if (location.IsSlot)
			{
				_slots.Remove((location.ParentKey, location.Kind, location.SlotId));
			}
			if (location.HasParent)
			{
				int next = _nextSibling[row];
				int previous = _prevSibling[row];
				if (previous >= 0)
				{
					_nextSibling[previous] = next;
				}
				else if (next >= 0)
				{
					_firstChild[location.ParentKey] = next;
				}
				else
				{
					_firstChild.Remove(location.ParentKey);
				}
				if (next >= 0)
				{
					_prevSibling[next] = previous;
				}
			}
			_nextSibling[row] = -1;
			_prevSibling[row] = -1;
			_location[row] = default;
		}

		private int FindResource(int row, short compartment, short type)
		{
			for (int resource = _firstResource[row]; resource >= 0; resource = _nextResource[resource])
			{
				if (_resourceCompartment[resource] == compartment && _resourceType[resource] == type)
				{
					return resource;
				}
			}
			return -1;
		}

		private void FreeResource(int resource)
		{
			_resourceItem[resource] = -1;
			_nextResource[resource] = -1;
			if (_freeResourceCount == _freeResources.Length)
			{
				Array.Resize(ref _freeResources, _freeResources.Length * 2);
			}
			_freeResources[_freeResourceCount++] = resource;
		}

		private void GrowItems(int capacity)
		{
			_itemCapacity = capacity;
			Array.Resize(ref _alive, capacity);
			Array.Resize(ref _guid, capacity);
			Array.Resize(ref _dataId, capacity);
			Array.Resize(ref _location, capacity);
			Array.Resize(ref _nextSibling, capacity);
			Array.Resize(ref _prevSibling, capacity);
			Array.Resize(ref _health, capacity);
			Array.Resize(ref _maxHealth, capacity);
			Array.Resize(ref _armor, capacity);
			Array.Resize(ref _tier, capacity);
			Array.Resize(ref _power, capacity);
			Array.Resize(ref _maxPower, capacity);
			Array.Resize(ref _bullets, capacity);
			Array.Resize(ref _lightOn, capacity);
			Array.Resize(ref _visorOn, capacity);
			Array.Resize(ref _weaponMod, capacity);
			Array.Resize(ref _active, capacity);
			Array.Resize(ref _stunned, capacity);
			Array.Resize(ref _detonated, capacity);
			Array.Resize(ref _velocity, capacity);
			Array.Resize(ref _torque, capacity);
			Array.Resize(ref _throwForce, capacity);
			Array.Resize(ref _firstResource, capacity);
			Array.Resize(ref _changes, capacity);
			Array.Resize(ref _listed, capacity);
		}

		private void GrowResources(int capacity)
		{
			_resourceCapacity = capacity;
			Array.Resize(ref _resourceItem, capacity);
			Array.Resize(ref _resourceCompartment, capacity);
			Array.Resize(ref _resourceType, capacity);
			Array.Resize(ref _resourceQuantity, capacity);
			Array.Resize(ref _nextResource, capacity);
		}
	}
}
