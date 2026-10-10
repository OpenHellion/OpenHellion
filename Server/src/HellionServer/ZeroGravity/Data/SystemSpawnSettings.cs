using System;

namespace ZeroGravity.Data;

[Serializable]
public class SystemSpawnSettings : ISceneData
{
	public SpawnSettingsCase Case = SpawnSettingsCase.EnableIf;

	public string Tag;

	public float ResourceRequirementMultiplier = 1f;
}
