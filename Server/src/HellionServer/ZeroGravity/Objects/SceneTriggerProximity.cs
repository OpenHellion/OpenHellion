using System.Collections.Generic;

namespace ZeroGravity.Objects;

public class SceneTriggerProximity
{
	public int TriggerID;

	public int ActiveStateID;

	public int InactiveStateID;

	public List<long> ObjectsInTrigger = new List<long>();
}
