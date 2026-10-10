using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using OpenHellion.IO;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Objects;

namespace ZeroGravity.Spawn;

public static class SpawnSerialization
{
	public enum AttachPointPriority
	{
		Default,
		Simple,
		Active,
		MachineryPartSlot,
		Scrap,
		Item,
		TransportBox
	}

	public class LootItemSerData
	{
		public class CargoResourceData
		{
			public List<string> Resources;

			public SpawnRange<float> Quantity;
		}

		public string ItemType;

		public string GenericItemSubType;

		public string MachineryPartType;

		public SpawnRange<float>? Health;

		public float? Armor;

		public List<string> Look;

		public SpawnRange<float>? Power;

		public SpawnRange<int>? Count;

		public bool? IsActive;

		public List<CargoResourceData> Cargo;

		public string AttachPointPriority;
	}

	public class LootTierData
	{
		public string TierName;

		public List<LootItemSerData> Items;
	}

	public class LootCategoryData
	{
		public string CategoryName;

		public List<LootTierData> Tiers;
	}

	public class SpawnRuleOrbitData
	{
		public string CelestialBody;

		public SpawnRange<double> PeriapsisDistance_Km;

		public SpawnRange<double> ApoapsisDistance_Km;

		public SpawnRange<float> Inclination_Deg;

		public SpawnRange<float> ArgumentOfPeriapsis_Deg;

		public SpawnRange<float> LongitudeOfAscendingNode_Deg;

		public SpawnRange<float> TrueAnomaly_Deg;

		public bool UseCurrentSolarSystemTime;
	}

	public class SpawnRuleSceneData
	{
		public string Scene;

		public SpawnRange<int> SceneCount;

		public SpawnRange<float> Health;
	}

	public class SpawnRuleLootData
	{
		public string CategoryName;

		public string Tier;

		public SpawnRange<int> LootCount;
	}

	public class SpawnRuleData
	{
		public string RuleName;

		public string StationName;

		public string StationBlueprint;

		public SpawnRuleOrbitData Orbit;

		public string LocationType;

		public string LocationTag;

		public double RespawnTimer_Minutes;

		public bool ForceRespawn;

		public double CheckPlayers_Km;

		public float? AsteroidResourcesMultiplier;

		public SpawnRange<float>[] AngularVelocity;

		public SpawnRange<int> NumberOfClusters;

		public List<SpawnRuleSceneData> LocationScenes;

		public List<SpawnRuleLootData> Loot;

		public bool DoNotRemoveVesselsFromSpawnSystem;

		public bool IsVisibleOnRadar;
	}

	private static string GetDirectory()
	{
		if (Server.ConfigDir.IsNullOrEmpty() || !Directory.Exists(Server.ConfigDir + "Data"))
		{
			return "";
		}
		return Server.ConfigDir;
	}

	public static Dictionary<string, Dictionary<LootTier, List<LootItemData>>> LoadLootData()
	{
		string dir = GetDirectory();
		List<LootCategoryData> data = JsonSerialiser.Load<List<LootCategoryData>>(dir + "Data/LootCategories.json");
		if (data is { Count: > 0 })
		{
			Dictionary<string, Dictionary<LootTier, List<LootItemData>>> categories = new Dictionary<string, Dictionary<LootTier, List<LootItemData>>>();
			foreach (LootCategoryData ser_cat in data)
			{
				if (ser_cat.CategoryName.IsNullOrEmpty())
				{
					Debug.LogError("SPAWN MANAGER - Loot category cannot be empty");
					continue;
				}
				if (categories.ContainsKey(ser_cat.CategoryName))
				{
					Debug.LogError($"SPAWN MANAGER - Loot category \"{ser_cat.CategoryName}\" already exists");
					continue;
				}
				Dictionary<LootTier, List<LootItemData>> dict = new Dictionary<LootTier, List<LootItemData>>();
				LootTier tmpLootTier = LootTier.T1;
				foreach (LootTierData ser_tier in ser_cat.Tiers)
				{
					if (ser_tier.TierName.IsNullOrEmpty() || !Enum.TryParse<LootTier>(ser_tier.TierName, out tmpLootTier))
					{
						Debug.LogError($"SPAWN MANAGER - Loot category \"{ser_cat.CategoryName}\" tier \"{ser_tier.TierName}\" is not valid");
						continue;
					}
					if (dict.ContainsKey(tmpLootTier))
					{
						Debug.LogError($"SPAWN MANAGER - Loot category \"{ser_cat.CategoryName}\" tier \"{ser_tier.TierName}\" already exists");
						continue;
					}
					List<LootItemData> lootItems = new List<LootItemData>();
					foreach (LootItemSerData aux_data in ser_tier.Items)
					{
						LootItemData lootIData = CreateLootItemFromSerializationData(ser_cat.CategoryName, tmpLootTier, aux_data);
						if (lootIData != null)
						{
							lootItems.Add(lootIData);
						}
					}
					dict.Add(tmpLootTier, lootItems);
				}
				categories.Add(ser_cat.CategoryName, dict);
			}
			return categories;
		}
		return null;
	}

