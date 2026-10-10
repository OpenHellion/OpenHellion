// SolarSystemStateTests.cs
//
// Copyright (C) 2026, OpenHellion contributors
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
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using System.Numerics;
using OpenHellion.State;

namespace HellionServer.Tests;

public class SolarSystemStateTests
{
	private const long Player = 5_000_000_000_000;
	private const long Vessel = 2_000_000;

	private static List<long> Children(SolarSystemState state, long parent)
	{
		List<long> guids = [];
		for (ItemId child = state.FirstChild(parent); child.IsValid; child = state.NextSibling(child))
		{
			guids.Add(state.Guid(child));
		}
		return guids;
	}

	[Test]
	public void MoveKeepsSlotIndexAndChildListsConsistent()
	{
		SolarSystemState state = new();
		ItemId gun = state.CreateItem(1001, 10);
		ItemId magazine = state.CreateItem(1002, 11);

		Assert.That(state.TryMoveItem(gun, new ItemLocation(LocationKind.Inventory, Player, -1)), Is.True);
		Assert.That(state.TryMoveItem(magazine, new ItemLocation(LocationKind.ItemSlot, 1001, 1)), Is.True);

		Assert.That(state.ItemInSlot(Player, LocationKind.Inventory, -1), Is.EqualTo(gun));
		Assert.That(state.ItemInSlot(1001, LocationKind.ItemSlot, 1), Is.EqualTo(magazine));
		Assert.That(Children(state, Player), Is.EqualTo(new[] { 1001L }));
		Assert.That(Children(state, 1001), Is.EqualTo(new[] { 1002L }));

		Assert.That(state.TryMoveItem(gun, new ItemLocation(LocationKind.Loose, Vessel, 0)), Is.True);

		Assert.That(state.ItemInSlot(Player, LocationKind.Inventory, -1).IsValid, Is.False);
		Assert.That(Children(state, Player), Is.Empty);
		Assert.That(Children(state, Vessel), Is.EqualTo(new[] { 1001L }));
		Assert.That(state.ValidateInvariants(), Is.Null);
	}

	[Test]
	public void DescendantsComeParentsFirst()
	{
		SolarSystemState state = new();
		ItemId outfit = state.CreateItem(1001, 10);
		ItemId helmet = state.CreateItem(1002, 11);
		ItemId battery = state.CreateItem(1003, 12);
		ItemId gun = state.CreateItem(1004, 13);
		ItemId magazine = state.CreateItem(1005, 14);
		ItemId elsewhere = state.CreateItem(1006, 15);
		state.TryMoveItem(outfit, new ItemLocation(LocationKind.Inventory, Player, -2));
		state.TryMoveItem(helmet, new ItemLocation(LocationKind.Inventory, Player, 1));
		state.TryMoveItem(battery, new ItemLocation(LocationKind.ItemSlot, 1002, 1));
		state.TryMoveItem(gun, new ItemLocation(LocationKind.Inventory, Player, -1));
		state.TryMoveItem(magazine, new ItemLocation(LocationKind.ItemSlot, 1004, 1));
		state.TryMoveItem(elsewhere, new ItemLocation(LocationKind.Loose, Vessel, 0));

		List<ItemId> descendants = [];
		state.AddDescendants(Player, descendants);
		List<long> guids = descendants.Select(state.Guid).ToList();

		Assert.That(guids, Is.EquivalentTo(new[] { 1001L, 1002L, 1003L, 1004L, 1005L }));
		Assert.That(guids.IndexOf(1002), Is.LessThan(guids.IndexOf(1003)));
		Assert.That(guids.IndexOf(1004), Is.LessThan(guids.IndexOf(1005)));

		descendants.Clear();
		state.AddDescendants(1004, descendants);
		Assert.That(descendants, Is.EqualTo(new[] { magazine }));
	}

	[Test]
	public void MoveIntoOccupiedSlotIsRejected()
	{
		SolarSystemState state = new();
		ItemId first = state.CreateItem(1001, 10);
		ItemId second = state.CreateItem(1002, 10);
		state.TryMoveItem(first, new ItemLocation(LocationKind.Inventory, Player, 3));
		state.TryMoveItem(second, new ItemLocation(LocationKind.Loose, Vessel, 0));

		Assert.That(state.TryMoveItem(second, new ItemLocation(LocationKind.Inventory, Player, 3)), Is.False);
		Assert.That(state.Location(second).Kind, Is.EqualTo(LocationKind.Loose));
		Assert.That(state.ItemInSlot(Player, LocationKind.Inventory, 3), Is.EqualTo(first));
		Assert.That(state.ValidateInvariants(), Is.Null);
	}

	[Test]
	public void MoveIntoOwnDescendantIsRejected()
	{
		SolarSystemState state = new();
		ItemId outfit = state.CreateItem(1001, 10);
		ItemId bag = state.CreateItem(1002, 11);
		state.TryMoveItem(bag, new ItemLocation(LocationKind.Inventory, 1001, 1));

		Assert.That(state.TryMoveItem(outfit, new ItemLocation(LocationKind.ItemSlot, 1002, 1)), Is.False);
		Assert.That(state.ValidateInvariants(), Is.Null);
	}

