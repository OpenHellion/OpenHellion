using System.Collections.Generic;

namespace ZeroGravity.Data;

public static class GameScenes
{
	public enum SceneId
	{
		SolarSystemSetup = -3,
		ItemScene = -2,
		None = -1,
		Slavica = 1,
		AltCorp_Ship_Tamara = 2,
		AltCorp_CorridorModule = 3,
		AltCorp_CorridorIntersectionModule = 4,
		AltCorp_Corridor45TurnModule = 5,
		AltCorp_Shuttle_SARA = 6,
		ALtCorp_PowerSupply_Module = 7,
		AltCorp_LifeSupportModule = 8,
		AltCorp_Cargo_Module = 9,
		AltCorp_CorridorVertical = 10,
		AltCorp_Command_Module = 11,
		AltCorp_Corridor45TurnRightModule = 12,
		AltCorp_StartingModule = 13,
		AltCorp_AirLock = 14,
		Generic_Debris_JuncRoom001 = 15,
		Generic_Debris_JuncRoom002 = 16,
		Generic_Debris_Corridor001 = 17,
		Generic_Debris_Corridor002 = 18,
		AltCorp_DockableContainer = 19,
		Generic_Debris_Outpost001 = 21,
		AltCorp_CrewQuarters_Module = 22,
		Generic_Debris_Spawn1 = 23,
		Generic_Debris_Spawn2 = 24,
		Generic_Debris_Spawn3 = 25,
		AltCorp_SolarPowerModule = 26,
		AltCorp_Shuttle_CECA = 27,
		AltCorp_FabricatorModule = 28,
		AltCorp_PatchModule = 30,
		SOE_Location002 = 31,
		AltCorp_Secure_Module = 32,
		AltCorp_Destroyed_Shuttle_CECA = 33,
		AltCorp_CorridorIntersection_MKII = 34,
		AlrCorp_Corridor_MKII = 35,
		Asteroid01 = 1000,
		Asteroid02 = 1001,
		Asteroid03 = 1002,
		Asteroid04 = 1003,
		Asteroid05 = 1004,
		Asteroid06 = 1005,
		Asteroid07 = 1006,
		Asteroid08 = 1007
	}

	public static class Ranges
	{
		public const int DebrisFrom = 15;

		public const int DebrisTo = 18;

		public const int AsteroidsFrom = 1000;

		public const int AsteroidsTo = 1007;

		public const int CelestialBodiesFrom = 1;

		public const int CelestialBodiesTo = 19;

		public const int DeathMatchArenaFrom = 3;

		public const int DeathMatchArenaTo = 6;

		public const int RefiningStationsFrom = 6;

		public const int RefiningStationsTo = 9;

		public static bool IsDerelict(SceneId sceneId)
		{
			return sceneId is SceneId.Generic_Debris_JuncRoom001 or SceneId.Generic_Debris_JuncRoom002 or SceneId.Generic_Debris_Corridor001 or SceneId.Generic_Debris_Corridor002;
		}

		public static bool IsShip(SceneId sceneId)
		{
			return sceneId is SceneId.AltCorp_Shuttle_SARA or SceneId.AltCorp_Ship_Tamara or SceneId.Slavica or SceneId.AltCorp_Shuttle_CECA;
		}

		public static bool IsAsteroid(SceneId sceneId)
		{
			return sceneId >= SceneId.Asteroid01;
		}

		public static bool IsModule(SceneId sceneId)
		{
			return !IsShip(sceneId) && !IsDerelict(sceneId) && !IsAsteroid(sceneId);
		}

		public static bool IsVessel(SceneId sceneId)
		{
			return IsShip(sceneId) || IsModule(sceneId);
		}
	}

