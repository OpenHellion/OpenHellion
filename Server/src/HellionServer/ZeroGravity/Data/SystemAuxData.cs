using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZeroGravity.Data;

public abstract class SystemAuxData : ISceneData
{
	public abstract SystemAuxDataType AuxDataType { get; }

	public static object GetJsonData(JObject jo, JsonSerializer serializer)
	{
		SystemAuxDataType type = (SystemAuxDataType)(int)jo["AuxDataType"];
		return type switch
		{
			SystemAuxDataType.AirDevice => jo.ToObject<SubSystemAirDevicesAuxData>(serializer), 
			SystemAuxDataType.ScrubberDevice => jo.ToObject<SubSystemScrubberDeviceAuxData>(serializer), 
			SystemAuxDataType.ACDevice => jo.ToObject<SubSystemACDeviceAuxData>(serializer), 
			SystemAuxDataType.RCS => jo.ToObject<SubSystemRCSAuxData>(serializer), 
			SystemAuxDataType.Engine => jo.ToObject<SubSystemEngineAuxData>(serializer), 
			SystemAuxDataType.FTL => jo.ToObject<SubSystemFTLAuxData>(serializer), 
			SystemAuxDataType.Capacitor => jo.ToObject<GeneratorCapacitorAuxData>(serializer), 
			SystemAuxDataType.PowerGenerator => jo.ToObject<GeneratorPowerAuxData>(serializer), 
			SystemAuxDataType.Refinery => jo.ToObject<SubSystemRefineryAuxData>(serializer), 
			SystemAuxDataType.Solar => jo.ToObject<GeneratorSolarAuxData>(serializer), 
			SystemAuxDataType.ScrubbedAirGenerator => jo.ToObject<GeneratorScrubbedAirAuxData>(serializer), 
			SystemAuxDataType.Fabricator => jo.ToObject<SubSystemFabricatorAuxData>(serializer), 
			SystemAuxDataType.VesselBaseSystem => jo.ToObject<VesselBaseSystemAuxData>(serializer), 
			SystemAuxDataType.AirTank => jo.ToObject<AirTankAuxData>(serializer), 
			SystemAuxDataType.Radar => jo.ToObject<RadarAuxData>(serializer), 
			_ => throw new Exception("Json deserializer was not implemented for item type " + type), 
		};
	}
}
