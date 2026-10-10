using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenHellion.IO;
using ZeroGravity.Math;
using ZeroGravity.Objects;
using ZeroGravity.ShipComponents;
using ZeroGravity.Spawn;

namespace ZeroGravity.Data;

public class StationBlueprint
{
	public class Structure
	{
		public int StructureID;

		public string StructureType;

		public List<DockingPort> DockingPorts;

		public bool? SystemsOnline;

		public bool? Invulnerable;

		public bool? DoorsLocked;

		public string Tag;

		public float? HealthMultiplier;

		public bool? DockingControlsDisabled;

		public float? AirPressure;

		public float? AirQuality;

		public bool? SecurityPanelsLocked;

		public GameScenes.SceneId SceneID => Server.NameGenerator.GetSceneId(StructureType);
	}

	public class DockingPort
	{
		public string PortName;

		public int OrderID;

		public int? DockedStructureID;

		public string DockedPortName;

		public bool Locked;
	}

	private static string configDir;

	public string Version;

	public string Name;

	public string LinkURI;

	public List<Structure> Structures;

	public double[] LocalPosition;

	public double[] LocalRotation;

	public double[] LocalAngularVelocity;

	public bool Invulnerable = true;

	public bool? SystemsOnline;

	public bool? DoorsLocked;

	public float? HealthMultiplier;

	public bool? DockingControlsDisabled;

	public bool? AutoStabilizationDisabled;

	public float? AirPressure;

	public float? AirQuality;

	public bool? MatchVelocity;

	public bool? SecurityPanelsLocked;

	static StationBlueprint()
	{
		if (Server.ConfigDir.IsNullOrEmpty() || !Directory.Exists(Server.ConfigDir + "Data"))
		{
			configDir = "";
		}
		else
		{
			configDir = Server.ConfigDir;
		}
	}

