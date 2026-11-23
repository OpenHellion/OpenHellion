// InGameGUI.cs
//
// Copyright (C) 2025, OpenHellion contributors
//
// SPDX-License-Identifier: GPL-3.0-or-later
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenHellion.IO;
using Steamworks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using ZeroGravity;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;
using ZeroGravity.Objects;
using ZeroGravity.UI;

namespace OpenHellion.UI
{
	public class InGameGUI : MonoBehaviour
	{
		[Title("Dead screen")] public GameObject DeadScreen;

		public GameObject PressAnyKey;

		public Text DeadMsgText;

		private float _showDeadMsgTime;

		public PauseMenu PauseMenu;

		public enum NotificationType
		{
			General,
			Quest,
			Alert
		}

		[Title("Crosshair")] public bool IsCroshairActive;

		public Image Croshair;

		public Animator CroshairAnim;

		public Image DotCroshair;

		public Text ItemName;

		[Title("Tooltip")] public GameObject TooltipEquip;

		public GameObject Tooltip;

		public GameObject ThrowingItem;

		public Image ThrowingFiller;

		private bool isThrowing;

		private float throwTimer;

		public GameObject Help;

		[Title("Tutorial")] public List<int> ShownTutorials = new List<int>();

		public GameObject TutorialUIPref;

		public GameObject TutorialAdditionalInfo;

		[Title("Alert")] public GameObject AlertObject;

		public Text AlertMessage;

		[Title("Helmet hud")] public HelmetHudUI HelmetHud;

		[Title("Notifications")] public NotificationUI NotificationUI;

		private bool _isNotificationActive;

		private float _notificationTimer;

		public GameObject Notifications;

		public Transform NotificationsHolder;

		[Title("Quest notifications")] public QuestCutScene QuestCutScene;

		public GameObject TrackingQuest;

		public Transform TrackingHolder;

		public TaskUI TrackingTaskPrefab;

		public Quest CurrentTrackingQuest;

		public Text TrackingQuestName;

		public GameObject QuestIndicatorPrefab;

		public QuestIndicators QuestIndicators;

		public QuestTitleUI QuestTitleUI;

		public QuestTracker QuestTracker;

		public List<QuestTrigger> ActivatedTriggers = new List<QuestTrigger>();

		public GameObject SeasonEnd;

		public InteractionCanvas InteractionCanvas;

		public Chat TextChat;

		public GameObject TextChatCanvas;

		public GameObject HitCanvas;

		public GameObject BusyLoadingInfo;

		public GameObject ScreenShootMod;

		[Title("Quick tips")] public bool DefaultInteractionTipSeen;

		public GameObject DefaultInteractionTip;

		public GameObject QuickTipHolder;

		public Text QuickTipContent;

		[Title("My player")] public PlayerOverview PlayerOverview;

		private float _hitCanvasTime;

		private bool _hitCanvasVisible;

		public InventoryUI InventoryUI;

		public bool OverlayCanvasIsOn;

		public bool IsInputFieldIsActive;

		public Material HighlightSlotMaterial;

		public Texture HighlightSlotNormal;

		public Texture HighlightSlotItemHere;

		private int _busyLoadingActiveCount;

		public bool IsConfirmBoxActive;

		public HelmetOverlayModel HelmetOverlayModel;

		[Title("Console")] public InGameConsole Console;

		[Title("Connection notifications")] public Image Latency;

		public bool IsPlayerOverviewOpen => PlayerOverview.gameObject.activeInHierarchy;

		public bool IsGameMenuOpen => _world.InWorldPanels.gameObject.activeInHierarchy;

		public bool ConsoleIsUp => Console.gameObject.activeInHierarchy;

		[SerializeField] private World _world;

		private void Awake()
		{
			Settings.OnSaveAction += () => ToggleCrosshair(Settings.SettingsData.GameSettings.ShowCrosshair);
			ToggleCroshair(true);
		}

