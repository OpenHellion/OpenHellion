using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenHellion;
using UnityEngine;
using UnityEngine.UI;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;
using ZeroGravity.Objects;
using ZeroGravity.UI;
using OpenHellion.Net;
using OpenHellion.UI;
using UnityEngine.InputSystem;
using OpenHellion.IO;
using Cysharp.Threading.Tasks;

namespace ZeroGravity
{
	public class InGameConsole : MonoBehaviour
	{
		private int CurrentScreen;

		public List<GameObject> Screens;

		public List<GameObject> Options;

		public Transform ItemsHolder;

		public ScrollRect Scroll;

		public InputField Input;

		public GameObject TextElement;

		private const int MaxElements = 200;

		private readonly List<GameObject> _lines = new();

		private readonly List<string> _history = new();

		private int _historyIndex = -1;

		public Toggle GodMode;

		public GameObject SpawnOptionUI;

		public GameObject NetworkingButton;

		public Text NetworkingReceived;

		public Text NetworkingSent;

		private bool NetworkingActive;

		[SerializeField] private InGameGUI _inGameGUI;

		private void Start()
		{
			CreateItemSpawnOptions();
			NetworkingButton.Activate(true);
		}

		private void Update()
		{
			if (CurrentScreen == 0)
			{
				if (Keyboard.current.enterKey.wasPressedThisFrame)
				{
					SubmitText();
				}
				else if (Keyboard.current.upArrowKey.wasPressedThisFrame && _history.Count > 0)
				{
					_historyIndex = _historyIndex < 0 ? _history.Count - 1 : Mathf.Max(_historyIndex - 1, 0);
					Input.text = _history[_historyIndex];
					Input.caretPosition = Input.text.Length;
					Input.Select();
					Input.ActivateInputField();
				}
				else if (Keyboard.current.downArrowKey.wasPressedThisFrame && _historyIndex >= 0)
				{
					_historyIndex++;
					Input.text = _historyIndex >= _history.Count ? string.Empty : _history[_historyIndex];
					if (_historyIndex >= _history.Count)
					{
						_historyIndex = -1;
					}

					Input.caretPosition = Input.text.Length;
					Input.Select();
					Input.ActivateInputField();
				}
			}

			if (NetworkingActive)
			{
				UpdateNewtorking();
			}
		}

		public void CreateTextElement(string text, Color? color = null)
		{
			if (_lines.Count >= MaxElements)
			{
				Destroy(_lines[0]);
				_lines.RemoveAt(0);
			}

			GameObject gameObject = Instantiate(TextElement, Scroll.content);
			gameObject.transform.SetAsLastSibling();
			gameObject.GetComponent<Text>().text = text;
			if (color.HasValue)
			{
				gameObject.GetComponent<Text>().color = color.Value;
			}

			gameObject.SetActive(value: true);
			gameObject.transform.Reset();
			Scroll.normalizedPosition = new Vector2(0f, 0f);
			Canvas.ForceUpdateCanvases();
			_lines.Add(gameObject);
		}

		public void SubmitText()
		{
			string sendText = Input.text;
			if (sendText.IsNullOrEmpty())
			{
				return;
			}

			CreateTextElement(sendText);
			_history.Add(sendText);
			_historyIndex = -1;
			Input.text = string.Empty;
			Input.ActivateInputField();
			Input.Select();
			Send(sendText).Forget();
		}

		/// <summary>
		/// 	Send a command to the server and print whatever it answers.
		/// 	The server always answers, so silence here means the connection is gone.
		/// </summary>
		private async UniTaskVoid Send(string command)
		{
			ConsoleMessage response = null;
			bool timedOut = false;
			try
			{
				response = await NetworkController.SendReceiveAsync(new ConsoleMessage
				{
					Text = command
				}, 30000) as ConsoleMessage;
			}
			catch (TimeoutException)
			{
				timedOut = true;
			}

			// If the world is exited before command arrives.
			if (this == null)
			{
				return;
			}

			if (timedOut)
			{
				CreateTextElement("No response from server.", Colors.RedText);
				return;
			}

			if (response == null || response.Text.IsNullOrEmpty())
			{
				return;
			}

			CreateTextElement(response.Text,
				response.Status == NetworkData.MessageStatus.Failure ? Colors.RedText : Colors.Orange);

			if (response.Text == "God mode: ON")
			{
				GodMode.SetIsOnWithoutNotify(true);
			}
			else if (response.Text == "God mode: OFF")
			{
				GodMode.SetIsOnWithoutNotify(false);
			}
		}