	public async Task<SpaceObjectVessel> AssembleStation(string name, string tag, SpawnRuleOrbit spawnRuleOrbit, long? nearArtificialBodyGUID, float? AsteroidResourcesMultiplier)
	{
		Dictionary<Structure, GameScenes.SceneId> structures = new Dictionary<Structure, GameScenes.SceneId>();
		foreach (Structure str2 in Structures)
		{
			GameScenes.SceneId sid = Server.NameGenerator.GetSceneId(str2.StructureType);
			if (sid == GameScenes.SceneId.None)
			{
				return null;
			}
			structures[str2] = sid;
		}
		SpaceObjectVessel mainVessel = null;
		Dictionary<Structure, SpaceObjectVessel> spawnedVessels = new Dictionary<Structure, SpaceObjectVessel>();
		foreach (KeyValuePair<Structure, GameScenes.SceneId> str in structures)
		{
			if (!spawnedVessels.TryGetValue(str.Key, out var vessel))
			{
				string vesselTag2 = tag != "" && str.Key.Tag != "" ? tag + ";" + str.Key.Tag : tag + str.Key.Tag;
				vessel = mainVessel != null ? await SpaceObjectVessel.CreateNew(str.Value, "", -1L, localRotation: MathHelper.RandomRotation(), vesselTag: vesselTag2, nearArtificialBodyGUIDs: new List<long> { mainVessel.Guid }, celestialBodyGUIDs: null, positionOffset: null, velocityAtPosition: null, checkPosition: false, AsteroidResourcesMultiplier: AsteroidResourcesMultiplier) : await SpaceObjectVessel.CreateNew(str.Value, "", -1L, localRotation: MathHelper.RandomRotation(), vesselTag: vesselTag2, spawnRuleOrbit: spawnRuleOrbit, nearArtificialBodyGUIDs: spawnRuleOrbit == null ? new List<long> { nearArtificialBodyGUID.Value } : null, celestialBodyGUIDs: null, positionOffset: null, velocityAtPosition: null, checkPosition: spawnRuleOrbit == null, AsteroidResourcesMultiplier: AsteroidResourcesMultiplier);
				vessel.VesselRegistration = !name.IsNullOrEmpty() ? name : Server.NameGenerator.GenerateStationRegistration();
				spawnedVessels[str.Key] = vessel;
			}
			else
			{
				vessel = spawnedVessels[str.Key];
			}
			if (mainVessel == null)
			{
				mainVessel = vessel;
			}
			if (str.Key.DockingPorts == null)
			{
				continue;
			}
			foreach (DockingPort dp in str.Key.DockingPorts.Where((DockingPort m) => m.DockedStructureID.HasValue))
			{
				KeyValuePair<Structure, GameScenes.SceneId> otherStr = structures.FirstOrDefault((KeyValuePair<Structure, GameScenes.SceneId> m) => m.Key.StructureID == dp.DockedStructureID);
				if (otherStr.Key.DockingPorts != null)
				{
					string vesselTag = tag != "" && otherStr.Key.Tag != "" ? tag + ";" + otherStr.Key.Tag : tag + otherStr.Key.Tag;
					if (!spawnedVessels.TryGetValue(otherStr.Key, out var otherVessel))
					{
						otherVessel = await SpaceObjectVessel.CreateNew(otherStr.Value, "", -1L, localRotation: MathHelper.RandomRotation(), vesselTag: vesselTag, nearArtificialBodyGUIDs: new List<long> { mainVessel.Guid }, celestialBodyGUIDs: null, positionOffset: null, velocityAtPosition: null, checkPosition: false, AsteroidResourcesMultiplier: AsteroidResourcesMultiplier);
						spawnedVessels[otherStr.Key] = otherVessel;
					}
					else
					{
						otherVessel = spawnedVessels[otherStr.Key];
					}
					otherVessel.VesselRegistration = !name.IsNullOrEmpty() ? name : Server.NameGenerator.GenerateStationRegistration();

					VesselDockingPort shipPort = vessel.DockingPorts.First(m => m.OrderID == dp.OrderID);
					DockingPort dp2 = otherStr.Key.DockingPorts.First((DockingPort m) => m.DockedStructureID == str.Key.StructureID);
					VesselDockingPort otherPort = otherVessel.DockingPorts.First(m => m.OrderID == dp2.OrderID);

					if (shipPort == null || otherPort == null)
					{
						throw new Exception($"Docking port not found. Station name: {name}, StructureType: {str.Key.StructureType}, StructureID: {str.Key.StructureID}, OrderID: {dp.OrderID}");
					}
					if (!shipPort.DockingStatus && !otherPort.DockingStatus)
					{
						await otherVessel.DockToVessel(otherPort, shipPort, vessel, disableStabilization: true, spawnRuleOrbit == null, buildingStation: true);
					}
				}
			}
		}
		foreach (KeyValuePair<Structure, SpaceObjectVessel> kv in spawnedVessels)
		{
			await SetStates(kv.Key, kv.Value);
		}
		if (spawnRuleOrbit != null)
		{
			mainVessel.Orbit = spawnRuleOrbit.GenerateRandomOrbit();
		}
		else if (nearArtificialBodyGUID.HasValue && LocalPosition is { Length: 3 })
		{
			ArtificialBody ab = Server.Instance.GetSpaceObject(nearArtificialBodyGUID.Value) as ArtificialBody;
			mainVessel.Orbit = new OrbitParameters();
			mainVessel.Orbit.CopyDataFrom(ab.Orbit, Server.SolarSystemTime, exactCopy: true);
			mainVessel.Orbit.RelativePosition += ab.Rotation * LocalPosition.ToVector3D();
			mainVessel.Orbit.InitFromCurrentStateVectors(Server.SolarSystemTime);
		}
		mainVessel.Orbit.UpdateOrbit();
		if (LocalAngularVelocity is { Length: 3 })
		{
			mainVessel.AngularVelocity = LocalAngularVelocity.ToVector3D();
		}
		return mainVessel;
	}