	public static Dictionary<string, SceneId> Scenes = new Dictionary<string, SceneId>
	{
		{
			"Assets/Scene/SolarSystemSetup.unity",
			SceneId.SolarSystemSetup
		},
		{
			"Assets/Scene/ItemScene.unity",
			SceneId.ItemScene
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/AltCorp_CorridorModule/AltCorp_CorridorModule.unity",
			SceneId.AltCorp_CorridorModule
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/AltCorp_CorridorIntersectionModule/AltCorp_CorridorIntersectionModule.unity",
			SceneId.AltCorp_CorridorIntersectionModule
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/Altcorp_Corridor45TurnModule/AltCorp_Corridor45TurnModule.unity",
			SceneId.AltCorp_Corridor45TurnModule
		},
		{
			"Assets/Scene/Environment/Ship/AltCorp_Shuttle_SARA/AltCorp_Shuttle_SARA.unity",
			SceneId.AltCorp_Shuttle_SARA
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/ALtCorp_PowerSupply_Module/ALtCorp_PowerSupply_Module.unity",
			SceneId.ALtCorp_PowerSupply_Module
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/AltCorp_LifeSupportModule/AltCorp_LifeSupportModule.unity",
			SceneId.AltCorp_LifeSupportModule
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/AltCorp_Cargo_Module/AltCorp_Cargo_Module.unity",
			SceneId.AltCorp_Cargo_Module
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/AltCorp_CorridorVertical/AltCorp_CorridorVertical.unity",
			SceneId.AltCorp_CorridorVertical
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/AltCorp_Command_Module/AltCorp_Command_Module.unity",
			SceneId.AltCorp_Command_Module
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/AltCorp_Corridor45TurnRightModule/AltCorp_Corridor45TurnRightModule.unity",
			SceneId.AltCorp_Corridor45TurnRightModule
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/AltCorp_StartingModule/AltCorp_StartingModule.unity",
			SceneId.AltCorp_StartingModule
		},
		{
			"Assets/Scene/Environment/Station/Module/Generic/Generic_Debris_Module_JuncRoom001/Generic_Debris_Module_JuncRoom001.unity",
			SceneId.Generic_Debris_JuncRoom001
		},
		{
			"Assets/Scene/Environment/Station/Module/Generic/Generic_Debris_Module_JuncRoom002/Generic_Debris_Module_JuncRoom002.unity",
			SceneId.Generic_Debris_JuncRoom002
		},
		{
			"Assets/Scene/Environment/Station/Module/Generic/Generic_Debris_Module_Corridor001/Generic_Debris_Module_Corridor001.unity",
			SceneId.Generic_Debris_Corridor001
		},
		{
			"Assets/Scene/Environment/Station/Module/Generic/Generic_Debris_Module_Corridor002/Generic_Debris_Module_Corridor002.unity",
			SceneId.Generic_Debris_Corridor002
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/AltCorp_Airlock_Module/AltCorp_Airlock_Module.unity",
			SceneId.AltCorp_AirLock
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/AltCorp_DockableContainer/AltCorp_DockableContainer.unity",
			SceneId.AltCorp_DockableContainer
		},
		{
			"Assets/Scene/Environment/Station/Module/Generic/Generic_Debris_Outpost001/Generic_Debris_Outpost001.unity",
			SceneId.Generic_Debris_Outpost001
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/AltCorp_CrewQuarters_Module/AltCorp_CrewQuarters_Module.unity",
			SceneId.AltCorp_CrewQuarters_Module
		},
		{
			"Assets/Scene/Environment/Station/Module/Generic/Generic_Debris_Spawn1/Generic_Debris_Spawn1.unity",
			SceneId.Generic_Debris_Spawn1
		},
		{
			"Assets/Scene/Environment/Station/Module/Generic/Generic_Debris_Spawn2/Generic_Debris_Spawn2.unity",
			SceneId.Generic_Debris_Spawn2
		},
		{
			"Assets/Scene/Environment/Station/Module/Generic/Generic_Debris_Spawn3/Generic_Debris_Spawn3.unity",
			SceneId.Generic_Debris_Spawn3
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/AltCorp_SolarPowerModule/AltCorp_SolarPowerModule.unity",
			SceneId.AltCorp_SolarPowerModule
		},
		{
			"Assets/Scene/Environment/Ship/AltCorp_Shuttle_CECA/AltCorp_Shuttle_CECA.unity",
			SceneId.AltCorp_Shuttle_CECA
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/AltCorp_FabricatorModule/AltCorp_FabricatorModule.unity",
			SceneId.AltCorp_FabricatorModule
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp/AltCorp_PatchModule/AltCorp_PatchModule.unity",
			SceneId.AltCorp_PatchModule
		},
		{
			"Assets/Scene/Environment/Other/SOE/SOE_Location002/SOE_Location002.unity",
			SceneId.SOE_Location002
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp_V02/AltCorp_Secure_Module/AltCorp_Secure_Module.unity",
			SceneId.AltCorp_Secure_Module
		},
		{
			"Assets/Scene/Environment/Ship/AltCorp_Shuttle_CECA/AltCorp_Destroyed_Shuttle_CECA.unity",
			SceneId.AltCorp_Destroyed_Shuttle_CECA
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp_V02/AltCorp_CorridorIntersection_Module/AltCorp_CorridorIntersection_MKII.unity",
			SceneId.AltCorp_CorridorIntersection_MKII
		},
		{
			"Assets/Scene/Environment/Station/Module/AltCorp_V02/AltCorp_Corridor_Module/AlrCorp_Corridor_MKII.unity",
			SceneId.AlrCorp_Corridor_MKII
		},
		{
			"Assets/Scene/Environment/SolarSystem/Asteroids/Asteroid01/Asteroid01.unity",
			SceneId.Asteroid01
		},
		{
			"Assets/Scene/Environment/SolarSystem/Asteroids/Asteroid02/Asteroid02.unity",
			SceneId.Asteroid02
		},
		{
			"Assets/Scene/Environment/SolarSystem/Asteroids/Asteroid03/Asteroid03.unity",
			SceneId.Asteroid03
		},
		{
			"Assets/Scene/Environment/SolarSystem/Asteroids/Asteroid04/Asteroid04.unity",
			SceneId.Asteroid04
		},
		{
			"Assets/Scene/Environment/SolarSystem/Asteroids/Asteroid05/Asteroid05.unity",
			SceneId.Asteroid05
		},
		{
			"Assets/Scene/Environment/SolarSystem/Asteroids/Asteroid06/Asteroid06.unity",
			SceneId.Asteroid06
		},
		{
			"Assets/Scene/Environment/SolarSystem/Asteroids/Asteroid07/Asteroid07.unity",
			SceneId.Asteroid07
		},
		{
			"Assets/Scene/Environment/SolarSystem/Asteroids/Asteroid08/Asteroid08.unity",
			SceneId.Asteroid08
		}
	};

