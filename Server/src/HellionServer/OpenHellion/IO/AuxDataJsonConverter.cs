using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZeroGravity.Data;

namespace OpenHellion.IO;

public class AuxDataJsonConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		if (objectType == typeof(DynamicObjectAuxData) || objectType == typeof(SystemAuxData))
		{
			return true;
		}
		return false;
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
		{
			return null;
		}
		if (objectType == typeof(DynamicObjectAuxData))
		{
			JObject jo = JObject.Load(reader);
			return DynamicObjectAuxData.GetJsonData(jo, serializer);
		}
		if (objectType == typeof(SystemAuxData))
		{
			JObject jo2 = JObject.Load(reader);
			return SystemAuxData.GetJsonData(jo2, serializer);
		}
		return null;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		serializer.Serialize(writer, value);
	}
}
