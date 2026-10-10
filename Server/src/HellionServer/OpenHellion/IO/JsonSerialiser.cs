using System.IO;
using Newtonsoft.Json;

namespace OpenHellion.IO;

public static class JsonSerialiser
{
	public enum Formatting
	{
		None,
		Indented
	}

	private static readonly AuxDataJsonConverter AuxDataJsonConverter = new AuxDataJsonConverter();

	private static readonly AttachPointDataJsonConverter AttachPointDataJsonConverter = new AttachPointDataJsonConverter();

	private static readonly PersistenceJsonConverter PersistenceJsonConverter = new PersistenceJsonConverter();

	public static string Serialize(object obj, Formatting format = Formatting.Indented)
	{
		return JsonConvert.SerializeObject(obj, (Newtonsoft.Json.Formatting)format);
	}

	public static void SerializeToFile(object obj, string filePath, Formatting format = Formatting.Indented, JsonSerializerSettings settings = null)
	{
		File.WriteAllText(filePath, JsonConvert.SerializeObject(obj, (Newtonsoft.Json.Formatting)format, settings));
	}

	public static T Deserialize<T>(string jsonString)
	{
		return JsonConvert.DeserializeObject<T>(jsonString, AuxDataJsonConverter, AttachPointDataJsonConverter, PersistenceJsonConverter);
	}

	public static T Load<T>(string filePath)
	{
		return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath), AuxDataJsonConverter, AttachPointDataJsonConverter, PersistenceJsonConverter);
	}
}
