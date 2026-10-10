using System.Collections.Generic;
using System.Linq;
using OpenHellion.State;

namespace ZeroGravity.Objects;

public class Inventory
{
	private Player parentPlayer;

	private Corpse parentCorpse;

	public Outfit CurrOutfit { get; private set; }

	public InventorySlot HandsSlot { get; private set; }

	public InventorySlot OutfitSlot { get; private set; }

	public SpaceObject Parent => (SpaceObject)parentPlayer ?? parentCorpse;

	public Inventory()
	{
		HandsSlot = new InventorySlot(InventorySlot.Type.Hands, -1, null, mustBeEmptyToRemoveOutfit: true, null, this);
		OutfitSlot = new InventorySlot(InventorySlot.Type.Equip, -2, null, mustBeEmptyToRemoveOutfit: false, null, this);
	}

	public Inventory(Player pl)
	{
		parentPlayer = pl;
		parentCorpse = null;
		HandsSlot = new InventorySlot(InventorySlot.Type.Hands, -1, null, mustBeEmptyToRemoveOutfit: true, null, this);
		OutfitSlot = new InventorySlot(InventorySlot.Type.Equip, -2, null, mustBeEmptyToRemoveOutfit: false, null, this);
	}

	public T Equipped<T>() where T : Item
	{
		return CurrOutfit?.InventorySlots.Values.Where(m => m.SlotType == InventorySlot.Type.Equip).Select(m => m.Item).OfType<T>().FirstOrDefault();
	}

	public InventorySlot GetSlot(short slotId)
	{
		return slotId switch
		{
			InventorySlot.HandsSlotID => HandsSlot,
			InventorySlot.OutfitSlotID => OutfitSlot,
			_ => CurrOutfit?.InventorySlots.GetValueOrDefault(slotId)
		};
	}

	public void ChangeParent(Corpse corpse)
	{
		List<(Item Item, short Slot)> carried = [(HandsSlot.Item, HandsSlot.SlotID), (OutfitSlot.Item, OutfitSlot.SlotID)];
		carried.AddRange(CurrOutfit?.InventorySlots.Values.Select(m => (m.Item, m.SlotID)) ?? []);
		parentPlayer = null;
		parentCorpse = corpse;
		foreach ((Item item, short slot) in carried.Where(m => m.Item != null))
		{
			item.DynamicObj.MoveTo(new ItemLocation(LocationKind.Inventory, corpse.Key, slot));
		}
	}

	public Item GetHandsItemIfType<T>()
	{
		if (HandsSlot.Item != null && typeof(T).IsAssignableFrom(HandsSlot.Item.GetType()))
		{
			return HandsSlot.Item;
		}
		return null;
	}

	private bool EquipOutfit(Outfit outfit)
	{
		if (CurrOutfit != null)
		{
			return false;
		}
		List<(Item Item, short Slot)> contents = [.. outfit.InventorySlots.Values.Where(m => m.Item != null).Select(m => (m.Item, m.SlotID))];
		if (!outfit.DynamicObj.MoveTo(new ItemLocation(LocationKind.Inventory, Parent.Key, InventorySlot.OutfitSlotID)))
		{
			return false;
		}
		CurrOutfit = outfit;
		foreach (InventorySlot slot in outfit.InventorySlots.Values)
		{
			slot.SetInventory(this);
		}
		foreach ((Item item, short slot) in contents)
		{
			item.DynamicObj.MoveTo(new ItemLocation(LocationKind.Inventory, Parent.Key, slot));
		}
		if (parentPlayer != null)
		{
			outfit.ExternalTemperature = parentPlayer.AmbientTemperature.HasValue ? parentPlayer.AmbientTemperature.Value : parentPlayer.CoreTemperature;
			outfit.InternalTemperature = parentPlayer.CoreTemperature;
		}
		return true;
	}

	/// <summary>
	/// 	Called while the worn outfit is leaving its slot: its contents go back into it.
	/// </summary>
	internal void ReleaseOutfit()
	{
		Outfit outfit = CurrOutfit;
		List<(Item Item, short Slot)> contents = [.. outfit.InventorySlots.Values.Where(m => m.Item != null).Select(m => (m.Item, m.SlotID))];
		CurrOutfit = null;
		foreach (InventorySlot slot in outfit.InventorySlots.Values)
		{
			slot.SetInventory(null);
		}
		foreach ((Item item, short slot) in contents)
		{
			item.DynamicObj.MoveTo(new ItemLocation(LocationKind.Inventory, outfit.GUID, slot));
		}
	}

	public bool AddItemToInventory(Item item, short slotID)
	{
		if (item is Outfit outfit && slotID == InventorySlot.OutfitSlotID)
		{
			return EquipOutfit(outfit);
		}
		InventorySlot newSlot = slotID == InventorySlot.HandsSlotID ? HandsSlot : CurrOutfit?.InventorySlots.GetValueOrDefault(slotID);
		if (newSlot == null || !newSlot.CanStoreItem(item))
		{
			Debug.LogWarning("AddItemToInventory refused", item.GUID, item.Type, "requestedSlot", slotID,
				"slotFound", newSlot != null, "canStore", newSlot != null && newSlot.CanStoreItem(item),
				"hasOutfit", CurrOutfit != null,
				"outfitSlotIds", CurrOutfit == null ? "none" : string.Join(",", CurrOutfit.InventorySlots.Keys));
			return false;
		}

		ItemLocation destination = new(LocationKind.Inventory, Parent.Key, slotID);
		Item displaced = newSlot.Item;
		if (displaced == item)
		{
			return true;
		}
		if (displaced != null)
		{
			ItemLocation origin = Server.Instance.SolarSystem.State.Location(item.DynamicObj.Row);
			ItemLocation refuge = origin.Kind switch
			{
				LocationKind.ItemSlot when origin.ParentKey != displaced.GUID && item.DynamicObj.Parent is DynamicObject { Item.Slots: { } slots } && slots.TryGetValue(origin.SlotId, out ItemSlot containerSlot) && containerSlot.CanFitItem(displaced) => origin,
				LocationKind.Inventory when item.Slot is { SlotID: not InventorySlot.OutfitSlotID } originSlot && originSlot.CanStoreItem(displaced) => origin,
				LocationKind.Inventory when CurrOutfit?.InventorySlots.Values.FirstOrDefault(m => m.Item == null && m.CanStoreItem(displaced)) is { } freeSlot => new ItemLocation(LocationKind.Inventory, Parent.Key, freeSlot.SlotID),
				_ => default
			};
			if (refuge.Kind == LocationKind.None)
			{
				return false;
			}
			item.DynamicObj.MoveTo(default);
			displaced.DynamicObj.MoveTo(refuge);
		}
		return item.DynamicObj.MoveTo(destination);
	}
}