	private static LootItemData CreateLootItemFromSerializationData(string categoryName, LootTier tier, LootItemSerData data)
	{
		ItemType tmpItemType = ItemType.None;
		GenericItemSubType tmpGenericSubType = GenericItemSubType.None;
		MachineryPartType tmpMachPartType = MachineryPartType.None;
		AttachPointPriority attachPointPriority = AttachPointPriority.Default;
		if (!data.AttachPointPriority.IsNullOrEmpty() && !Enum.TryParse<AttachPointPriority>(data.AttachPointPriority, out attachPointPriority))
		{
			Debug.LogError($"SPAWN MANAGER - Loot cateory \"{categoryName}\" tier \"{tier.ToString()}\" has unknown AttachPointPriority \"{data.ItemType}\"");
			return null;
		}
		if (!Enum.TryParse<ItemType>(data.ItemType, out tmpItemType))
		{
			Debug.LogError($"SPAWN MANAGER - Loot cateory \"{categoryName}\" tier \"{tier.ToString()}\" has unknown item type \"{data.ItemType}\"");
			return null;
		}
		if (data.Health.HasValue && (data.Health.Value.Min < 0f || data.Health.Value.Max <= 0f || data.Health.Value.Min > data.Health.Value.Max))
		{
			Debug.LogError($"SPAWN MANAGER - Loot category \"{categoryName}\" tier \"{tier.ToString()}\" health is not valid (min: {data.Health.Value.Min}, max: {data.Health.Value.Max})");
			return null;
		}
		if (tmpItemType == ItemType.GenericItem && (data.GenericItemSubType.IsNullOrEmpty() || !Enum.TryParse<GenericItemSubType>(data.GenericItemSubType, out tmpGenericSubType)))
		{
			Debug.LogError($"SPAWN MANAGER - Loot cateory \"{categoryName}\" tier \"{tier.ToString()}\" item \"{data.ItemType}\" has unknown sub type \"{data.GenericItemSubType}\"");
			return null;
		}
		if (tmpItemType == ItemType.MachineryPart && (data.MachineryPartType.IsNullOrEmpty() || !Enum.TryParse<MachineryPartType>(data.MachineryPartType, out tmpMachPartType)))
		{
			Debug.LogError($"SPAWN MANAGER - Loot cateory \"{categoryName}\" tier \"{tier.ToString()}\" item \"{data.ItemType}\" has unknown sub type \"{data.MachineryPartType}\"");
			return null;
		}
		return new LootItemData
		{
			Type = tmpItemType,
			GenericSubType = tmpGenericSubType,
			PartType = tmpMachPartType,
			Health = data.Health,
			Armor = data.Armor,
			Look = data.Look,
			Power = data.Power,
			Count = data.Count,
			IsActive = data.IsActive,
			Cargo = GenerateCargoData(categoryName, tier, data),
			Tier = (int)tier,
			AttachPointPriority = attachPointPriority
		};
	}

	private static List<LootItemData.CargoResourceData> GenerateCargoData(string categoryName, LootTier tier, LootItemSerData data)
	{
		if (data.Cargo == null)
		{
			return null;
		}
		ResourceType tmpResType = ResourceType.None;
		List<LootItemData.CargoResourceData> retVal = new List<LootItemData.CargoResourceData>();
		foreach (LootItemSerData.CargoResourceData item in data.Cargo)
		{
			if (item.Resources == null || item.Resources.Count == 0)
			{
				Debug.LogError($"SPAWN MANAGER - Loot cateory \"{categoryName}\" tier \"{tier}\", cargo has no resource set");
				continue;
			}
			if (item.Quantity.Max < 0f || item.Quantity.Min > item.Quantity.Max)
			{
				Debug.LogError($"SPAWN MANAGER - Loot cateory \"{categoryName}\" tier \"{tier}\", cargo quantity is not valid (min: {item.Quantity.Min}, max: {item.Quantity.Max})");
				continue;
			}
			LootItemData.CargoResourceData tmpData = new LootItemData.CargoResourceData
			{
				Quantity = item.Quantity,
				Resources = new List<ResourceType>()
			};
			foreach (string res in item.Resources)
			{
				if (!Enum.TryParse<ResourceType>(res, out tmpResType))
				{
					Debug.LogError($"SPAWN MANAGER - Loot cateory \"{categoryName}\" tier \"{tier}\", cargo resource type \"{res}\" does not exit");
				}
				else
				{
					tmpData.Resources.Add(tmpResType);
				}
			}
			if (tmpData.Resources.Count > 0)
			{
				retVal.Add(tmpData);
			}
		}
		if (retVal.Count > 0)
		{
			return retVal;
		}
		return null;
	}

