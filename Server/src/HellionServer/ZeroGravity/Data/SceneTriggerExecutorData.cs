using System.Collections.Generic;

namespace ZeroGravity.Data;

public class SceneTriggerExecutorData : ISceneData
{
	public int InSceneID;

	public int DefaultStateID;

	public TagAction TagAction;

	public string Tags;

	public List<SceneTriggerExecutorStateData> States;

	public List<SceneTriggerProximityData> ProximityTriggers;
}
