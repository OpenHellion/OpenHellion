using System;
using System.Collections.Generic;
using System.Linq;
using OpenHellion.Net;
using UnityEngine;
using UnityEngine.Events;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.LevelDesign
{
	public class SceneQuestTrigger : MonoBehaviour
	{
		[Tooltip("Used only if Task is not assigned"), ShowIf(nameof(Task), null)]
		public uint QuestID;

		[Tooltip("Used only if Task is not assigned"), ShowIf(nameof(Task), null)]
		public uint QuestTriggerID;

		[Tooltip("The event that should complete this quest.")]
		public SceneQuestTriggerEvent TriggerEvent;

		[Tooltip("The quest task to complete.")]
		public QuestTaskObject Task;

		[Tooltip("A list of additional quest tasks to complete if Task is completed."), HideIf(nameof(Task), null)]
		public List<QuestTaskObject> AdditionalTasksToComplete;

		[Tooltip("Whether or not the quest indicator is visible.")]
		public QuestIndicatorVisibility Visibility = QuestIndicatorVisibility.AlwaysVisible;

		[Tooltip("The maximum distance the quest indicator can be seen from."),
		 ShowIf(nameof(Visibility), QuestIndicatorVisibility.Proximity)]
		public float ProximityDistance = 10f;

		[Tooltip("Position of the quest indicator relative to the object.")]
		public Vector3 QuestIndicatorPosition = new Vector3(0f, 1f, 0f);

		public GameObject QuestTriggerObject;

		[Tooltip("Collider enabled when quest is activated.")]
		public Collider Collider;

		[Tooltip("An optional localization key for a tooltip to be shown on the screen.")]
		public string QuickTooltip;

		public UnityEvent PostTriggerEvent;

		[Tooltip("Events executed when task becomes active.")]
		public UnityEvent ActiveTriggerEvent;

		[Tooltip("Events executed when task is completed.")]
		public UnityEvent CompleteTriggerEvent;

		private Quest _quest;

		private QuestTrigger _questTrigger;

		private SpaceObjectVessel _parentVessel;

		private double _lastActivationTime;

		public QuestTrigger QuestTrigger => _questTrigger;

		public SpaceObjectVessel ParentVessel => _parentVessel;

		private void Awake()
		{
			_parentVessel = gameObject.GetComponentInParent<SpaceObjectVessel>();
		}

		private void Start()
		{
			if (Collider != null)
			{
				Collider.enabled = false;
			}

			if (Task != null)
			{
				QuestID = Task.QuestID;
				QuestTriggerID = Task.QuestTriggerID;
				_quest = _parentVessel.World.Quests.FirstOrDefault((Quest m) => m.ID == QuestID);
				_questTrigger = _quest.QuestTriggers.FirstOrDefault((QuestTrigger m) => m.ID == QuestTriggerID);
				OnQuestTriggerUpdate();
				UpdateQuestTriggerMarker();
				UpdateAvailableQuestMarker();
				if (TriggerEvent == SceneQuestTriggerEvent.EnterTrigger ||
				    TriggerEvent == SceneQuestTriggerEvent.ExitTrigger)
				{
					gameObject.SetLayerRecursively("Triggers");
				}

				if (TriggerEvent == SceneQuestTriggerEvent.Interact)
				{
					gameObject.SetLayerRecursively("InteractiveTriggers");
				}
			}
			else
			{
				Debug.LogError("Scene Quest Trigger " + gameObject.name + " has no Task assigned!");
			}
		}

		private void OnDestroy()
		{
			_parentVessel.World.InGameGUI.QuestIndicators.StopQuestIndicator(this);
		}

		private void OnDisable()
		{
			_parentVessel.World.InGameGUI.QuestIndicators.StopQuestIndicator(this);
		}

		public void TriggerEvents()
		{
			PostTriggerEvent.Invoke();
			if (QuickTooltip != string.Empty)
			{
				_parentVessel.World.InGameGUI.QuickTip(Localization.GetLocalizedField(QuickTooltip));
			}
		}

		public void CompleteQuest()
		{
			if (Time.realtimeSinceStartup - _lastActivationTime < 1.0 || (_quest.DependencyQuests != null &&
			                                                              _parentVessel.World.Quests.FirstOrDefault((Quest m) =>
				                                                              _quest.DependencyQuests.Contains(m.ID) &&
				                                                              m.Status != QuestStatus.Completed) !=
			                                                              null))
			{
				return;
			}

			_lastActivationTime = Time.realtimeSinceStartup;
			if (_quest.IsFinished || _questTrigger == null || _questTrigger.Status != QuestStatus.Active ||
			    !_questTrigger.CheckLocation(_parentVessel.World, ParentVessel))
			{
				return;
			}

			NetworkController.SendAndForget(new QuestTriggerMessage
			{
				QuestID = QuestID,
				TriggerID = QuestTriggerID
			});
			foreach (QuestTaskObject item in AdditionalTasksToComplete)
			{
				NetworkController.SendAndForget(new QuestTriggerMessage
				{
					QuestID = item.QuestID,
					TriggerID = item.QuestTriggerID
				});
			}
		}

		public void UpdateQuestTriggerMarker()
		{
			Quest quest = null;
			if (_parentVessel.World.InGameGUI.CurrentTrackingQuest != null)
			{
				quest = _parentVessel.World.InGameGUI.CurrentTrackingQuest;
			}

			if (quest != null)
			{
				if (this._quest == quest && _questTrigger.Status == QuestStatus.Active &&
				    SceneHelper.CompareTags(ParentVessel.Tag, SceneTagObject.TagsToString(Task.Tags)))
				{
					_parentVessel.World.InGameGUI.QuestIndicators.AddQuestIndicator(this);
				}
				else if ((_questTrigger.Type != QuestTriggerType.Activate ||
				          _questTrigger.Status != QuestStatus.Active) &&
				         SceneHelper.CompareTags(ParentVessel.Tag, SceneTagObject.TagsToString(Task.Tags)))
				{
					_parentVessel.World.InGameGUI.QuestIndicators.StopQuestIndicator(this);
				}
			}
			else if (_questTrigger.Type != QuestTriggerType.Activate || _questTrigger.Status != QuestStatus.Active)
			{
				_parentVessel.World.InGameGUI.QuestIndicators.StopQuestIndicator(this);
			}
		}

		public void UpdateAvailableQuestMarker()
		{
			bool flag = true;
			if (Task.Tags.Count > 0)
			{
				flag = SceneHelper.CompareTags(ParentVessel.Tag, SceneTagObject.TagsToString(Task.Tags));
			}

			if (_questTrigger.Type != QuestTriggerType.Activate || _questTrigger.Status != QuestStatus.Active || !flag)
			{
				return;
			}

			bool flag2 = true;
			if (_questTrigger.Quest != null && _questTrigger.Quest.DependencyQuests != null)
			{
				foreach (uint dependencyQuestID in _questTrigger.Quest.DependencyQuests)
				{
					Quest quest = _parentVessel.World.Quests.FirstOrDefault((Quest m) => m.ID == dependencyQuestID);
					if (quest != null && quest.Status != QuestStatus.Completed)
					{
						flag2 = false;
						break;
					}
				}
			}

			if (flag2)
			{
				_parentVessel.World.InGameGUI.QuestIndicators.AddAvailableQuestIndicator(this);
			}
		}

		public void OnQuestTriggerUpdate()
		{
			if (Collider != null)
			{
				Collider.enabled = false;
			}

			if (QuestTriggerObject != null)
			{
				QuestTriggerObject.SetActive(_questTrigger.Status == QuestStatus.Active);
			}

			if (_questTrigger.Status == QuestStatus.Active)
			{
				if (Collider != null)
				{
					Collider.enabled = true;
				}

				ActiveTriggerEvent.Invoke();
			}

			if (_questTrigger.Status == QuestStatus.Completed)
			{
				CompleteTriggerEvent.Invoke();
			}
		}

		private void OnDrawGizmos()
		{
			Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
			if (GetComponent<BoxCollider>() != null)
			{
				Gizmos.color = new Color(1f, 0f, 1f, 0.05f);
				Gizmos.DrawCube(GetComponent<BoxCollider>().center, GetComponent<BoxCollider>().size);
				Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
				Gizmos.DrawWireCube(GetComponent<BoxCollider>().center, GetComponent<BoxCollider>().size);
			}

			if (Visibility != QuestIndicatorVisibility.AlwaysVisible &&
			    Visibility != QuestIndicatorVisibility.Proximity)
			{
				return;
			}

			if (Task != null)
			{
				if (Task.QuestTriggerID == 0)
				{
					Gizmos.DrawIcon(transform.TransformPoint(QuestIndicatorPosition), "NewQuestIndicator");
				}
				else
				{
					Gizmos.DrawIcon(transform.TransformPoint(QuestIndicatorPosition), "QuestTrigger", false);
				}
			}
			else
			{
				Gizmos.DrawIcon(transform.TransformPoint(QuestIndicatorPosition), "QuestTrigger", false);
			}
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
			if (Visibility == QuestIndicatorVisibility.Proximity)
			{
				Gizmos.DrawWireSphere(Vector3.zero, ProximityDistance);
			}
		}

		/// <summary>
		/// 	Checks if the quests on the provided gameobject can be completed. Completes them if the trigger event is the same.
		/// </summary>
		public static void OnTrigger(GameObject go, SceneQuestTriggerEvent triggerEvent)
		{
			if (go.GetComponents<SceneQuestTrigger>() == null)
			{
				return;
			}

			foreach (SceneQuestTrigger item in from m in go.GetComponents<SceneQuestTrigger>()
			         where m.TriggerEvent == triggerEvent && m.QuestTrigger != null &&
			               m.QuestTrigger.Status == QuestStatus.Active
			         select m)
			{
				item.CompleteQuest();
			}
		}

		/// <summary>
		/// 	Checks if the quests on children of the provided gameobject can be completed. Completes them if the trigger event is the same.
		/// </summary>
		public static void OnTriggerInChildren(GameObject go, SceneQuestTriggerEvent triggerEvent)
		{
			if (go.GetComponentsInChildren<SceneQuestTrigger>() == null)
			{
				return;
			}

			foreach (SceneQuestTrigger item in from m in go.GetComponentsInChildren<SceneQuestTrigger>()
			         where m.TriggerEvent == triggerEvent && m.QuestTrigger != null &&
			               m.QuestTrigger.Status == QuestStatus.Active
			         select m)
			{
				item.CompleteQuest();
			}
		}
	}
}
