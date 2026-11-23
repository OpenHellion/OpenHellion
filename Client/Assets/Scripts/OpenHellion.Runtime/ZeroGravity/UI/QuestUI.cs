using System;
using OpenHellion;
using OpenHellion.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.UI
{
	public class QuestUI : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public QuestSystemUI QuestSystem;

		public Quest Quest;

		public Text Name;

		public Text Status;

		public GameObject Selected;

		public GameObject TrackingButton;

		public GameObject Tracking;

		public GameObject Completed;

		public GameObject LinkedQuest;

		public Text LinkedQuestName;

		private InGameGUI _inGameGUI;

		public void Init(InGameGUI inGameGUI)
		{
			_inGameGUI = inGameGUI;
		}

		private void Start()
		{
			Name.text = Quest.Name;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			QuestSystem.SelectedQuest = this;
			Selected.SetActive(true);
			QuestSystem.RefreshSelectedQuest();
		}

		public void SelectQuest()
		{
		}

		public void RefreshQuestUI()
		{
			if (Quest.Status == QuestStatus.Completed)
			{
				Status.text = Localization.QuestCompleted;
				Status.color = Colors.Green;
			}
			else if (Quest.Status == QuestStatus.Failed)
			{
				Status.text = Localization.QuestFailed;
				Status.color = Colors.Red;
			}
			else
			{
				Status.text = Localization.InProgress;
				Status.color = Colors.Yellow;
			}

			if (Quest.QuestObject.ParentQuest != null)
			{
				LinkedQuest.Activate(true);
				LinkedQuestName.text = Quest.QuestObject.ParentQuest.Name;
			}
			else
			{
				LinkedQuest.Activate(false);
				LinkedQuestName.text = string.Empty;
			}

			TrackingButton.SetActive(Quest.Status != QuestStatus.Completed);
			Completed.SetActive(Quest.Status == QuestStatus.Completed);
			Tracking.SetActive(_inGameGUI.CurrentTrackingQuest == Quest);
			Selected.SetActive(this == QuestSystem.SelectedQuest);
		}

		public void TrackQuest()
		{
			foreach (QuestUI allQuest in QuestSystem.AllQuests)
			{
				allQuest.Tracking.SetActive(false);
			}

			if (_inGameGUI.CurrentTrackingQuest == Quest)
			{
				_inGameGUI.SetCurrentTrackingQuest(null);
				return;
			}

			Tracking.SetActive(true);
			_inGameGUI.SetCurrentTrackingQuest(Quest);
		}
	}
}
