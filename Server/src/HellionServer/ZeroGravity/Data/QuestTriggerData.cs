using System;
using ProtoBuf;

namespace ZeroGravity.Data;

[Serializable]
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class QuestTriggerData : ISceneData
{
	public uint ID;

	public uint BatchID;

	public QuestTriggerType Type;

	public string Station;

	public string Tag;

	public CelestialBodyGUID Celestial;

	public uint DependencyBatchID;

	public QuestTriggerDependencyTpe DependencyTpe;

	public string SpawnRuleName;
}
