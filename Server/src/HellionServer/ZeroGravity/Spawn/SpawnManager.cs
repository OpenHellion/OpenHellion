using OpenHellion.State;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.Spawn;

public static class SpawnManager
{
	public static class Settings
	{
		public static double StartingLocationCheckDistance = 1000.0;

		public static double RandomLocationCheckDistance = 10000.0;

		public static double RandomLocationClusterItemCheckDistance = 20.0;

		public static bool PrintCategories = false;

		public static bool PrintSpawnRules = false;

		public static bool PrintItemAttachPoints = false;

		public static bool PrintItemTypeIDs = false;

		public static bool PrintInfo => PrintCategories || PrintSpawnRules || PrintItemAttachPoints || PrintItemTypeIDs;
	}

	private static Dictionary<string, Dictionary<LootTier, List<LootItemData>>> lootCategories;

	public static List<SpawnRule> spawnRules;

	private static readonly List<SpawnRule> startingSceneSpawnRules = [];

	private static readonly List<SpawnRule> questSpawnRules = [];

	public static List<SpawnRule> timedSpawnRules = [];

	public static ConcurrentDictionary<long, Tuple<SpawnRule, SpawnRuleScene, int>> SpawnedVessels = new();

	public static ConcurrentDictionary<long, Tuple<SpawnRule, SpawnRuleLoot>> SpawnedDynamicObjects = new();

	private static readonly ConcurrentDictionary<ItemType, Dictionary<int, short>> itemTypeItemID = new();

	private static void LoadData()
	{
		lootCategories = SpawnSerialization.LoadLootData();
		spawnRules = SpawnSerialization.LoadSpawnRuleData();
		for (int i = 0; i < spawnRules.Count; i++)
		{
			if (spawnRules[i].LocationType == SpawnRuleLocationType.StartingScene)
			{
				startingSceneSpawnRules.Add(spawnRules[i]);
			}
			else if (spawnRules[i].LocationType == SpawnRuleLocationType.Quest)
			{
				questSpawnRules.Add(spawnRules[i]);
			}
			else if (spawnRules[i].RespawnTimerSec > double.Epsilon)
			{
				spawnRules[i].CurrTimerSec = spawnRules[i].RespawnTimerSec * MathHelper.RandomRange(0.0, 0.3);
				timedSpawnRules.Add(spawnRules[i]);
			}
		}
		foreach (KeyValuePair<short, DynamicObjectData> val in StaticData.DynamicObjectsDataList)
		{
			if (val.Value.ItemType == ItemType.GenericItem && val.Value.DefaultAuxData is GenericItemData data)
			{
				AddItemTypeItemID(val.Value.ItemType, (int)data.SubType, val.Key);
			}
			else if (val.Value.ItemType == ItemType.MachineryPart && val.Value.DefaultAuxData is MachineryPartData partData)
			{
				AddItemTypeItemID(val.Value.ItemType, (int)partData.PartType, val.Key);
			}
			else if (val.Value.ItemType != ItemType.GenericItem && val.Value.ItemType != ItemType.MachineryPart && val.Value.ItemType != 0)
			{
				AddItemTypeItemID(val.Value.ItemType, 0, val.Key);
			}
		}
	}

	private static void AddItemTypeItemID(ItemType itemType, int itemSubType, short itemID)
	{
		if ((itemType != ItemType.GenericItem && itemType != ItemType.MachineryPart) || itemSubType != 0)
		{
			if (!itemTypeItemID.ContainsKey(itemType))
			{
				itemTypeItemID.TryAdd(itemType, []);
			}
			if (!itemTypeItemID[itemType].ContainsKey(itemSubType))
			{
				itemTypeItemID[itemType].Add(itemSubType, itemID);
			}
		}
	}

