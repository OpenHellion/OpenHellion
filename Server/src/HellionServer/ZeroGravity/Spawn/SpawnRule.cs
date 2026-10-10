using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Objects;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.Spawn;

public class SpawnRule
{
	public string Name;

	public string StationName;

	public string StationBlueprint;

	public SpawnRuleOrbit Orbit;

	public SpawnRuleLocationType LocationType;

	public string LocationTag;

	public double RespawnTimerSec;

	public bool ForceRespawn;

	public double CheckPlayersDistance;

	public float? AsteroidResourcesMultiplier;

	public SpawnRange<float>[] AngularVelocity;

	public bool DoNotRemoveVesselsFromSpawnSystem;

	public SpawnRange<int> NumberOfClusters;

	public List<SpawnRuleScene> ScenePool;

	public List<SpawnRuleLoot> LootPool;

	public bool IsVisibleOnRadar;

	public double CurrTimerSec = 0.0;

	public List<SpaceObjectVessel> SpawnedVessels = [];

	private int maxScenesPerCluster;

	private SpawnRange<int> totalScenes = default;

	private Dictionary<int, int> clusterVesselsCount = [];

	private bool vesselsRemoved;

	public int NumberOfClustersCurr => clusterVesselsCount.Count;

	public bool IsOneTimeSpawnRule => LocationType is SpawnRuleLocationType.StartingScene or SpawnRuleLocationType.Quest;

	private void removeVesselFromCluster(int cluster)
	{
		if (clusterVesselsCount.ContainsKey(cluster))
		{
			clusterVesselsCount[cluster]--;
			if (clusterVesselsCount[cluster] <= 0)
			{
				clusterVesselsCount.Remove(cluster);
			}
		}
	}

	public bool AddVesselToRule(SpaceObjectVessel ves, SpawnRuleScene pool, int cluster)
	{
		totalScenes.Min++;
		pool.Count--;
		if (pool.Count < 0)
		{
			pool.Count = 0;
		}
		if (!clusterVesselsCount.ContainsKey(cluster))
		{
			clusterVesselsCount.Add(cluster, 0);
		}
		clusterVesselsCount[cluster]++;
		SpawnedVessels.Add(ves);
		return true;
	}

	public bool AddDynamicObjectToRule(DynamicObject dobj, SpawnRuleLoot pool)
	{
		pool.Count--;
		if (pool.Count < 0)
		{
			pool.Count = 0;
		}
		return true;
	}

	public async Task Initialize(bool isPersistenceInitialize)
	{
		totalScenes.Max = 0;
		if (ScenePool is { Count: > 0 })
		{
			foreach (SpawnRuleScene sc in ScenePool)
			{
				totalScenes.Max += sc.CountMax;
			}
		}
		if (totalScenes.Max > 0 && totalScenes.Max < NumberOfClusters.Max)
		{
			Debug.LogWarning($"SPAWN MANAGER - Spawn rule \"{Name}\" max number of clusters \"{NumberOfClusters.Max}\" are lower than number of total scenes \"{totalScenes.Max}\", clusters auto adjusted");
			NumberOfClusters.Max = totalScenes.Max;
		}
		if (totalScenes.Max > 0 && totalScenes.Max < NumberOfClusters.Min)
		{
			Debug.LogWarning($"SPAWN MANAGER - Spawn rule \"{Name}\" min number of clusters \"{NumberOfClusters.Min}\" are lower than number of total scenes \"{totalScenes.Max}\", clusters auto adjusted");
			NumberOfClusters.Min = totalScenes.Max;
		}
		if (NumberOfClusters.Min > NumberOfClusters.Max)
		{
			NumberOfClusters.Min = NumberOfClusters.Max;
		}
		if (NumberOfClusters.Max == 0)
		{
			maxScenesPerCluster = 0;
		}
		else
		{
			maxScenesPerCluster = System.Math.Max(totalScenes.Max / NumberOfClusters.Max, 1);
		}
		if (isPersistenceInitialize)
		{
			return;
		}
		if (LocationType == SpawnRuleLocationType.Random)
		{
			if (NumberOfClustersCurr < NumberOfClusters.Min)
			{
				for (int i = 0; i < NumberOfClusters.Min; i++)
				{
					await ExecuteRule();
				}
			}
		}
		else if (!IsOneTimeSpawnRule)
		{
			await ExecuteRule();
		}
	}

