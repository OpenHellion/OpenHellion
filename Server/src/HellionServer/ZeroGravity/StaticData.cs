using System.Collections.Generic;
using System.IO;
using OpenHellion.IO;
using ZeroGravity.Data;
using ZeroGravity.Math;

namespace ZeroGravity;

public static class StaticData
{
	private static SolarSystemData _SolarSystem;

	private static Dictionary<string, ServerCollisionData> _collisionDataList;

	private static List<StructureSceneData> _StructuresDataList;

	private static List<CelestialSceneData> _AsteroidDataList;

	private static Dictionary<short, DynamicObjectData> _DynamicObjectsDataList;

	private static List<ItemIngredientsData> _ItemsIngredients;

	private static List<QuestData> _QuestsData;

	private static List<ItemCompoundType> _DefaultBlueprints;

	public static SolarSystemData SolarSystem
	{
		get
		{
			if (_SolarSystem == null)
			{
				LoadData();
			}
			return _SolarSystem;
		}
	}

	public static Dictionary<string, ServerCollisionData> CollisionDataList
	{
		get
		{
			if (_collisionDataList == null)
			{
				LoadData();
			}
			return _collisionDataList;
		}
	}

	public static List<StructureSceneData> StructuresDataList
	{
		get
		{
			if (_StructuresDataList == null)
			{
				LoadData();
			}
			return _StructuresDataList;
		}
	}

	public static List<CelestialSceneData> AsteroidDataList
	{
		get
		{
			if (_AsteroidDataList == null)
			{
				LoadData();
			}
			return _AsteroidDataList;
		}
	}

	public static Dictionary<short, DynamicObjectData> DynamicObjectsDataList
	{
		get
		{
			if (_DynamicObjectsDataList == null)
			{
				LoadData();
			}
			return _DynamicObjectsDataList;
		}
	}

	public static List<ItemIngredientsData> ItemsIngredients
	{
		get
		{
			if (_ItemsIngredients == null)
			{
				LoadData();
			}
			return _ItemsIngredients;
		}
	}

	public static List<QuestData> QuestsData
	{
		get
		{
			if (_QuestsData == null)
			{
				LoadData();
			}
			return _QuestsData;
		}
	}

	public static List<ItemCompoundType> DefaultBlueprints
	{
		get
		{
			if (_DefaultBlueprints == null)
			{
				LoadData();
			}
			return _DefaultBlueprints;
		}
	}

	public static void LoadData()
	{
		string dir = !Server.ConfigDir.IsNullOrEmpty() && Directory.Exists(Server.ConfigDir + "Data") ? Server.ConfigDir : "";
		_SolarSystem = JsonSerialiser.Load<SolarSystemData>(dir + "Data/SolarSystem.json");
		_StructuresDataList = JsonSerialiser.Load<List<StructureSceneData>>(dir + "Data/Structures.json");
		_collisionDataList = new Dictionary<string, ServerCollisionData>();
		_AsteroidDataList = JsonSerialiser.Load<List<CelestialSceneData>>(dir + "Data/Asteroids.json");
		_ItemsIngredients = JsonSerialiser.Load<List<ItemIngredientsData>>(dir + "Data/ItemsIngredients.json");
		_QuestsData = JsonSerialiser.Load<List<QuestData>>(dir + "Data/Quests.json");
		_DefaultBlueprints = new List<ItemCompoundType>();
		List<DynamicObjectData> tmpDynamicObjectDataList = JsonSerialiser.Load<List<DynamicObjectData>>(dir + "Data/DynamicObjects.json");
		_DynamicObjectsDataList = new Dictionary<short, DynamicObjectData>();
		foreach (DynamicObjectData data in tmpDynamicObjectDataList)
		{
			_DynamicObjectsDataList.Add(data.ItemID, data);
			if (data.DefaultBlueprint)
			{
				_DefaultBlueprints.Add(data.CompoundType);
			}
		}
		foreach (StructureSceneData a2 in _StructuresDataList)
		{
			if (!_collisionDataList.ContainsKey(a2.Collision))
			{
				a2.Colliders = JsonSerialiser.Load<ServerCollisionData>(dir + "Data/Collision/" + a2.Collision + ".json");
				_collisionDataList.Add(a2.Collision, a2.Colliders);
			}
		}
		foreach (CelestialSceneData a in _AsteroidDataList)
		{
			if (a is { Collision: not null } && !_collisionDataList.ContainsKey(a.Collision))
			{
				a.Colliders = JsonSerialiser.Load<ServerCollisionData>(dir + "Data/Collision/" + a.Collision + ".json");
				_collisionDataList.Add(a.Collision, a.Colliders);
			}
		}

		Debug.Log("Loaded static data.");
	}

	public static float GetVesselExposureDamage(double distance)
	{
		if (SolarSystem.VesselExposureValues == null)
		{
			return 1f;
		}
		return SolarSystem.VesselExposureValues[(int)(MathHelper.Clamp(distance / SolarSystem.ExposureRange, 0.0, 1.0) * 99.0)];
	}

	public static float GetPlayerExposureDamage(double distance)
	{
		if (SolarSystem.PlayerExposureValues == null)
		{
			return 0f;
		}
		return SolarSystem.PlayerExposureValues[(int)(MathHelper.Clamp(distance / SolarSystem.ExposureRange, 0.0, 1.0) * 99.0)];
	}

	public static float GetBaseSunExposure(double distance)
	{
		if (SolarSystem.BaseSunExposureValues == null)
		{
			return 1f;
		}
		return SolarSystem.BaseSunExposureValues[(int)(MathHelper.Clamp(distance / SolarSystem.ExposureRange, 0.0, 1.0) * 99.0)];
	}
}