	public static IItemSlot GetItemSlot(LootItemData data, ref List<SpaceObjectVessel> vessels)
	{
		if (vessels.Count == 0)
		{
			return null;
		}
		SpawnSerialization.AttachPointPriority priority = data.AttachPointPriority;
		if (priority == SpawnSerialization.AttachPointPriority.Default)
		{
			priority = data.PartType != 0 ? SpawnSerialization.AttachPointPriority.MachineryPartSlot : data.Type == ItemType.PortableTurret ? SpawnSerialization.AttachPointPriority.Active : data.GenericSubType != GenericItemSubType.BrokenArmature && data.GenericSubType != GenericItemSubType.BurnedPDU && data.GenericSubType != GenericItemSubType.DamagedTransmitter && data.GenericSubType != GenericItemSubType.FriedElectronics && data.GenericSubType != GenericItemSubType.RupturedInsulation && data.GenericSubType != GenericItemSubType.ShatteredPlating ? SpawnSerialization.AttachPointPriority.Simple : SpawnSerialization.AttachPointPriority.Scrap;
		}
		if (priority is SpawnSerialization.AttachPointPriority.Item or SpawnSerialization.AttachPointPriority.TransportBox)
		{
			ItemSlot isl2 = Enumerable.OrderBy(keySelector: priority != SpawnSerialization.AttachPointPriority.TransportBox ? (DynamicObject m) => m.ItemType == ItemType.GenericItem && (m.Item as GenericItem).SubType == GenericItemSubType.TransportBox : (Func<DynamicObject, bool>)((DynamicObject m) => m.ItemType != ItemType.GenericItem || (m.Item as GenericItem).SubType != GenericItemSubType.TransportBox), source: vessels.OrderBy((SpaceObjectVessel m) => MathHelper.RandomNextDouble()).SelectMany((SpaceObjectVessel m) => Server.Instance.ItemsInSpaceObject(m))).ThenBy((DynamicObject m) => MathHelper.RandomNextDouble()).SelectMany((DynamicObject m) => m.Item.Slots.Values)
				.FirstOrDefault((ItemSlot m) => m.Item == null && m.CanFitItem(data.Type, data.GenericSubType, data.PartType));
			if (isl2 != null)
			{
				return isl2;
			}
			List<VesselAttachPoint> attachPoints2 = (from m in vessels.SelectMany((SpaceObjectVessel m) => m.AttachPoints.Values)
				where m.Item == null && m.CanSpawnItems && m.CanFitItem(data.Type, data.GenericSubType, data.PartType)
				select m).ToList();
			VesselAttachPoint ap2 = attachPoints2.OrderBy((VesselAttachPoint m) => MathHelper.RandomNextDouble()).FirstOrDefault();
			if (ap2 != null)
			{
				return ap2;
			}
		}
		else
		{
			List<VesselAttachPoint> attachPoints = (from m in vessels.SelectMany((SpaceObjectVessel m) => m.AttachPoints.Values)
				where m.Item == null && m.CanSpawnItems && m.CanFitItem(data.Type, data.GenericSubType, data.PartType)
				select m).ToList();
			Func<VesselAttachPoint, bool> keySelector = (VesselAttachPoint m) => m.Type != AttachPointType.Simple;
			switch (priority)
			{
			case SpawnSerialization.AttachPointPriority.Active:
				keySelector = (VesselAttachPoint m) => m.Type != AttachPointType.Active;
				break;
			case SpawnSerialization.AttachPointPriority.MachineryPartSlot:
				keySelector = (VesselAttachPoint m) => m.Type != AttachPointType.MachineryPartSlot;
				break;
			case SpawnSerialization.AttachPointPriority.Scrap:
				keySelector = (VesselAttachPoint m) => m.Type != AttachPointType.Scrap;
				break;
			}
			VesselAttachPoint ap = attachPoints.OrderBy(keySelector).ThenBy((VesselAttachPoint m) => MathHelper.RandomNextDouble()).FirstOrDefault();
			if (ap != null)
			{
				return ap;
			}
			ItemSlot isl = (from m in vessels.OrderBy((SpaceObjectVessel m) => MathHelper.RandomNextDouble()).SelectMany((SpaceObjectVessel m) => Server.Instance.ItemsInSpaceObject(m))
				orderby MathHelper.RandomNextDouble()
				select m).SelectMany((DynamicObject m) => m.Item.Slots.Values).FirstOrDefault((ItemSlot m) => m.Item == null && m.CanFitItem(data.Type, data.GenericSubType, data.PartType));
			if (isl != null)
			{
				return isl;
			}
		}
		return null;
	}

