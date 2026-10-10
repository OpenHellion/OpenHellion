using System;
using ProtoBuf;

namespace ZeroGravity.Data;

[Serializable]
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ItemCompoundType : ISceneData
{
	public ItemType Type;

	public GenericItemSubType SubType;

	public MachineryPartType PartType;

	public int Tier = 1;

	public override bool Equals(object obj)
	{
		ItemCompoundType ict = obj as ItemCompoundType;
		return Type == ict.Type && SubType == ict.SubType && PartType == ict.PartType && Tier == ict.Tier;
	}

	public override int GetHashCode()
	{
		return new int[4]
		{
			(int)Type,
			(int)SubType,
			(int)PartType,
			Tier
		}.GetHashCode();
	}
}
