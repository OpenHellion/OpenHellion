namespace ZeroGravity.Data;

public class DynamicObjectData : ISceneData
{
	public short ItemID;

	public ItemType ItemType;

	public string PrefabPath;

	public string MetaGUID;

	public DynamicObjectAuxData DefaultAuxData;

	public bool DefaultBlueprint;

	public ItemCompoundType CompoundType
	{
		get
		{
			if (ItemType == ItemType.GenericItem)
			{
				ItemCompoundType itemCompoundType = new ItemCompoundType();
				itemCompoundType.Type = ItemType;
				itemCompoundType.SubType = (DefaultAuxData as GenericItemData).SubType;
				itemCompoundType.PartType = MachineryPartType.None;
				itemCompoundType.Tier = DefaultAuxData.Tier;
				return itemCompoundType;
			}
			if (ItemType == ItemType.MachineryPart)
			{
				ItemCompoundType itemCompoundType = new ItemCompoundType();
				itemCompoundType.Type = ItemType;
				itemCompoundType.SubType = GenericItemSubType.None;
				itemCompoundType.PartType = (DefaultAuxData as MachineryPartData).PartType;
				itemCompoundType.Tier = DefaultAuxData.Tier;
				return itemCompoundType;
			}
			return new ItemCompoundType
			{
				Type = ItemType,
				SubType = GenericItemSubType.None,
				PartType = MachineryPartType.None,
				Tier = DefaultAuxData.Tier
			};
		}
	}
}
