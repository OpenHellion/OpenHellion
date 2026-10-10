using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZeroGravity.Data;

namespace OpenHellion.IO;

public class AttachPointDataJsonConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		if (objectType == typeof(BaseAttachPointData))
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
		JObject jo = JObject.Load(reader);
		switch ((int)jo["AttachPointType"])
		{
		case 1:
			return jo.ToObject<AttachPointData>(serializer);
		case 8:
			return jo.ToObject<ActiveAttachPointData>(serializer);
		case 7:
			return jo.ToObject<ScrapAttachPointData>(serializer);
		case 2:
			return jo.ToObject<MachineryPartSlotData>(serializer);
		case 3:
			return jo.ToObject<ResourcesTransferPointData>(serializer);
		case 5:
			return jo.ToObject<ResourcesAutoTransferPointData>(serializer);
		case 4:
			return jo.ToObject<BatteryRechargePointData>(serializer);
		case 6:
			return jo.ToObject<ItemRecyclerAtachPointData>(serializer);
		}
		return null;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		serializer.Serialize(writer, value);
	}
}
