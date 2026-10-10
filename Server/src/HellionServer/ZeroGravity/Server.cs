using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BulletSharp;
using OpenHellion;
using OpenHellion.IO;
using OpenHellion.Net;
using OpenHellion.Net.Message;
using OpenHellion.Social;
using OpenHellion.Social.Message;
using OpenHellion.State;
using ZeroGravity.BulletPhysics;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;
using ZeroGravity.ShipComponents;
using ZeroGravity.Spawn;

namespace ZeroGravity;

public sealed class Server
{
	public class DynamicObjectsRespawn
	{
		public DynamicObjectSceneData Data;

		public SpaceObject Parent;

		public AttachPointDetails ApDetails;

		public double Timer;

		public double RespawnTime;

		public float MaxHealth = -1f;

		public float MinHealth = -1f;
	}

	public class SpawnPointInviteData
	{
		public ShipSpawnPoint SpawnPoint;

		public double InviteTimer = 300.0;
	}

	public static class NameGenerator
	{
		private static DateTime _lastClearDate = DateTime.UtcNow;

		private static readonly Dictionary<GameScenes.SceneId, int> DailySpawnCount = new Dictionary<GameScenes.SceneId, int>();

		private static readonly List<char> MonthCodes = new List<char>
		{
			'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
			'K', 'L'
		};

		private static readonly Dictionary<GameScenes.SceneId, string> ShipNaming = new Dictionary<GameScenes.SceneId, string>
		{
			{
				GameScenes.SceneId.AltCorp_LifeSupportModule,
				"LSM-AC:HE3/"
			},
			{
				GameScenes.SceneId.ALtCorp_PowerSupply_Module,
				"PSM-AC:HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_AirLock,
				"AM-AC:HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_Cargo_Module,
				"CBM-AC:HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_Command_Module,
				"CM-AC:HE3/"
			},
			{
				GameScenes.SceneId.AltCorp_DockableContainer,
				"IC-AC:HE2/"
			},
			{
				GameScenes.SceneId.AltCorp_CorridorIntersectionModule,
				"CTM-AC:HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_Corridor45TurnModule,
				"CLM-AC:HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_Corridor45TurnRightModule,
				"CRM-AC:HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_CorridorVertical,
				"CSM-AC:HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_CorridorModule,
				"CIM-AC:HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_StartingModule,
				"OUTPOST "
			},
			{
				GameScenes.SceneId.AltCorp_Shuttle_SARA,
				"AC-ARG HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_CrewQuarters_Module,
				"CQM-AC:HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_SolarPowerModule,
				"SPM-AC:HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_FabricatorModule,
				"FM-AC:HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_Shuttle_CECA,
				"Steropes:HE1/"
			},
			{
				GameScenes.SceneId.Generic_Debris_Spawn1,
				"Small Debris:HE1/"
			},
			{
				GameScenes.SceneId.Generic_Debris_Spawn2,
				"Large Debris:HE1/"
			},
			{
				GameScenes.SceneId.Generic_Debris_Spawn3,
				"Medium Debris:HE1/"
			},
			{
				GameScenes.SceneId.Generic_Debris_Outpost001,
				"Doomed outpost:HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_PatchModule,
				"Bulkhead:HE1/"
			},
			{
				GameScenes.SceneId.SOE_Location002,
				"SOE Derelict:HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_Secure_Module,
				"CM-SDS:HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_Destroyed_Shuttle_CECA,
				"Steropes:HE1/"
			},
			{
				GameScenes.SceneId.AltCorp_CorridorIntersection_MKII,
				"CTM-SDS:HE1/"
			},
			{
				GameScenes.SceneId.AlrCorp_Corridor_MKII,
				"CIM-SDS:HE1/"
			}
		};

		private static readonly List<GameScenes.SceneId> Derelicts = new List<GameScenes.SceneId>
		{
			GameScenes.SceneId.Generic_Debris_Corridor001,
			GameScenes.SceneId.Generic_Debris_Corridor002,
			GameScenes.SceneId.Generic_Debris_JuncRoom001,
			GameScenes.SceneId.Generic_Debris_JuncRoom002
		};

		public static string GenerateObjectRegistration(SpaceObjectType type, CelestialBody parentCelestialBody, GameScenes.SceneId sceneId)
		{
			string name = "";
			string dailySpawnDigits = "000";
			if (type == SpaceObjectType.Ship && !Derelicts.Contains(sceneId))
			{
				if (ShipNaming.TryGetValue(sceneId, out var value))
				{
					name += value;
				}
				else
				{
					Debug.LogWarning("No name tag for ship", sceneId);
					name = name + type + MathHelper.RandomNextInt().ToString("X");
				}
			}
			else
			{
				string parentBody;
				if (parentCelestialBody != null)
				{
					string plnt = ((CelestialBodyGUID)parentCelestialBody.GUID).ToString();
					parentBody = plnt.Substring(0, System.Math.Min(3, plnt.Length)).ToUpper();
				}
				else
				{
					parentBody = "HEL";
				}
				switch (type)
				{
				case SpaceObjectType.Ship:
					name = name + "DERELICT " + parentBody + "-";
					dailySpawnDigits = "00";
					break;
				case SpaceObjectType.Asteroid:
					name = name + "ASTEROID " + parentBody + "-";
					break;
				}
			}
			if ((DateTime.UtcNow - _lastClearDate).TotalDays > 1.0)
			{
				_lastClearDate = DateTime.UtcNow;
				DailySpawnCount.Clear();
			}
			if (DailySpawnCount.ContainsKey(sceneId))
			{
				DailySpawnCount[sceneId]++;
			}
			else
			{
				DailySpawnCount.Add(sceneId, 1);
			}
			return name + DateTime.UtcNow.Day.ToString("00") + MonthCodes[DateTime.UtcNow.Month - 1] + DailySpawnCount[sceneId].ToString(dailySpawnDigits);
		}

		public static string GenerateStationRegistration()
		{
			return "STATION";
		}

