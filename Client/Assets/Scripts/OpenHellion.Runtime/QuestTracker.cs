using System.Collections.Generic;
using System.Linq;
using OpenHellion;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using ZeroGravity;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;
using ZeroGravity.UI;

public class QuestTracker : MonoBehaviour
{
	private static readonly int SwitchHash = Animator.StringToHash("Switch");

	private static readonly int CompletedHash = Animator.StringToHash("Completed");

	private static readonly int CloseHash = Animator.StringToHash("Close");

	public Dictionary<QuestTrigger, TaskUI> Tasks = new Dictionary<QuestTrigger, TaskUI>();

	public TaskUI TaskUIPrefab;

	public Transform Content;

	private Quest _currentQuest;

	public Animator Animator;

	public Text QuestName;

	public Text AltQuestName;

	public Text AltTriggerName;

	public void Refresh(Quest quest)
	{
		if (quest == null || quest.Status != QuestStatus.Active)
		{
			if (_currentQuest != null && gameObject.activeInHierarchy)
			{
				foreach (TaskUI task in Tasks.Values.Where(m => m.gameObject.activeSelf))
				{
					task.Animator.SetTrigger(CloseHash);
				}

				Animator.SetBool(CloseHash, value: true);
			}
			else if (_currentQuest != null)
			{
				gameObject.SetActive(value: false);
			}

			_currentQuest = null;
			return;
		}

		if (quest != _currentQuest)
		{
			foreach (TaskUI task in Tasks.Values.Where(m => m != null))
			{
				Destroy(task.gameObject);
			}

			Tasks.Clear();
			_currentQuest = quest;
			gameObject.SetActive(value: true);
			Animator.SetBool(CloseHash, value: false);
			foreach (QuestTrigger trigger in quest.QuestTriggers.Where(m => m.Type != QuestTriggerType.Activate))
			{
				TaskUI task = Instantiate(TaskUIPrefab, Content);
				task.transform.Reset();
				task.transform.SetAsLastSibling();
				task.Name.text = trigger.Name;
				task.gameObject.SetActive(trigger.Status == QuestStatus.Active);
				Tasks.Add(trigger, task);
			}

			QuestName.text = Localization.GetLocalizedField(quest.Name, useDefault: true).ToUpper();
		}

		foreach ((QuestTrigger trigger, TaskUI task) in Tasks)
		{
			if (trigger.Status == QuestStatus.Active)
			{
				task.gameObject.SetActive(value: true);
			}
			else if (trigger.Status == QuestStatus.Completed && task.gameObject.activeInHierarchy)
			{
				task.Animator.SetBool(CompletedHash, value: true);
			}
			else
			{
				task.gameObject.SetActive(value: false);
			}
		}

		Diagnostics.Assert(Tasks.Keys.Any(m => m.Status == QuestStatus.Active), $"quest tracker shows active quest {quest.ID} without active tasks");
	}

	public void TriggerNonTrackedTask(QuestTrigger trigger)
	{
		AltQuestName.text = trigger.Quest.Name;
		AltTriggerName.text = trigger.Name;
		Animator.SetTrigger(SwitchHash);
	}
}
