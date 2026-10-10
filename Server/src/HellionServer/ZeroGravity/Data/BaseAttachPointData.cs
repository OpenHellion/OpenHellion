using System.Collections.Generic;

namespace ZeroGravity.Data;

public abstract class BaseAttachPointData : ISceneData
{
	public int InSceneID;

	public List<ItemType> ItemTypes;

	public List<GenericItemSubType> GenericSubTypes;

	public List<MachineryPartType> MachineryPartTypes;

	public abstract AttachPointType AttachPointType { get; }
}
