using System.Collections.Generic;
using ZeroGravity.Data;

namespace ZeroGravity.Spawn;

public class LootItemData
{
	public class CargoResourceData
	{
		public List<ResourceType> Resources;

		public SpawnRange<float> Quantity;
	}

	public ItemType Type;

	public GenericItemSubType GenericSubType;

	public MachineryPartType PartType;

	public SpawnRange<float>? Health;

	public float? Armor;

	public List<string> Look;

	public SpawnRange<float>? Power;

	public SpawnRange<int>? Count;

	public bool? IsActive;

	public List<CargoResourceData> Cargo;

	public int Tier;

	public SpawnSerialization.AttachPointPriority AttachPointPriority;

	public int GetSubType()
	{
		if (Type == ItemType.GenericItem)
		{
			return (int)GenericSubType;
		}
		if (Type == ItemType.MachineryPart)
		{
			return (int)PartType;
		}
		return 0;
	}
}