		public void Open()
		{
			_inGameGUI.IsInputFieldIsActive = true;
			Globals.ToggleCursor(true);
			MyPlayer.Instance.FpsController.ToggleMovement(false);
			MyPlayer.Instance.FpsController.ToggleAttached(true);
			if (!MyPlayer.Instance.FpsController.IsZeroG)
			{
				MyPlayer.Instance.FpsController.ResetVelocity();
			}

			_historyIndex = -1;
			gameObject.SetActive(value: true);
			SetScreen(0);
			Canvas.ForceUpdateCanvases();
			Scroll.normalizedPosition = new Vector2(0f, 0f);
		}

		public void Close()
		{
			_inGameGUI.IsInputFieldIsActive = false;
			gameObject.SetActive(value: false);
			if (MyPlayer.Instance == null)
			{
				return;
			}

			if (!MyPlayer.Instance.IsLockedToTrigger)
			{
				Globals.ToggleCursor(false);
				MyPlayer.Instance.FpsController.ToggleAttached(false);
				MyPlayer.Instance.FpsController.ToggleMovement(!MyPlayer.Instance.SittingOnPilotSeat);
				if (MyPlayer.Instance.SittingOnPilotSeat)
				{
					MyPlayer.Instance.FpsController.ToggleCameraAttachToHeadBone(true);
				}

				MyPlayer.Instance.FpsController.ToggleAutoFreeLook(MyPlayer.Instance.SittingOnPilotSeat ||
				                                                   MyPlayer.Instance.InLadderTrigger ||
				                                                   (MyPlayer.Instance.InLockState &&
				                                                    !MyPlayer.Instance.IsLockedToTrigger &&
				                                                    MyPlayer.Instance.Parent is SpaceObjectVessel &&
				                                                    (MyPlayer.Instance.Parent as SpaceObjectVessel)
				                                                    .SpawnPoints.Values
				                                                    .FirstOrDefault((SceneSpawnPoint m) =>
					                                                    m.PlayerGUID == MyPlayer.Instance.Guid) !=
				                                                    null));
			}
			else if (MyPlayer.Instance.IsDrivingShip)
			{
				Globals.ToggleCursor(false);
			}
		}

		public void Spawn(string itemToSpawn)
		{
			Send("spawn " + itemToSpawn).Forget();
		}

		public void Action(string actionToDo)
		{
			Send(actionToDo).Forget();
		}

		public void ToggleGodmod()
		{
			Send(GodMode.isOn ? "god 1" : "god 0").Forget();
		}

		public void CheckIfGodAndUpdate()
		{
			Send("god").Forget();
		}

		public void SetScreen(int optionIndex)
		{
			NetworkingActive = false;
			foreach (GameObject option in Options)
			{
				option.Activate(value: false);
			}

			foreach (GameObject screen in Screens)
			{
				screen.Activate(value: false);
			}

			Screens[optionIndex].Activate(value: true);
			Options[optionIndex].Activate(value: true);
			CurrentScreen = optionIndex;
			if (optionIndex == 0)
			{
				Input.ActivateInputField();
				Input.Select();
				Input.text = string.Empty;
				Scroll.normalizedPosition = new Vector2(0f, 0f);
			}

			if (optionIndex == 5)
			{
				NetworkingActive = true;
			}
		}