	[Test]
	public void DestroyFreesSlotAndRowIsReused()
	{
		SolarSystemState state = new();
		ItemId item = state.CreateItem(1001, 10);
		state.TryMoveItem(item, new ItemLocation(LocationKind.AttachPoint, Vessel, 7));
		state.SetResource(item, 0, 3, 50f);

		state.DestroyItem(item);

		Assert.That(state.IsAlive(item), Is.False);
		Assert.That(state.TryGetItem(1001, out _), Is.False);
		Assert.That(state.ItemInSlot(Vessel, LocationKind.AttachPoint, 7).IsValid, Is.False);
		Assert.That(Children(state, Vessel), Is.Empty);

		ItemId reused = state.CreateItem(1003, 10);
		Assert.That(reused.Row, Is.EqualTo(item.Row));
		Assert.That(state.FirstResource(reused), Is.LessThan(0));
		Assert.That(state.ValidateInvariants(), Is.Null);
	}

	[Test]
	public void DestroyWithChildrenThrows()
	{
		SolarSystemState state = new();
		ItemId gun = state.CreateItem(1001, 10);
		ItemId magazine = state.CreateItem(1002, 11);
		state.TryMoveItem(magazine, new ItemLocation(LocationKind.ItemSlot, 1001, 1));

		Assert.Throws<InvalidOperationException>(() => state.DestroyItem(gun));
	}

	[Test]
	public void WritesSetExactlyTheirChangeBitAndClearResets()
	{
		SolarSystemState state = new();
		ItemId helmet = state.CreateItem(1001, 10);
		ItemId battery = state.CreateItem(1002, 11);

		state.SetLightOn(helmet, true);
		state.SetPower(battery, 20f);
		state.SetPower(battery, 20f);
		state.SetHealth(battery, 90f);

		Assert.That(state.Changes(helmet), Is.EqualTo(ItemChanges.LightOn));
		Assert.That(state.Changes(battery), Is.EqualTo(ItemChanges.Power | ItemChanges.Health));
		Assert.That(state.ChangedItems.ToArray(), Is.EqualTo(new[] { helmet, battery }));
		Assert.That(state.ValidateInvariants(), Is.Null);

		state.ClearChanges();

		Assert.That(state.Changes(helmet), Is.EqualTo(ItemChanges.None));
		Assert.That(state.ChangedItems.Length, Is.EqualTo(0));

		state.SetLightOn(helmet, true);
		Assert.That(state.Changes(helmet), Is.EqualTo(ItemChanges.None));
	}

	[Test]
	public void ImpulseIsTransient()
	{
		SolarSystemState state = new();
		ItemId item = state.CreateItem(1001, 10);
		state.SetImpulse(item, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);

		Assert.That(state.Changes(item), Is.EqualTo(ItemChanges.Impulse));
		Assert.That(state.ThrowForce(item), Is.EqualTo(Vector3.UnitZ));

		state.ClearChanges();

		Assert.That(state.ThrowForce(item), Is.EqualTo(Vector3.Zero));
	}

	[Test]
	public void ResourcesBelongToTheirItem()
	{
		SolarSystemState state = new();
		ItemId jetpack = state.CreateItem(1001, 10);
		ItemId canister = state.CreateItem(1002, 11);

		state.SetResource(jetpack, 1, 4, 10f);
		state.SetResource(jetpack, 2, 5, 20f);
		state.SetResource(canister, 0, 4, 30f);
		state.SetResource(jetpack, 1, 4, 15f);

		Assert.That(state.ResourceQuantity(jetpack, 1, 4), Is.EqualTo(15f));
		Assert.That(state.ResourceQuantity(jetpack, 2, 5), Is.EqualTo(20f));
		Assert.That(state.ResourceQuantity(canister, 0, 4), Is.EqualTo(30f));

		state.RemoveResource(jetpack, 1, 4);

		Assert.That(state.ResourceQuantity(jetpack, 1, 4), Is.EqualTo(0f));
		int count = 0;
		for (int resource = state.FirstResource(jetpack); resource >= 0; resource = state.NextResource(resource))
		{
			count++;
		}
		Assert.That(count, Is.EqualTo(1));
		Assert.That(state.Changes(jetpack), Is.EqualTo(ItemChanges.Resources));
		Assert.That(state.ValidateInvariants(), Is.Null);
	}

	[Test]
	public void StateStaysConsistentAfterMixedOperations()
	{
		SolarSystemState state = new();
		Random random = new(1234);
		List<ItemId> items = [];
		for (int i = 0; i < 300; i++)
		{
			items.Add(state.CreateItem(10_000 + i, 1));
		}

		for (int step = 0; step < 5000; step++)
		{
			ItemId item = items[random.Next(items.Count)];
			if (!state.IsAlive(item))
			{
				continue;
			}
			switch (random.Next(5))
			{
				case 0:
					state.TryMoveItem(item, new ItemLocation(LocationKind.Inventory, Player + random.Next(3), (short)random.Next(-2, 6)));
					break;
				case 1:
					state.TryMoveItem(item, new ItemLocation(LocationKind.ItemSlot, 10_000 + random.Next(items.Count), (short)random.Next(3)));
					break;
				case 2:
					state.TryMoveItem(item, new ItemLocation(LocationKind.Loose, Vessel, 0));
					break;
				case 3:
					state.TryMoveItem(item, new ItemLocation(LocationKind.Floating, 0, 0));
					break;
				case 4:
					if (!state.FirstChild(state.Guid(item)).IsValid)
					{
						state.DestroyItem(item);
					}
					break;
			}
			if (step % 500 == 0)
			{
				state.ClearChanges();
			}
		}

		Assert.That(state.ValidateInvariants(), Is.Null);
	}
}