	public static void GenerateLootSampleData(bool force = false)
	{
		string dir = GetDirectory();
		if (force || !File.Exists(dir + "Data/LootCategories.json"))
		{
			List<LootCategoryData> sampleData = new List<LootCategoryData>
			{
				new LootCategoryData
				{
					CategoryName = "Sample",
					Tiers = new List<LootTierData>
					{
						new LootTierData
						{
							TierName = "T1",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "MilitaryHandGun01"
								},
								new LootItemSerData
								{
									ItemType = "MilitaryHandGunAmmo01",
									Count = new SpawnRange<int>(10, 20)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "WarpCell",
									Health = new SpawnRange<float>(22f, 33f)
								},
								new LootItemSerData
								{
									ItemType = "AltairRefinedCanister",
									Cargo = new List<LootItemSerData.CargoResourceData>
									{
										new LootItemSerData.CargoResourceData
										{
											Resources = new List<string> { "Oxygen", "Nitrogen" },
											Quantity = new SpawnRange<float>(54f, 66f)
										}
									}
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "Poster",
									Look = new List<string> { "Hellion", "Bethyr", "Burner", "Turret", "CrewQuaters" },
									Health = new SpawnRange<float>(15f, 35f)
								},
								new LootItemSerData
								{
									ItemType = "PortableTurret",
									IsActive = true
								},
								new LootItemSerData
								{
									ItemType = "AltairPressurisedJetpack",
									Power = new SpawnRange<float>(100f, 100f)
								}
							}
						}
					}
				},
				new LootCategoryData
				{
					CategoryName = "Weapons",
					Tiers = new List<LootTierData>
					{
						new LootTierData
						{
							TierName = "T1",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "MilitaryHandGun01"
								}
							}
						},
						new LootTierData
						{
							TierName = "T2",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "AltairRifle"
								},
								new LootItemSerData
								{
									ItemType = "MilitaryHandGun02"
								},
								new LootItemSerData
								{
									ItemType = "APGrenade"
								}
							}
						},
						new LootTierData
						{
							TierName = "T3",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "MilitaryAssaultRifle"
								},
								new LootItemSerData
								{
									ItemType = "MilitarySniperRifle"
								},
								new LootItemSerData
								{
									ItemType = "APGrenade"
								},
								new LootItemSerData
								{
									ItemType = "EMPGrenade"
								}
							}
						}
					}
				},
				new LootCategoryData
				{
					CategoryName = "Ammo",
					Tiers = new List<LootTierData>
					{
						new LootTierData
						{
							TierName = "T1",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "MilitaryHandGunAmmo01"
								}
							}
						},
						new LootTierData
						{
							TierName = "T2",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "MilitaryHandGunAmmo01"
								},
								new LootItemSerData
								{
									ItemType = "AltairRifleAmmo"
								},
								new LootItemSerData
								{
									ItemType = "MilitaryHandGunAmmo02"
								}
							}
						},
						new LootTierData
						{
							TierName = "T3",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "AltairRifleAmmo"
								},
								new LootItemSerData
								{
									ItemType = "MilitaryHandGunAmmo02"
								},
								new LootItemSerData
								{
									ItemType = "MilitaryAssaultRifleAmmo"
								},
								new LootItemSerData
								{
									ItemType = "MilitarySniperRifleAmmo"
								}
							}
						}
					}
				},
				new LootCategoryData
				{
					CategoryName = "Parts",
					Tiers = new List<LootTierData>
					{
						new LootTierData
						{
							TierName = "T1",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "ServoMotor",
									Health = new SpawnRange<float>(45f, 55f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "AirProcessingController",
									Health = new SpawnRange<float>(45f, 55f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "CarbonFilters",
									Health = new SpawnRange<float>(45f, 55f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "AirFilterUnit",
									Health = new SpawnRange<float>(45f, 55f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "PressureRegulator",
									Health = new SpawnRange<float>(45f, 55f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "CoreContainmentFieldGenerator",
									Health = new SpawnRange<float>(45f, 55f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "ResourceInjector",
									Health = new SpawnRange<float>(45f, 55f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "EMFieldController",
									Health = new SpawnRange<float>(45f, 55f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "ThermonuclearCatalyst",
									Health = new SpawnRange<float>(45f, 55f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "PlasmaAccelerator",
									Health = new SpawnRange<float>(45f, 55f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "HighEnergyLaser",
									Health = new SpawnRange<float>(45f, 55f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "SingularityCellDetonator",
									Health = new SpawnRange<float>(45f, 55f)
								}
							}
						},
						new LootTierData
						{
							TierName = "T2",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "ServoMotor",
									Health = new SpawnRange<float>(65f, 75f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "AirProcessingController",
									Health = new SpawnRange<float>(65f, 75f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "CarbonFilters",
									Health = new SpawnRange<float>(65f, 75f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "AirFilterUnit",
									Health = new SpawnRange<float>(65f, 75f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "PressureRegulator",
									Health = new SpawnRange<float>(65f, 75f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "CoreContainmentFieldGenerator",
									Health = new SpawnRange<float>(65f, 75f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "ResourceInjector",
									Health = new SpawnRange<float>(65f, 75f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "EMFieldController",
									Health = new SpawnRange<float>(65f, 75f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "ThermonuclearCatalyst",
									Health = new SpawnRange<float>(65f, 75f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "PlasmaAccelerator",
									Health = new SpawnRange<float>(65f, 75f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "HighEnergyLaser",
									Health = new SpawnRange<float>(65f, 75f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "SingularityCellDetonator",
									Health = new SpawnRange<float>(65f, 75f)
								}
							}
						},
						new LootTierData
						{
							TierName = "T3",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "ServoMotor",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "AirProcessingController",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "CarbonFilters",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "AirFilterUnit",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "PressureRegulator",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "CoreContainmentFieldGenerator",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "ResourceInjector",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "EMFieldController",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "ThermonuclearCatalyst",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "PlasmaAccelerator",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "HighEnergyLaser",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "SingularityCellDetonator",
									Health = new SpawnRange<float>(85f, 100f)
								}
							}
						}
					}
				},
				new LootCategoryData
				{
					CategoryName = "Fuel",
					Tiers = new List<LootTierData>
					{
						new LootTierData
						{
							TierName = "T1",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "AltairResourceContainer",
									Cargo = new List<LootItemSerData.CargoResourceData>
									{
										new LootItemSerData.CargoResourceData
										{
											Resources = new List<string> { "Nitro" },
											Quantity = new SpawnRange<float>(33f, 100f)
										}
									}
								}
							}
						},
						new LootTierData
						{
							TierName = "T2",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "WarpCell",
									Health = new SpawnRange<float>(22f, 33f)
								},
								new LootItemSerData
								{
									ItemType = "AltairRefinedCanister",
									Cargo = new List<LootItemSerData.CargoResourceData>
									{
										new LootItemSerData.CargoResourceData
										{
											Resources = new List<string> { "Nitro", "Hydrogen" },
											Quantity = new SpawnRange<float>(22f, 33f)
										}
									}
								}
							}
						},
						new LootTierData
						{
							TierName = "T3",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "WarpCell",
									Health = new SpawnRange<float>(54f, 66f)
								},
								new LootItemSerData
								{
									ItemType = "AltairRefinedCanister",
									Cargo = new List<LootItemSerData.CargoResourceData>
									{
										new LootItemSerData.CargoResourceData
										{
											Resources = new List<string> { "Nitro", "Hydrogen", "Deuterium" },
											Quantity = new SpawnRange<float>(54f, 66f)
										}
									}
								}
							}
						},
						new LootTierData
						{
							TierName = "T4",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "MachineryPart",
									MachineryPartType = "WarpCell",
									Health = new SpawnRange<float>(86f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "AltairRefinedCanister",
									Cargo = new List<LootItemSerData.CargoResourceData>
									{
										new LootItemSerData.CargoResourceData
										{
											Resources = new List<string> { "Nitro", "Hydrogen", "Deuterium" },
											Quantity = new SpawnRange<float>(86f, 100f)
										}
									}
								}
							}
						}
					}
				},
				new LootCategoryData
				{
					CategoryName = "Resources",
					Tiers = new List<LootTierData>
					{
						new LootTierData
						{
							TierName = "T1",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "AltairResourceContainer",
									Cargo = new List<LootItemSerData.CargoResourceData>
									{
										new LootItemSerData.CargoResourceData
										{
											Resources = new List<string> { "Oxygen" },
											Quantity = new SpawnRange<float>(33f, 100f)
										}
									}
								}
							}
						},
						new LootTierData
						{
							TierName = "T2",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "AltairRefinedCanister",
									Cargo = new List<LootItemSerData.CargoResourceData>
									{
										new LootItemSerData.CargoResourceData
										{
											Resources = new List<string> { "Oxygen" },
											Quantity = new SpawnRange<float>(22f, 33f)
										}
									}
								},
								new LootItemSerData
								{
									ItemType = "AltairHandDrillCanister",
									Cargo = new List<LootItemSerData.CargoResourceData>
									{
										new LootItemSerData.CargoResourceData
										{
											Resources = new List<string> { "Ice", "DryIce" },
											Quantity = new SpawnRange<float>(22f, 33f)
										}
									}
								}
							}
						},
						new LootTierData
						{
							TierName = "T3",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "AltairRefinedCanister",
									Cargo = new List<LootItemSerData.CargoResourceData>
									{
										new LootItemSerData.CargoResourceData
										{
											Resources = new List<string> { "Oxygen", "Nitrogen" },
											Quantity = new SpawnRange<float>(54f, 66f)
										}
									}
								},
								new LootItemSerData
								{
									ItemType = "AltairHandDrillCanister",
									Cargo = new List<LootItemSerData.CargoResourceData>
									{
										new LootItemSerData.CargoResourceData
										{
											Resources = new List<string> { "Ice", "DryIce", "NitrateMinerals" },
											Quantity = new SpawnRange<float>(54f, 66f)
										}
									}
								}
							}
						},
						new LootTierData
						{
							TierName = "T4",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "AltairRefinedCanister",
									Cargo = new List<LootItemSerData.CargoResourceData>
									{
										new LootItemSerData.CargoResourceData
										{
											Resources = new List<string> { "Oxygen", "Nitrogen" },
											Quantity = new SpawnRange<float>(86f, 100f)
										}
									}
								},
								new LootItemSerData
								{
									ItemType = "AltairHandDrillCanister",
									Cargo = new List<LootItemSerData.CargoResourceData>
									{
										new LootItemSerData.CargoResourceData
										{
											Resources = new List<string> { "Ice", "DryIce", "NitrateMinerals", "HeavyIce" },
											Quantity = new SpawnRange<float>(86f, 100f)
										}
									}
								}
							}
						}
					}
				},
				new LootCategoryData
				{
					CategoryName = "Vanity",
					Tiers = new List<LootTierData>
					{
						new LootTierData
						{
							TierName = "T1",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "Poster",
									Look = new List<string> { "Hellion", "Bethyr", "Burner", "Turret", "CrewQuaters" },
									Health = new SpawnRange<float>(15f, 35f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "BasketBall",
									Health = new SpawnRange<float>(15f, 35f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "Hoop",
									Health = new SpawnRange<float>(15f, 35f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "LavaLamp",
									Health = new SpawnRange<float>(15f, 35f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "AltCorp_Cup",
									Health = new SpawnRange<float>(15f, 35f)
								}
							}
						},
						new LootTierData
						{
							TierName = "T2",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "Poster",
									Look = new List<string> { "Hellion", "Bethyr", "Burner", "Turret", "CrewQuaters" },
									Health = new SpawnRange<float>(45f, 65f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "BasketBall",
									Health = new SpawnRange<float>(45f, 65f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "Hoop",
									Health = new SpawnRange<float>(45f, 65f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "LavaLamp",
									Health = new SpawnRange<float>(45f, 65f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "AltCorp_Cup",
									Health = new SpawnRange<float>(45f, 65f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "CoffeeMachine",
									Health = new SpawnRange<float>(45f, 65f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "PlantRing",
									Health = new SpawnRange<float>(45f, 65f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "PlantCanister",
									Health = new SpawnRange<float>(45f, 65f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "TeslaBall",
									Health = new SpawnRange<float>(45f, 65f)
								}
							}
						},
						new LootTierData
						{
							TierName = "T3",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "Poster",
									Look = new List<string> { "Hellion", "Bethyr", "Burner", "Turret", "CrewQuaters", "Everest" },
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "BasketBall",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "Hoop",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "LavaLamp",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "AltCorp_Cup",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "CoffeeMachine",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "PlantRing",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "PlantCanister",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "TeslaBall",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "PlantZikaLeaf",
									Health = new SpawnRange<float>(85f, 100f)
								},
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "Picture",
									Look = new List<string> { "Atlas", "Actaeon" },
									Health = new SpawnRange<float>(85f, 100f)
								}
							}
						},
						new LootTierData
						{
							TierName = "T4",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "GenericItem",
									GenericItemSubType = "BookHolder",
									Health = new SpawnRange<float>(100f, 100f)
								}
							}
						}
					}
				},
				new LootCategoryData
				{
					CategoryName = "Suits",
					Tiers = new List<LootTierData>
					{
						new LootTierData
						{
							TierName = "T1",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "AltairPressurisedSuit"
								},
								new LootItemSerData
								{
									ItemType = "AltairPressurisedHelmet"
								},
								new LootItemSerData
								{
									ItemType = "AltairPressurisedJetpack"
								}
							}
						},
						new LootTierData
						{
							TierName = "T2",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "AltairEVASuit"
								},
								new LootItemSerData
								{
									ItemType = "AltairEVAHelmet"
								},
								new LootItemSerData
								{
									ItemType = "AltairEVAJetpack"
								}
							}
						}
					}
				},
				new LootCategoryData
				{
					CategoryName = "Utilities",
					Tiers = new List<LootTierData>
					{
						new LootTierData
						{
							TierName = "T1",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "AltairMedpackSmall"
								}
							}
						},
						new LootTierData
						{
							TierName = "T2",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "AltairMedpackBig"
								}
							}
						},
						new LootTierData
						{
							TierName = "T3",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "AltairDisposableHackingTool"
								}
							}
						}
					}
				},
				new LootCategoryData
				{
					CategoryName = "Mining",
					Tiers = new List<LootTierData>
					{
						new LootTierData
						{
							TierName = "T1",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "AltairHandDrill"
								},
								new LootItemSerData
								{
									ItemType = "AltairHandDrillCanister"
								},
								new LootItemSerData
								{
									ItemType = "AltairHandDrillBattery"
								},
								new LootItemSerData
								{
									ItemType = "AltairHandheldAsteroidScanningTool"
								}
							}
						}
					}
				},
				new LootCategoryData
				{
					CategoryName = "Turrets",
					Tiers = new List<LootTierData>
					{
						new LootTierData
						{
							TierName = "T1",
							Items = new List<LootItemSerData>
							{
								new LootItemSerData
								{
									ItemType = "PortableTurret"
								}
							}
						}
					}
				}
			};
			JsonSerialiser.SerializeToFile(sampleData, dir + "Data/LootCategories.json", JsonSerialiser.Formatting.Indented, new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			});
		}
	}

	public static List<SpawnRule> LoadSpawnRuleData()
	{
		string dir = GetDirectory();
		List<SpawnRuleData> data = new List<SpawnRuleData>();
		try
		{
			data = JsonSerialiser.Load<List<SpawnRuleData>>(dir + "Data/SpawnRules.json");
		}
		catch (Exception)
		{
			return new List<SpawnRule>();
		}
		if (data is { Count: > 0 })
		{
			List<SpawnRule> spawnRules = new List<SpawnRule>();
			CelestialBodyGUID tmpCelBody = CelestialBodyGUID.Bethyr;
			SpawnRuleLocationType tmpLocType = SpawnRuleLocationType.Random;
			List<SpawnRuleScene> tmpScenePool = null;
			List<SpawnRuleLoot> tmpLootPool = null;
			foreach (SpawnRuleData srd in data)
			{
				if (srd.RuleName.IsNullOrEmpty())
				{
					Debug.LogError($"SPAWN MANAGER - Spawn rule name is not set");
					continue;
				}
				foreach (SpawnRule ex2 in spawnRules)
				{
					if (ex2.Name == srd.RuleName)
					{
						Debug.LogError($"SPAWN MANAGER - Spawn rule \"{srd.RuleName}\" already exists");
					}
				}
				if (srd.Orbit == null)
				{
					Debug.LogError($"SPAWN MANAGER - Spawn rule \"{srd.RuleName}\" orbit is not set");
					continue;
				}
				if (srd.Orbit.CelestialBody.IsNullOrEmpty() || !Enum.TryParse<CelestialBodyGUID>(srd.Orbit.CelestialBody, out tmpCelBody))
				{
					Debug.LogError($"SPAWN MANAGER - Spawn rule \"{srd.RuleName}\" orbit celestial body \"{srd.Orbit.CelestialBody}\" does not exist");
					continue;
				}
				if (srd.Orbit.PeriapsisDistance_Km.Min < 0.0 || srd.Orbit.PeriapsisDistance_Km.Max <= 0.0 || srd.Orbit.PeriapsisDistance_Km.Min > srd.Orbit.PeriapsisDistance_Km.Max)
				{
					Debug.LogError($"SPAWN MANAGER - Spawn rule \"{srd.RuleName}\" orbit periapsis distance is not valid (min: {srd.Orbit.PeriapsisDistance_Km.Min}, max: {srd.Orbit.PeriapsisDistance_Km.Max})");
					continue;
				}
				if (srd.Orbit.ApoapsisDistance_Km.Min < 0.0 || srd.Orbit.ApoapsisDistance_Km.Max <= 0.0 || srd.Orbit.ApoapsisDistance_Km.Min > srd.Orbit.ApoapsisDistance_Km.Max)
				{
					Debug.LogError($"SPAWN MANAGER - Spawn rule \"{srd.RuleName}\" orbit apoapsis distance is not valid (min: {srd.Orbit.ApoapsisDistance_Km.Min}, max: {srd.Orbit.ApoapsisDistance_Km.Max})");
					continue;
				}
				if (srd.LocationType.IsNullOrEmpty() || !Enum.TryParse<SpawnRuleLocationType>(srd.LocationType, out tmpLocType))
				{
					Debug.LogError($"SPAWN MANAGER - Spawn rule \"{srd.RuleName}\" location type \"{srd.LocationType}\" is not valid");
					continue;
				}
				if (srd.Orbit.Inclination_Deg.Min > srd.Orbit.Inclination_Deg.Max)
				{
					Debug.LogError($"SPAWN MANAGER - Spawn rule \"{srd.RuleName}\" orbit inclination is not valid (min: {srd.Orbit.Inclination_Deg.Min}, max: {srd.Orbit.Inclination_Deg.Max})");
					continue;
				}
				if (srd.Orbit.ArgumentOfPeriapsis_Deg.Min > srd.Orbit.ArgumentOfPeriapsis_Deg.Max)
				{
					Debug.LogError($"SPAWN MANAGER - Spawn rule \"{srd.RuleName}\" orbit argument of periapsis is not valid (min: {srd.Orbit.ArgumentOfPeriapsis_Deg.Min}, max: {srd.Orbit.ArgumentOfPeriapsis_Deg.Max})");
					continue;
				}
				if (srd.Orbit.LongitudeOfAscendingNode_Deg.Min > srd.Orbit.LongitudeOfAscendingNode_Deg.Max)
				{
					Debug.LogError($"SPAWN MANAGER - Spawn rule \"{srd.RuleName}\" orbit longitude of ascending node is not valid (min: {srd.Orbit.LongitudeOfAscendingNode_Deg.Min}, max: {srd.Orbit.LongitudeOfAscendingNode_Deg.Max})");
					continue;
				}
				if (srd.Orbit.TrueAnomaly_Deg.Min < 0f || srd.Orbit.TrueAnomaly_Deg.Max < 0f || srd.Orbit.TrueAnomaly_Deg.Min > srd.Orbit.TrueAnomaly_Deg.Max)
				{
					Debug.LogError($"SPAWN MANAGER - Spawn rule \"{srd.RuleName}\" orbit true anomaly is not valid (min: {srd.Orbit.TrueAnomaly_Deg.Min}, max: {srd.Orbit.TrueAnomaly_Deg.Max})");
					continue;
				}
				if (tmpLocType == SpawnRuleLocationType.Random && (srd.NumberOfClusters.Min < 0 || srd.NumberOfClusters.Max <= 0 || srd.NumberOfClusters.Min > srd.NumberOfClusters.Max))
				{
					Debug.LogError($"SPAWN MANAGER - Spawn rule \"{srd.RuleName}\" number of clusters are not valid (min: {srd.NumberOfClusters.Min}, max: {srd.NumberOfClusters.Max})");
					continue;
				}
				tmpScenePool = null;
				if (tmpLocType == SpawnRuleLocationType.Random && !GenerateScenePoolData(srd, out tmpScenePool))
				{
					Debug.LogError($"SPAWN MANAGER - Spawn rule \"{srd.RuleName}\" scenes are not valid");
					continue;
				}
				tmpLootPool = null;
				if (!GenerateLootPoolData(srd, tmpLocType, out tmpLootPool))
				{
					Debug.LogError($"SPAWN MANAGER - Spawn rule \"{srd.RuleName}\" loot is not valid");
					continue;
				}
				CelestialBody cb = Server.Instance.SolarSystem.GetCelestialBody((long)tmpCelBody);
				if (srd.Orbit.PeriapsisDistance_Km.Min > cb.Orbit.GravityInfluenceRadius / 1000.0 || srd.Orbit.PeriapsisDistance_Km.Max > cb.Orbit.GravityInfluenceRadius / 1000.0)
				{
					Debug.LogError(string.Format("SPAWN MANAGER - Spawn rule \"{0}\" orbit periapsis distance is larger than gravity influence radius \"{3}\" (min: {1}, max: {2})", srd.RuleName, srd.Orbit.PeriapsisDistance_Km.Min, srd.Orbit.PeriapsisDistance_Km.Max, cb.Orbit.GravityInfluenceRadius / 1000.0));
					continue;
				}
				if (srd.Orbit.ApoapsisDistance_Km.Min > cb.Orbit.GravityInfluenceRadius / 1000.0 || srd.Orbit.ApoapsisDistance_Km.Max > cb.Orbit.GravityInfluenceRadius / 1000.0)
				{
					Debug.LogError(string.Format("SPAWN MANAGER - Spawn rule \"{0}\" orbit periapsis distance is larger than gravity influence radius \"{3}\" (min: {1}, max: {2})", srd.RuleName, srd.Orbit.ApoapsisDistance_Km.Min, srd.Orbit.ApoapsisDistance_Km.Max, cb.Orbit.GravityInfluenceRadius / 1000.0));
					continue;
				}
				SpawnRule rule = new SpawnRule
				{
					Name = srd.RuleName,
					StationName = srd.StationName,
					StationBlueprint = srd.StationBlueprint,
					Orbit = new SpawnRuleOrbit
					{
						CelestialBody = tmpCelBody,
						PeriapsisDistance = new SpawnRange<double>(srd.Orbit.PeriapsisDistance_Km.Min * 1000.0, srd.Orbit.PeriapsisDistance_Km.Max * 1000.0),
						ApoapsisDistance = new SpawnRange<double>(srd.Orbit.ApoapsisDistance_Km.Min * 1000.0, srd.Orbit.ApoapsisDistance_Km.Max * 1000.0),
						Inclination = new SpawnRange<double>(srd.Orbit.Inclination_Deg.Min % 360.0, srd.Orbit.Inclination_Deg.Max % 360.0),
						ArgumentOfPeriapsis = new SpawnRange<double>(srd.Orbit.ArgumentOfPeriapsis_Deg.Min % 360.0, srd.Orbit.ArgumentOfPeriapsis_Deg.Max % 360.0),
						LongitudeOfAscendingNode = new SpawnRange<double>(srd.Orbit.LongitudeOfAscendingNode_Deg.Min % 360.0, srd.Orbit.LongitudeOfAscendingNode_Deg.Max % 360.0),
						TrueAnomaly = new SpawnRange<double>(srd.Orbit.TrueAnomaly_Deg.Min % 360.0, srd.Orbit.TrueAnomaly_Deg.Max % 360.0),
						UseCurrentSolarSystemTime = srd.Orbit.UseCurrentSolarSystemTime
					},
					LocationType = tmpLocType,
					LocationTag = srd.LocationTag,
					RespawnTimerSec = System.Math.Max(-1.0, srd.RespawnTimer_Minutes * 60.0),
					ForceRespawn = srd.ForceRespawn,
					CheckPlayersDistance = srd.CheckPlayers_Km * 1000.0,
					AsteroidResourcesMultiplier = srd.AsteroidResourcesMultiplier,
					AngularVelocity = srd.AngularVelocity,
					DoNotRemoveVesselsFromSpawnSystem = srd.DoNotRemoveVesselsFromSpawnSystem,
					NumberOfClusters = tmpLocType != SpawnRuleLocationType.Random ? new SpawnRange<int>(1, 1) : srd.NumberOfClusters,
					ScenePool = tmpScenePool,
					LootPool = tmpLootPool,
					IsVisibleOnRadar = srd.IsVisibleOnRadar
				};
				if (rule.RespawnTimerSec > 0.0)
				{
					rule.RespawnTimerSec += MathHelper.RandomRange((0.0 - rule.RespawnTimerSec) / 10.0, rule.RespawnTimerSec / 10.0);
				}
				spawnRules.Add(rule);
			}
			return spawnRules;
		}
		return null;
	}

	private static bool GenerateScenePoolData(SpawnRuleData data, out List<SpawnRuleScene> retVal)
	{
		retVal = new List<SpawnRuleScene>();
		GameScenes.SceneId tmpSceneID = GameScenes.SceneId.None;
		if (data.LocationScenes != null)
		{
			foreach (SpawnRuleSceneData sc in data.LocationScenes)
			{
				if (sc.Scene.IsNullOrEmpty() || !Enum.TryParse<GameScenes.SceneId>(sc.Scene, out tmpSceneID))
				{
					Debug.LogWarning($"SPAWN MANAGER - Spawn rule \"{data.RuleName}\" scene \"{sc.Scene}\" is not valid");
				}
				else if (sc.SceneCount.Min < 0 || sc.SceneCount.Max <= 0 || sc.SceneCount.Min > sc.SceneCount.Max)
				{
					Debug.LogError($"SPAWN MANAGER - Spawn rule \"{data.RuleName}\" scene \"{sc.Scene}\" count is not valid (min: {sc.SceneCount.Min}, max: {sc.SceneCount.Max})");
				}
				else
				{
					int scenesInPool = MathHelper.RandomRange(sc.SceneCount.Min, sc.SceneCount.Max + 1);
					retVal.Add(new SpawnRuleScene
					{
						SceneID = tmpSceneID,
						Count = scenesInPool,
						CountMax = scenesInPool,
						HealthMin = sc.Health.Min,
						HealthMax = sc.Health.Max
					});
				}
			}
		}
		return retVal.Count > 0;
	}

	private static bool GenerateLootPoolData(SpawnRuleData data, SpawnRuleLocationType locType, out List<SpawnRuleLoot> retVal)
	{
		retVal = new List<SpawnRuleLoot>();
		if (data.Loot != null)
		{
			LootTier tmpTier = LootTier.T1;
			foreach (SpawnRuleLootData lc in data.Loot)
			{
				if (lc.Tier.IsNullOrEmpty() || !Enum.TryParse<LootTier>(lc.Tier, out tmpTier))
				{
					Debug.LogWarning($"SPAWN MANAGER - Spawn rule \"{data.RuleName}\" tier \"{lc.Tier}\" is not valid");
					continue;
				}
				if (lc.LootCount.Max <= 0 || lc.LootCount.Min > lc.LootCount.Max)
				{
					Debug.LogError($"SPAWN MANAGER - Spawn rule \"{data.RuleName}\" tier \"{lc.Tier}\" loot count is not valid (min: {lc.LootCount.Min}, max: {lc.LootCount.Max})");
					continue;
				}
				int poolSize = MathHelper.RandomRange(lc.LootCount.Min, lc.LootCount.Max + 1);
				if (poolSize <= 0)
				{
					continue;
				}
				List<LootItemData> lootItems = SpawnManager.GetLootItemDataFromCategory(data.RuleName, lc.CategoryName, tmpTier);
				if (lootItems.Count == 0)
				{
					continue;
				}
				if (lootItems.Count == 1)
				{
					retVal.Add(new SpawnRuleLoot
					{
						Count = poolSize,
						CountMax = poolSize,
						Data = lootItems[0]
					});
					continue;
				}
				int[] lootItemsCount = new int[lootItems.Count];
				for (int j = 0; j < poolSize; j++)
				{
					lootItemsCount[MathHelper.RandomRange(0, lootItemsCount.Length)]++;
				}
				for (int i = 0; i < lootItemsCount.Length; i++)
				{
					if (lootItemsCount[i] != 0)
					{
						retVal.Add(new SpawnRuleLoot
						{
							Count = lootItemsCount[i],
							CountMax = lootItemsCount[i],
							Data = lootItems[i]
						});
					}
				}
			}
		}
		return true;
	}

	public static void GenerateSpawnRuleSampleData(bool force = false)
	{
		string dir = GetDirectory();
		if (force || !File.Exists(dir + "Data/SpawnRules.json"))
		{
			List<SpawnRuleData> sampleData = new List<SpawnRuleData>
			{
				new SpawnRuleData
				{
					RuleName = "Fresh starts",
					Orbit = new SpawnRuleOrbitData
					{
						CelestialBody = "Bethyr",
						PeriapsisDistance_Km = new SpawnRange<double>(34782.0, 36782.0),
						ApoapsisDistance_Km = new SpawnRange<double>(34782.0, 44268.0),
						Inclination_Deg = new SpawnRange<float>(-1f, 1f),
						ArgumentOfPeriapsis_Deg = new SpawnRange<float>(-1f, 1f),
						LongitudeOfAscendingNode_Deg = new SpawnRange<float>(-1f, 1f),
						TrueAnomaly_Deg = new SpawnRange<float>(0f, 359.999f)
					},
					LocationType = "StartingScene",
					LocationTag = "StartingScene",
					IsVisibleOnRadar = false
				},
				new SpawnRuleData
				{
					RuleName = "Bethyr derelict outposts T1",
					Orbit = new SpawnRuleOrbitData
					{
						CelestialBody = "Bethyr",
						PeriapsisDistance_Km = new SpawnRange<double>(34282.0, 37782.0),
						ApoapsisDistance_Km = new SpawnRange<double>(34282.0, 43631.0),
						Inclination_Deg = new SpawnRange<float>(-1.5f, 1.5f),
						ArgumentOfPeriapsis_Deg = new SpawnRange<float>(-1.5f, 1.5f),
						LongitudeOfAscendingNode_Deg = new SpawnRange<float>(-1.5f, 1.5f),
						TrueAnomaly_Deg = new SpawnRange<float>(0f, 359.999f)
					},
					LocationType = "Random",
					RespawnTimer_Minutes = 180.0,
					NumberOfClusters = new SpawnRange<int>(30, 30),
					LocationScenes = new List<SpawnRuleSceneData>
					{
						new SpawnRuleSceneData
						{
							Scene = "AltCorp_StartingModule",
							SceneCount = new SpawnRange<int>(10, 10)
						},
						new SpawnRuleSceneData
						{
							Scene = "AltCorp_AirLock",
							SceneCount = new SpawnRange<int>(15, 15)
						},
						new SpawnRuleSceneData
						{
							Scene = "AltCorp_LifeSupportModule",
							SceneCount = new SpawnRange<int>(10, 10)
						},
						new SpawnRuleSceneData
						{
							Scene = "AltCorp_CorridorModule",
							SceneCount = new SpawnRange<int>(7, 7)
						},
						new SpawnRuleSceneData
						{
							Scene = "AltCorp_Corridor45TurnModule",
							SceneCount = new SpawnRange<int>(7, 7)
						},
						new SpawnRuleSceneData
						{
							Scene = "AltCorp_Corridor45TurnRightModule",
							SceneCount = new SpawnRange<int>(6, 6)
						},
						new SpawnRuleSceneData
						{
							Scene = "Generic_Debris_JuncRoom001",
							SceneCount = new SpawnRange<int>(16, 16)
						},
						new SpawnRuleSceneData
						{
							Scene = "Generic_Debris_JuncRoom002",
							SceneCount = new SpawnRange<int>(16, 16)
						},
						new SpawnRuleSceneData
						{
							Scene = "Generic_Debris_Corridor001",
							SceneCount = new SpawnRange<int>(16, 16)
						},
						new SpawnRuleSceneData
						{
							Scene = "Generic_Debris_Corridor002",
							SceneCount = new SpawnRange<int>(17, 17)
						}
					},
					Loot = new List<SpawnRuleLootData>
					{
						new SpawnRuleLootData
						{
							CategoryName = "Resources",
							Tier = "T1",
							LootCount = new SpawnRange<int>(50, 50)
						},
						new SpawnRuleLootData
						{
							CategoryName = "Fuel",
							Tier = "T1",
							LootCount = new SpawnRange<int>(30, 30)
						},
						new SpawnRuleLootData
						{
							CategoryName = "Weapons",
							Tier = "T1",
							LootCount = new SpawnRange<int>(25, 25)
						},
						new SpawnRuleLootData
						{
							CategoryName = "Ammo",
							Tier = "T1",
							LootCount = new SpawnRange<int>(70, 70)
						},
						new SpawnRuleLootData
						{
							CategoryName = "Resources",
							Tier = "T2",
							LootCount = new SpawnRange<int>(35, 35)
						},
						new SpawnRuleLootData
						{
							CategoryName = "Vanity",
							Tier = "T1",
							LootCount = new SpawnRange<int>(15, 15)
						}
					},
					IsVisibleOnRadar = false
				},
				new SpawnRuleData
				{
					RuleName = "Bethyr fuel outpost T1",
					Orbit = new SpawnRuleOrbitData
					{
						CelestialBody = "Bethyr",
						PeriapsisDistance_Km = new SpawnRange<double>(40782.0, 40782.0),
						ApoapsisDistance_Km = new SpawnRange<double>(63787.0, 63787.0),
						Inclination_Deg = new SpawnRange<float>(88f, 88f),
						ArgumentOfPeriapsis_Deg = new SpawnRange<float>(0f, 0f),
						LongitudeOfAscendingNode_Deg = new SpawnRange<float>(0f, 0f),
						TrueAnomaly_Deg = new SpawnRange<float>(0f, 359.999f)
					},
					NumberOfClusters = new SpawnRange<int>(1, 1),
					RespawnTimer_Minutes = 180.0,
					LocationType = "Emergency_Staging_Post_D8",
					Loot = new List<SpawnRuleLootData>
					{
						new SpawnRuleLootData
						{
							CategoryName = "Resources",
							Tier = "T1",
							LootCount = new SpawnRange<int>(50, 50)
						},
						new SpawnRuleLootData
						{
							CategoryName = "Fuel",
							Tier = "T1",
							LootCount = new SpawnRange<int>(30, 30)
						},
						new SpawnRuleLootData
						{
							CategoryName = "Resources",
							Tier = "T2",
							LootCount = new SpawnRange<int>(35, 35)
						}
					},
					IsVisibleOnRadar = false
				},
				new SpawnRuleData
				{
					RuleName = "Bethyr loot outpost T2",
					Orbit = new SpawnRuleOrbitData
					{
						CelestialBody = "Bethyr",
						PeriapsisDistance_Km = new SpawnRange<double>(40782.0, 40782.0),
						ApoapsisDistance_Km = new SpawnRange<double>(63787.0, 63787.0),
						Inclination_Deg = new SpawnRange<float>(88f, 88f),
						ArgumentOfPeriapsis_Deg = new SpawnRange<float>(0f, 0f),
						LongitudeOfAscendingNode_Deg = new SpawnRange<float>(0f, 0f),
						TrueAnomaly_Deg = new SpawnRange<float>(0f, 359.999f)
					},
					NumberOfClusters = new SpawnRange<int>(1, 1),
					RespawnTimer_Minutes = 180.0,
					LocationType = "Automated_Refinery_B7",
					Loot = new List<SpawnRuleLootData>
					{
						new SpawnRuleLootData
						{
							CategoryName = "Resources",
							Tier = "T1",
							LootCount = new SpawnRange<int>(50, 50)
						},
						new SpawnRuleLootData
						{
							CategoryName = "Fuel",
							Tier = "T1",
							LootCount = new SpawnRange<int>(30, 30)
						},
						new SpawnRuleLootData
						{
							CategoryName = "Weapons",
							Tier = "T1",
							LootCount = new SpawnRange<int>(25, 25)
						},
						new SpawnRuleLootData
						{
							CategoryName = "Ammo",
							Tier = "T1",
							LootCount = new SpawnRange<int>(70, 70)
						},
						new SpawnRuleLootData
						{
							CategoryName = "Resources",
							Tier = "T2",
							LootCount = new SpawnRange<int>(35, 35)
						},
						new SpawnRuleLootData
						{
							CategoryName = "Vanity",
							Tier = "T1",
							LootCount = new SpawnRange<int>(15, 15)
						}
					},
					IsVisibleOnRadar = true
				},
				new SpawnRuleData
				{
					RuleName = "Bethyr asteroids",
					Orbit = new SpawnRuleOrbitData
					{
						CelestialBody = "Bethyr",
						PeriapsisDistance_Km = new SpawnRange<double>(40982.0, 41782.0),
						ApoapsisDistance_Km = new SpawnRange<double>(40982.0, 48109.0),
						Inclination_Deg = new SpawnRange<float>(10f, 10f),
						ArgumentOfPeriapsis_Deg = new SpawnRange<float>(0f, 0f),
						LongitudeOfAscendingNode_Deg = new SpawnRange<float>(0f, 0f),
						TrueAnomaly_Deg = new SpawnRange<float>(0f, 359.999f)
					},
					LocationType = "Random",
					RespawnTimer_Minutes = -1.0,
					NumberOfClusters = new SpawnRange<int>(8, 8),
					LocationScenes = new List<SpawnRuleSceneData>
					{
						new SpawnRuleSceneData
						{
							Scene = "Asteroid01",
							SceneCount = new SpawnRange<int>(1, 1)
						},
						new SpawnRuleSceneData
						{
							Scene = "Asteroid02",
							SceneCount = new SpawnRange<int>(1, 1)
						},
						new SpawnRuleSceneData
						{
							Scene = "Asteroid03",
							SceneCount = new SpawnRange<int>(1, 1)
						},
						new SpawnRuleSceneData
						{
							Scene = "Asteroid04",
							SceneCount = new SpawnRange<int>(1, 1)
						},
						new SpawnRuleSceneData
						{
							Scene = "Asteroid05",
							SceneCount = new SpawnRange<int>(1, 1)
						},
						new SpawnRuleSceneData
						{
							Scene = "Asteroid06",
							SceneCount = new SpawnRange<int>(1, 1)
						},
						new SpawnRuleSceneData
						{
							Scene = "Asteroid07",
							SceneCount = new SpawnRange<int>(1, 1)
						},
						new SpawnRuleSceneData
						{
							Scene = "Asteroid08",
							SceneCount = new SpawnRange<int>(1, 1)
						}
					},
					IsVisibleOnRadar = false
				}
			};
			JsonSerialiser.SerializeToFile(sampleData, dir + "Data/SpawnRules.json", JsonSerialiser.Formatting.Indented, new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			});
		}
	}
}