		public static GameScenes.SceneId GetSceneId(string text)
		{
			foreach (GameScenes.SceneId scene in Enum.GetValues(typeof(GameScenes.SceneId)))
			{
				if (scene.ToString().StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
				{
					return scene;
				}
			}
			foreach (KeyValuePair<GameScenes.SceneId, string> kv2 in ShipNaming)
			{
				if (kv2.Value.StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
				{
					return kv2.Key;
				}
			}
			foreach (KeyValuePair<GameScenes.SceneId, string> kv in ShipNaming)
			{
				if (kv.Value.Contains(text, StringComparison.CurrentCultureIgnoreCase))
				{
					return kv.Key;
				}
			}
			return GameScenes.SceneId.None;
		}
	}

	public static Properties Properties;

	public static string ConfigDir = "";

	public static double PersistenceSaveInterval = 900.0;

	private static bool _cleanStart;

	public static bool Restart;

	public static bool CleanRestart = false;

	public const double CelestialBodyDeathDistance = 10000.0;

	public SpatialCollection<SpaceObject> SpaceObjects { get; private set; } = new(100000);

	private readonly ConcurrentDictionary<long, Player> _players = new();

	private readonly ConcurrentBag<Player> _playersToAdd = [];

	private readonly ConcurrentBag<Player> _playersToRemove = [];

	private readonly ConcurrentQueue<string> _charactersToDelete = new();

	private readonly ConcurrentDictionary<long, SpaceObjectVessel> _vessels = new();

	private readonly ConcurrentDictionary<long, DynamicObject> _updateableDynamicObjects = new();

	// Artificial body optimisation
	private readonly ConcurrentDictionary<long, ArtificialBody> _artificialBodies = new();

	private ArtificialBody[] _artificialBodiesCache = [];

	private volatile bool _artificialBodiesChanged = true;

	private readonly ConcurrentDictionary<UpdateTimer.TimerStep, UpdateTimer> _timers = new();

	public readonly SolarSystem SolarSystem;

	public static bool IsRunning;

	private long _numberOfTicks = 64L;

	public static int GamePort = 6004;

	public static bool OfflineMode;

	public static string ServerName = "Hellion Server";

	public static string ServerDescription = "";

	public static string ServerPassword = "";

	public static int StatusPort = 6005;

	public static bool SavePersistenceDataOnShutdown;

	public bool WorldInitialized;

	private readonly DateTime _serverStartTime = DateTime.UtcNow;

	private float _solarSystemStartTime = -1f;

	public static int MaxNumberOfSaveFiles = 10;

	private IpAddressRange[] _adminIpAddressRanges = [];

	private readonly Dictionary<string, SpawnPointInviteData> _spawnPointInvites = [];

	public static readonly AutoResetEvent MainLoopEnded = new AutoResetEvent(initialState: false);

	private bool _mainLoopStarted;

	public readonly List<DebrisField> DebrisFields = new List<DebrisField>();

	public static double VesselDecayRateMultiplier = 1.0;

	public static double VesselDecayGracePeriod;

	public static double VesselExplosionRadiusMultiplier = 1.0;

	public static double VesselExplosionDamageMultiplier = 1.0;

	public static double VesselCollisionDamageMultiplier = 1.0;

	public static double SelfDestructExplosionRadiusMultiplier = 1.0;

	public static double SelfDestructExplosionDamageMultiplier = 1.0;

	public static double DebrisVesselExplosionRadiusMultiplier = 1.0;

	public static double DebrisVesselExplosionDamageMultiplier = 1.0;

	public static double DebrisVesselCollisionDamageMultiplier = 1.0;

	public static double ActivateRepairPointChanceMultiplier = 1.0;

	public static float StartingSetDespawnTimeSec = 900f;

	public static int JunkItemsCleanupScope = 1;

	public static double JunkItemsTimeToLive = 3600.0;

	public static double JunkItemsCleanupInterval = 900.0;

	public static bool CanWarpThroughCelestialBodies;

	public static double MaxAngularVelocity = 300.0;

	private static List<string> _serverAdmins = new List<string>();

	public static double ServerRestartTimeSec = -1.0;

	public static DateTime RestartTime;

	private double _timeToRestart = 1800.0;

	public readonly List<DeathMatchArenaController> DeathMatchArenaControllers = new List<DeathMatchArenaController>();

	public readonly DoomedShipController DoomedShipController = new DoomedShipController();

	private const double MovementMessageSendInterval = 0.1;

	private const double PilotStateSendInterval = 1.0 / 30.0;

	private const double ShipSystemSweepSeconds = 1.0;

	private static double _pilotStateTimer;

	private static double _movementMessageTimer;

	public readonly BulletPhysicsController PhysicsController;

	private double _persistenceSaveTimer;

	private bool _printDebugObjects;

	private static readonly uint NetworkDataHash = ClassHasher.GetClassHashCode(typeof(NetworkData));

	private static readonly uint SceneDataHash = ClassHasher.GetClassHashCode(typeof(ISceneData));

	public static readonly uint CombinedHash = unchecked(NetworkDataHash * SceneDataHash);

	private bool _manualSave;

	private string _manualSaveFileName;

	private SaveFileAuxData _manualSaveAuxData;

	private SpaceObjectVessel[] _shipSystemSweep = [];

	private int _shipSystemCursor;

	private double _shipSystemSweepTime;

	private double _shipSystemSweepSeconds = ShipSystemSweepSeconds;

	public readonly ConcurrentDictionary<long, VesselDataUpdate> VesselsDataUpdate = new ConcurrentDictionary<long, VesselDataUpdate>();

	public List<DynamicObjectsRespawn> DynamicObjectsRespawnList = new List<DynamicObjectsRespawn>();

	public double DeltaTime;

	private long _lastTickTimestamp;

	public const double SpawnPointInviteTimer = 300.0;

	private static string _loadPersistenceFromFile;

	public IEnumerable<SpaceObjectVessel> AllVessels
	{
		get
		{
			foreach (KeyValuePair<long, SpaceObjectVessel> vessel in _vessels)
			{
				yield return vessel.Value;
			}
		}
	}

	public IEnumerable<Player> AllPlayers
	{
		get
		{
			foreach (KeyValuePair<long, Player> player in _players)
			{
				yield return player.Value;
			}
		}
	}

	public ArtificialBody[] ArtificialBodies
	{
		get
		{
			if (_artificialBodiesChanged)
			{
				_artificialBodiesChanged = false;
				_artificialBodiesCache = [.. _artificialBodies.Values];
			}
			return _artificialBodiesCache;
		}
	}

	public int ArtificialBodiesCount => _artificialBodies.Count;

	public static Server Instance { get; private set; }

	public TimeSpan RunTime => DateTime.UtcNow - _serverStartTime;

	public static double SolarSystemTime => Instance.SolarSystem.CurrentTime;

	[Obsolete]
	public bool DoesObjectExist(long guid)
	{
		return SpaceObjects.Contains(guid) || SolarSystem.State.TryGetItem(guid, out _);
	}

	public SpaceObject GetSpaceObject(long guid)
	{
		if (SpaceObjects.TryGet(guid, out var spaceObject))
		{
			return spaceObject;
		}
		return null;
	}

	public bool TryGetSpaceObject(long guid, out SpaceObject spaceObject)
	{
		return SpaceObjects.TryGet(guid, out spaceObject);
	}

	public DynamicObject GetDynamicObject(long guid)
	{
		SpaceObjects.TryGet(guid, out SpaceObject spaceObject);
		return spaceObject as DynamicObject ?? (spaceObject as Pivot)?.Child as DynamicObject;
	}

	public bool TryGetDynamicObject(long guid, out DynamicObject dynamicObject)
	{
		dynamicObject = GetDynamicObject(guid);
		return dynamicObject != null;
	}

	// TODO: Remove when moving space objects to state.
	public SpaceObject ResolveKey(long key)
	{
		if (SpaceObjects.TryGet(key, out SpaceObject spaceObject))
		{
			return spaceObject is Pivot pivot ? pivot.Child : spaceObject;
		}
		return _players.Values.Concat(_playersToAdd).FirstOrDefault(m => m.FakeGuid == key);
	}

	/// <summary>
	/// 	Get a player with a guid.
	/// </summary>
	public Player GetPlayer(long guid)
	{
		_players.TryGetValue(guid, out Player player);
		return player;
	}

	/// <summary>
	/// 	Gets a player from a specified player id.
	/// </summary>
	public Player GetPlayerFromPlayerId(string playerId)
	{
		return GetPlayer(GUIDFactory.PlayerIdToGuid(playerId));
	}

	public SpaceObjectVessel GetVessel(long guid)
	{
		return _vessels.TryGetValue(guid, out var vessel) ? vessel : null;
	}

	public Item GetItem(long guid)
	{
		return GetDynamicObject(guid)?.Item;
	}

	/// <summary>
	/// 	The items directly inside an object: loose or attached in a vessel, carried by a player or corpse,
	/// 	or fitted into another item.
	/// </summary>
	public List<DynamicObject> ItemsInSpaceObject(SpaceObject parent)
	{
		List<DynamicObject> items = [];
		for (ItemId item = SolarSystem.State.FirstChild(parent.Key); item.IsValid; item = SolarSystem.State.NextSibling(item))
		{
			if (GetDynamicObject(SolarSystem.State.Guid(item)) is { } dynamicObject)
			{
				items.Add(dynamicObject);
			}
		}
		return items;
	}

	public void Add(Player player)
	{
		if (_mainLoopStarted)
		{
			_playersToAdd.Add(player);
			return;
		}
		_players[player.Guid] = player;
		SpaceObjects.TryAdd(player.FakeGuid, player);
	}

	public void Add(SpaceObjectVessel vessel)
	{
		if (vessel is null)
		{
			Debug.LogError("Tried to add null vessel to vessels and objects list.");
			return;
		}

		_vessels[vessel.Guid] = vessel;
		_artificialBodies[vessel.Guid] = vessel;
		_artificialBodiesChanged = true;
		SpaceObjects.TryAdd(vessel.Guid, vessel, vessel.Position);
	}

	public void Add(DynamicObject dynamicObject)
	{
		SpaceObjects.TryAdd(dynamicObject.Guid, dynamicObject);
		if (dynamicObject.Item is IUpdateable)
		{
			_updateableDynamicObjects[dynamicObject.Guid] = dynamicObject;
		}
	}

	public void Add(Corpse corpse)
	{
		SpaceObjects.TryAdd(corpse.Guid, corpse);
	}

	public void Remove(Player player)
	{
		if (_mainLoopStarted)
		{
			_playersToRemove.Add(player);
			return;
		}
		_players.TryRemove(player.Guid, out _);
		SpaceObjects.TryRemove(player.FakeGuid, out _);
	}

	public void DeleteCharacter(string playerId)
	{
		_charactersToDelete.Enqueue(playerId);
	}

	public void Remove(SpaceObjectVessel vessel)
	{
		_vessels.TryRemove(vessel.Guid, out _);
		_artificialBodies.TryRemove(vessel.Guid, out _);
		_artificialBodiesChanged = true;
		SpaceObjects.TryRemove(vessel.Guid, out _);
	}

	public void TrackArtificialBody(ArtificialBody body)
	{
		_artificialBodies[body.Guid] = body;
		_artificialBodiesChanged = true;
	}

	public void UntrackArtificialBody(ArtificialBody body)
	{
		// Make sure we're not removing a pivot by accident.
		if (!_artificialBodies.TryGetValue(body.Guid, out ArtificialBody tracked) || !ReferenceEquals(tracked, body))
		{
			return;
		}

		_artificialBodies.TryRemove(body.Guid, out _);
		_artificialBodiesChanged = true;
	}

	public void Remove(DynamicObject dobj)
	{
		SpaceObjects.TryRemove(dobj.Guid, out _);
		_updateableDynamicObjects.TryRemove(dobj.Guid, out _);
	}

	public void Remove(Corpse corpse)
	{
		SpaceObjects.TryRemove(corpse.Guid, out _);
	}

	public Server()
	{
		IsRunning = true;
		Instance = this;
		PhysicsController = new BulletPhysicsController();
		SolarSystem = new SolarSystem();
		LoadServerSettings();
		Console.Title = "(id: " + (NetworkController.ServerId == null ? "Not yet assigned" : string.Concat(NetworkController.ServerId)) + ")";
		Stopwatch stopWatch = new Stopwatch();
		stopWatch.Start();
		Thread.Sleep(1);
		stopWatch.Stop();
		long maxTicks = (long)(1000.0 / stopWatch.Elapsed.TotalMilliseconds);
		Debug.UnformattedMessage(string.Format("Game port: {4}\nStatus port: {5}\nStart date: {0}\nServer ticks: {1}{3}\nMax server ticks (not precise): {2}\nCombined hash: {6}", DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss.ffff"), _numberOfTicks, maxTicks, _numberOfTicks > maxTicks ? " WARNING: Server ticks is larger than max tick" : "", GamePort, StatusPort, CombinedHash));
		StaticData.LoadData();
	}

	/// <summary>
	/// 	Gets the arguments provided to the program and sets the properties accordingly. Only executed at the start of the program.
	/// </summary>
	public static void InitProperties(string[] args)
	{
		// Parse arguments.
		string gPort = null;
		string sPort = null;
		string randomShips = null;
		for (int i = 0; i < args.Length; i++)
		{
			switch(args[i].ToLower()) {
				case "-configdir":
					i++;

					if (i >= args.Length)
					{
						Debug.LogError("-configdir was not supplied a path.");
						break;
					}

					ConfigDir = args[i];
					if (!ConfigDir.EndsWith("/"))
					{
						ConfigDir += "/";
					}
				break;
				case "-clean":
				case "-noload":
					_cleanStart = true;
				break;
				case "-load":
					i++;

					if (i >= args.Length)
					{
						Debug.LogError("-load was not supplied a path.");
						break;
					}

					_loadPersistenceFromFile = args[i];
				break;
				case "-randomships":
					i++;

					if (i >= args.Length)
					{
						Debug.LogError("-randomships was not supplied a number.");
						break;
					}

					randomShips = args[++i];
				break;
				case "-gport":
					i++;

					if (i >= args.Length)
					{
						Debug.LogError("-gport was not supplied a port.");
						break;
					}

					gPort = args[++i];
				break;
				case "-sport":
					i++;

					if (i >= args.Length)
					{
						Debug.LogError("-sport was not supplied a port.");
						break;
					}

					sPort = args[++i];
				break;
			}
		}

		// Set properties.
		Properties = new Properties(ConfigDir + "GameServer.ini");
		if (gPort != null)
		{
			Properties.SetProperty("game_port", gPort);
		}
		if (sPort != null)
		{
			Properties.SetProperty("status_port", sPort);
		}
		if (randomShips != null)
		{
			Properties.SetProperty("spawn_random_ships_count", randomShips);
		}
	}

	private void LoadServerSettings()
	{
		Properties.TryGetPropertySafe("server_tick_count", ref _numberOfTicks);
		Properties.TryGetPropertySafe("game_port", ref GamePort);
		Properties.TryGetPropertySafe("status_port", ref StatusPort);
		Properties.TryGetPropertySafe("offline_mode", ref OfflineMode);
		Properties.TryGetPropertySafe("server_name", ref ServerName);
		Properties.TryGetPropertySafe("server_description", ref ServerDescription);
		Properties.TryGetPropertySafe("server_password", ref ServerPassword);
		Properties.TryGetPropertySafe("http_key", ref SocialServerConnection.HttpKey);
		Properties.TryGetPropertySafe("main_server_ip", ref SocialServerConnection.IpAddress);
		Properties.TryGetPropertySafe("main_server_port", ref SocialServerConnection.Port);
		if (Properties.TryGetProperty("server_admins", out string admins))
		{
			string[] adminsArray = admins.Split(',');
			_serverAdmins = adminsArray.Where((string m) => m != "").ToList();
		}
		Properties.TryGetPropertySafe("solar_system_time", ref _solarSystemStartTime);
		Properties.TryGetPropertySafe("save_interval", ref PersistenceSaveInterval);
		Properties.TryGetPropertySafe("max_players", ref NetworkController.MaxPlayers);
		Properties.TryGetPropertySafe("number_of_save_files", ref MaxNumberOfSaveFiles);
		MaxNumberOfSaveFiles = MathHelper.Clamp(MaxNumberOfSaveFiles, 1, 100);
		Properties.TryGetPropertySafe("vessel_decay_rate_multiplier", ref VesselDecayRateMultiplier);
		Properties.TryGetPropertySafe("vessel_decay_grace_period", ref VesselDecayGracePeriod);
		Properties.TryGetPropertySafe("vessel_explosion_radius_multiplier", ref VesselExplosionRadiusMultiplier);
		Properties.TryGetPropertySafe("vessel_explosion_damage_multiplier", ref VesselExplosionDamageMultiplier);
		Properties.TryGetPropertySafe("self_destruct_explosion_radius_multiplier", ref SelfDestructExplosionRadiusMultiplier);
		Properties.TryGetPropertySafe("self_destruct_explosion_damage_multiplier", ref SelfDestructExplosionDamageMultiplier);
		Properties.TryGetPropertySafe("vessel_collision_damage_multiplier", ref VesselCollisionDamageMultiplier);
		Properties.TryGetPropertySafe("debris_vessel_explosion_radius_multiplier", ref DebrisVesselExplosionRadiusMultiplier);
		Properties.TryGetPropertySafe("debris_vessel_explosion_damage_multiplier", ref DebrisVesselExplosionDamageMultiplier);
		Properties.TryGetPropertySafe("debris_vessel_collision_damage_multiplier", ref DebrisVesselCollisionDamageMultiplier);
		Properties.TryGetPropertySafe("activate_repair_point_chance_multiplier", ref ActivateRepairPointChanceMultiplier);
		Properties.TryGetPropertySafe("starting_set_despawn_time", ref StartingSetDespawnTimeSec);
		Properties.TryGetPropertySafe("server_restart_time", ref ServerRestartTimeSec);
		Properties.TryGetPropertySafe("junk_items_cleanup_scope", ref JunkItemsCleanupScope);
		Properties.TryGetPropertySafe("junk_items_time_to_live", ref JunkItemsTimeToLive);
		Properties.TryGetPropertySafe("junk_items_cleanup_interval", ref JunkItemsCleanupInterval);
		Properties.TryGetPropertySafe("can_warp_through_celestial_bodies", ref CanWarpThroughCelestialBodies);
		Properties.TryGetPropertySafe("max_angular_velocity", ref MaxAngularVelocity);
		Properties.TryGetPropertySafe("arena_ship_respawn_timer", ref SpaceObjectVessel.ArenaRescueTime);
		Properties.TryGetPropertySafe("print_debug_objects", ref _printDebugObjects);
		Properties.TryGetPropertySafe("spawn_manager_print_categories", ref SpawnManager.Settings.PrintCategories);
		Properties.TryGetPropertySafe("spawn_manager_print_spawn_rules", ref SpawnManager.Settings.PrintSpawnRules);
		Properties.TryGetPropertySafe("spawn_manager_print_item_attach_points", ref SpawnManager.Settings.PrintItemAttachPoints);
		Properties.TryGetPropertySafe("spawn_manager_print_item_type_ids", ref SpawnManager.Settings.PrintItemTypeIDs);
	}

	public async Task<Player> GetOrCreateConnectedPlayerAsync(long guid, string playerId, CharacterData characterData)
	{
		if (!WorldInitialized)
		{
			await SpawnManager.Initialize();;
		}

		Player player = GetPlayer(guid);
		if (player is null)
		{
			Debug.Log("Creating new player for client with guid:", guid);

			if (characterData is null)
			{
				Debug.LogError("Cannot create a player without character data.", playerId);
				return null;
			}

			player = await Player.CreatePlayerAsync(guid, Vector3D.Zero, QuaternionD.Identity, characterData.Name, playerId, characterData.Gender, characterData.HeadType, characterData.HairType);
			Add(player);
		}

#if DEBUG
		player.IsAdmin = true;
#else
		player.IsAdmin = _serverAdmins.Contains(player.PlayerId) || _serverAdmins.Contains("*");
#endif

		return player;
	}

	private async Task ResetSpawnPointsForPlayerAsync(Player pl, Ship skipShip)
	{
		if (!pl.PlayerId.IsNullOrEmpty() && _spawnPointInvites.ContainsKey(pl.PlayerId))
		{
			await ClearSpawnPointInvitation(pl.PlayerId);
		}
		foreach (SpaceObjectVessel ves in _vessels.Values)
		{
			if (ves is Ship ship && ship != skipShip)
			{
				await ship.ResetSpawnPointsForPlayer(pl, sendStatsMessage: true);
			}
		}
	}

	private async Task<bool> AddPlayerToShipAsync(Player pl, SpawnSetupType setupType, long shipId)
	{
		if (shipId == 0L && setupType == SpawnSetupType.None)
		{
			return false;
		}
		Ship foundShip = null;
		ShipSpawnPoint foundSpawnPoint = null;
		switch (setupType)
		{
			case SpawnSetupType.Continue:
				if (pl.AuthorizedSpawnPoint == null)
				{
					return false;
				}
				foundShip = pl.AuthorizedSpawnPoint.Ship;
				foundSpawnPoint = pl.AuthorizedSpawnPoint;
				break;
			case SpawnSetupType.None:
				if (shipId > 0 && _spawnPointInvites.TryGetValue(pl.PlayerId, out SpawnPointInviteData inviteData) && inviteData.SpawnPoint.Ship.Guid == shipId)
				{
					foundShip = GetVessel(shipId) as Ship;
					foundSpawnPoint = foundShip != null ? inviteData.SpawnPoint : null;
					if (foundSpawnPoint != null)
					{
						await ResetSpawnPointsForPlayerAsync(pl, null);
					}
				}
				break;
			default:
				await ResetSpawnPointsForPlayerAsync(pl, null);
				foundShip = await SpawnManager.SpawnStartingSetup(setupType);
				if (foundShip != null)
				{
					foundSpawnPoint = SpawnManager.SetStartingSetupSpawnPoints(foundShip, pl);
				}
				if (foundShip == null || foundSpawnPoint == null)
				{
					Debug.LogError("FAILED TO SPAWN STARTING SETUP", pl.Guid, foundShip?.Guid ?? -1);
				}
				break;
		}

		if (foundShip != null && foundSpawnPoint != null)
		{
			foundSpawnPoint.Player = pl;
			if (foundSpawnPoint.Executor != null)
			{
				foundSpawnPoint.IsPlayerInSpawnPoint = true;
			}
			await foundSpawnPoint.AuthorizePlayerToSpawnPoint(pl, sendMessage: true);
			pl.Parent = foundSpawnPoint.Ship;
			pl.SetSpawnPoint(foundSpawnPoint);
			pl.SubscribeTo(foundSpawnPoint.Ship.MainVessel);
			return true;
		}
		return false;
	}

	public async Task RemovePlayerAsync(Player player)
	{
		if (player.Parent != null)
		{
			if (player.Parent.ObjectType == SpaceObjectType.PlayerPivot)
			{
				await (player.Parent as Pivot).Destroy();
			}
			if (player.Parent.ObjectType is SpaceObjectType.Ship or SpaceObjectType.Asteroid)
			{
				(player.Parent as SpaceObjectVessel).RemovePlayerFromCrew(player, checkDetails: true);
			}
			else if (player.Parent.ObjectType != SpaceObjectType.Station)
			{
			}
		}
		Remove(player);
	}

	public List<SpawnPointDetails> GetAvailableSpawnPoints(Player pl)
	{
		List<SpawnPointDetails> retVal = new();
		if (pl.PlayerId != null && _spawnPointInvites.TryGetValue(pl.PlayerId, out var invite))
		{
			ShipSpawnPoint sp = invite.SpawnPoint;
			SpawnPointDetails spd = new SpawnPointDetails
			{
				Name = sp.Ship.FullName,
				IsPartOfCrew = false,
				SpawnPointParentID = sp.Ship.Guid,
				PlayersOnShip = new List<string>()
			};
			foreach (Player item in sp.Ship.VesselCrew)
			{
				spd.PlayersOnShip.Add(item.Name);
			}
			retVal.Add(spd);
		}
		return retVal;
	}

	private async void LatencyTestListener(NetworkData data)
	{
		await NetworkController.SendAsync(data.Sender, data);
	}

	private async Task Start()
	{
		EventSystem.AddSyncRequestListener<PlayerSpawnRequest>(PlayerSpawnRequestListener);
		EventSystem.AddSyncRequestListener<AvailableSpawnPointsRequest>(AvailableSpawnPointsRequestListener);
		EventSystem.AddSyncRequestListener<ObjectsInfoRequest>(ObjectsInfoRequestListener);
		EventSystem.AddListener<StateUpdateRequest>(SolarSystem.StateUpdateRequestListener);
		EventSystem.AddSyncRequestListener<MapDataRequest>(MapDataRequestListener);
		EventSystem.AddListener<TextChatMessage>(TextChatMessageListener);
		EventSystem.AddListener<TransferResourceMessage>(TransferResourcesMessageListener);
		EventSystem.AddListener<FabricateItemMessage>(FabricateItemMessageListener);
		EventSystem.AddListener<CancelFabricationMessage>(CancelFabricationMessageListener);
		EventSystem.AddListener<PlayersOnServerRequest>(PlayersOnServerRequestListener);
		EventSystem.AddListener<RepairItemMessage>(RepairMessageListener);
		EventSystem.AddListener<RepairVesselMessage>(RepairMessageListener);
		EventSystem.AddListener<HurtPlayerMessage>(HurtPlayerMessageListener);
		EventSystem.AddSyncRequestListener<ConsoleMessage>(ConsoleMessageListener);
		EventSystem.AddListener<ExplosionMessage>(ExplosionMessageListener);
		EventSystem.AddListener<LatencyTestMessage>(LatencyTestListener);
		EventSystem.AddListener<ServerShutDownMessage>(ServerShutDownMessageListener);
		EventSystem.AddListener<NameTagMessage>(NameTagMessageListener);

		if (!OfflineMode)
		{
			try
			{
				RegisterServerResponse response = await SocialServerConnection.Send<RegisterServerResponse>(new RegisterServerRequest
				{
					GamePort = GamePort,
					StatusPort = StatusPort
				});

				NetworkController.ServerId = response.ServerId;
				_adminIpAddressRanges = response.AdminIpAddressRanges;
			}
			catch
			{
				Debug.LogError("Could not connect to main server. Check if ip, port and auth key is correct.");
				IsRunning = false;
				return;
			}
		}

		Console.Title = " (id: " + (NetworkController.ServerId == null ? "Not yet assigned" : string.Concat(NetworkController.ServerId)) + ")";
		Debug.UnformattedMessage("r\n\tServer ID: " + NetworkController.ServerId + "\r\n");
	}

	public void TransferResourcesMessageListener(NetworkData data)
	{
		var message = data as TransferResourceMessage;
		SpaceObjectVessel fromVessel = Instance.GetVessel(message.FromVesselGuid);
		SpaceObjectVessel toVessel = Instance.GetVessel(message.ToVesselGuid);
		if (message.FromLocationType == ResourceLocationType.ResourcesTransferPoint)
		{
			ICargo toCargo2 = null;
			if (message.ToLocationType == ResourceLocationType.CargoBay)
			{
				toCargo2 = toVessel.CargoBay;
			}
			else if (message.ToLocationType == ResourceLocationType.ResourceTank)
			{
				toCargo2 = toVessel.MainDistributionManager.GetResourceContainer(new VesselObjectID(message.ToVesselGuid, message.ToInSceneID));
			}
			DynamicObject dobj2 = fromVessel.AttachPoints.GetValueOrDefault(message.FromInSceneID)?.Item?.DynamicObj;
			if (dobj2 is { Item: ICargo item } && toCargo2 != null)
			{
				if (message.ToLocationType != 0)
				{
					TransferResources(item, message.FromCompartmentID, toCargo2, message.ToCompartmentID, message.ResourceType, message.Quantity);
				}
				else
				{
					VentResources(item, message.FromCompartmentID, message.ResourceType, message.Quantity);
				}
			}
			return;
		}
		if (message.ToLocationType == ResourceLocationType.ResourcesTransferPoint)
		{
			ICargo fromCargo2 = null;
			if (message.FromLocationType == ResourceLocationType.CargoBay)
			{
				fromCargo2 = fromVessel.CargoBay;
			}
			else if (message.FromLocationType == ResourceLocationType.ResourceTank)
			{
				fromCargo2 = fromVessel.MainDistributionManager.GetResourceContainer(new VesselObjectID(message.FromVesselGuid, message.FromInSceneID));
			}
			DynamicObject dobj = toVessel.AttachPoints.GetValueOrDefault(message.ToInSceneID)?.Item?.DynamicObj;
			if (dobj is { Item: ICargo item } && fromCargo2 != null)
			{
				if (message.ToLocationType != 0)
				{
					TransferResources(fromCargo2, message.FromCompartmentID, item, message.ToCompartmentID, message.ResourceType, message.Quantity);
				}
				else
				{
					VentResources(item, message.FromCompartmentID, message.ResourceType, message.Quantity);
				}
			}
			return;
		}
		ICargo fromCargo = null;
		if (message.FromLocationType == ResourceLocationType.CargoBay)
		{
			fromCargo = fromVessel.CargoBay;
		}
		else if (message.FromLocationType == ResourceLocationType.Refinery)
		{
			fromCargo = fromVessel.MainDistributionManager.GetSubSystem(new VesselObjectID(message.FromVesselGuid, message.FromInSceneID)) as SubSystemRefinery;
		}
		else if (message.FromLocationType == ResourceLocationType.Fabricator)
		{
			fromCargo = fromVessel.MainDistributionManager.GetSubSystem(new VesselObjectID(message.FromVesselGuid, message.FromInSceneID)) as SubSystemFabricator;
		}
		else if (message.FromLocationType == ResourceLocationType.ResourceTank)
		{
			fromCargo = fromVessel.MainDistributionManager.GetResourceContainer(new VesselObjectID(message.FromVesselGuid, message.FromInSceneID));
		}
		ICargo toCargo = null;
		if (message.ToLocationType == ResourceLocationType.CargoBay)
		{
			toCargo = toVessel.CargoBay;
		}
		else if (message.ToLocationType == ResourceLocationType.Refinery)
		{
			toCargo = toVessel.MainDistributionManager.GetSubSystem(new VesselObjectID(message.ToVesselGuid, message.ToInSceneID)) as SubSystemRefinery;
		}
		else if (message.ToLocationType == ResourceLocationType.Fabricator)
		{
			toCargo = toVessel.MainDistributionManager.GetSubSystem(new VesselObjectID(message.ToVesselGuid, message.ToInSceneID)) as SubSystemFabricator;
		}
		else if (message.ToLocationType == ResourceLocationType.ResourceTank)
		{
			toCargo = toVessel.MainDistributionManager.GetResourceContainer(new VesselObjectID(message.ToVesselGuid, message.ToInSceneID));
		}
		else if (message.ToLocationType == ResourceLocationType.None)
		{
			VentResources(fromCargo, message.FromCompartmentID, message.ResourceType, message.Quantity);
			return;
		}
		if (fromCargo != null && toCargo != null)
		{
			TransferResources(fromCargo, message.FromCompartmentID, toCargo, message.ToCompartmentID, message.ResourceType, message.Quantity);
		}
	}

	public void TransferResources(ICargo fromCargo, short fromCompartmentId, ICargo toCargo, short toCompartmentId, ResourceType resourceType, float quantity)
	{
		CargoCompartmentData fromCompartment = fromCargo.Compartments.Find((CargoCompartmentData m) => m.ID == fromCompartmentId);
		CargoCompartmentData toCompartment = toCargo.Compartments.Find((CargoCompartmentData m) => m.ID == toCompartmentId);
		if (fromCompartment == null || toCompartment == null || (toCompartment.AllowedResources.Count > 0 && !toCompartment.AllowedResources.Contains(resourceType)) || (toCompartment.AllowOnlyOneType && toCompartment.Resources.Count > 0 && toCompartment.Resources[0].ResourceType != resourceType))
		{
			return;
		}
		float qty = quantity;
		CargoResourceData res = fromCompartment.Resources.Find((CargoResourceData m) => m != null && m.ResourceType == resourceType);
		if (res == null)
		{
			return;
		}
		float available = res.Quantity;
		if (!(available <= float.Epsilon))
		{
			if (qty > available)
			{
				qty = available;
			}
			float free = toCompartment.Capacity - toCompartment.Resources.Sum((CargoResourceData m) => m.Quantity);
			if (qty > free)
			{
				qty = free;
			}
			fromCargo.ChangeQuantityByAsync(fromCompartmentId, resourceType, 0f - qty);
			toCargo.ChangeQuantityByAsync(toCompartmentId, resourceType, qty);
		}
	}

	private void VentResources(ICargo fromCargo, short fromCompartmentId, ResourceType resourceType, float quantity)
	{
		CargoCompartmentData fromCompartment = fromCargo.Compartments.Find((CargoCompartmentData m) => m.ID == fromCompartmentId);
		if (fromCompartment == null)
		{
			return;
		}
		float qty = quantity;
		CargoResourceData res = fromCompartment.Resources.Find((CargoResourceData m) => m != null && m.ResourceType == resourceType);
		if (res != null)
		{
			float available = res.Quantity;
			if (qty > available)
			{
				qty = available;
			}
			fromCargo.ChangeQuantityByAsync(fromCompartmentId, resourceType, 0f - qty);
		}
	}

	private void FabricateItemMessageListener(NetworkData data)
	{
		var message = data as FabricateItemMessage;
		SpaceObjectVessel vessel = GetVessel(message.ID.VesselGUID);
		if (vessel != null && vessel.DistributionManager.GetSubSystem(message.ID) is SubSystemFabricator fabricator)
		{
			fabricator.Fabricate(message.ItemType);
		}
	}

	private async void CancelFabricationMessageListener(NetworkData data)
	{
		var message = data as CancelFabricationMessage;
		SpaceObjectVessel vessel = GetVessel(message.ID.VesselGUID);
		if (vessel != null && vessel.DistributionManager.GetSubSystem(message.ID) is SubSystemFabricator fabricator)
		{
			await fabricator.Cancel(message.CurrentItemOnly);
		}
	}

	private async void RepairMessageListener(NetworkData data)
	{
		Player pl = GetPlayer(data.Sender);
		Item rTool = pl?.PlayerInventory.HandsSlot.Item;
		if (rTool is not RepairTool rt)
		{
			return;
		}
		if (data is RepairVesselMessage rvm)
		{
			await rt.RepairVessel(rvm.ID);
		}
		else if (data is RepairItemMessage rim)
		{
			if (rim.GUID > 0)
			{
				await rt.RepairItem(rim.GUID);
			}
			else
			{
				await rt.ConsumeFuel(rt.RepairAmount * rt.FuelConsumption);
			}
		}
	}

	private async void HurtPlayerMessageListener(NetworkData data)
	{
		var message = data as HurtPlayerMessage;
		Player pl = GetPlayer(message.Sender);
		await pl.TakeDamage(message.Duration, "hurt-trigger", message.Damage);
	}

	private async Task<NetworkData> ConsoleMessageListener(NetworkData data)
	{
		var message = data as ConsoleMessage;
		Player player = GetPlayer(message.Sender);

		if (player is null)
		{
			return new ConsoleMessage { Text = "Unknown sender.", Status = NetworkData.MessageStatus.Failure };
		}

		if (!player.IsAdmin)
		{
			return new ConsoleMessage
			{
				Text = "You are not an admin on this server.",
				Status = NetworkData.MessageStatus.Failure
			};
		}

		return await ProcessConsoleCommand(message.Text, player);
	}

	private async void TextChatMessageListener(NetworkData data)
	{
		var message = data as TextChatMessage;
		Player player = _players[message.Sender];
		message.GUID = player.FakeGuid;
		message.Name = player.Name;
		if (message.MessageText.Length > 250)
		{
			message.MessageText = message.MessageText.Substring(0, 250);
		}
		if (message.Local)
		{
			{
				foreach (Player pl in _players.Values)
				{
					if ((pl.Position - player.Position).SqrMagnitude < 1000000.0 && pl != player)
					{
						await NetworkController.SendAsync(pl.Guid, message);
					}
				}
				return;
			}
		}
		await NetworkController.SendToAllAsync(message, message.Sender);
	}

	private async Task<ConsoleMessage> ProcessConsoleCommand(string cmd, Player player)
	{
		static ConsoleMessage Ok(string text) =>
			new() { Text = text, Status = NetworkData.MessageStatus.Success };
		static ConsoleMessage Error(string text) =>
			new() { Text = text, Status = NetworkData.MessageStatus.Failure };

		string[] parts = cmd?.Split(' ',
			StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

		if (parts.Length == 0)
		{
			return Error("Empty command. Type 'help' for a list.");
		}

		try
		{
			for (int i = 0; i < parts.Length && i != 2; i++)
			{
				parts[i] = parts[i].ToLower();
			}
			SpaceObject parent = player.Parent;

			switch (parts[0])
			{
			case "help":
			{
				return Ok(string.Join("\r\n",
					"help                                 list these commands",
					"refill                               refill held item, suit and station storages",
					"restock                              refill every empty attach point on this vessel",
					"spawn <item|scene> [tier|tag]        spawn an item, corpse, ship or asteroid",
					"station <name>                       assemble a station from Data/Stations/<name>.json",
					"torpedo [seconds]                    launch a dockable container where you look",
					"hitme                                send a corridor module at you",
					"selfdestruct <seconds>               arm self destruct on this vessel",
					"collision <0|1>                      toggle collision on this vessel",
					"blink <km>                           jump this vessel forward",
					"teleport <player|vessel>             move to a player or vessel",
					"whereami                             print your player location",
					"countships [range|celestial body]    count spawned ships",
					"countitems [range|celestial body]    count spawned items",
					"vent                                 depressurize the current room",
					"pressurize                           pressurize the current room",
					"airquality <0-1>                     set air quality in the current room",
					"sethealth <health>                   set health of held item, or of this vessel",
					"god [0|1]                            query or set god mode",
					"setmain                              make this vessel the current station's main vessel",
					"respawn <spawn rule>                 respawn a blueprint spawn rule",
					"spawnrulescleanup                    destroy vessels no spawn rule owns",
					"unmatchall                           disable stabilisation everywhere",
					"resetblueprints                      reset every player's blueprints"));
			}
			case "refill":
			{
				if (parts.Length != 1)
				{
					return Error("refill: expected no arguments.");
				}

				Item handsItem = player.PlayerInventory.HandsSlot.Item;
				if (handsItem is ICargo cargo)
				{
					foreach (CargoCompartmentData ccd in cargo.Compartments.Where((CargoCompartmentData m) => m.AllowOnlyOneType))
					{
						using List<CargoResourceData>.Enumerator enumerator2 = ccd.Resources.GetEnumerator();
						if (enumerator2.MoveNext())
						{
							CargoResourceData r = enumerator2.Current;
							await cargo.ChangeQuantityByAsync(ccd.ID, r.ResourceType, ccd.Capacity);
						}
					}
				}
				if (player.PlayerInventory.CurrOutfit != null)
				{
					foreach (InventorySlot os in player.PlayerInventory.CurrOutfit.InventorySlots.Values)
					{
						Item item2 = os.Item;
						if (item2 is not ICargo cargo1)
						{
							continue;
						}
						foreach (CargoCompartmentData ccd2 in cargo1.Compartments.Where((CargoCompartmentData m) => m.AllowOnlyOneType))
						{
							using List<CargoResourceData>.Enumerator enumerator5 = ccd2.Resources.GetEnumerator();
							if (enumerator5.MoveNext())
							{
								await cargo1.ChangeQuantityByAsync(ccd2.ID, enumerator5.Current.ResourceType, ccd2.Capacity);
							}
						}
					}
				}
				if (parent is not SpaceObjectVessel vessel)
				{
					return Ok("refill: inventory refilled.");
				}
				foreach (ResourceContainer rc in vessel.MainDistributionManager.GetResourceContainers())
				{
					foreach (CargoCompartmentData ccd3 in rc.Compartments)
					{
						foreach (CargoResourceData crd in ccd3.Resources)
						{
							await rc.ChangeQuantityByAsync(ccd3.ID, crd.ResourceType, ccd3.Capacity);
						}
					}
				}
				foreach (GeneratorCapacitor cap in (from m in vessel.MainDistributionManager.GetGenerators()
					where m is GeneratorCapacitor
					select m).Cast<GeneratorCapacitor>())
				{
					cap.Capacity = cap.MaxCapacity;
				}
				return Ok("refill: inventory and vessel storages refilled.");
			}
			case "spawn":
			{
				if (parts.Length is not (2 or 3))
				{
					return Error("spawn: expected <item|scene> [tier|tag].");
				}

				Vector3D spawnItemPosition = player.LocalPosition + player.LocalRotation * Vector3D.Forward;
				if (parts[1].ToLower() == "corpse")
				{
					// TODO: This needs a parent.
					_ = new Corpse(player)
					{
						LocalPosition = player.LocalPosition + player.LocalRotation * Vector3D.Forward
					};
					return Ok("spawn: corpse spawned.");
				}
				int tier = 1;
				if (parts.Length == 3 && !int.TryParse(parts[2], out tier))
				{
					tier = 1;
				}
				List<InventorySlot> inventorySlots = [player.PlayerInventory.OutfitSlot];
				if (player.PlayerInventory.CurrOutfit != null)
				{
					inventorySlots.AddRange(player.PlayerInventory.CurrOutfit.InventorySlots.Values.Where((InventorySlot m) => m.SlotType == InventorySlot.Type.Equip));
				}
				inventorySlots.Add(player.PlayerInventory.HandsSlot);
				if (player.PlayerInventory.CurrOutfit != null)
				{
					inventorySlots.AddRange(player.PlayerInventory.CurrOutfit.InventorySlots.Values.Where((InventorySlot m) => m.SlotType == InventorySlot.Type.General));
				}
				foreach (DynamicObjectData dod in StaticData.DynamicObjectsDataList.Values)
				{
					if (!dod.PrefabPath.ToLower().EndsWith(parts[1].ToLower()))
					{
						continue;
					}
					if (dod.ItemType == ItemType.GenericItem)
					{
						InventorySlot slot = inventorySlots.FirstOrDefault((InventorySlot m) => m.Item == null && m.CanStoreItem(ItemType.GenericItem));
						await DynamicObject.SpawnDynamicObject(dod.ItemType, (dod.DefaultAuxData as GenericItemData).SubType, MachineryPartType.None, parent, -1, spawnItemPosition, null, null, tier, slot, refill: true);
					}
					else if (dod.ItemType == ItemType.MachineryPart)
					{
						InventorySlot slot2 = inventorySlots.FirstOrDefault((InventorySlot m) => m.Item == null && m.CanStoreItem(ItemType.MachineryPart));
						await DynamicObject.SpawnDynamicObject(dod.ItemType, GenericItemSubType.None, (dod.DefaultAuxData as MachineryPartData).PartType, parent, -1, spawnItemPosition, null, null, tier, slot2, refill: true);
					}
					else
					{
						InventorySlot slot3 = inventorySlots.FirstOrDefault((InventorySlot m) => m.Item == null && m.CanStoreItem(dod.ItemType));
						await DynamicObject.SpawnDynamicObject(dod.ItemType, GenericItemSubType.None, MachineryPartType.None, parent, -1, spawnItemPosition, null, null, tier, slot3, refill: true);
					}
					return Ok("spawn: spawned " + dod.ItemType + ".");
				}
				foreach (ItemType v in Enum.GetValues(typeof(ItemType)))
				{
					if (v.ToString().Contains(parts[1], StringComparison.CurrentCultureIgnoreCase))
					{
						InventorySlot slot4 = inventorySlots.FirstOrDefault((InventorySlot m) => m.Item == null && m.CanStoreItem(v));
						await DynamicObject.SpawnDynamicObject(v, GenericItemSubType.None, MachineryPartType.None, parent, -1, spawnItemPosition, null, null, tier, slot4, refill: true);
						return Ok("spawn: spawned " + v + ".");
					}
				}
				foreach (GenericItemSubType v4 in Enum.GetValues(typeof(GenericItemSubType)))
				{
					if (v4.ToString().Contains(parts[1], StringComparison.CurrentCultureIgnoreCase))
					{
						InventorySlot slot5 = inventorySlots.FirstOrDefault((InventorySlot m) => m.Item == null && m.CanStoreItem(ItemType.GenericItem));
						await DynamicObject.SpawnDynamicObject(ItemType.GenericItem, v4, MachineryPartType.None, parent, -1, spawnItemPosition, null, null, tier, slot5, refill: true);
						return Ok("spawn: spawned " + v4 + ".");
					}
				}
				foreach (MachineryPartType v5 in Enum.GetValues(typeof(MachineryPartType)))
				{
					if (v5.ToString().Contains(parts[1], StringComparison.CurrentCultureIgnoreCase))
					{
						InventorySlot slot6 = inventorySlots.FirstOrDefault((InventorySlot m) => m.Item == null && m.CanStoreItem(ItemType.MachineryPart));
						await DynamicObject.SpawnDynamicObject(ItemType.MachineryPart, GenericItemSubType.None, v5, parent, -1, spawnItemPosition, null, null, tier, slot6, refill: true);
						return Ok("spawn: spawned " + v5 + ".");
					}
				}
				{
					foreach (GameScenes.SceneId v6 in Enum.GetValues(typeof(GameScenes.SceneId)))
					{
						if (v6.ToString().Contains(parts[1], StringComparison.CurrentCultureIgnoreCase))
						{
							string tag = parts.Length == 3 ? parts[2] : "";
							if (GameScenes.Ranges.IsShip(v6))
							{
								tag = tag + (tag == "" || tag.EndsWith(";") ? "" : ";") + "_RescueVessel";
							}
							Vector3D offset;
							if (parent is SpaceObjectVessel vessel6)
							{
								offset = player.Position - vessel6.MainVessel.Position + player.Rotation * QuaternionD.Euler(0f - player.MouseLook, 0.0, 0.0) * Vector3D.Forward * 25.0;
							}
							else
							{
								offset = player.LocalPosition + player.LocalRotation * Vector3D.Forward * 50.0;
							}
							if (GameScenes.Ranges.IsAsteroid(v6))
							{
								Asteroid asteroid = Asteroid.CreateNewAsteroid(v6, "", -1L, [parent is SpaceObjectVessel vessel ? vessel.MainVessel.Guid : parent.Guid], null, offset * 10.0, null, null, tag, checkPosition: false);
								asteroid.AngularVelocity = new Vector3D(MathHelper.RandomNextDouble(), MathHelper.RandomNextDouble(), MathHelper.RandomNextDouble()).Normalized * 6.0;
							}
							else
							{
								Ship ship2 = await Ship.CreateNewShip(v6, "", -1L, new List<long> { parent is SpaceObjectVessel vessel ? vessel.MainVessel.Guid : parent.Guid }, null, offset, null, null, tag, checkPosition: false);
								ship2.AngularVelocity = new Vector3D(MathHelper.RandomNextDouble(), MathHelper.RandomNextDouble(), MathHelper.RandomNextDouble()).Normalized * 1.0;
							}
							return Ok("spawn: spawned " + v6 + ".");
						}
					}
					return Error("spawn: nothing matches '" + parts[1] + "'.");
				}
			}
			case "selfdestruct":
			{
				if (parts.Length != 2 || !int.TryParse(parts[1], out var time2))
				{
					return Error("selfdestruct: expected <seconds>.");
				}
				if (parent is not SpaceObjectVessel vessel)
				{
					return Error("selfdestruct: you are not inside a vessel.");
				}

				vessel.SelfDestructTimer = new SelfDestructTimer(vessel, time2);
				return Ok("selfdestruct: armed for " + time2 + "s on " + vessel.VesselName + ".");
			}
			case "hitme":
			{
				if (parts.Length != 1)
				{
					return Error("hitme: expected no arguments.");
				}

				double distance = 500.0;
				double velocity = 50.0;
				double radius2 = 10.0;
				Vector3D offset3 = parent is not SpaceObjectVessel vessel ? player.LocalRotation * Vector3D.Forward * distance : vessel.Rotation * player.LocalRotation * Vector3D.Forward * distance;
				Ship ship3 = await Ship.CreateNewShip(GameScenes.SceneId.AltCorp_CorridorModule, "", -1L, [parent.Guid], null, offset3, null, null, checkPosition: false);
				Vector3D thrust2 = -offset3.Normalized * velocity;
				Vector3D offset4 = new Vector3D(MathHelper.RandomNextDouble() - 0.5, MathHelper.RandomNextDouble() - 0.5, MathHelper.RandomNextDouble() - 0.5) * radius2 * 2.0;
				ship3.AngularVelocity = new Vector3D(MathHelper.RandomNextDouble(), MathHelper.RandomNextDouble(), MathHelper.RandomNextDouble()) * 50.0;
				ship3.Orbit.InitFromStateVectors(ship3.Orbit.Parent, ship3.Orbit.Position + offset4, ship3.Orbit.Velocity + thrust2, Instance.SolarSystem.CurrentTime, areValuesRelative: false);
				return Ok("hitme: corridor module inbound.");
			}
			case "setmain":
			{
				if (parts.Length != 1)
				{
					return Error("setmain: expected no arguments.");
				}
				if (parent is not SpaceObjectVessel objectVessel)
				{
					return Error("setmain: you are not inside a vessel.");
				}

				objectVessel.SetMainVessel();
				return Ok("setmain: " + objectVessel.VesselName + " is now the main vessel.");
			}
			case "pressurize":
			{
				if (parts.Length != 1)
				{
					return Error("pressurize: expected no arguments.");
				}
				if (parent is not SpaceObjectVessel || player.CurrentRoom is null)
				{
					return Error("pressurize: you are not inside a room.");
				}

				player.CurrentRoom.CompoundRoom.AirPressure = 1f;
				player.CurrentRoom.CompoundRoom.AirQuality = 1f;
				return Ok("pressurize: room pressurized.");
			}
			case "airquality":
			{
				if (parts.Length != 2 || !float.TryParse(parts[1], out var aq))
				{
					return Error("airquality: expected <0-1>.");
				}
				if (parent is not SpaceObjectVessel || player.CurrentRoom is null)
				{
					return Error("airquality: you are not inside a room.");
				}

				player.CurrentRoom.CompoundRoom.AirQuality = aq;
				return Ok("airquality: set to " + aq + ".");
			}
			case "vent":
			{
				if (parts.Length != 1)
				{
					return Error("vent: expected no arguments.");
				}
				if (parent is not SpaceObjectVessel || player.CurrentRoom is null)
				{
					return Error("vent: you are not inside a room.");
				}

				player.CurrentRoom.CompoundRoom.AirPressure = 0f;
				return Ok("vent: room depressurized.");
			}
			case "torpedo":
			{
				if (parts.Length > 2)
				{
					return Error("torpedo: expected [seconds].");
				}

				Vector3D direction = (player.Rotation * QuaternionD.Euler(0f - player.MouseLook, 0.0, 0.0) * Vector3D.Forward).Normalized;
				SpaceObject launcher = parent is SpaceObjectVessel vessel7 ? vessel7.MainVessel : player.Parent;
				Vector3D offset2 = player.Position - launcher.Position + direction * 25.0;
				Ship ship = await Ship.CreateNewShip(GameScenes.SceneId.AltCorp_DockableContainer, "PHTORP MK4", -1L, new List<long> { launcher.Guid }, null, offset2, null, null, checkPosition: false);
				ship.Rotation = QuaternionD.LookRotation(direction);
				Vector3D thrust = direction * 80.0;
				ship.Orbit.InitFromStateVectors(ship.Orbit.Parent, ship.Orbit.Position, ship.Orbit.Velocity + thrust, Instance.SolarSystem.CurrentTime, areValuesRelative: false);
				await ship.SetHealthAsync(5f);
				ship.VesselName = "Torpedo";
				ship.Mass = 5000000.0;
				ship.MaxHealth = 50000f;
				if (parts.Length == 2)
				{
					if (!float.TryParse(parts[1], out var time))
					{
						return Error("torpedo: expected [seconds].");
					}
					ship.SelfDestructTimer = new SelfDestructTimer(ship, time);
				}
				else
				{
					ship.SelfDestructTimer = new SelfDestructTimer(ship, 60f);
				}
				return Ok("torpedo: launched.");
			}
			case "countships":
			case "countitems":
			{
				if (parts.Length is not (1 or 2))
				{
					return Error(parts[0] + ": expected [range in meters|celestial body].");
				}

				double radius = 2000;
				string msg = "";
				List<SpaceObjectVessel> vessels;
				if (parts.Length == 1 || (parts.Length == 2 && double.TryParse(parts[1], out radius)))
				{
					vessels = [.. SolarSystem.GetVesselsInRange(parent.Position, radius, parent.Guid)];
				}
				else
				{
					CelestialBodyData cbd = StaticData.SolarSystem.CelestialBodies.FirstOrDefault((CelestialBodyData m) => m.Name.ToLower() == parts[1].ToLower());
					if (cbd == null)
					{
						cbd = StaticData.SolarSystem.CelestialBodies.FirstOrDefault((CelestialBodyData m) => m.Name.Contains(parts[1], StringComparison.CurrentCultureIgnoreCase));
						if (cbd == null)
						{
							return Error(parts[0] + ": '" + parts[1] + "' is neither a range nor a celestial body.");
						}
					}
					CelestialBody cb = SolarSystem.GetCelestialBodies().FirstOrDefault((CelestialBody m) => m.GUID == cbd.GUID);
					radius = cb.Orbit.GravityInfluenceRadius;
					vessels = [.. from m in SolarSystem.GetNearbySpaceObjects(cb.Position, radius) where m is SpaceObjectVessel vessel && vessel.Orbit.Parent.CelestialBody == cb select m as SpaceObjectVessel];
					msg = cbd.Name + ", r=" + (int)(radius / 1000.0) + "km";
				}
				Dictionary<string, int> count = [];
				if (parts[0] == "countships")
				{
					foreach (SpaceObjectVessel ves2 in vessels)
					{
						string name2 = ves2.SceneId.ToString();
						if (!count.ContainsKey(name2))
						{
							count[name2] = 0;
						}
						count[name2]++;
					}
				}
				else if (parts[0] == "countitems")
				{
					foreach (SpaceObjectVessel ves in vessels)
					{
						foreach (DynamicObject dobj in ItemsInSpaceObject(ves))
						{
							if (dobj.Item != null)
							{
								string name = dobj.Item.TypeName;
								count[name] = count.GetValueOrDefault(name) + 1;
							}
						}
					}
				}
				foreach (KeyValuePair<string, int> kv in count)
				{
					msg = msg + (msg == "" ? "" : "\r\n") + kv.Key + ": " + kv.Value;
				}
				return Ok(msg == "" ? parts[0] + ": nothing found." : msg);
			}
			case "station":
			{
				if (parts.Length != 2)
				{
					return Error("station: expected <name of a json in Data/Stations>.");
				}

				var vessels = await StationBlueprint.AssembleStation(parts[1], "JsonStation", "JsonStation", null, parent.Guid);
				vessels[0].MainVessel.AddPlayerToCrew(player);
				return Ok("station: assembled '" + parts[1] + "'.");
			}
			case "collision":
			{
				if (parts.Length != 2)
				{
					return Error("collision: expected <0|1>.");
				}
				if (parent is not SpaceObjectVessel vessel5)
				{
					return Error("collision: you are not inside a vessel.");
				}

				CollisionFlags flags = parts[1] == "0" ? CollisionFlags.NoContactResponse : CollisionFlags.None;
				vessel5.MainVessel.RigidBody.CollisionFlags = flags;
				foreach (SpaceObjectVessel v3 in vessel5.MainVessel.AllDockedVessels)
				{
					v3.RigidBody.CollisionFlags = flags;
				}
				return Ok("collision: " + (parts[1] == "0" ? "disabled" : "enabled") + ".");
			}
			case "god":
			{
				if (parts.Length > 2)
				{
					return Error("god: expected [0|1].");
				}
				if (parts.Length == 2)
				{
					player.GodMode = parts[1] != "0";
				}

				// The client keys its god mode toggle on this exact text, so don't change.
				return Ok("God mode: " + (player.GodMode ? "ON" : "OFF"));
			}
			case "teleport":
			{
				if (parts.Length != 2)
				{
					return Error("teleport: expected <player name|vessel name>.");
				}

				ArtificialBody target = null;
				Player p = _players.Values.FirstOrDefault((Player m) => m.PlayerId == parts[1] || m.Name.ToLower() == parts[1].ToLower());
				if (p is { Parent: ArtificialBody body })
				{
					target = body is not SpaceObjectVessel vessel ? body : vessel.MainVessel;
				}
				else
				{
					SpaceObjectVessel v2 = (from m in SolarSystem.GetArtificialBodies()
						where m is SpaceObjectVessel
						select m as SpaceObjectVessel).FirstOrDefault((SpaceObjectVessel m) => m.FullName.Replace(' ', '_').Contains(parts[1], StringComparison.CurrentCultureIgnoreCase));
					if (v2 != null)
					{
						target = v2.MainVessel;
					}
				}

				if (target is null)
				{
					return Error("teleport: no player or vessel matches '" + parts[1] + "'.");
				}

				ArtificialBody myAb = parent is SpaceObjectVessel spaceObjectVessel ? spaceObjectVessel.MainVessel : parent as ArtificialBody;
				if (target == myAb)
				{
					return Error("teleport: you are already there.");
				}

				await myAb.DisableStabilization(disableForChildren: true, updateBeforeDisable: true);
				myAb.Orbit.CopyDataFrom(target.Orbit, SolarSystem.CurrentTime, exactCopy: true);
				if (myAb is Pivot)
				{
					myAb.Orbit.RelativePosition -= player.LocalPosition + player.LocalRotation * Vector3D.Forward * (target.Radius + 100.0);
					myAb.Orbit.RelativeVelocity -= player.LocalVelocity;
				}
				else
				{
					myAb.Orbit.RelativePosition -= myAb.Rotation * Vector3D.Forward * (target.Radius + 100.0);
				}
				myAb.Orbit.InitFromCurrentStateVectors(SolarSystem.CurrentTime);
				myAb.Orbit.UpdateOrbit();
				return Ok("teleport: moved to " + (target is SpaceObjectVessel targetVessel ? targetVessel.FullName : parts[1]) + ".");
			}
			case "respawn":
			{
				if (parts.Length != 2)
				{
					return Error("respawn: expected <spawn rule name>.");
				}

				if (await SpawnManager.RespawnBlueprintRule(parts[1]))
				{
					return Ok("respawn: respawned '" + parts[1] + "'.");
				}

				return Error("respawn: invalid spawn rule.");
			}
			case "sethealth":
			{
				if (parts.Length != 2 || !float.TryParse(parts[1], out var health))
				{
					return Error("sethealth: expected <health>.");
				}
				if (player.PlayerInventory.HandsSlot.Item != null)
				{
					player.PlayerInventory.HandsSlot.Item.Health = health;
					return Ok("sethealth: held item set to " + health + ".");
				}
				if (parent is not SpaceObjectVessel parentVessel)
				{
					return Error("sethealth: nothing in hands and you are not inside a vessel.");
				}

				foreach (VesselRepairPoint repairPoint in parentVessel.RepairPoints)
				{
					await repairPoint.SetHealthAsync(repairPoint.MaxHealth);
				}
				await parentVessel.SetHealthAsync(health);
				parentVessel.MainVessel.UpdateVesselData();
				return Ok("sethealth: " + parentVessel.VesselName + " set to " + health + ".");
			}
			case "spawnrulescleanup":
			{
				int destroyed = 0;
				ArtificialBody[] artificialBodies2 = SolarSystem.GetArtificialBodies();
				foreach (ArtificialBody ab in artificialBodies2)
				{
					if (ab is not SpaceObjectVessel vessel || (!vessel.IsInvulnerable && !vessel.IsPartOfSpawnSystem && vessel is not Asteroid))
					{
						continue;
					}
					bool found = false;
					foreach (SpawnRule sr2 in SpawnManager.spawnRules)
					{
						if (sr2.SpawnedVessels.FirstOrDefault((SpaceObjectVessel m) => m.Guid == ab.Guid) != null)
						{
							found = true;
							break;
						}
					}
					if (!found)
					{
						vessel.MarkForDestruction = true;
						SpawnManager.SpawnedVessels.TryRemove(vessel.Guid, out _);
						destroyed++;
					}
				}
				return Ok("spawnrulescleanup: marked " + destroyed + " vessels for destruction.");
			}
			case "unmatchall":
			{
				ArtificialBody[] artificialBodies3 = SolarSystem.GetArtificialBodies();
				foreach (ArtificialBody ab2 in artificialBodies3)
				{
					await ab2.DisableStabilization(disableForChildren: true, updateBeforeDisable: true);
				}
				return Ok("unmatchall: stabilisation disabled on " + artificialBodies3.Length + " bodies.");
			}
			case "resetblueprints":
			{
				foreach (Player pl in _players.Values)
				{
					pl.Blueprints = ObjectCopier.DeepCopy(StaticData.DefaultBlueprints);
					await NetworkController.SendAsync(pl.Guid, new UpdateBlueprintsMessage
					{
						Blueprints = pl.Blueprints
					});
				}
				return Ok("resetblueprints: reset for " + _players.Count + " players.");
			}
			case "blink":
			{
				if (parts.Length != 2 || !double.TryParse(parts[1], out var dist))
				{
					return Error("blink: expected <distance in km>.");
				}
				if (parent is not SpaceObjectVessel vessel3)
				{
					return Error("blink: you are not inside a vessel.");
				}

				await vessel3.MainVessel.DisableStabilization(disableForChildren: false, updateBeforeDisable: true);
				vessel3.MainVessel.Orbit.RelativePosition += vessel3.Rotation * Vector3D.Forward * dist * 1000.0;
				vessel3.MainVessel.Orbit.InitFromCurrentStateVectors(SolarSystemTime);
				vessel3.MainVessel.Orbit.UpdateOrbit();
				return Ok("blink: moved " + dist + "km forward.");
			}
			case "whereami":
			{
				if (parts.Length != 1)
				{
					return Error("whereami: expected no arguments.");
				}
				string response = "At coordinates '" + player.Position + "', ";
				if (parent is SpaceObjectVessel vessel)
				{
					response += "inside vessel '" + vessel.VesselName + "' near " + (CelestialBodyGUID)vessel.Orbit.Parent.CelestialBody.GUID;
					foreach (SpawnRule sr in SpawnManager.spawnRules)
					{
						if (sr.SpawnedVessels.FirstOrDefault((SpaceObjectVessel m) => m == vessel) != null)
						{
							response += ", spawn rule '" + sr.Name + "'";
						}
					}
					return Ok(response);
				}
				else if (parent is Pivot pivot)
				{
					response += "near " + (CelestialBodyGUID)pivot.Orbit.Parent.CelestialBody.GUID;
					return Ok(response);
				}

				Debug.Assert(parent != null);
				Debug.Assert(parent is not SpaceObjectTransferable);
				return Error("whereami: you are neither inside a vessel nor at a pivot.");

			}
			case "restock":
			{
				if (parts.Length != 1)
				{
					return Error("restock: expected no arguments.");
				}
				if (parent is not SpaceObjectVessel vessel)
				{
					return Error("restock: you are not inside a vessel.");
				}

				List<SpaceObjectVessel> list = [vessel.MainVessel];
				{
					foreach (SpaceObjectVessel vessel2 in list.Concat(vessel.MainVessel.AllDockedVessels))
					{
						foreach (VesselAttachPoint ap in vessel2.AttachPoints.Values.Where((VesselAttachPoint m) => m.Item == null))
						{
							ItemType it = ap.ItemTypes.OrderBy((ItemType m) => MathHelper.RandomNextDouble()).FirstOrDefault();
							GenericItemSubType gt = GenericItemSubType.None;
							MachineryPartType mt = MachineryPartType.None;
							if (it == ItemType.None)
							{
								gt = ap.GenericSubTypes.OrderBy((GenericItemSubType m) => MathHelper.RandomNextDouble()).FirstOrDefault();
								if (gt == GenericItemSubType.None)
								{
									mt = ap.MachineryPartTypes.OrderBy((MachineryPartType m) => MathHelper.RandomNextDouble()).FirstOrDefault();
									if (mt == MachineryPartType.None)
									{
										continue;
									}
									it = ItemType.MachineryPart;
								}
								else
								{
									it = ItemType.GenericItem;
								}
							}
							await DynamicObject.SpawnDynamicObject(it, gt, mt, vessel2, ap.InSceneID, null, null, null, MathHelper.RandomRange(1, 5), null, refill: true);
							if (mt != 0)
							{
								vessel2.MainDistributionManager.GetVesselComponentByPartSlot(ap.Item.AttachPointID)?.FitPartToSlot(ap.Item.AttachPointID, (MachineryPart)ap.Item);
							}
						}
					}
				}
				return Ok("restock: attach points refilled.");
			}
			default:
			{
				return Error("Unknown command '" + parts[0] + "'. Type 'help' for a list.");
			}
			}
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			return Error(ex.Message);
		}
	}

	public TextChatMessage SendSystemMessage(SystemMessagesTypes type, Ship sh)
	{
		TextChatMessage tcm = new TextChatMessage
		{
			GUID = -1L,
			Name = "System",
			MessageType = type,
			MessageText = ""
		};
		switch (type)
		{
		case SystemMessagesTypes.DoomedOutpostSpawned:
			if (sh.Orbit.Parent is { CelestialBody: not null })
			{
				TimeSpan t = new TimeSpan(0, 0, (int)sh.TimeToLive);
				tcm.MessageParam = new string[3]
				{
					sh.VesselRegistration,
					((CelestialBodyGUID)sh.Orbit.Parent.CelestialBody.GUID).ToString(),
					t.ToString()
				};
			}
			break;
		case SystemMessagesTypes.RestartServerTime:
		{
					string timeLeftToRestart = !(_timeToRestart <= 10.0) ? (_timeToRestart / 60.0).ToString() : _timeToRestart.ToString();
					tcm.MessageParam = new string[2]
			{
				timeLeftToRestart,
				_timeToRestart <= 10.0 ? "seconds" : "minutes"
			};
			break;
		}
		}
		tcm.Local = false;
		return tcm;
	}

	private async Task<NetworkData> PlayerSpawnRequestListener(NetworkData data)
	{
		try
		{
			var request = data as PlayerSpawnRequest;
			Player pl = GetPlayer(request.Sender);
			if (pl == null)
			{
				Debug.LogError("Player spawn request error, player is null", request.Sender);
				return new PlayerSpawnResponse() { Status = NetworkData.MessageStatus.Failure };
			}

			pl.MessagesReceivedWhileLoading = new ConcurrentQueue<ShipStatsMessage>();
			var spawnResponse = new PlayerSpawnResponse();

			bool spawnSuccess;
			if (pl.IsAlive)
			{
				spawnSuccess = pl.Parent is ArtificialBody;
			}
			else
			{
				spawnSuccess = await AddPlayerToShipAsync(pl, request.SpawnSetupType, request.SpawnPointParentId);
				Debug.LogInfoFormat("Adding player ({2}) to ship with id {0}, with setup type {1}. Success? {3}.", request.SpawnPointParentId, request.SpawnSetupType, pl.Guid, spawnSuccess);
			}

			if (spawnSuccess)
			{
				spawnResponse.Status = NetworkData.MessageStatus.Success;
				if (pl.Parent is Pivot { StabilizeToTargetObj: not null } pivot)
				{
					await pivot.DisableStabilization(disableForChildren: false, updateBeforeDisable: true);
				}
				if (pl.Parent != null && !pl.IsSubscribedTo(pl.Parent.Guid))
				{
					pl.SubscribeTo(pl.Parent);
				}

				ArtificialBody parentBody = pl.Parent as ArtificialBody;

				spawnResponse.ParentGuid = parentBody.Guid;

				ArtificialBody mainVessel = parentBody is SpaceObjectVessel parentVessel ? parentVessel.MainVessel : parentBody;
				pl.KnownView = SolarSystem.GetPlayerView(pl, mainVessel, SpaceObjects.QueryRadius<ArtificialBody>(pl.Position, SolarSystem.ViewRadius));
				spawnResponse.AllNearbySpaceObjects = [.. pl.KnownView];

				if (pl.CurrentSpawnPoint != null
					&& ((pl.CurrentSpawnPoint.IsPlayerInSpawnPoint && pl.CurrentSpawnPoint.Ship == pl.Parent)
					|| (pl.CurrentSpawnPoint.Type == SpawnPointType.SimpleSpawn && pl.CurrentSpawnPoint.Executor == null && !pl.IsAlive)))
				{
					spawnResponse.SpawnPointId = pl.CurrentSpawnPoint.SpawnPointID;
				}

				if (pl.AuthorizedSpawnPoint != null)
				{
					spawnResponse.HomeGuid = pl.AuthorizedSpawnPoint.Ship.MainVessel.Guid;
				}

				if (ServerRestartTimeSec > 0.0)
				{
					spawnResponse.TimeUntilServerRestart = (RestartTime - DateTime.UtcNow).TotalSeconds;
				}

				spawnResponse.Quests = [.. pl.Quests.Select((Quest m) => m.GetDetails())];
				spawnResponse.Blueprints = pl.Blueprints;
				spawnResponse.Health = (int)pl.Health;
				spawnResponse.IsAdmin = pl.IsAdmin;
				pl.UpdateAnchor();
				Vector3D anchorPosition = TryGetSpaceObject(pl.AnchorGuid, out SpaceObject anchor)
					? anchor.Position
					: pl.Position;

				spawnResponse.AnchorGuid = pl.AnchorGuid;
				spawnResponse.OriginWorldPosition = anchorPosition.ToArray();
				spawnResponse.Position = pl.LocalPosition.ToFloatArray();
				spawnResponse.Rotation = pl.LocalRotation.ToFloatArray();
			}
			else
			{
				spawnResponse.Status = NetworkData.MessageStatus.Failure;
			}
			return spawnResponse;
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			return new PlayerSpawnResponse() { Status = NetworkData.MessageStatus.Failure };
		}
	}

	private Task<NetworkData> ObjectsInfoRequestListener(NetworkData data)
	{
		var request = data as ObjectsInfoRequest;
		var response = new ObjectsInfoResponse();
		Player pl = GetPlayer(request.Sender);
		if (request.Guids == null || request.Guids.Length == 0 || pl == null)
		{
			Debug.LogWarning("Got empty objects info request.");
			return Task.FromResult<NetworkData>(response);
		}

		List<ObjectsInfoResponse.ShipData> ships = [];
		List<ObjectsInfoResponse.AsteroidData> asteroids = [];
		List<ObjectsInfoResponse.PivotData> pivots = [];
		List<DynamicObject> dynamicObjects = [];
		List<ObjectsInfoResponse.CorpseData> corpses = [];
		List<ObjectsInfoResponse.PlayerData> players = [];

		Vector3D anchorPosition = TryGetSpaceObject(pl.AnchorGuid, out SpaceObject playerAnchor)
			? playerAnchor.Position
			: pl.Position;

		foreach (long guid in request.Guids)
		{
			switch (GetSpaceObject(guid))
			{
				case Ship ship when ship.IsMainVessel:
					ships.Add(ship.GetShipData(anchorPosition));
					break;
				case Asteroid asteroid when asteroid.IsMainVessel:
					asteroids.Add(asteroid.GetAsteroidData(anchorPosition));
					break;
				case Pivot pivot:
				{
					pivots.Add(new ObjectsInfoResponse.PivotData
					{
						Guid = pivot.Guid,
						Position = (pivot.Position - anchorPosition).ToFloatArray(),
						Rotation = pivot.Rotation.ToFloatArray(),
						PivotType = pivot.ObjectType,
					});
					switch (pivot.Child)
					{
						case DynamicObject dynamicObjectChild:
							dynamicObjects.Add(dynamicObjectChild);
							break;
						case Corpse corpseChild:
							corpses.Add(corpseChild.GetCorpseData(pl));
							break;
						case Player playerChild when playerChild != pl:
							players.Add(playerChild.GetPlayerData(pl));
							break;
					}
					break;
				}
				case DynamicObject dynamicObject when dynamicObject.IsVisibleTo(pl):
					dynamicObjects.Add(dynamicObject);
					break;
				case Corpse corpse:
					corpses.Add(corpse.GetCorpseData(pl));
					break;
				case Player player when player != pl:
					players.Add(player.GetPlayerData(pl));
					break;
			}
		}

		response.ShipObjects = [.. ships];
		response.AsteroidObjects = [.. asteroids];
		response.PivotObjects = [.. pivots];
		response.DynamicObjects = [.. dynamicObjects.OrderBy(m =>
		{
			// Make sure parents are loaded before their children.
			// TODO: Make state handle this by itself.
			int depth = 0;
			for (SpaceObject holder = m.Parent; holder is DynamicObject; holder = holder.Parent)
			{
				depth++;
			}
			return depth;
		}).ThenBy(m => m.InvSlotID == InventorySlot.OutfitSlotID ? 0 : 1).Select(m => m.GetDetails())];
		response.CorpseObjects = [.. corpses];
		response.Players = [.. players];
		return Task.FromResult<NetworkData>(response);
	}

	private Task<NetworkData> MapDataRequestListener(NetworkData data)
	{
		var response = new MapDataResponse { Objects = [] };
		Player pl = GetPlayer((data as MapDataRequest).Sender);
		if (pl == null)
		{
			return Task.FromResult<NetworkData>(response);
		}

		long parentGuid = (pl.Parent as SpaceObjectVessel)?.MainVessel.Guid ?? 0L;
		List<MapDataResponse.MapDetailsData> mapDetails = [];
		foreach (SpaceObjectVessel vessel in AllVessels)
		{
			if (!vessel.IsMainVessel || vessel.IsDebrisFragment)
			{
				continue;
			}

			bool visible = vessel.IsAlwaysVisible || vessel.IsDistressSignalActive
				|| vessel.Guid == parentGuid || pl.DiscoveredVessels.Contains(vessel.Guid);
			if (!visible)
			{
				continue;
			}

			OrbitData orbit = new();
			vessel.Orbit.FillOrbitData(ref orbit, vessel);

			mapDetails.Add(new MapDataResponse.MapDetailsData
			{
				Guid = vessel.Guid,
				Type = vessel is Asteroid ? SpaceObjectType.Asteroid : SpaceObjectType.Ship,
				Name = vessel.VesselName,
				Registration = vessel.VesselRegistration,
				SpawnRuleId = vessel.SpawnRuleId,
				Orbit = orbit,
				RadarSignature = vessel.GetCompoundRadarSignature(),
				IsAlwaysVisible = vessel.IsAlwaysVisible,
				IsDistressSignalActive = vessel.IsDistressSignalActive,
			});
		}

		response.Objects = [.. mapDetails];
		return Task.FromResult<NetworkData>(response);
	}

	private async Task UpdateDynamicObjectsRespawnTimers(double deltaTime)
	{
		List<DynamicObjectsRespawn> toRemove = new List<DynamicObjectsRespawn>();
		foreach (DynamicObjectsRespawn dos2 in DynamicObjectsRespawnList)
		{
			if (dos2.Timer > 0.0)
			{
				dos2.Timer -= deltaTime;
			}
			else
			{
				toRemove.Add(dos2);
			}
		}
		if (toRemove.Count <= 0)
		{
			return;
		}
		foreach (DynamicObjectsRespawn dos in toRemove)
		{
			if (dos.Parent is SpaceObjectVessel && dos.ApDetails != null && SolarSystem.State.ItemInSlot(dos.Parent.Guid, LocationKind.AttachPoint, (short)dos.ApDetails.InSceneID).IsValid)
			{
				dos.Timer = dos.RespawnTime;
				continue;
			}
			DynamicObjectsRespawnList.Remove(dos);
			if (_vessels.ContainsKey(dos.Parent.Guid))
			{
				DynamicObject dobj = await DynamicObject.CreateDynamicObjectAsync(dos.Data, dos.Parent, -1L);
				if (dos.Data.AttachPointInSceneId > 0 && dobj.Item != null)
				{
					(dos.Parent as SpaceObjectVessel).AttachItem(dobj.Item, (short)dos.ApDetails.InSceneID);
				}
				dobj.APDetails = dos.ApDetails;
				dobj.RespawnTime = dos.Data.SpawnSettings.Length != 0 ? dos.Data.SpawnSettings[0].RespawnTime : -1f;
				if (dobj.Item is not null && dos.MaxHealth >= 0f && dos.MinHealth >= 0f)
				{
					IDamageable idmg = dobj.Item;
					idmg.Health = (int)(idmg.MaxHealth * MathHelper.Clamp(MathHelper.RandomRange(dos.MinHealth, dos.MaxHealth), 0f, 1f));
				}
			}
		}
		toRemove.Clear();
	}

	private async Task UpdateData(double deltaTime)
	{
		await NetworkController.Tick();

		SolarSystem.UpdateTime(deltaTime);

		await SolarSystem.UpdatePositions();

		foreach (Player player in AllPlayers)
		{
			await player.ApplyPendingMoveRequests();
			player.UpdateAnchor();
		}

		PhysicsController.Update();

		await UpdateDynamicObjectsRespawnTimers(deltaTime);

		await UpdateObjectTimers(deltaTime);

		await UpdatePlayerInvitationTimers(deltaTime);

		// Tick ships over several updates instead of one singular tick.
		_shipSystemSweepTime += deltaTime;
		int sweepTarget = (int)(_shipSystemSweep.Length * _shipSystemSweepTime / ShipSystemSweepSeconds);
		while (_shipSystemCursor < sweepTarget && _shipSystemCursor < _shipSystemSweep.Length)
		{
			SpaceObjectVessel vessel = _shipSystemSweep[_shipSystemCursor++];
			if (vessel is null || !_vessels.ContainsKey(vessel.Guid))
			{
				continue;
			}
			vessel.RefreshSunlightExposure();
			foreach (SpaceObjectVessel dockedVessel in vessel.AllDockedVessels)
			{
				dockedVessel.RefreshSunlightExposure();
			}
			await vessel.UpdateVesselSystems();
			vessel.DecayGraceTimer = MathHelper.Clamp(vessel.DecayGraceTimer - _shipSystemSweepSeconds, 0.0, double.MaxValue);
			if (vessel.DecayGraceTimer <= double.Epsilon)
			{
				await vessel.ChangeHealthBy(
					(float)((0f - vessel.ExposureDamage) * VesselDecayRateMultiplier * _shipSystemSweepSeconds), null,
					VesselRepairPoint.Priority.Internal, force: false, VesselDamageType.Decay, _shipSystemSweepSeconds);
			}
		}
		if (_shipSystemCursor >= _shipSystemSweep.Length && _shipSystemSweepTime >= ShipSystemSweepSeconds)
		{
			foreach (DebrisField debrisField in DebrisFields)
			{
				await debrisField.CheckVessels(_shipSystemSweepTime);
			}
			_shipSystemSweepSeconds = _shipSystemSweepTime;
			_shipSystemSweep = [.. _vessels.Values];
			_shipSystemCursor = 0;
			_shipSystemSweepTime = 0.0;
		}

		_pilotStateTimer += deltaTime;
		if (_pilotStateTimer >= PilotStateSendInterval)
		{
			_pilotStateTimer = _pilotStateTimer > PilotStateSendInterval * 2.0
				? 0.0
				: _pilotStateTimer - PilotStateSendInterval;
			Player[] pilots = [.. AllPlayers.Where((Player m) => m.EnvironmentReady && m.IsAlive && m.IsPilotingVessel)];
			if (pilots.Length > 0)
			{
				await Parallel.ForEachAsync(pilots, async (pl, ct) => await SolarSystem.SendPilotStateMessageToPlayer(pl)).WaitAsync(new TimeSpan(0, 0, 10));
			}
		}

		_movementMessageTimer += deltaTime;
		if (_movementMessageTimer >= MovementMessageSendInterval)
		{
			_movementMessageTimer = _movementMessageTimer > MovementMessageSendInterval * 2.0
				? 0.0
				: _movementMessageTimer - MovementMessageSendInterval;
			Player[] players = [.. AllPlayers.Where((Player m) => m.EnvironmentReady && m.IsAlive)];
			if (players.Length > 0)
			{
				await Parallel.ForEachAsync(players, async (pl, ct) => await SolarSystem.SendMovementMessageToPlayer(pl)).WaitAsync(new TimeSpan(0, 0, 10));
			}
			await SolarSystem.SendStateMessages(players);
#if DEBUG
			if (SolarSystem.State.ValidateInvariants() is { } broken)
			{
				Debug.LogError("Solar system state is inconsistent", broken);
			}
#endif
		}

		if (!VesselsDataUpdate.IsEmpty)
		{
			await NetworkController.SendToAllAsync(new UpdateVesselDataMessage
			{
				VesselsDataUpdate = VesselsDataUpdate.Values.ToList()
			});
			VesselsDataUpdate.Clear();
		}
	}

	public async Task DestroyArtificialBody(ArtificialBody ab, bool destroyChildren = true, bool vesselExploded = false)
	{
		if (ab == null)
		{
			return;
		}
		if (ab is SpaceObjectVessel ves)
		{
			if (destroyChildren)
			{
				foreach (SpaceObjectVessel child in ves.AllDockedVessels.ToArray())
				{
					await DestroyArtificialBody(child, destroyChildren: false, vesselExploded);
				}
			}
			foreach (Player pl in ves.VesselCrew.ToArray())
			{
				await pl.KillPlayer(HurtType.Shipwreck, createCorpse: false);
			}
			if (vesselExploded)
			{
				await ves.DamageVesselsInExplosionRadius();
				await NetworkController.SendToClientsSubscribedTo(new DestroyVesselMessage
				{
					GUID = ves.Guid
				}, -1L, ves);
			}
			await ves.Destroy();
		}
		else
		{
			await ab.Destroy();
		}
	}

	public async Task UpdateObjectTimers(double deltaTime)
	{
		HashSet<SpaceObjectVessel> destroyVessels = null;
		foreach (SpaceObjectVessel vessel in AllVessels)
		{
			await vessel.UpdateTimers(deltaTime);
			if (vessel is not Ship || !(vessel.Health < float.Epsilon))
			{
				continue;
			}
			if (vessel.DockedToVessel == null && vessel.DockedVessels.Count == 0)
			{
				if (destroyVessels == null)
				{
					destroyVessels = new HashSet<SpaceObjectVessel>();
				}
				destroyVessels.Add(vessel.DockedToMainVessel is not null ? vessel.DockedToMainVessel : vessel);
			}
			else
			{
				vessel.UndockAll();
			}
		}
		if (destroyVessels != null)
		{
			foreach (SpaceObjectVessel dv in destroyVessels)
			{
				await DestroyArtificialBody(dv, destroyChildren: true, vesselExploded: true);
			}
		}
		foreach (DynamicObject dobj in _updateableDynamicObjects.Values)
		{
			await (dobj.Item as IUpdateable).Update(deltaTime);
		}
		foreach (Player pl in AllPlayers)
		{
			await pl.UpdateTimers(deltaTime);
		}
		foreach (DebrisField df in DebrisFields)
		{
			df.SpawnFragments();
		}
	}

	private Task PrintObjectsDebug(double time)
	{
		Debug.LogInfo("Server stats, objects", SpaceObjects.Count, "players", _players.Count, "vessels", _vessels.Count, "artificial bodies", SolarSystem.ArtificialBodiesCount);
		return Task.CompletedTask;
	}

	public async Task MainLoop()
	{
		SolarSystem.InitializeData();
		InitializeDebrisFields();
		if (_cleanStart || PersistenceSaveInterval < 0.0 || (_cleanStart = !await Persistence.Load(_loadPersistenceFromFile)))
		{
			if (_solarSystemStartTime < 0.0)
			{
				SolarSystem.CalculatePositionsAfterTime(MathHelper.RandomRange(86400.0, 5256000.0));
			}
			else
			{
				SolarSystem.CalculatePositionsAfterTime(_solarSystemStartTime);
			}

			if (!WorldInitialized)
			{
				await SpawnManager.Initialize();
				WorldInitialized = true;
			}
		}
		else
		{
			WorldInitialized = true;
		}
		await Start();
		NetworkController.Start();
		long tickPeriod = Stopwatch.Frequency / System.Math.Max(_numberOfTicks, 1L);
		Volatile.Write(ref _lastTickTimestamp, Stopwatch.GetTimestamp());
		if (ServerRestartTimeSec > 0.0)
		{
			RestartTime = DateTime.UtcNow.AddSeconds(ServerRestartTimeSec);
			SubscribeToTimer(UpdateTimer.TimerStep.Step_1_0_sec, ServerAutoRestartTimer);
		}
		DoomedShipController.SubscribeToTimer();
		DateTime lastOverrunLogTime = DateTime.MinValue;
		if (_printDebugObjects)
		{
			SubscribeToTimer(UpdateTimer.TimerStep.Step_1_0_hr, PrintObjectsDebug);
		}
		_mainLoopStarted = true;
		new Thread(StartMainLoopWatcher).Start();
		long tickDeadline = Stopwatch.GetTimestamp() + tickPeriod;
		while (IsRunning)
		{
			long now = Stopwatch.GetTimestamp();
			if (now < tickDeadline)
			{
				await Task.Delay(TimeSpan.FromSeconds((double)(tickDeadline - now) / Stopwatch.Frequency));
				continue;
			}

			await AddRemovePlayers();
			DeltaTime = (double)(now - Volatile.Read(ref _lastTickTimestamp)) / Stopwatch.Frequency;
			Volatile.Write(ref _lastTickTimestamp, now);

			await UpdateData(DeltaTime);

			foreach (KeyValuePair<UpdateTimer.TimerStep, UpdateTimer> timer in _timers)
			{
				try
				{
					await timer.Value.AddTime(DeltaTime);
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}
			}

			await SpawnManager.UpdateTimers(DeltaTime);

			long tickDuration = Stopwatch.GetTimestamp() - now;
			if (tickDuration > tickPeriod && (DateTime.UtcNow - lastOverrunLogTime).TotalSeconds > 60.0)
			{
				Debug.LogWarningFormat("Tick overran its budget. Tick ms {0:F1}. Budget ms {1:F2}. Objects {2}. Players {3}. Vessels {4}. Artificial bodies {5}.", tickDuration * 1000.0 / Stopwatch.Frequency, tickPeriod * 1000.0 / Stopwatch.Frequency, SpaceObjects.Count, _players.Count, _vessels.Count, SolarSystem.ArtificialBodiesCount);
				lastOverrunLogTime = DateTime.UtcNow;
			}

			if (PersistenceSaveInterval > 0.0 || _manualSave)
			{
				_persistenceSaveTimer += DeltaTime;
				if (_persistenceSaveTimer >= PersistenceSaveInterval || _manualSave)
				{
					_persistenceSaveTimer = 0.0;
					Persistence.Save(_manualSaveFileName, _manualSaveAuxData);
				}
				_manualSave = false;
				_manualSaveFileName = null;
				_manualSaveAuxData = null;
			}

			tickDeadline += tickPeriod;
			if (tickDeadline < Stopwatch.GetTimestamp())
			{
				tickDeadline = Stopwatch.GetTimestamp() + tickPeriod;
			}
		}

		// Shutting down...
		Debug.Log("Main game loop ended; Shutting down server...");

		NetworkController.Stop();

		if (SavePersistenceDataOnShutdown)
		{
			Persistence.Save();
		}

		MainLoopEnded.Set();
		if (ParentProcess.FileName == "GameServerWatchdog.exe" && !ParentProcess.GetParentProcess().HasExited)
		{
			Restart = false;
		}

		if (Restart)
		{
			RestartServer(CleanRestart);
		}
		else if (!OfflineMode)
		{
			try
			{
				await SocialServerConnection.Send(new UnregisterServer()
				{
					GamePort = GamePort,
					StatusPort = StatusPort,
					ServerId = NetworkController.ServerId
				});
			}
			catch
			{
				// Ignored
			}
		}
	}

	/// <summary>
	/// 	Watches how long it has been since the last tick.
	/// </summary>
	private void StartMainLoopWatcher()
	{
		double warnedAtSeconds = 0.0;
		while (IsRunning)
		{
			double stalledSeconds = (double)(Stopwatch.GetTimestamp() - Volatile.Read(ref _lastTickTimestamp)) / Stopwatch.Frequency;
			if (stalledSeconds > 5.0)
			{
				if (stalledSeconds > warnedAtSeconds * 2.0)
				{
					Debug.LogWarning($"Main loop has not completed a tick for {stalledSeconds:F1} sec.");
					warnedAtSeconds = stalledSeconds;
				}
			}
			else
			{
				warnedAtSeconds = 0.0;
			}
			Thread.Sleep(1000);
		}
	}

	private async Task AddRemovePlayers()
	{
		while (_playersToAdd.TryTake(out Player player))
		{
			_players[player.Guid] = player;
			SpaceObjects.TryAdd(player.FakeGuid, player);
		}

		while (_charactersToDelete.TryDequeue(out string playerId))
		{
			Player player = GetPlayerFromPlayerId(playerId);
			if (player is not null && !NetworkController.IsPlayerConnected(player.Guid))
			{
				await player.Destroy();
			}
		}

		while (_playersToRemove.TryTake(out Player player))
		{
			_players.TryRemove(player.Guid, out _);
			SpaceObjects.TryRemove(player.FakeGuid, out _);
		}
	}

	public static void RestartServer(bool clean)
	{
		string fileName = "";
		string arguments = "";
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		foreach (string arg in commandLineArgs)
		{
			if (fileName == "")
			{
				fileName = arg;
			}
			else if (arg.ToLower() != "-noload" && arg.ToLower() != "-clean")
			{
				arguments = arguments + arg + " ";
			}
		}
		if (clean)
		{
			arguments += " -noload";
		}
		Process.Start(fileName, arguments);
		Process.GetCurrentProcess().Kill();
	}

	public void SubscribeToTimer(UpdateTimer.TimerStep step, UpdateTimer.TimeStepDelegate del)
	{
		UpdateTimer timer = _timers.GetOrAdd(step, static s => new UpdateTimer(s));
		UpdateTimer.TimeStepDelegate current;
		do
		{
			current = timer.OnTick;
		}
		while (Interlocked.CompareExchange(ref timer.OnTick, (UpdateTimer.TimeStepDelegate)Delegate.Combine(current, del), current) != current);
	}

	public void UnsubscribeFromTimer(UpdateTimer.TimerStep step, UpdateTimer.TimeStepDelegate del)
	{
		if (!_timers.TryGetValue(step, out UpdateTimer timer))
		{
			return;
		}
		UpdateTimer.TimeStepDelegate current;
		do
		{
			current = timer.OnTick;
		}
		while (Interlocked.CompareExchange(ref timer.OnTick, (UpdateTimer.TimeStepDelegate)Delegate.Remove(current, del), current) != current);
	}

	public async void PlayersOnServerRequestListener(NetworkData data)
	{
		var req = data as PlayersOnServerRequest;
		SpaceObjectVessel ves = null;
		if (req.SpawnPointID != null)
		{
			ves = GetVessel(req.SpawnPointID.VesselGUID);
		}
		else if (req.SecuritySystemID != null)
		{
			ves = GetVessel(req.SecuritySystemID.VesselGUID);
		}
		if (ves == null)
		{
			return;
		}

		// Create request.
		PlayersOnServerResponse res = new PlayersOnServerResponse();
		if (req.SpawnPointID != null)
		{
			// If we are spawning on a spawnpoint.
			if (ves.SpawnPoints.Find(m => m.SpawnPointID == req.SpawnPointID.InSceneID) == null)
			{
				return;
			}
			res.SpawnPointID = new VesselObjectID(req.SpawnPointID.VesselGUID, req.SpawnPointID.InSceneID);

			res.PlayersOnServer = new List<PlayerOnServerData>();
			foreach (Player pl in NetworkController.GetAllConnectedPlayers())
			{
				if (!pl.PlayerId.IsNullOrEmpty())
				{
					res.PlayersOnServer.Add(new PlayerOnServerData
					{
						PlayerId = pl.PlayerId,
						Name = pl.Name,
						AlreadyHasInvite = _spawnPointInvites.ContainsKey(pl.PlayerId)
					});
				}
				if (pl.PlayerId.IsNullOrEmpty())
				{
					Debug.LogError("Player ID is null or empty", pl.Guid, pl.Name);
				}
			}
		}
		else
		{
			// If we are not spawning on a spawnpoint.
			if (req.SecuritySystemID == null)
			{
				return;
			}
			res.SecuritySystemID = new VesselObjectID(req.SecuritySystemID.VesselGUID, 0);

			res.PlayersOnServer = new List<PlayerOnServerData>();
			foreach (Player pl in NetworkController.GetAllConnectedPlayers())
			{
				if (!pl.PlayerId.IsNullOrEmpty())
				{
					res.PlayersOnServer.Add(new PlayerOnServerData
					{
						PlayerId = pl.PlayerId,
						Name = pl.Name,
						AlreadyHasInvite = false
					});
				}
				if (pl.PlayerId.IsNullOrEmpty())
				{
					Debug.LogError("Player ID is null or empty", pl.Guid, pl.Name);
				}
			}
		}

		await NetworkController.SendAsync(req.Sender, res);
	}

	public Task<NetworkData> AvailableSpawnPointsRequestListener(NetworkData data)
	{
		var request = data as AvailableSpawnPointsRequest;
		Player pl = GetPlayer(request.Sender);
		if (pl != null)
		{
			return Task.FromResult<NetworkData>(new AvailableSpawnPointsResponse
			{
				SpawnPoints = GetAvailableSpawnPoints(pl)
			});
		}

		return Task.FromResult<NetworkData>(null);
	}

	public void ServerShutDownMessageListener(NetworkData data)
	{
	}

	private async void NameTagMessageListener(NetworkData data)
	{
		var message = data as NameTagMessage;
		SpaceObjectVessel vessel = GetVessel(message.ID.VesselGUID);
		await NetworkController.SendToClientsSubscribedTo(message, -1L, vessel);
		try
		{
			vessel.NameTags.Find((NameTagData m) => m.InSceneID == message.ID.InSceneID).NameTagText = message.NameTagText;
		}
		catch
		{
		}
	}

	public bool IsAddressAutorized(string address)
	{
		try
		{
			if (address is "127.0.0.1" or "localhost")
			{
				return true;
			}
			byte[] bytes = System.Net.IPAddress.Parse(address).GetAddressBytes();
			Array.Reverse(bytes);
			uint addr = BitConverter.ToUInt32(bytes, 0);
			IpAddressRange[] adminIpAddressRanges = _adminIpAddressRanges;
			foreach (IpAddressRange range in adminIpAddressRanges)
			{
				byte[] sBytes = System.Net.IPAddress.Parse(range.StartAddress).GetAddressBytes();
				Array.Reverse(sBytes);
				byte[] eBytes = System.Net.IPAddress.Parse(range.EndAddress).GetAddressBytes();
				Array.Reverse(eBytes);
				uint start = BitConverter.ToUInt32(sBytes, 0);
				uint end = BitConverter.ToUInt32(eBytes, 0);
				if (addr >= start && addr <= end)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	public async Task UpdatePlayerInvitationTimers(double deltaTime)
	{
		if (_spawnPointInvites.Count == 0)
		{
			return;
		}
		List<string> removedKeys = null;
		foreach (KeyValuePair<string, SpawnPointInviteData> item in _spawnPointInvites)
		{
			item.Value.InviteTimer -= deltaTime;
			if (item.Value.InviteTimer <= 0.0)
			{
				if (removedKeys == null)
				{
					removedKeys = new List<string>();
				}
				removedKeys.Add(item.Key);
			}
		}
		if (removedKeys is not { Count: > 0 })
		{
			return;
		}
		foreach (string key in removedKeys)
		{
			await ClearSpawnPointInvitation(key);
		}
	}

	public async Task ClearSpawnPointInvitation(string playerId)
	{
		if (_spawnPointInvites.ContainsKey(playerId))
		{
			await _spawnPointInvites[playerId].SpawnPoint.SetInvitation("", "", true);
			_spawnPointInvites.Remove(playerId);
		}
	}

	public async Task CreateSpawnPointInvitation(ShipSpawnPoint sp, string playerId, string playerName)
	{
		_spawnPointInvites.Add(playerId, new SpawnPointInviteData
		{
			SpawnPoint = sp,
			InviteTimer = SpawnPointInviteTimer
		});
		await sp.SetInvitation(playerId, playerName, true);
	}

	public async Task<bool> PlayerInviteChanged(ShipSpawnPoint sp, string invitedPlayerId, string invitedPlayerName, Player sender)
	{
		if (!invitedPlayerId.IsNullOrEmpty())
		{
			if (_spawnPointInvites.ContainsKey(invitedPlayerId) && _spawnPointInvites[invitedPlayerId].SpawnPoint == sp && sp.InvitedPlayerId == invitedPlayerId)
			{
				return false;
			}
			if (_spawnPointInvites.ContainsKey(invitedPlayerId))
			{
				await ClearSpawnPointInvitation(invitedPlayerId);
			}
			if (!sp.InvitedPlayerId.IsNullOrEmpty() && _spawnPointInvites.ContainsKey(sp.InvitedPlayerId))
			{
				await ClearSpawnPointInvitation(sp.InvitedPlayerId);
			}
			await CreateSpawnPointInvitation(sp, invitedPlayerId, invitedPlayerName);
			return true;
		}
		if (!sp.InvitedPlayerId.IsNullOrEmpty())
		{
			if (_spawnPointInvites.ContainsKey(sp.InvitedPlayerId))
			{
				await ClearSpawnPointInvitation(sp.InvitedPlayerId);
			}
			else
			{
				await sp.SetInvitation("", "", sendMessage: true);
			}
			return true;
		}
		return false;
	}

	private async Task ServerAutoRestartTimer(double time)
	{
		DateTime currentTime = DateTime.UtcNow;
		if (currentTime.AddSeconds(_timeToRestart) > RestartTime)
		{
			if ((RestartTime - currentTime).TotalSeconds >= _timeToRestart - 2.0)
			{
				await NetworkController.SendToAllAsync(SendSystemMessage(SystemMessagesTypes.RestartServerTime, null), -1L);
			}
			if (_timeToRestart == 1800.0)
			{
				_timeToRestart = 900.0;
			}
			else if (_timeToRestart == 900.0)
			{
				_timeToRestart = 300.0;
			}
			else if (_timeToRestart == 300.0)
			{
				_timeToRestart = 60.0;
			}
			else if (_timeToRestart == 60.0)
			{
				_timeToRestart = 10.0;
			}
			else if (_timeToRestart <= 10.0)
			{
				_timeToRestart -= 1.0;
			}
			else
			{
				_timeToRestart -= 300.0;
			}
		}
		if (currentTime > RestartTime)
		{
			IsRunning = false;
			Restart = true;
			SavePersistenceDataOnShutdown = true;
			RestartTime = currentTime.AddSeconds(ServerRestartTimeSec);
		}
	}


	public List<DebrisFieldDetails> GetDebrisFieldsDetails()
	{
		List<DebrisFieldDetails> list = new List<DebrisFieldDetails>();
		foreach (DebrisField df in DebrisFields)
		{
			list.Add(df.GetDetails());
		}
		return list;
	}

	private void InitializeDebrisFields()
	{
		Debug.Log("Initialising debris fields...");
		string dir = !ConfigDir.IsNullOrEmpty() && Directory.Exists(ConfigDir + "Data") ? ConfigDir : "";
		List<DebrisField.DebrisFieldData> list = JsonSerialiser.Load<List<DebrisField.DebrisFieldData>>(dir + "Data/DebrisFields.json");
		foreach (DebrisField.DebrisFieldData dfd in list)
		{
			if (dfd.DamageVesselChance > 0f && dfd.Damage.Max >= dfd.Damage.Min && dfd.Damage.Max > 0f)
			{
				DebrisFields.Add(new DebrisField(dfd));
			}
		}
	}

	private async void ExplosionMessageListener(NetworkData data)
	{
		var message = data as ExplosionMessage;
		Item item = GetItem(message.ItemGUID);
		if (item == null)
		{
			return;
		}
		HashSet<long> affectedObjects = new HashSet<long>();
		if (message.AffectedGUIDs != null && message.AffectedGUIDs.Length != 0)
		{
			Vector3D itemPos = item.DynamicObj.Position;
			long[] affectedGuiDs = message.AffectedGUIDs;
			foreach (long affectedGuid in affectedGuiDs)
			{
				SpaceObject tmpSp = Instance.GetSpaceObject(affectedGuid);
				if ((tmpSp.Position - item.DynamicObj.Position).Magnitude < item.ExplosionRadius * 1.5f && affectedObjects.Add(tmpSp.Guid))
				{
					if (tmpSp is Player player)
					{
						await player.TakeDamage(HurtType.Explosion, item.ExplosionDamage, "item-explosion");
					}
					if (tmpSp is DynamicObject { Item: not null } dynamicObject)
					{
						await dynamicObject.Item.TakeDamage(new Dictionary<TypeOfDamage, float> { { item.ExplosionDamageType, item.ExplosionDamage } });
					}
				}
			}
		}
		if (item.ExplosionDamageType == TypeOfDamage.Impact && item.DynamicObj.Parent is SpaceObjectVessel parentVessel && affectedObjects.Add(parentVessel.Guid))
		{
			List<VesselRepairPoint> list = null;
			if (message.RepairPointIDs != null)
			{
				list = new List<VesselRepairPoint>();
				VesselObjectID[] repairPointIDs = message.RepairPointIDs;
				foreach (VesselObjectID rpid in repairPointIDs)
				{
					SpaceObjectVessel vessel = Instance.GetVessel(rpid.VesselGUID);
					if (vessel != null)
					{
						list.Add(parentVessel.RepairPoints.Find((VesselRepairPoint m) => m.ID.InSceneID == rpid.InSceneID));
					}
				}
			}
			if (await parentVessel.ChangeHealthBy(0f - item.ExplosionDamage, list, VesselRepairPoint.Priority.None, force: false, VesselDamageType.GrenadeExplosion) != 0f)
			{
				ShipCollisionMessage scm = new ShipCollisionMessage
				{
					CollisionVelocity = 0f,
					ShipOne = parentVessel.Guid,
					ShipTwo = -1L
				};
				await NetworkController.SendToClientsSubscribedTo(scm, -1L, parentVessel);
			}
		}

		item.DestroyAfter(1.0);
	}
}
