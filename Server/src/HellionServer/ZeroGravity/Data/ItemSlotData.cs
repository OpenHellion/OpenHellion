using System.Collections.Generic;

namespace ZeroGravity.Data;

public class ItemSlotData : ISceneData
{
	public short ID;

	public List<ItemType> ItemTypes;

	public List<GenericItemSubType> GenericSubTypes;

	public List<MachineryPartType> MachineryPartTypes;

	public ItemCompoundType SpawnItem;
}
