using System.Collections.Generic;
using OpenHellion.State;
using ZeroGravity.Data;

namespace ZeroGravity.Objects;

public class VesselAttachPoint : IItemSlot
{
	public SpaceObjectVessel Vessel;

	public int InSceneID;

	public AttachPointType Type;

	public List<ItemType> ItemTypes;

	public List<GenericItemSubType> GenericSubTypes;

	public List<MachineryPartType> MachineryPartTypes;

	public Item Item => Vessel != null && Server.Instance.SolarSystem.State.ItemInSlot(Vessel.Guid, LocationKind.AttachPoint, (short)InSceneID) is { IsValid: true } item
		? Server.Instance.GetItem(Server.Instance.SolarSystem.State.Guid(item))
		: null;

	public SpaceObject Parent => Vessel;

	public bool CanSpawnItems => Type is AttachPointType.Simple or AttachPointType.Active or AttachPointType.MachineryPartSlot or AttachPointType.Scrap;

	public bool CanFitItem(Item item)
	{
		return CanFitItem(item.Type, item is GenericItem genericItem ? genericItem.SubType : GenericItemSubType.None, item is MachineryPart part ? part.PartType : MachineryPartType.None);
	}

	public bool CanFitItem(ItemType itemType, GenericItemSubType subType, MachineryPartType partType)
	{
		if (Type == AttachPointType.ItemRecycler && ItemTypes.Count == 0 && GenericSubTypes.Count == 0 && MachineryPartTypes.Count == 0)
		{
			return true;
		}
		return itemType switch
		{
			ItemType.GenericItem => GenericSubTypes.Contains(subType), 
			ItemType.MachineryPart => MachineryPartTypes.Contains(partType), 
			_ => ItemTypes.Contains(itemType), 
		};
	}
}