	private async Task SetStates(Structure structure, SpaceObjectVessel vessel)
	{
		vessel.IsInvulnerable = structure.Invulnerable.HasValue ? structure.Invulnerable.Value : Invulnerable;
		if (structure.HealthMultiplier.HasValue)
		{
			await vessel.SetHealthAsync(vessel.Health * structure.HealthMultiplier.Value);
		}
		else if (HealthMultiplier.HasValue)
		{
			await vessel.SetHealthAsync(vessel.Health * HealthMultiplier.Value);
		}
		if (structure.DockingControlsDisabled.HasValue)
		{
			vessel.DockingControlsDisabled = structure.DockingControlsDisabled.Value;
		}
		else if (DockingControlsDisabled.HasValue)
		{
			vessel.DockingControlsDisabled = DockingControlsDisabled.Value;
		}
		if (structure.SecurityPanelsLocked.HasValue)
		{
			vessel.SecurityPanelsLocked = structure.SecurityPanelsLocked.Value;
		}
		else if (SecurityPanelsLocked.HasValue)
		{
			vessel.SecurityPanelsLocked = SecurityPanelsLocked.Value;
		}
		if (AutoStabilizationDisabled.HasValue)
		{
			vessel.AutoStabilizationDisabled = AutoStabilizationDisabled.Value;
		}
		foreach (DistributionManager.CompoundRoom compRoom in vessel.Rooms.Select((Room m) => m.CompoundRoom).Distinct())
		{
			if (structure.AirPressure.HasValue)
			{
				compRoom.AirPressure = MathHelper.Clamp(structure.AirPressure.Value, 0f, 1f);
			}
			else if (AirPressure.HasValue)
			{
				compRoom.AirPressure = MathHelper.Clamp(AirPressure.Value, 0f, 1f);
			}
			if (structure.AirQuality.HasValue)
			{
				compRoom.AirQuality = MathHelper.Clamp(structure.AirQuality.Value, 0f, 1f);
			}
			else if (AirQuality.HasValue)
			{
				compRoom.AirQuality = MathHelper.Clamp(AirQuality.Value, 0f, 1f);
			}
		}
		if (vessel.DistributionManager != null)
		{
			foreach (VesselComponent vc in from m in vessel.DistributionManager.GetVesselComponents()
				where m.CanBlueprintForceState
				select m)
			{
				if ((structure.SystemsOnline.HasValue && structure.SystemsOnline.Value) || (SystemsOnline.HasValue && SystemsOnline.Value))
				{
					await vc.GoOnLine();
				}
				else if ((structure.SystemsOnline.HasValue && !structure.SystemsOnline.Value) || (SystemsOnline.HasValue && !SystemsOnline.Value))
				{
					await vc.GoOffLine(autoRestart: false);
				}
			}
		}
		foreach (Door door in vessel.Doors)
		{
			if ((structure.DoorsLocked.HasValue && structure.DoorsLocked.Value) || (DoorsLocked.HasValue && DoorsLocked.Value))
			{
				door.IsLocked = true;
			}
			else if ((structure.DoorsLocked.HasValue && structure.DoorsLocked.Value) || (DoorsLocked.HasValue && DoorsLocked.Value))
			{
				door.IsLocked = false;
			}
		}
		if (vessel.DockingPorts != null)
		{
			foreach (VesselDockingPort vdp in vessel.DockingPorts)
			{
				DockingPort dp = structure.DockingPorts.FirstOrDefault((DockingPort m) => m.OrderID == vdp.OrderID);
				if (dp != null)
				{
					vdp.Locked = dp.Locked;
				}
			}
		}
	}

	public static async Task<List<SpaceObjectVessel>> AssembleStation(string blueprintName, string name, string tag, SpawnRuleOrbit spawnRuleOrbit, long? nearArtificialBodyGUID, float? AsteroidResourcesMultiplier = 1f)
	{
		List<SpaceObjectVessel> list = [];
		string fileName = configDir + "Data/Stations/" + blueprintName + ".json";
		try
		{
			Queue<StationBlueprint> stationBlueprints = new(JsonSerialiser.Load<StationBlueprint[]>(fileName));

			var mainVesselBlueprint = stationBlueprints.Dequeue();
			SpaceObjectVessel mainVessel = await mainVesselBlueprint.AssembleStation(name.IsNullOrEmpty() ? mainVesselBlueprint.Name : name, tag, spawnRuleOrbit, nearArtificialBodyGUID, AsteroidResourcesMultiplier);
			mainVessel.UpdateVesselData();
			list.Add(mainVessel);

			if (stationBlueprints.Count != 0)
			{
				foreach (StationBlueprint blueprint in stationBlueprints)
				{
					SpaceObjectVessel vessel = await blueprint.AssembleStation(blueprint.Name, tag, null, mainVessel.Guid, AsteroidResourcesMultiplier);
					if (blueprint.MatchVelocity.HasValue && blueprint.MatchVelocity.Value)
					{
						vessel.StabilizeToTarget(mainVessel, forceStabilize: true);
					}
					vessel.UpdateVesselData();
					list.Add(vessel);
				}
			}

			return list;
		}
		catch (JsonSerializationException)
		{
			Debug.LogWarningFormat("Invalid JSON in bluprint {0}", fileName);
		}
		return null;
	}
}