	private Vector3D FindEmptyRelativePosition(SpaceObjectVessel ves, ref List<SpaceObjectVessel> spawnedVessels)
	{
		SpaceObjectVessel first = spawnedVessels[0];
		int sanityCheck = 0;
		int numOfTries = 0;
		int distnceMultiplier = 1;
		while (sanityCheck < 200)
		{
			Vector3D pos = first.Orbit.RelativePosition + MathHelper.RandomRotation() * (Vector3D.Forward * (ves.Radius + first.Radius + SpawnManager.Settings.RandomLocationClusterItemCheckDistance * distnceMultiplier));
			bool positionClear = true;
			for (int i = 1; i < spawnedVessels.Count; i++)
			{
				if (spawnedVessels[i].Orbit.RelativePosition.DistanceSquared(pos) < System.Math.Pow(ves.Radius + first.RadarSignature + SpawnManager.Settings.RandomLocationClusterItemCheckDistance, 2.0))
				{
					positionClear = false;
					break;
				}
			}
			if (positionClear)
			{
				return pos - first.Orbit.RelativePosition;
			}
			sanityCheck++;
			numOfTries++;
			if (numOfTries == 5)
			{
				numOfTries = 0;
				distnceMultiplier = sanityCheck >= 100 ? distnceMultiplier + 5 : distnceMultiplier + 1;
			}
		}
		return Vector3D.Zero;
	}

