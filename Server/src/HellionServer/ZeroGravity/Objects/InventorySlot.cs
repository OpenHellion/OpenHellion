using System.Collections.Generic;
using OpenHellion.State;
using ZeroGravity.Data;

namespace ZeroGravity.Objects;

public class InventorySlot
{
	public enum Type
	{
		Hands,
		General,
		Equip
	}

	public const short NoneSlotID = -1111;

	public const short HandsSlotID = -1;

	public const short StartSlotID = 1;

	public const short OutfitSlotID = -2;

	public Inventory Inventory { get; private set; }

	public Outfit Outfit { get; private set; }

	public Type SlotType { get; private set; }

	public short SlotID { get; private set; }

	public List<ItemType> ItemTypes { get; private set; }

	public bool MustBeEmptyToRemoveOutfit { get; private set; }

	public Item Item => GetParent() is { } owner && Server.Instance.SolarSystem.State.ItemInSlot(owner.Key, LocationKind.Inventory, SlotID) is { IsValid: true } item
		? Server.Instance.GetItem(Server.Instance.SolarSystem.State.Guid(item))
		: null;

	public InventorySlot(Type slotType, short slotID, List<ItemType> itemTypes, bool mustBeEmptyToRemoveOutfit, Outfit outfit, Inventory inventory)
	{
		SlotType = slotType;
		SlotID = slotID;
		MustBeEmptyToRemoveOutfit = mustBeEmptyToRemoveOutfit;
		if (itemTypes != null)
		{
			ItemTypes = new List<ItemType>(itemTypes);
		}
		Outfit = outfit;
		Inventory = inventory;
	}

	public void SetInventory(Inventory inv)
	{
		Inventory = inv;
	}

	public bool CanStoreItem(Item item)
	{
		return CanStoreItem(item.Type);
	}

	public bool CanStoreItem(ItemType itemType)
	{
		return SlotType == Type.Hands || (this == Inventory?.OutfitSlot && itemType is >= ItemType.AltairPressurisedSuit and <= (ItemType)399) || (ItemTypes?.Contains(itemType) ?? false);
	}

	public SpaceObject GetParent()
	{
		if (Inventory != null)
		{
			return Inventory.Parent;
		}
		if (Outfit != null)
		{
			return Outfit.DynamicObj;
		}
		Debug.LogError("Slot has no parent", SlotID, SlotType);
		return null;
	}

}
