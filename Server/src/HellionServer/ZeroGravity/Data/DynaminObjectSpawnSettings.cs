using System;

namespace ZeroGravity.Data;

[Serializable]
public class DynaminObjectSpawnSettings : ISceneData
{
	public SpawnSettingsCase Case = SpawnSettingsCase.EnableIf;

	public string Tag;

	public float RespawnTime = -1f;

	public float SpawnChance = -1f;

	public float MinHealth = -1f;

	public float MaxHealth = -1f;

	public float WearMultiplier = 1f;
}