	private async Task<SpaceObjectVessel> ExecuteRandomRule()
	{
		if (LocationType != SpawnRuleLocationType.Random)
		{
			return null;
		}
		bool createdNewCluster = false;
		if (NumberOfClustersCurr < NumberOfClusters.Max)
		{
			int scenesToSpawnTotal = 1;
			if (totalScenes.Max > NumberOfClusters.Max)
			{
				scenesToSpawnTotal = MathHelper.Clamp((totalScenes.Max - totalScenes.Min) / (NumberOfClusters.Max - NumberOfClustersCurr), 1, maxScenesPerCluster);
			}
			int nextClusterIndex = 0;
			if (clusterVesselsCount.Count > 0)
			{
				for (int i = 0; i < NumberOfClusters.Max; i++)
				{
					if (!clusterVesselsCount.Keys.Contains(i))
					{
						nextClusterIndex = i;
						break;
					}
				}
			}
			SpaceObjectVessel firstVessel = null;
			List<SpaceObjectVessel> clusterVessels = new List<SpaceObjectVessel>();
			bool forceSpawn = false;
			for (int j = 0; j < ScenePool.Count; j++)
			{
				if (scenesToSpawnTotal <= 0)
				{
					break;
				}
				if (ScenePool[j].Count == 0)
				{
					if (j == ScenePool.Count - 1 && scenesToSpawnTotal > 0)
					{
						forceSpawn = true;
						j = -1;
					}
					continue;
				}
				SpawnRuleScene sc = ScenePool[j];
				int scenesToSpawn = !forceSpawn && scenesToSpawnTotal == 1 ? MathHelper.RandomRange(0, 2) : forceSpawn ? 1 : sc.CountMax < NumberOfClusters.Max ? MathHelper.RandomRange(0, 2) : MathHelper.RandomRange(1, sc.CountMax / NumberOfClusters.Max + 1);
				if (scenesToSpawn > sc.Count)
				{
					scenesToSpawn = sc.Count;
				}
				if (scenesToSpawn > scenesToSpawnTotal)
				{
					scenesToSpawn = scenesToSpawnTotal;
				}
				if (scenesToSpawn > 0)
				{
					int startIndex = 0;
					createdNewCluster = true;
					if (firstVessel == null)
					{
						firstVessel = await SpaceObjectVessel.CreateNew(sc.SceneID, "", -1L, null, null, null, null, MathHelper.RandomRotation(), LocationTag, checkPosition: true, spawnRuleOrbit: Orbit, artificialBodyDistanceCheck: SpawnManager.Settings.RandomLocationCheckDistance, AsteroidResourcesMultiplier: AsteroidResourcesMultiplier.HasValue ? AsteroidResourcesMultiplier.Value : 1f);
						if (firstVessel is Ship ship && IsVisibleOnRadar)
						{
							ship.IsAlwaysVisible = true;
						}
						SpawnRange<float>[] angularVelocity = AngularVelocity;
						if (angularVelocity is { Length: 3 })
						{
							firstVessel.AngularVelocity = new Vector3D(MathHelper.RandomRange(AngularVelocity[0].Min, AngularVelocity[0].Max), MathHelper.RandomRange(AngularVelocity[1].Min, AngularVelocity[1].Max), MathHelper.RandomRange(AngularVelocity[2].Min, AngularVelocity[2].Max));
						}
						firstVessel.IsPartOfSpawnSystem = true;
						clusterVessels.Add(firstVessel);
						SpawnManager.SpawnedVessels.TryAdd(firstVessel.Guid, new Tuple<SpawnRule, SpawnRuleScene, int>(this, sc, nextClusterIndex));
						await firstVessel.SetHealthAsync(MathHelper.RandomRange(firstVessel.MaxHealth * sc.HealthMin, firstVessel.MaxHealth * sc.HealthMax));
						sc.Count--;
						totalScenes.Min++;
						scenesToSpawnTotal--;
						startIndex = 1;
					}
					for (int k = startIndex; k < scenesToSpawn; k++)
					{
						SpaceObjectVessel currVessel = await SpaceObjectVessel.CreateNew(sc.SceneID, "", -1L, localRotation: MathHelper.RandomRotation(), vesselTag: LocationTag, nearArtificialBodyGUIDs: new List<long> { firstVessel.Guid }, celestialBodyGUIDs: null, positionOffset: null, velocityAtPosition: null, checkPosition: false, AsteroidResourcesMultiplier: AsteroidResourcesMultiplier.HasValue ? AsteroidResourcesMultiplier.Value : 1f);
						Vector3D relativePos = FindEmptyRelativePosition(currVessel, ref clusterVessels);
						if (relativePos.IsEpsilonZero())
						{
							Debug.LogError("SPAWN MANAGER - Failed to find empty spawn position for rule", Name);
							await Server.Instance.DestroyArtificialBody(currVessel);
							continue;
						}
						currVessel.Orbit.RelativePosition = firstVessel.Orbit.RelativePosition + relativePos;
						SpawnRange<float>[] angularVelocity2 = AngularVelocity;
						if (angularVelocity2 is { Length: 3 })
						{
							currVessel.AngularVelocity = new Vector3D(MathHelper.RandomRange(AngularVelocity[0].Min, AngularVelocity[0].Max), MathHelper.RandomRange(AngularVelocity[1].Min, AngularVelocity[1].Max), MathHelper.RandomRange(AngularVelocity[2].Min, AngularVelocity[2].Max));
						}
						currVessel.StabilizeToTarget(firstVessel, forceStabilize: true);
						currVessel.IsPartOfSpawnSystem = true;
						clusterVessels.Add(currVessel);
						SpawnManager.SpawnedVessels.TryAdd(currVessel.Guid, new Tuple<SpawnRule, SpawnRuleScene, int>(this, sc, nextClusterIndex));
						await currVessel.SetHealthAsync(MathHelper.RandomRange(currVessel.MaxHealth * sc.HealthMin, currVessel.MaxHealth * sc.HealthMax));
						sc.Count--;
						totalScenes.Min++;
						scenesToSpawnTotal--;
					}
				}
				if (scenesToSpawnTotal <= 0)
				{
					break;
				}
				if (j == ScenePool.Count - 1 && scenesToSpawnTotal > 0)
				{
					forceSpawn = true;
					j = -1;
				}
			}
			if (createdNewCluster)
			{
				SpawnedVessels.AddRange(clusterVessels);
				clusterVesselsCount.Add(nextClusterIndex, clusterVessels.Count);
				await DistributeLoot(clusterVessels);
			}
			return firstVessel;
		}
		if (!createdNewCluster)
		{
			await DistributeLoot(SpawnedVessels);
		}
		return null;
	}

	public async Task<SpaceObjectVessel> ExecuteQuestRule(QuestTrigger questTrigger)
	{
		try
		{
			List<SpaceObjectVessel> mainVessels = await Data.StationBlueprint.AssembleStation(StationBlueprint, StationName, LocationTag, Orbit, null, AsteroidResourcesMultiplier);
			if (mainVessels != null)
			{
				AuthorizedPerson ap = new AuthorizedPerson
				{
					Name = questTrigger.Quest.Player.Name,
					Rank = AuthorizedPersonRank.Crewman,
					PlayerId = questTrigger.Quest.Player.PlayerId
				};
				QuestTrigger.QuestTriggerID qtid = questTrigger.GetQuestTriggerID();
				List<SpaceObjectVessel> vessels = mainVessels.SelectMany((SpaceObjectVessel m) => m.AllVessels).ToList();
				foreach (SpaceObjectVessel vessel in vessels)
				{
					if (vessel.AuthorizedPersonel.FirstOrDefault((AuthorizedPerson m) => m.PlayerId == questTrigger.Quest.Player.PlayerId) == null)
					{
						vessel.AuthorizedPersonel.Add(ap);
					}
					vessel.QuestTriggerID = qtid;
				}
				await questTrigger.Quest.Player.SendAuthorizedVesselsResponse();
				await DistributeLoot(vessels);
			}
			return mainVessels[0];
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			Debug.LogError($"SPAWN MANAGER - Spawn rule \"{Name}\" location type \"{LocationType}\" is not valid");
		}
		return null;
	}

