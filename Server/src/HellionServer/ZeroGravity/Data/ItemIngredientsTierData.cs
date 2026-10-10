using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Data;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ItemIngredientsTierData : ISceneData
{
	public Dictionary<ResourceType, float> Recycle;

	public Dictionary<ResourceType, float> Craft;
}