	private static bool AddResourcesToCargoCompartment(ref CargoCompartmentData compartment, LootItemData.CargoResourceData cargo)
	{
		float quantity = MathHelper.RandomRange(cargo.Quantity.Min, cargo.Quantity.Max);
		if (quantity < float.Epsilon)
		{
			return false;
		}
		ResourceType resType = ResourceType.None;
		List<ResourceType> res = new List<ResourceType>(cargo.Resources);
		while (res.Count > 0)
		{
			resType = res[MathHelper.RandomRange(0, res.Count)];
			res.Remove(resType);
			if (!compartment.AllowedResources.Contains(resType) || (compartment.AllowOnlyOneType && compartment.Resources != null && compartment.Resources.Count != 0 && compartment.Resources[0].ResourceType != resType))
			{
				continue;
			}
			if (compartment.Resources == null)
			{
				compartment.Resources = new List<CargoResourceData>();
			}
			float remainingQuantity = compartment.Capacity;
			CargoResourceData crd = null;
			foreach (CargoResourceData c in compartment.Resources)
			{
				if (c.ResourceType == resType)
				{
					crd = c;
				}
				else
				{
					remainingQuantity -= c.Quantity;
				}
			}
			quantity = MathHelper.Clamp(quantity, 0f, remainingQuantity);
			if (quantity <= float.Epsilon)
			{
				return true;
			}
			if (crd == null)
			{
				compartment.Resources.Add(new CargoResourceData
				{
					ResourceType = resType,
					Quantity = quantity
				});
			}
			else
			{
				crd.Quantity = quantity;
			}
			return true;
		}
		return false;
	}

	private static DynamicObjectAuxData GetDynamicObjectAuxData(short itemID, LootItemData data)
	{
		if (!StaticData.DynamicObjectsDataList.ContainsKey(itemID))
		{
			return null;
		}
		DynamicObjectAuxData defAuxData = ObjectCopier.DeepCopy(StaticData.DynamicObjectsDataList[itemID].DefaultAuxData);
		defAuxData.Tier = data.Tier;
		if (defAuxData is JetpackData dat2)
		{
			dat2.OxygenCompartment.Resources[0].Quantity = 0f;
			dat2.OxygenCompartment.Resources[0].SpawnSettings = null;
			dat2.PropellantCompartment.Resources[0].Quantity = 0f;
			dat2.PropellantCompartment.Resources[0].SpawnSettings = null;
			if (data.Cargo is { Count: > 0 })
			{
				foreach (LootItemData.CargoResourceData cargo3 in data.Cargo)
				{
					if (!AddResourcesToCargoCompartment(ref dat2.OxygenCompartment, cargo3))
					{
						AddResourcesToCargoCompartment(ref dat2.PropellantCompartment, cargo3);
					}
				}
			}
		}
		else if (defAuxData is MagazineData dat8)
		{
			if (data.Count.HasValue)
			{
				dat8.BulletCount = System.Math.Max(MathHelper.RandomRange(data.Count.Value.Min, data.Count.Value.Max), 0);
				if (dat8.MaxBulletCount < dat8.BulletCount)
				{
					dat8.MaxBulletCount = dat8.BulletCount;
				}
			}
		}
		else if (defAuxData is BatteryData dat7)
		{
			if (data.Power.HasValue)
			{
				dat7.CurrentPower = System.Math.Max(MathHelper.RandomRange(data.Power.Value.Min, data.Power.Value.Max), 0f);
				if (dat7.MaxPower < dat7.CurrentPower)
				{
					dat7.MaxPower = dat7.CurrentPower;
				}
			}
		}
		else if (defAuxData is CanisterData dat6)
		{
			if (data.Cargo is { Count: > 0 })
			{
				dat6.CargoCompartment.Resources = null;
				foreach (LootItemData.CargoResourceData cargo2 in data.Cargo)
				{
					AddResourcesToCargoCompartment(ref dat6.CargoCompartment, cargo2);
				}
			}
		}
		else if (defAuxData is RepairToolData dat5)
		{
			if (data.Cargo is { Count: > 0 })
			{
				dat5.FuelCompartment.Resources[0].Quantity = 0f;
				dat5.FuelCompartment.Resources[0].SpawnSettings = null;
				foreach (LootItemData.CargoResourceData cargo in data.Cargo)
				{
					AddResourcesToCargoCompartment(ref dat5.FuelCompartment, cargo);
				}
			}
		}
		else if (defAuxData is GlowStickData dat4)
		{
			if (data.IsActive.HasValue)
			{
				dat4.IsOn = data.IsActive.Value;
			}
		}
		else if (defAuxData is PortableTurretData dat3)
		{
			if (data.IsActive.HasValue)
			{
				dat3.IsActive = data.IsActive.Value;
				dat3.Damage = dat3.Damage;
			}
		}
		else if (defAuxData is GenericItemData dat && data.Look is { Count: > 0 })
		{
			dat.Look = data.Look[MathHelper.RandomRange(0, data.Look.Count)];
		}
		return defAuxData;
	}

