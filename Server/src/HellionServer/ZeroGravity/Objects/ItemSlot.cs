using System.Collections.Generic;
using OpenHellion.State;
using ZeroGravity.Data;

namespace ZeroGravity.Objects;

public class ItemSlot : IItemSlot
{
	public short ID;

	public List<ItemType> ItemTypes;

	public List<GenericItemSubType> GenericSubTypes;

	public List<MachineryPartType> MachineryPartTypes;

	public Item Item => Parent is DynamicObject container && Server.Instance.SolarSystem.State.ItemInSlot(container.Guid, LocationKind.ItemSlot, ID) is { IsValid: true } item
		? Server.Instance.GetItem(Server.Instance.SolarSystem.State.Guid(item))
		: null;

	public SpaceObject Parent { get; set; }

	public ItemSlot(ItemSlotData data)
	{
		ID = data.ID;
		ItemTypes = data.ItemTypes;
		GenericSubTypes = data.GenericSubTypes;
		MachineryPartTypes = data.MachineryPartTypes;
	}

	public bool FitItem(Item item)
	{
		return Parent is DynamicObject container && CanFitItem(item) && item.DynamicObj.MoveTo(new ItemLocation(LocationKind.ItemSlot, container.Guid, ID));
	}

	public bool CanFitItem(Item item)
	{
		return CanFitItem(item.Type, item is GenericItem genericItem ? genericItem.SubType : GenericItemSubType.None, item is MachineryPart part ? part.PartType : MachineryPartType.None);
	}

	public bool CanFitItem(ItemType itemType, GenericItemSubType subType, MachineryPartType partType)
	{
		return itemType switch
		{
			ItemType.GenericItem => GenericSubTypes.Contains(subType), 
			ItemType.MachineryPart => MachineryPartTypes.Contains(partType), 
			_ => ItemTypes.Contains(itemType), 
		};
	}

	public ItemSlotData GetData()
	{
		return new ItemSlotData
		{
			ID = ID,
			ItemTypes = ItemTypes,
			GenericSubTypes = GenericSubTypes,
			MachineryPartTypes = MachineryPartTypes
		};
	}
}