	private static Dictionary<SceneId, string> shortVesselClassNames = new Dictionary<SceneId, string>
	{
		{
			SceneId.AltCorp_LifeSupportModule,
			"LSM"
		},
		{
			SceneId.ALtCorp_PowerSupply_Module,
			"PSM"
		},
		{
			SceneId.AltCorp_AirLock,
			"AM"
		},
		{
			SceneId.AltCorp_Cargo_Module,
			"CBM"
		},
		{
			SceneId.AltCorp_Command_Module,
			"CM"
		},
		{
			SceneId.AltCorp_DockableContainer,
			"IC"
		},
		{
			SceneId.AltCorp_CorridorIntersectionModule,
			"CTM"
		},
		{
			SceneId.AltCorp_Corridor45TurnModule,
			"CLM"
		},
		{
			SceneId.AltCorp_Corridor45TurnRightModule,
			"CRM"
		},
		{
			SceneId.AltCorp_CorridorVertical,
			"CSM"
		},
		{
			SceneId.AltCorp_CorridorModule,
			"CIM"
		},
		{
			SceneId.AltCorp_StartingModule,
			"OUTP"
		},
		{
			SceneId.AltCorp_Shuttle_SARA,
			"ARG"
		},
		{
			SceneId.AltCorp_CrewQuarters_Module,
			"CQM"
		},
		{
			SceneId.AltCorp_SolarPowerModule,
			"SPM"
		},
		{
			SceneId.AltCorp_FabricatorModule,
			"FM"
		},
		{
			SceneId.AltCorp_Shuttle_CECA,
			"STE"
		},
		{
			SceneId.AltCorp_PatchModule,
			"PM"
		},
		{
			SceneId.AltCorp_Secure_Module,
			"SCM"
		},
		{
			SceneId.AltCorp_CorridorIntersection_MKII,
			"CTM2"
		},
		{
			SceneId.AlrCorp_Corridor_MKII,
			"CIM2"
		}
	};

	public static string GetShortVesselClassName(SceneId sceneID)
	{
		try
		{
			return shortVesselClassNames[sceneID];
		}
		catch
		{
			return "";
		}
	}
}