		private void Start()
		{
			ScreenShootMod.Activate(value: false);
			UpdateTooltipKeys();
			UpdateQuestObjects();
		}

		private void Update()
		{
			if (_hitCanvasVisible)
			{
				_hitCanvasTime += Time.deltaTime;
				if (_hitCanvasTime >= 0.1f)
				{
					_hitCanvasTime = 0f;
					HitCanvas.SetActive(value: false);
					_hitCanvasVisible = false;
				}
			}

			if (MyPlayer.Instance != null && !_world.IsChatOpened &&
			    (Keyboard.current.f2Key.wasPressedThisFrame ||
			     (ConsoleIsUp && Keyboard.current.escapeKey.wasPressedThisFrame)))
			{
				if (ConsoleIsUp)
				{
					Console.Close();
				}
				else if (!IsInputFieldIsActive)
				{
					Console.Open();
				}

				return;
			}

			if (Keyboard.current.escapeKey.wasPressedThisFrame)
			{
				if (IsConfirmBoxActive || _world.InWorldPanels.gameObject.activeInHierarchy)
				{
					return;
				}

				if (ScreenShootMod.activeInHierarchy)
				{
					ToggleScreenShootMod();
					return;
				}
			}

			if (Keyboard.current.enterKey.wasPressedThisFrame)
			{
				if (IsConfirmBoxActive)
				{
					return;
				}

				if (!OverlayCanvasIsOn)
				{
					if (IsPlayerOverviewOpen)
					{
						PlayerOverview.Toggle(val: false);
					}
				}

				if (isThrowing && throwTimer <= World.DROP_MAX_TIME)
				{
					throwTimer += Time.deltaTime;
					ThrowingFiller.fillAmount = throwTimer / World.DROP_MAX_TIME;
				}

				if (_isNotificationActive)
				{
					_notificationTimer += Time.deltaTime;
					if (_notificationTimer > 4f && NotificationsHolder.childCount == 0)
					{
						Notifications.SetActive(value: false);
						_isNotificationActive = false;
						_notificationTimer = 0f;
					}
				}

				if (MyPlayer.Instance != null && MyPlayer.Instance.Inventory != null)
				{
					if (!TooltipEquip.activeInHierarchy && MyPlayer.Instance.Inventory.ItemInHands != null &&
					    MyPlayer.Instance.Inventory.ItemInHands.IsInvetoryEquipable)
					{
						ToggleTooltipEquip(val: true);
					}
					else if (TooltipEquip.activeInHierarchy && (MyPlayer.Instance.Inventory.ItemInHands == null ||
					                                            !MyPlayer.Instance.Inventory.ItemInHands
						                                            .IsInvetoryEquipable))
					{
						ToggleTooltipEquip(val: false);
					}
				}

				if (!IsCroshairActive && Croshair.gameObject.activeInHierarchy)
				{
					CroshairAnim.SetTrigger("hide");
				}
			}

			if (DeadScreen.activeInHierarchy && Keyboard.current.anyKey.isPressed &&
			    Time.time - _showDeadMsgTime > 3f)
			{
				ToggleDeadMsg(val: false);
			}

			if (ScreenShootMod.activeInHierarchy && Keyboard.current.f11Key.wasPressedThisFrame)
			{
				StartCoroutine(TakeScreenShoot());
			}

			if (MyPlayer.Instance != null && Keyboard.current.f10Key.wasPressedThisFrame)
			{
				ToggleScreenShootMod();
			}

		}

