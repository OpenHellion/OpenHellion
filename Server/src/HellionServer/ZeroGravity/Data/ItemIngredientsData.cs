using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Data;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ItemIngredientsData : ISceneData
{
	public ItemType Type;

	public GenericItemSubType SubType;

	public MachineryPartType PartType;

	public string Name;

	public Dictionary<int, ItemIngredientsTierData> IngredientsTiers;
}
