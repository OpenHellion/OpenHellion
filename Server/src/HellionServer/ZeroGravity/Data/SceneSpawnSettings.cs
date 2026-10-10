using System;

namespace ZeroGravity.Data;

[Serializable]
public class SceneSpawnSettings : ISceneData
{
	public SpawnSettingsCase Case = SpawnSettingsCase.EnableIf;

	public string Tag;

	public float MinHealth = -1f;

	public float MaxHealth = -1f;
}
