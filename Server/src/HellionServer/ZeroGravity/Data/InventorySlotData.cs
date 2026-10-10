using System.Collections.Generic;
using ZeroGravity.Objects;

namespace ZeroGravity.Data;

public class InventorySlotData
{
	public short SlotID;

	public InventorySlot.Type SlotType;

	public List<ItemType> ItemTypes;

	public bool MustBeEmptyToRemoveOutfit;
}
