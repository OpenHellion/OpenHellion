using System;
using System.Collections.Generic;

namespace ZeroGravity;

public class PersistenceObjectDataSpawnManager : PersistenceObjectData
{
	public class SpawnRule
	{
		public double CurrTimerSec;

		public List<SpawnRuleScene> ScenePool;

		public List<SpawnRuleLoot> LootPool;

		public List<long> SpawnedVessels;
	}

	public class SpawnRuleScene
	{
		public List<Tuple<long, int>> Vessels;
	}

	public class SpawnRuleLoot
	{
		public List<long> DynamicObjects;
	}

	public Dictionary<string, SpawnRule> SpawnRules;
}