		/// <summary>
		/// 	Open the in-game menu.
		/// </summary>
		public void Open()
		{
			ToggleCanvas(true);
			Globals.ToggleCursor(true);
			MyPlayer.Instance.FpsController.ToggleCameraController(false);
			MyPlayer.Instance.FpsController.ToggleCameraMovement(false);
			MyPlayer.Instance.FpsController.ToggleMovement(false);
			if (_world.IsChatOpened)
			{
				TextChat.CloseChat();
			}

			if (!MyPlayer.Instance.FpsController.IsZeroG)
			{
				MyPlayer.Instance.FpsController.ResetVelocity();
			}

			if (MyPlayer.Instance != null && MyPlayer.Instance.CurrentActiveItem != null)
			{
				MyPlayer.Instance.CurrentActiveItem.PrimaryReleased();
			}
		}

		public void Close()
		{
			ToggleCanvas(false);
			Globals.ToggleCursor(false);
			MyPlayer.Instance.FpsController.ToggleCameraController(true);
			MyPlayer.Instance.FpsController.ToggleCameraMovement(true);
			MyPlayer.Instance.FpsController.ToggleMovement(true);
		}

		public void BeingHit()
		{
			HitCanvas.SetActive(value: true);
			_hitCanvasTime = 0f;
			_hitCanvasVisible = true;
		}

		// Just turn on and of graphical elements.
		private void ToggleCanvas(bool val)
		{
			PauseMenu.gameObject.SetActive(val);
			PauseMenu.MainMenu(val);
		}

		public void ToggleDeadMsg(bool val)
		{
			if (val)
			{
				QuestCutScene.OnCutSceneFinished();
				_world.InWorldPanels.Detach();
				_showDeadMsgTime = Time.time;
				DeadScreen.SetActive(value: true);
				if (MyPlayer.Instance != null)
				{
					MyPlayer.Instance.FpsController.ToggleMovement(false);
					MyPlayer.Instance.FpsController.ToggleCameraMovement(false);
				}

				if (_world.IsChatOpened)
				{
					TextChat.CloseChat();
				}

				if (IsPlayerOverviewOpen)
				{
					PlayerOverview.Toggle(val: false);
				}

				if (IsGameMenuOpen)
				{
					ToggleCanvas(val: false);
				}

				if (ConsoleIsUp)
				{
					Console.Close();
				}

				_world.InWorldPanels.Detach();

				PressAnyKey.Activate(true);
			}
			else
			{
				_showDeadMsgTime = 0f;
				DeadScreen.SetActive(value: false);
				GlobalGUI.ShowLoadingScreen(GlobalGUI.LoadingScreenType.ConnectingToMain);
				_world.ReturnToMainMenu();
			}
		}

		public void ToggleScreenShootMod()
		{
			ScreenShootMod.Activate(!ScreenShootMod.activeInHierarchy);
		}

		private IEnumerator TakeScreenShoot()
		{
			yield return null;
			ScreenShootMod.Activate(value: false);
			yield return new WaitForEndOfFrame();
			Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, mipChain: false);
			tex.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0, recalculateMipMaps: false);
			tex.Apply(updateMipmaps: false);
			byte[] bytes = tex.GetRawTextureData();
			Destroy(tex);
			byte[] flipped = new byte[bytes.Length];
			for (int i = 0; i < tex.height; i++)
			{
				for (int j = 0; j < tex.width * 3; j++)
				{
					flipped[i * tex.width * 3 + j] = bytes[(tex.height - i - 1) * tex.width * 3 + j];
				}
			}