	public static async Task<bool> SpawnDynamicObject(SpawnRule rule, SpawnRuleLoot loot, IItemSlot ap)
	{
		if (ap == null)
		{
			Debug.LogWarning("SPAWN MANAGER - Unable to spawn item because atach point is null", loot.Data.Type);
			return false;
		}
		if (!itemTypeItemID.ContainsKey(loot.Data.Type))
		{
			throw new Exception("SPAWN MANAGER - Unable to spawn item because type is unknown, " + loot.Data.Type);
		}
		int subType = loot.Data.GetSubType();
		if (!itemTypeItemID[loot.Data.Type].ContainsKey(subType))
		{
			throw new Exception(string.Concat("SPAWN MANAGER - Unable to spawn item because subtype is unknown, ", loot.Data.Type, ", ", subType));
		}
		short itemID = itemTypeItemID[loot.Data.Type][subType];
		DynamicObjectAuxData auxData = GetDynamicObjectAuxData(itemID, loot.Data);
		if (auxData == null)
		{
			throw new Exception(string.Concat("SPAWN MANAGER - Unable to spawn item because aux data does not exist, ", loot.Data.Type, ", ", subType));
		}
		DynamicObjectSceneData dynamicObjectSceneData = new DynamicObjectSceneData();
		dynamicObjectSceneData.ItemID = itemID;
		dynamicObjectSceneData.Position = Vector3D.Zero.ToFloatArray();
		dynamicObjectSceneData.Forward = Vector3D.Forward.ToFloatArray();
		dynamicObjectSceneData.Up = Vector3D.Up.ToFloatArray();
		dynamicObjectSceneData.AttachPointInSceneId = ap is VesselAttachPoint attachPoint ? attachPoint.InSceneID : 0;
		dynamicObjectSceneData.AuxData = auxData;
		dynamicObjectSceneData.SpawnSettings = null;
		DynamicObjectSceneData sceneData = dynamicObjectSceneData;
		DynamicObject dobj = await DynamicObject.CreateDynamicObjectAsync(sceneData, ap.Parent, -1L);
		if (dobj.Item == null)
		{
			return true;
		}
		if (dobj.Item != null)
		{
			IDamageable idmg = dobj.Item;
			if (loot.Data.Health.HasValue)
			{
				idmg.Health = MathHelper.RandomRange(loot.Data.Health.Value.Min, loot.Data.Health.Value.Max);
			}
			if (loot.Data.Armor.HasValue)
			{
				idmg.Armor = loot.Data.Armor.Value;
			}
		}
		if (ap is VesselAttachPoint point)
		{
			if (dobj.Item is MachineryPart part)
			{
				part.WearMultiplier = 1f;
			}
			point.Vessel.AttachItem(dobj.Item, (short)point.InSceneID);
			dobj.APDetails = new AttachPointDetails
			{
				InSceneID = point.InSceneID
			};
		}
		else if (ap is ItemSlot slot)
		{
			slot.FitItem(dobj.Item);
		}
		if (!rule.IsOneTimeSpawnRule)
		{
			dobj.IsPartOfSpawnSystem = true;
			SpawnedDynamicObjects.TryAdd(dobj.Guid, new Tuple<SpawnRule, SpawnRuleLoot>(rule, loot));
		}
		return true;
	}

	public static List<LootItemData> GetLootItemDataFromCategory(string ruleName, string categoryName, LootTier tier)
	{
		if (!lootCategories.ContainsKey(categoryName))
		{
			Debug.LogError($"SPAWN MANAGER - Rule \"{ruleName}\", loot category \"{categoryName}\" does not exist");
			return new List<LootItemData>();
		}
		if (!lootCategories[categoryName].ContainsKey(tier))
		{
			Debug.LogError($"SPAWN MANAGER - Rule \"{ruleName}\", loot category \"{categoryName}\" has no tier \"{tier.ToString()}\"");
			return new List<LootItemData>();
		}
		return lootCategories[categoryName][tier];
	}

