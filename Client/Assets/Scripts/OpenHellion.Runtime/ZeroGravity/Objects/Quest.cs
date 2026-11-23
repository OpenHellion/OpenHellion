using System.Collections.Generic;
using System.Linq;
using OpenHellion;
using OpenHellion.Social.RichPresence;
using UnityEngine;
using ZeroGravity.Data;
using ZeroGravity.Network;

namespace ZeroGravity.Objects
{
	public class Quest
	{
		public uint ID;

		public List<QuestTrigger> QuestTriggers;

		public QuestTriggerDependencyTpe ActivationDependencyTpe;

		public QuestTriggerDependencyTpe CompletionDependencyTpe;

		public QuestStatus Status;

		public List<uint> DependencyQuests;

		public QuestObject QuestObject;

		public bool CanSkip;

		public string Name
		{
			get
			{
				if (QuestObject != null)
				{
					return Localization.GetLocalizedField(QuestObject.Name, useDefault: true);
				}

				Debug.LogError("Quest has missing QuestObject (Quest ID: " + ID);
				return string.Empty;
			}
		}

		public string Description
		{
			get
			{
				if (QuestObject != null)
				{
					return Localization.GetLocalizedField(QuestObject.Description, useDefault: true);
				}

				Debug.LogError("Quest has missing QuestObject (Quest ID: " + ID);
				return string.Empty;
			}
		}

		public bool IsFinished => Status == QuestStatus.Completed || Status == QuestStatus.Failed;

		public Quest(QuestData data, QuestCollectionObject questCollection)
		{
			Quest quest = this;
			ID = data.ID;
			QuestObject = questCollection.Quests.FirstOrDefault((QuestObject m) => m.ID == data.ID);
			QuestTriggers = data.QuestTriggers.Select((QuestTriggerData m) => new QuestTrigger(questCollection, quest, m)).ToList();
			ActivationDependencyTpe = data.ActivationDependencyTpe;
			CompletionDependencyTpe = data.CompletionDependencyTpe;
			DependencyQuests = data.DependencyQuests;
			QuestObject.Quest = this;
			if (QuestObject != null && QuestObject.Achivement != 0)
			{
				CanSkip = RichPresenceManager.GetAchievement(QuestObject.Achivement);
			}
		}

		public QuestTrigger SetDetails(QuestDetails details)
		{
			Status = details.Status;
			QuestTrigger completed = null;
			foreach (QuestTriggerDetails qtd in details.QuestTriggers)
			{
				QuestTrigger trigger = QuestTriggers.FirstOrDefault((QuestTrigger m) => m.ID == qtd.ID);
				if (trigger == null)
				{
					Diagnostics.Assert(false, $"quest {ID} has no trigger {qtd.ID}, its status {qtd.Status} was dropped");
					continue;
				}

				if (trigger.Status != qtd.Status && qtd.Status == QuestStatus.Completed)
				{
					completed = trigger;
				}

				trigger.SetDetails(qtd);
			}

			if (Status == QuestStatus.Completed && QuestObject != null && QuestObject.Achivement != 0)
			{
				RichPresenceManager.SetAchievement(QuestObject.Achivement);
			}

			return completed;
		}
	}
}