		private void CreateItemSpawnOptions()
		{
			List<DynamicObjectData> list = new List<DynamicObjectData>(
				StaticData.DynamicObjectsDataList.Values.Where((DynamicObjectData m) =>
					m.DefaultAuxData.Category == ItemCategory.Weapons));
			List<DynamicObjectData> list2 = new List<DynamicObjectData>(
				StaticData.DynamicObjectsDataList.Values.Where((DynamicObjectData m) =>
					m.DefaultAuxData.Category == ItemCategory.Magazines));
			List<DynamicObjectData> list3 = new List<DynamicObjectData>(
				StaticData.DynamicObjectsDataList.Values.Where((DynamicObjectData m) =>
					m.DefaultAuxData.Category == ItemCategory.Medical));
			List<DynamicObjectData> list4 = new List<DynamicObjectData>(
				StaticData.DynamicObjectsDataList.Values.Where((DynamicObjectData m) =>
					m.DefaultAuxData.Category == ItemCategory.Suits));
			List<DynamicObjectData> list5 = new List<DynamicObjectData>(
				StaticData.DynamicObjectsDataList.Values.Where((DynamicObjectData m) =>
					m.DefaultAuxData.Category == ItemCategory.Tools));
			List<DynamicObjectData> list6 = new List<DynamicObjectData>(
				StaticData.DynamicObjectsDataList.Values.Where((DynamicObjectData m) =>
					m.DefaultAuxData.Category == ItemCategory.Parts));
			List<DynamicObjectData> list7 = new List<DynamicObjectData>(
				StaticData.DynamicObjectsDataList.Values.Where((DynamicObjectData m) =>
					m.DefaultAuxData.Category == ItemCategory.Utility));
			List<DynamicObjectData> list8 = new List<DynamicObjectData>(
				StaticData.DynamicObjectsDataList.Values.Where((DynamicObjectData m) =>
					m.DefaultAuxData.Category == ItemCategory.Containers));
			List<DynamicObjectData> list9 = new List<DynamicObjectData>(
				StaticData.DynamicObjectsDataList.Values.Where((DynamicObjectData m) =>
					m.DefaultAuxData.Category == ItemCategory.General));
			InstantiateItems(list, Colors.Red);
			InstantiateItems(list2, Colors.Red);
			InstantiateItems(list3, Colors.Green);
			InstantiateItems(list4, Colors.Yellow);
			InstantiateItems(list7, Colors.PowerRed);
			InstantiateItems(list5, Colors.Blue);
			InstantiateItems(list8, Colors.Orange);
			InstantiateItems(list6, Colors.Cyan);
			InstantiateItems(list9, Colors.Black);
		}

		private void InstantiateItems(List<DynamicObjectData> list, Color col)
		{
			foreach (DynamicObjectData item in list)
			{
				GameObject gameObject = Instantiate(SpawnOptionUI, ItemsHolder);
				ConsoleSpawnOptionUI component = gameObject.GetComponent<ConsoleSpawnOptionUI>();
				component.Console = this;
				if (item.ItemType == ItemType.MachineryPart)
				{
					MachineryPartData machineryPartData = item.DefaultAuxData as MachineryPartData;
					component.Name.text = machineryPartData.PartType.ToLocalizedString();
					component.Icon.sprite = SpriteManager.Instance.GetSprite(machineryPartData.PartType);
				}
				else if (item.ItemType == ItemType.GenericItem)
				{
					GenericItemData genericItemData = item.DefaultAuxData as GenericItemData;
					component.Name.text = genericItemData.SubType.ToLocalizedString();
					component.Icon.sprite = SpriteManager.Instance.GetSprite(genericItemData.SubType);
				}
				else
				{
					component.Name.text = item.ItemType.ToLocalizedString();
					component.Icon.sprite = SpriteManager.Instance.GetSprite(item.ItemType);
				}

				component.SetSpawnOption(Path.GetFileName(item.PrefabPath));
				component.GetComponent<Image>().color = col;
			}
		}

		public void UpdateNewtorking()
		{
			NetworkingReceived.text = ProtoSerialiser.ReceivedPacketStatistics;
			NetworkingSent.text = ProtoSerialiser.SentPacketStatistics;
		}

		public void RestartNetworking()
		{
			ProtoSerialiser.ResetStatistics();
		}
	}
}