			SteamScreenshots.WriteScreenshot(flipped, (uint)flipped.Length, tex.width, tex.height);
			ScreenShootMod.Activate(value: true);
		}

		public void ToggleTextChatCanvas(bool val)
		{
			TextChatCanvas.SetActive(val);
		}

		public void ShowInteractionCanvasMessage(string text, float hideTime = 1f)
		{
			InteractionCanvas.gameObject.SetActive(value: true);
			InteractionCanvas.ShowCanvas(text, hideTime);
		}

		public void ToggleBusyLoading(bool isActive)
		{
			if (InitialisingSceneManager.SceneLoadType == InitialisingSceneManager.SceneLoadTypeValue.PreloadWithCopy)
			{
				isActive = false;
			}

			if (isActive)
			{
				_busyLoadingActiveCount++;
			}
			else
			{
				_busyLoadingActiveCount--;
			}

			if (_busyLoadingActiveCount > 0)
			{
				BusyLoadingInfo.SetActive(value: true);
			}
			else
			{
				BusyLoadingInfo.SetActive(value: false);
				_busyLoadingActiveCount = 0;
			}
		}

		public void ToggleCrosshair(bool value)
		{
			DotCroshair.gameObject.SetActive(value);
		}

		public void ToggleCroshair(bool show, BaseSceneTrigger lookingAtTrigger = null, bool canLoot = false)
		{
			IsCroshairActive = show;
			if (IsCroshairActive)
			{
				Croshair.gameObject.Activate(value: true);
			}
			else if (CroshairAnim.gameObject.activeSelf)
			{
				CroshairAnim.SetTrigger("hide");
			}

			ToggleItemName(show);
			if (!show)
			{
				DotCroshair.gameObject.SetActive(false);
				Tooltip.SetActive(value: false);
			}
			else
			{
				DotCroshair.gameObject.SetActive(false);
				SetTooltip(lookingAtTrigger, canLoot);
			}

			if (Settings.SettingsData.GameSettings.ShowTips && lookingAtTrigger != null &&
			    lookingAtTrigger.Glossary != null)
			{
				Help.Activate(value: true);
			}
			else
			{
				Help.Activate(value: false);
			}
		}

		private void SetTooltip(BaseSceneTrigger lookingAtTrigger = null, bool canLoot = false)
		{
			if (canLoot)
			{
				SetItemName(null);
				Tooltip.SetActive(value: true);
				Tooltip.GetComponentInChildren<Text>().text = string.Format(Localization.HoldToLoot,
					ControlsSubsystem.GetAxisKeyName(ControlsSubsystem.ConfigAction.Interact));
			}
			else if (lookingAtTrigger != null && Settings.SettingsData.GameSettings.ShowTips)
			{
				Tooltip.SetActive(value: true);
				if (lookingAtTrigger is BaseSceneAttachPoint { InteractionTip: not null } attachPoint)
				{
					Tooltip.GetComponentInChildren<Text>().text =
						attachPoint.InteractionTip;
				}
				else if (lookingAtTrigger.StandardTip != 0)
				{
					if (lookingAtTrigger.StandardTip == Localization.StandardInteractionTip.ExitCryo)
					{
						ToggleDefaultInteractionTip();
					}

					Tooltip.GetComponentInChildren<Text>().text = lookingAtTrigger.StandardTip.ToLocalizedString();
				}
				else
				{
					Tooltip.SetActive(value: false);
				}
			}
			else
			{
				Tooltip.SetActive(value: false);
			}
		}

		public void ToggleDefaultInteractionTip()
		{
			if (!DefaultInteractionTipSeen && Settings.SettingsData.GameSettings.ShowTips)
			{
				DefaultInteractionTip.GetComponentInChildren<Text>(includeInactive: true).text = string
					.Format(Localization.PressToInteract,
						ControlsSubsystem.GetAxisKeyName(ControlsSubsystem.ConfigAction.Interact)).ToUpper();
				DefaultInteractionTip.Activate(value: true);
				DefaultInteractionTipSeen = true;
			}
		}

		public void SetItemName(Item item)
		{
			if (item == null)
			{
				ItemName.text = string.Empty;
				ItemName.color = Colors.White;
				return;
			}

			string text = item.Name;
			ItemName.text = string.Empty;
			ItemName.text = text.Replace("<br>", "\n");
			ItemName.color = Colors.Tier[item.Tier];
		}

		private void ToggleItemName(bool selected)
		{
			if (selected)
			{
				if (!ItemName.gameObject.activeInHierarchy)
				{
					ItemName.gameObject.SetActive(value: true);
				}
			}
			else if (ItemName.gameObject.activeInHierarchy)
			{
				ItemName.gameObject.SetActive(value: false);
			}
		}

		public void ToggleTooltipEquip(bool val)
		{
			if (val)
			{
				if (MyPlayer.Instance.Inventory.ItemInHands is Outfit && MyPlayer.Instance.CurrentOutfit == null)
				{
					TooltipEquip.SetActive(value: true);
					return;
				}

				InventorySlot inventorySlot =
					MyPlayer.Instance.Inventory.FindEmptyOutfitSlot(MyPlayer.Instance.Inventory.ItemInHands,
						museBeEquip: true);
				if (inventorySlot != null)
				{
					TooltipEquip.SetActive(value: true);
				}
			}
			else
			{
				TooltipEquip.SetActive(false);
			}
		}

		public void UpdateTooltipKeys()
		{
			TooltipEquip.GetComponentInChildren<Text>(includeInactive: true).text =
				string.Format(Localization.HoldToEquip, ControlsSubsystem.GetAxisKeyName(ControlsSubsystem.ConfigAction.Equip));
		}

		public void ThrowingItemToggle(bool val)
		{
			ThrowingItem.SetActive(val);
			isThrowing = val;
			if (!val)
			{
				throwTimer = 0f;
				ThrowingFiller.fillAmount = 0f;
			}
		}

		public void ShowTutorialUI(int tut)
		{
			if (Settings.SettingsData.GameSettings.ShowTutorial && !ShownTutorials.Contains(tut))
			{
				string text = Localization.TutorialText[0];
				if (Localization.TutorialText.TryGetValue(tut, out var value))
				{
					text = value;
					ShownTutorials.Add(tut);
				}

				if (text.Length != 0 && tut != 0)
				{
					GameObject tutorialUIElement = Instantiate(TutorialUIPref, TutorialUIPref.transform.parent);
					tutorialUIElement.SetActive(value: true);
					tutorialUIElement.transform.SetAsFirstSibling();
					TutorialUI component = tutorialUIElement.GetComponent<TutorialUI>();
					component.MessageText.text = text;
				}

				if (tut == 1)
				{
					TutorialAdditionalInfo.SetActive(value: true);
					TutorialAdditionalInfo.transform.SetAsFirstSibling();
					TutorialAdditionalInfo.GetComponentInChildren<Text>().text = Localization.Tut_1AdditionalInfo;
				}
				else
				{
					TutorialAdditionalInfo.SetActive(value: false);
				}
			}
		}

		public void Alert(string msg)
		{
			AlertMessage.text = msg;
			AlertObject.SetActive(value: true);
		}

		public void Notification(string msg, NotificationType tip)
		{
			NotificationUI notificationUI = Instantiate(NotificationUI, NotificationsHolder);
			notificationUI.transform.Reset();
			notificationUI.transform.SetAsFirstSibling();
			notificationUI.Activate(NotificationsHolder.childCount * 0.1f);
			notificationUI.Type = tip;
			notificationUI.Content.text = msg;
			_isNotificationActive = true;
			_notificationTimer = 0f;
			Notifications.SetActive(value: true);
		}

		public void SetCurrentTrackingQuest(Quest quest)
		{
			Diagnostics.Assert(quest == null || quest.Status != QuestStatus.Completed, $"tracking completed quest {quest?.ID}");
			CurrentTrackingQuest = quest;
			QuestTracker.Refresh(quest);
			UpdateQuestMarkers();
		}

		public void UpdateQuestMarkers()
		{
			UnityEngine.SceneManagement.Scene activeScene = SceneManager.GetActiveScene();
			List<GameObject> list = new List<GameObject>();
			activeScene.GetRootGameObjects(list);
			foreach (GameObject item in list)
			{
				SceneQuestTrigger[] componentsInChildren = item.GetComponentsInChildren<SceneQuestTrigger>();
				foreach (SceneQuestTrigger sceneQuestTrigger in componentsInChildren)
				{
					if (sceneQuestTrigger.QuestTrigger != null)
					{
						sceneQuestTrigger.UpdateQuestTriggerMarker();
						sceneQuestTrigger.UpdateAvailableQuestMarker();
					}
				}
			}
		}

		public void UpdateQuestObjects()
		{
			UnityEngine.SceneManagement.Scene activeScene = SceneManager.GetActiveScene();
			List<GameObject> list = new List<GameObject>();
			activeScene.GetRootGameObjects(list);
			foreach (GameObject item in list)
			{
				SceneQuestTrigger[] componentsInChildren = item.GetComponentsInChildren<SceneQuestTrigger>();
				foreach (SceneQuestTrigger sceneQuestTrigger in componentsInChildren)
				{
					if (sceneQuestTrigger.QuestTrigger != null)
					{
						sceneQuestTrigger.OnQuestTriggerUpdate();
					}
				}
			}
		}

		public void TriggerQuestEvents(QuestTrigger trigger)
		{
			UnityEngine.SceneManagement.Scene activeScene = SceneManager.GetActiveScene();
			List<GameObject> list = new List<GameObject>();
			activeScene.GetRootGameObjects(list);
			foreach (GameObject item in list)
			{
				foreach (SceneQuestTrigger item2 in from m in item.GetComponentsInChildren<SceneQuestTrigger>()
				         where m.Task == trigger.TaskObject
				         select m)
				{
					if (item2.QuestTrigger != null)
					{
						item2.TriggerEvents();
					}
				}
			}
		}

		public void QuestTriggerUpdate(QuestTrigger trigger)
		{
			if (trigger.Quest == CurrentTrackingQuest)
			{
				if (trigger.Type == QuestTriggerType.Task || trigger.Type == QuestTriggerType.Complete)
				{
					QuestCutScene.PlayCutScene(trigger);
				}

				if (trigger.Type == QuestTriggerType.Complete && trigger.Quest.Status == QuestStatus.Completed)
				{
					QuestTitleUI.CompleteQuest(trigger.Quest);
					if (trigger.TaskObject.LastTask)
					{
						SeasonEnd.Activate(value: true);
					}

					QuestObject next = trigger.Quest.QuestObject.QuestToTrackWhenCompleted;
					SetCurrentTrackingQuest(next != null
						? _world.Quests.FirstOrDefault((Quest m) => m.ID == next.ID && m.Status == QuestStatus.Active)
						: _world.Quests.FirstOrDefault((Quest m) => m.Status == QuestStatus.Active));
				}
				else
				{
					QuestTracker.Refresh(CurrentTrackingQuest);
					UpdateQuestMarkers();
				}
			}
			else if (trigger.Type == QuestTriggerType.Activate)
			{
				if (trigger.Quest.QuestObject.TrackOnPickUp || CurrentTrackingQuest == null)
				{
					SetCurrentTrackingQuest(trigger.Quest);
				}

				QuestTitleUI.ShowTitle(trigger.Quest);
				QuestCutScene.PlayCutScene(trigger);
				QuickTip(Localization.GetLocalizedField("NewQuestAvailable"));
			}
			else
			{
				QuestCutScene.PlayCutScene(trigger);
				if (CurrentTrackingQuest == null && trigger.Quest.Status == QuestStatus.Active)
				{
					SetCurrentTrackingQuest(trigger.Quest);
				}
				else
				{
					QuestTracker.TriggerNonTrackedTask(trigger);
				}

				UpdateQuestMarkers();
			}

			UpdateQuestObjects();
			TriggerQuestEvents(trigger);
		}

		public void QuickTip(string msg)
		{
			QuickTipHolder.Activate(value: false);
			QuickTipContent.text = msg;
			QuickTipHolder.SetActive(value: true);
		}
	}
}