	private async Task<SpaceObjectVessel> ExecuteBlueprintRule(bool force = false)
	{
		if (!force)
		{
			List<SpaceObjectVessel> liveVessels = SpawnedVessels.Where((SpaceObjectVessel m) => m is Asteroid || m.Health > 0f).ToList();
			foreach (Player pl in Server.Instance.AllPlayers)
			{
				if (liveVessels.Contains(pl.CurrentSpawnPoint?.Ship) || liveVessels.Contains(pl.AuthorizedSpawnPoint?.Ship))
				{
					return null;
				}
			}
			if (CheckPlayersDistance > 0.0)
			{
				foreach (SpaceObjectVessel ves in liveVessels)
				{
					if (ves.GetNearestPlayer(out double dist) != null && dist < CheckPlayersDistance)
					{
						return null;
					}
				}
			}
		}
		if (vesselsRemoved || ForceRespawn || force)
		{
			foreach (SpaceObjectVessel ves2 in new List<SpaceObjectVessel>(SpawnedVessels.Where((SpaceObjectVessel m) => m is Asteroid || (m.Health > 0f && m.IsMainVessel))))
			{
				await Server.Instance.DestroyArtificialBody(ves2);
			}
			SpawnedVessels.Clear();
		}
		else if (SpawnedVessels.Count > 0)
		{
			await DistributeLoot(SpawnedVessels);
			return null;
		}
		try
		{
			List<SpaceObjectVessel> mainVessels = await Data.StationBlueprint.AssembleStation(StationBlueprint, StationName, LocationTag, Orbit, null, AsteroidResourcesMultiplier);
			int count = 0;
			foreach (SpaceObjectVessel mainVessel in mainVessels)
			{
				mainVessel.IsPrefabStationVessel = true;
				mainVessel.IsAlwaysVisible = IsVisibleOnRadar;
				mainVessel.IsPartOfSpawnSystem = true;
				mainVessel.SpawnRuleId = (GetHashCode() << 10) + count++;
				SpawnedVessels.Add(mainVessel);
				SpawnManager.SpawnedVessels.TryAdd(mainVessel.Guid, new Tuple<SpawnRule, SpawnRuleScene, int>(this, null, 0));
				foreach (SpaceObjectVessel vessel in mainVessel.AllDockedVessels)
				{
					vessel.IsPrefabStationVessel = true;
					vessel.IsAlwaysVisible = IsVisibleOnRadar;
					vessel.IsPartOfSpawnSystem = true;
					SpawnedVessels.Add(vessel);
					SpawnManager.SpawnedVessels.TryAdd(vessel.Guid, new Tuple<SpawnRule, SpawnRuleScene, int>(this, null, 0));
				}
			}
			await DistributeLoot(SpawnedVessels);
			vesselsRemoved = false;
			return mainVessels[0];
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			Debug.LogError($"SPAWN MANAGER - Spawn rule \"{Name}\" location type \"{LocationType}\" is not valid");
		}
		return null;
	}

	private async Task<SpaceObjectVessel> ExecuteStaringRule()
	{
		if (LocationType != SpawnRuleLocationType.StartingScene)
		{
			return null;
		}
		long startingSetId = GUIDFactory.NextLongRandom(1L, long.MaxValue);
		List<SpaceObjectVessel> mainVessels = await Data.StationBlueprint.AssembleStation(StationBlueprint, StationName, LocationTag, Orbit, null, 1f);
		List<SpaceObjectVessel> vessels = mainVessels.SelectMany((SpaceObjectVessel m) => m.AllVessels).ToList();
		foreach (SpaceObjectVessel mainVessel in vessels)
		{
			mainVessel.StartingSetId = startingSetId;
		}
		await DistributeLoot(vessels);
		return mainVessels[0];
	}

	public Task<SpaceObjectVessel> ExecuteRule(bool force = false)
	{
		return LocationType switch
		{
			SpawnRuleLocationType.Random => ExecuteRandomRule(),
			SpawnRuleLocationType.StartingScene => ExecuteStaringRule(),
			SpawnRuleLocationType.Station => ExecuteBlueprintRule(force),
			_ => null,
		};
	}