	public static ShipSpawnPoint SetStartingSetupSpawnPoints(SpaceObjectVessel ves, Player pl)
	{
		ShipSpawnPoint ret = null;
		List<ShipSpawnPoint> spawnPoints = ves.AllVessels.SelectMany((SpaceObjectVessel m) => m.SpawnPoints).ToList();
		foreach (ShipSpawnPoint sp in spawnPoints)
		{
			sp.Type = SpawnPointType.WithAuthorization;
			sp.Player = pl;
			if (ret == null)
			{
				sp.State = SpawnPointState.Authorized;
				ret = sp;
			}
			else
			{
				sp.State = SpawnPointState.Locked;
			}
		}
		return ret;
	}

	public static async Task<Ship> SpawnStartingSetup(SpawnSetupType spawnSetup)
	{
		SpawnRule sr = startingSceneSpawnRules.OrderBy((SpawnRule m) => MathHelper.RandomNextDouble()).FirstOrDefault((SpawnRule m) => m.Name == spawnSetup.ToString());
		return await sr.ExecuteRule() as Ship;
	}

	public static async Task<Ship> SpawnQuestSetup(QuestTrigger questTrigger)
	{
		SpawnRule sr = questSpawnRules.OrderBy((SpawnRule m) => MathHelper.RandomNextDouble()).FirstOrDefault((SpawnRule m) => m.Name == questTrigger.SpawnRuleName);
		return await sr.ExecuteQuestRule(questTrigger) as Ship;
	}

	private static string ItemDataDebugString(LootItemData data)
	{
		string retVal = "";
		retVal = data.Type == ItemType.GenericItem ? data.Type.ToString() + " - " + data.GenericSubType : data.Type != ItemType.MachineryPart ? data.Type.ToString() : data.Type.ToString() + " - " + data.PartType;
		if (data.Health.HasValue)
		{
			retVal = retVal + ", Health (" + data.Health.Value.Min + ", " + data.Health.Value.Max + ")";
		}
		return retVal;
	}

	private static void PrintDebugInfo(bool printCategories = false, bool printSpawnRules = false, bool printAttachPoints = false, bool printItemTypeIDs = false)
	{
		string dbgString = "";
		if (printCategories && lootCategories is { Count: > 0 })
		{
			dbgString = dbgString + "\nCategories:\n" + new string('=', 78);
			foreach (KeyValuePair<string, Dictionary<LootTier, List<LootItemData>>> cat in lootCategories)
			{
				dbgString = dbgString + "\nName: " + cat.Key + "\n";
				foreach (KeyValuePair<LootTier, List<LootItemData>> tier in cat.Value)
				{
					dbgString = dbgString + "  Tier: " + tier.Key.ToString() + "\n";
					foreach (LootItemData item in tier.Value)
					{
						dbgString = dbgString + "    " + ItemDataDebugString(item) + "\n";
					}
				}
			}
		}
		if (printSpawnRules)
		{
			dbgString = dbgString + "\n\nRules:\n" + new string('=', 78);
			foreach (SpawnRule rule in spawnRules)
			{
				dbgString = string.Concat(dbgString, "\nName: ", rule.Name, "\n  Orbit: ", rule.Orbit.CelestialBody.ToString(), "\n    PER (", rule.Orbit.PeriapsisDistance.Min, ", ", rule.Orbit.PeriapsisDistance.Max, ")\n    APO (", rule.Orbit.ApoapsisDistance.Min, ", ", rule.Orbit.ApoapsisDistance.Max, ")\n    INC (", rule.Orbit.Inclination.Min, ", ", rule.Orbit.Inclination.Max, ")\n    AOP (", rule.Orbit.ArgumentOfPeriapsis.Min, ", ", rule.Orbit.ArgumentOfPeriapsis.Max, ")\n    LOA (", rule.Orbit.LongitudeOfAscendingNode.Min, ", ", rule.Orbit.LongitudeOfAscendingNode.Max, ")\n    TAN (", rule.Orbit.TrueAnomaly.Min, ", ", rule.Orbit.TrueAnomaly.Max, ")\n  Location type: ", rule.LocationType, ", tag: ", rule.LocationTag, "\n  Respawn Timer: (", rule.RespawnTimerSec, " - ", rule.CurrTimerSec, ")\n  Clusters (", rule.NumberOfClusters.Min, ", ", rule.NumberOfClusters.Max, ")\n");
				if (rule.ScenePool != null)
				{
					dbgString += "  Scene Pool:\n";
					foreach (SpawnRuleScene sp in rule.ScenePool)
					{
						dbgString = dbgString + "    " + sp.SceneID.ToString() + ", Count: (" + sp.Count + "," + sp.CountMax + "), Health: (" + sp.HealthMin + "," + sp.HealthMax + ")\n";
					}
				}
				if (rule.LootPool == null)
				{
					continue;
				}
				dbgString += "  Loot Pool\n";
				foreach (SpawnRuleLoot lp in rule.LootPool)
				{
					dbgString = dbgString + "    " + ItemDataDebugString(lp.Data) + ", Count: (" + lp.Count + "," + lp.CountMax + ")\n";
				}
			}
		}
		if (printItemTypeIDs && itemTypeItemID is { Count: > 0 })
		{
			dbgString = dbgString + "\n\nItem Type - Item ID:\n" + new string('=', 78);
			foreach (KeyValuePair<ItemType, Dictionary<int, short>> type in itemTypeItemID)
			{
				foreach (KeyValuePair<int, short> subtype in type.Value)
				{
					dbgString = dbgString + "\n" + type.Key;
					if (subtype.Key != 0 && type.Key == ItemType.GenericItem)
					{
						dbgString = dbgString + " - " + (GenericItemSubType)subtype.Key;
					}
					else if (subtype.Key != 0 && type.Key == ItemType.MachineryPart)
					{
						dbgString = dbgString + " - " + (MachineryPartType)subtype.Key;
					}
					dbgString = dbgString + " ID: " + subtype.Value;
				}
			}
		}
		Debug.LogInfo(dbgString + "\n");
	}

