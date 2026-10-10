using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;

namespace ZeroGravity.Objects;

public class Quest
{
	public uint ID;

	public List<QuestTrigger> QuestTriggers = [];

	public QuestTriggerDependencyTpe ActivationDependencyTpe;

	public QuestTriggerDependencyTpe CompletionDependencyTpe;

	public QuestStatus Status;

	public List<uint> DependencyQuests;

	public bool AutoActivate;

	public Player Player;

	public bool IsFineshed => Status is QuestStatus.Completed or QuestStatus.Failed;

	private Quest()
	{
	}

	public static async Task<Quest> CreateQuestAsync(QuestData data, Player player)
	{
		var quest = new Quest
		{
			ID = data.ID,
			ActivationDependencyTpe = data.ActivationDependencyTpe,
			CompletionDependencyTpe = data.CompletionDependencyTpe,
			DependencyQuests = data.DependencyQuests,
			AutoActivate = data.AutoActivate,
			Player = player
		};
		foreach (var trigger in data.QuestTriggers)
		{
			quest.QuestTriggers.Add(await QuestTrigger.CreateQuestTriggerAsync(quest, trigger));
		}
		return quest;
	}

	public static async Task<List<Quest>> CreateQuestsAsync(List<QuestData> data, Player player)
	{
		List<Quest> quests = [];
		foreach (QuestData element in data)
		{
			Quest quest = new()
			{
				ID = element.ID,
				ActivationDependencyTpe = element.ActivationDependencyTpe,
				CompletionDependencyTpe = element.CompletionDependencyTpe,
				DependencyQuests = element.DependencyQuests,
				AutoActivate = element.AutoActivate,
				Player = player
			};

			if (element.QuestTriggers?.Count > 0)
			{
				quest.QuestTriggers = await QuestTrigger.CreateQuestTriggersAsync(quest, element.QuestTriggers);
			}

			quests.Add(quest);
		}

		return quests;
	}

	public QuestDetails GetDetails()
	{
		return new QuestDetails
		{
			ID = ID,
			QuestTriggers = QuestTriggers != null ? QuestTriggers.Select((QuestTrigger m) => m.GetDetails()).ToList() : null,
			Status = Status
		};
	}

	public void UpdateActivation()
	{
		if (Status == QuestStatus.Inactive)
		{
			List<QuestTrigger> batch = QuestTriggers.Where((QuestTrigger m) => m.Type == QuestTriggerType.Activate).ToList();
			int completedCount = batch.Count((QuestTrigger m) => m.Status == QuestStatus.Completed);
			if ((ActivationDependencyTpe == QuestTriggerDependencyTpe.Any && completedCount > 0) || (ActivationDependencyTpe == QuestTriggerDependencyTpe.All && completedCount == batch.Count))
			{
				Status = QuestStatus.Active;
			}
		}
	}

	public void UpdateCompletion()
	{
		if (!IsFineshed)
		{
			List<QuestTrigger> batch = QuestTriggers.Where((QuestTrigger m) => m.Type == QuestTriggerType.Complete).ToList();
			int completedCount = batch.Count((QuestTrigger m) => m.Status == QuestStatus.Completed);
			if ((CompletionDependencyTpe == QuestTriggerDependencyTpe.Any && completedCount > 0) || (CompletionDependencyTpe == QuestTriggerDependencyTpe.All && completedCount == batch.Count))
			{
				Status = QuestStatus.Completed;
			}
		}
	}
}