	public async Task DistributeLoot(List<SpaceObjectVessel> vessels)
	{
		int lootToSpawn = 0;
		List<SpawnRuleLoot> removeLoot = new List<SpawnRuleLoot>();
		IEnumerable<SpawnRuleLoot> enumerable = LootPool.OrderBy((SpawnRuleLoot m) => GetDynamicObjectData(m.Data)?.DefaultAuxData.Slots?.Count).Reverse();
		foreach (SpawnRuleLoot loot in enumerable)
		{
			if (loot.Count <= 0)
			{
				continue;
			}
			if (LocationType == SpawnRuleLocationType.Random)
			{
				int lootPerCluster = loot.CountMax / NumberOfClusters.Max;
				lootToSpawn = lootPerCluster;
				if (lootToSpawn > loot.Count)
				{
					lootToSpawn = loot.Count;
				}
			}
			else
			{
				lootToSpawn = loot.Count;
			}
			int spawned = 0;
			while (spawned < lootToSpawn)
			{
				IItemSlot isl = SpawnManager.GetItemSlot(loot.Data, ref vessels);
				if (isl == null)
				{
					break;
				}
				try
				{
					if (await SpawnManager.SpawnDynamicObject(this, loot, isl))
					{
						spawned++;
					}
				}
				catch (Exception e)
				{
					Debug.LogWarning(e.Message);
					removeLoot.Add(loot);
					lootToSpawn--;
				}
			}
			if (!IsOneTimeSpawnRule)
			{
				loot.Count -= spawned;
			}
		}
		foreach (SpawnRuleLoot rem in removeLoot)
		{
			LootPool.Remove(rem);
		}
		foreach (Asteroid ast in vessels.Where((SpaceObjectVessel m) => m is Asteroid).Cast<Asteroid>())
		{
			double dist;
			Player pl = ast.GetNearestPlayer(out dist);
			if (pl != null && dist < CheckPlayersDistance)
			{
				continue;
			}
			foreach (AsteroidMiningPoint amp in ast.MiningPoints.Values)
			{
				amp.Quantity = amp.MaxQuantity;
			}
		}
	}

	private DynamicObjectData GetDynamicObjectData(LootItemData data)
	{
		DynamicObjectData dod = null;
		if (data.Type == ItemType.GenericItem)
		{
			dod = StaticData.DynamicObjectsDataList.Values.FirstOrDefault((DynamicObjectData m) => m.ItemType == data.Type && (m.DefaultAuxData as GenericItemData).SubType == data.GenericSubType);
		}
		if (data.Type == ItemType.MachineryPart)
		{
			return StaticData.DynamicObjectsDataList.Values.FirstOrDefault((DynamicObjectData m) => m.ItemType == data.Type && (m.DefaultAuxData as MachineryPartData).PartType == data.PartType);
		}
		return StaticData.DynamicObjectsDataList.Values.FirstOrDefault((DynamicObjectData m) => m.ItemType == data.Type);
	}

	public bool RemoveDynamicObject(DynamicObject dobj, SpawnRuleLoot loot)
	{
		loot.Count++;
		return true;
	}

	public bool RemoveSpaceObjectVessel(SpaceObjectVessel ves)
	{
		SpawnedVessels.RemoveAll((SpaceObjectVessel m) => m == ves);
		totalScenes.Min--;
		ves.IsPartOfSpawnSystem = false;
		ves.IsAlwaysVisible = false;
		ves.IsPrefabStationVessel = false;
		ves.IsInvulnerable = false;
		ves.DockingControlsDisabled = false;
		ves.AutoStabilizationDisabled = false;
		ves.UpdateVesselData(vesselRegistration: Server.NameGenerator.GenerateObjectRegistration(ves.ObjectType, ves.Orbit.Parent.CelestialBody, ves.SceneId));
		ves.SpawnRuleId = 0L;
		vesselsRemoved = true;
		return true;
	}

	public bool RemoveSpaceObjectVessel(SpaceObjectVessel ves, SpawnRuleScene srs, int count)
	{
		removeVesselFromCluster(count);
		srs.Count++;
		SpawnedVessels.RemoveAll((SpaceObjectVessel m) => m == ves);
		totalScenes.Min--;
		ves.IsPartOfSpawnSystem = false;
		vesselsRemoved = true;
		return true;
	}
}
