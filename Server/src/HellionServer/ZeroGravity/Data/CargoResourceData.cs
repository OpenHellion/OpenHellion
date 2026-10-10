using System;
using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Data;

[Serializable]
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class CargoResourceData : ISceneData
{
	public ResourceType ResourceType;

	public float Quantity;

	public ResourcesSpawnSettings[] SpawnSettings;

	public static Dictionary<ResourceType, float> ListToDictionary(List<CargoResourceData> list)
	{
		Dictionary<ResourceType, float> dict = new Dictionary<ResourceType, float>();
		if (list != null)
		{
			foreach (CargoResourceData crd in list)
			{
				dict[crd.ResourceType] = crd.Quantity;
			}
		}
		return dict;
	}

	public static List<CargoResourceData> DictionaryToList(Dictionary<ResourceType, float> dictionary)
	{
		List<CargoResourceData> list = new List<CargoResourceData>();
		if (dictionary != null)
		{
			foreach (KeyValuePair<ResourceType, float> kv in dictionary)
			{
				list.Add(new CargoResourceData
				{
					ResourceType = kv.Key,
					Quantity = kv.Value
				});
			}
		}
		return list;
	}
}
