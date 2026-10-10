using System;
using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Data;

[Serializable]
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class QuestData : ISceneData
{
	public uint ID;

	public List<QuestTriggerData> QuestTriggers;

	public QuestTriggerDependencyTpe ActivationDependencyTpe;

	public QuestTriggerDependencyTpe CompletionDependencyTpe;

	public List<uint> DependencyQuests;

	public bool AutoActivate;
}