	public static async Task Initialize(bool isPersistenceInitialize = false)
	{
		Debug.Log("Initialising spawn rules...");
		LoadData();
		if (spawnRules == null || spawnRules.Count == 0)
		{
			throw new Exception("SPAWN MANAGER - Spawn rules are not set.");
		}
		if (!isPersistenceInitialize && Settings.PrintInfo)
		{
			PrintDebugInfo(Settings.PrintCategories, Settings.PrintSpawnRules, Settings.PrintItemAttachPoints, Settings.PrintItemTypeIDs);
		}
		foreach (SpawnRule sr in spawnRules)
		{
			if (!sr.IsOneTimeSpawnRule)
			{
				await sr.Initialize(isPersistenceInitialize);
			}
		}
	}

	public static PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataSpawnManager data = new PersistenceObjectDataSpawnManager();
		data.SpawnRules = new Dictionary<string, PersistenceObjectDataSpawnManager.SpawnRule>();
		Dictionary<SpawnRuleScene, PersistenceObjectDataSpawnManager.SpawnRuleScene> spawnRuleSceneXRef = new Dictionary<SpawnRuleScene, PersistenceObjectDataSpawnManager.SpawnRuleScene>();
		Dictionary<SpawnRuleLoot, PersistenceObjectDataSpawnManager.SpawnRuleLoot> spawnRuleLootXRef = new Dictionary<SpawnRuleLoot, PersistenceObjectDataSpawnManager.SpawnRuleLoot>();
		if (spawnRules is { Count: > 0 })
		{
			foreach (SpawnRule sr in spawnRules)
			{
				if (sr.IsOneTimeSpawnRule)
				{
					continue;
				}
				PersistenceObjectDataSpawnManager.SpawnRule p_sr = new PersistenceObjectDataSpawnManager.SpawnRule
				{
					CurrTimerSec = sr.CurrTimerSec,
					ScenePool = new List<PersistenceObjectDataSpawnManager.SpawnRuleScene>(),
					LootPool = new List<PersistenceObjectDataSpawnManager.SpawnRuleLoot>(),
					SpawnedVessels = new List<long>()
				};
				if (sr.ScenePool != null)
				{
					foreach (SpawnRuleScene sp in sr.ScenePool)
					{
						PersistenceObjectDataSpawnManager.SpawnRuleScene p_srs = spawnRuleSceneXRef[sp] = new PersistenceObjectDataSpawnManager.SpawnRuleScene
						{
							Vessels = new List<Tuple<long, int>>()
						};
						p_sr.ScenePool.Add(p_srs);
					}
				}
				if (sr.LootPool != null)
				{
					foreach (SpawnRuleLoot lp in sr.LootPool)
					{
						PersistenceObjectDataSpawnManager.SpawnRuleLoot p_srl = spawnRuleLootXRef[lp] = new PersistenceObjectDataSpawnManager.SpawnRuleLoot
						{
							DynamicObjects = new List<long>()
						};
						p_sr.LootPool.Add(p_srl);
					}
				}
				foreach (SpaceObjectVessel vessel in sr.SpawnedVessels)
				{
					p_sr.SpawnedVessels.Add(vessel.Guid);
				}
				data.SpawnRules[sr.Name] = p_sr;
			}
		}
		if (SpawnedVessels is { Count: > 0 })
		{
			foreach (KeyValuePair<long, Tuple<SpawnRule, SpawnRuleScene, int>> ves in SpawnedVessels)
			{
				if (ves.Value.Item2 != null && spawnRuleSceneXRef.ContainsKey(ves.Value.Item2))
				{
					spawnRuleSceneXRef[ves.Value.Item2].Vessels.Add(new Tuple<long, int>(ves.Key, ves.Value.Item3));
				}
			}
		}
		if (SpawnedDynamicObjects is { Count: > 0 })
		{
			foreach (KeyValuePair<long, Tuple<SpawnRule, SpawnRuleLoot>> obj in SpawnedDynamicObjects)
			{
				if (spawnRuleLootXRef.ContainsKey(obj.Value.Item2))
				{
					spawnRuleLootXRef[obj.Value.Item2].DynamicObjects.Add(obj.Key);
				}
			}
		}
		return data;
	}

	public static async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await Initialize(isPersistenceInitialize: true);
		if (persistenceData is not PersistenceObjectDataSpawnManager data || data.SpawnRules == null)
		{
			return;
		}
		SpawnRule tmpSr = null;
		SpaceObjectVessel tmpVess = null;
		foreach (KeyValuePair<string, PersistenceObjectDataSpawnManager.SpawnRule> p_sr in data.SpawnRules)
		{
			tmpSr = spawnRules.Find((SpawnRule m) => m.Name == p_sr.Key);
			if (tmpSr == null)
			{
				continue;
			}
			tmpSr.CurrTimerSec = p_sr.Value.CurrTimerSec;
			if (p_sr.Value.ScenePool != null)
			{
				for (int j = 0; j < p_sr.Value.ScenePool.Count && j < tmpSr.ScenePool.Count; j++)
				{
					if (p_sr.Value.ScenePool[j].Vessels == null)
					{
						continue;
					}
					foreach (Tuple<long, int> p_ves in p_sr.Value.ScenePool[j].Vessels)
					{
						tmpVess = Server.Instance.GetVessel(p_ves.Item1);
						if (tmpVess != null && tmpSr.AddVesselToRule(tmpVess, tmpSr.ScenePool[j], p_ves.Item2))
						{
							tmpVess.IsPartOfSpawnSystem = true;
							SpawnedVessels[tmpVess.Guid] = new Tuple<SpawnRule, SpawnRuleScene, int>(tmpSr, tmpSr.ScenePool[j], p_ves.Item2);
						}
					}
				}
			}
			if (p_sr.Value.LootPool != null)
			{
				for (int i = 0; i < p_sr.Value.LootPool.Count && i < tmpSr.LootPool.Count; i++)
				{
					if (p_sr.Value.LootPool[i].DynamicObjects == null)
					{
						continue;
					}
					foreach (long p_dobj in p_sr.Value.LootPool[i].DynamicObjects)
					{
						if (Server.Instance.GetSpaceObject(p_dobj) is DynamicObject tmpDobj && tmpSr.AddDynamicObjectToRule(tmpDobj, tmpSr.LootPool[i]))
						{
							tmpDobj.IsPartOfSpawnSystem = true;
							SpawnedDynamicObjects.TryAdd(tmpDobj.Guid, new Tuple<SpawnRule, SpawnRuleLoot>(tmpSr, tmpSr.LootPool[i]));
						}
					}
				}
			}
			if (p_sr.Value.SpawnedVessels == null)
			{
				continue;
			}
			foreach (long svGuid in p_sr.Value.SpawnedVessels)
			{
				SpaceObjectVessel vessel = Server.Instance.GetVessel(svGuid);
				if (vessel != null && !tmpSr.SpawnedVessels.Contains(vessel))
				{
					tmpSr.SpawnedVessels.Add(vessel);
					if (p_sr.Value.ScenePool == null || p_sr.Value.ScenePool.Count == 0)
					{
						vessel.IsPartOfSpawnSystem = true;
						SpawnedVessels[vessel.Guid] = new Tuple<SpawnRule, SpawnRuleScene, int>(tmpSr, null, 0);
					}
				}
			}
		}
		if (Settings.PrintInfo)
		{
			PrintDebugInfo(Settings.PrintCategories, Settings.PrintSpawnRules, Settings.PrintItemAttachPoints, Settings.PrintItemTypeIDs);
		}
	}

	public static async Task UpdateTimers(double deltaTime)
	{
		foreach (SpawnRule sr in timedSpawnRules)
		{
			sr.CurrTimerSec += deltaTime;
			if (sr.CurrTimerSec >= sr.RespawnTimerSec)
			{
				if (!sr.IsOneTimeSpawnRule)
				{
					await sr.ExecuteRule();
				}
				sr.CurrTimerSec = 0.0;
			}
		}
	}

	private static void RemoveContainedSpawnSystemObjects(SpaceObject container)
	{
		SolarSystemState state = Server.Instance.SolarSystem.State;
		List<ItemId> items = [];
		state.AddDescendants(container.Key, items);
		foreach (ItemId item in items)
		{
			if (Server.Instance.GetDynamicObject(state.Guid(item)) is { IsPartOfSpawnSystem: true } contained)
			{
				RemoveSpawnSystemObject(contained, checkChildren: false);
			}
		}
	}

	public static void RemoveSpawnSystemObject(SpaceObject obj, bool checkChildren)
	{
		obj.IsPartOfSpawnSystem = false;
		if (obj is DynamicObject dobj2 && SpawnedDynamicObjects.ContainsKey(dobj2.Guid))
		{
			if (!SpawnedDynamicObjects[dobj2.Guid].Item1.RemoveDynamicObject(dobj2, SpawnedDynamicObjects[dobj2.Guid].Item2))
			{
				return;
			}
			if (checkChildren)
			{
				RemoveContainedSpawnSystemObjects(dobj2);
			}
			SpawnedDynamicObjects.TryRemove(dobj2.Guid, out _);
		}
		else
		{
			if (obj is not SpaceObjectVessel ves || !SpawnedVessels.ContainsKey(ves.Guid))
			{
				return;
			}

			SpawnRule sr = SpawnedVessels[ves.Guid].Item1;
			if (sr.LocationType == SpawnRuleLocationType.Station && sr.DoNotRemoveVesselsFromSpawnSystem)
			{
				return;
			}
			if (checkChildren)
			{
				RemoveContainedSpawnSystemObjects(ves);
			}
			if (sr.LocationType == SpawnRuleLocationType.Station)
			{
				sr.RemoveSpaceObjectVessel(ves);
			}
			else
			{
				sr.RemoveSpaceObjectVessel(ves, SpawnedVessels[ves.Guid].Item2, SpawnedVessels[ves.Guid].Item3);
			}
			SpawnedVessels.Remove(ves.Guid, out _);
		}
	}

	public static long GetStationMainVesselGUID(string stationName, CelestialBodyGUID? celestial = null)
	{
		if (stationName != null && stationName != "")
		{
			SpawnRule sr = spawnRules.FirstOrDefault((SpawnRule m) => m.StationName == stationName && (!celestial.HasValue || (celestial.HasValue && m.Orbit.CelestialBody == celestial.Value)));
			if (sr != null)
			{
				SpaceObjectVessel ves2 = sr.SpawnedVessels.FirstOrDefault((SpaceObjectVessel m) => m.IsMainVessel);
				if (ves2 != null)
				{
					return ves2.Guid;
				}
			}
			else
			{
				SpaceObjectVessel ves = null;
				sr = spawnRules.FirstOrDefault((SpawnRule m) => (ves = m.SpawnedVessels.FirstOrDefault((SpaceObjectVessel n) => n.IsMainVessel && n.VesselRegistration == stationName)) != null && (!celestial.HasValue || (celestial.HasValue && m.Orbit.CelestialBody == celestial.Value)));
				if (ves != null)
				{
					return ves.Guid;
				}
			}
		}
		return 0L;
	}

	public static async Task<bool> RespawnBlueprintRule(string name)
	{
		SpawnRule sr = spawnRules.FirstOrDefault((SpawnRule m) => m.Name.ToLower().Replace(' ', '_').Contains(name.ToLower()));
		if (sr is not { LocationType: SpawnRuleLocationType.Station })
		{
			return false;
		}
		await sr.ExecuteRule(force: true);
		return true;
	}
}
