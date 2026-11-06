using System;
using System.Collections.Generic;
using System.Linq;
using OpenHellion.Net;
using OpenHellion.Social.RichPresence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using ZeroGravity;
using ZeroGravity.Network;
using ZeroGravity.UI;
using Cysharp.Threading.Tasks;

namespace OpenHellion.UI
{
	public class MainMenuGUI : MonoBehaviour
	{
		public enum Screen
		{
			None,
			CreateCharacter,
			StartingPoint
		}

		public enum StartingPointOption
		{
			NewGame = 2,
			FreshStart = 3,
			Continue = 4,
			Invite = 5,
			Eva = 7,
			StrandedMiner = 8,
			Soe = 9
		}

		[Title("Start screen")] public SplashScreen SplashScreen;

		public ServerListGUI ServerList;

		public GameObject MainMenu;

		public GameObject Disclamer;

		private static bool ShowDisclaimer = true;

		public TextMeshProUGUI VersionText;

		[Title("Disconnect screen")] public GameObject DisconnectScreen;

		public static bool WasDisconnectUncontrolled { private get; set; }

		// Set by GameStarter when a connection attempt fails.
		public static bool ReturnToServerList;

		[Title("Character screen")]
		public InputField CharacterInputField;

		public GameObject CreateCharacterPanel;

		public Text CurrentGenderText;

		[Title("Spawn point selection screen")]
		public List<StartingPointOptionData> StartingPointData = new List<StartingPointOptionData>();

		public GameObject StartingPointScreen;

		public Transform SpawnOptions;

		public Transform FreshStartSpawnOptions;

		public StartingPointOptionUI StartingPointUI;

		[Title("Spawn point selection screen")]
		public GameObject SpawnPointScreen;

		[FormerlySerializedAs("SpawnPointHolder")]
		public Transform SpawnPointsHolder;

		public SpawnPointOptionUI SpawnPointOptionPrefab;

		private Gender _currentGenderGUI;

		private Action<CharacterData> _onCharacterCreated;

		private Action<SpawnPointDetails> _onSpawnChosen;

		private void Awake()
		{
			if (WasDisconnectUncontrolled)
			{
				DisconnectScreen.SetActive(value: true);
				WasDisconnectUncontrolled = false;
			}

#if UNITY_DEBUG
			RichPresenceManager.SetAchievement(AchievementID.other_testing_squad_member);
#endif
		}

		private void Start()
		{
			// Localize text in child objects.
			if (Localization.MainMenuLocalisation?.Count > 0)
			{
				foreach (Text text in GetComponentsInChildren<Text>(true))
				{
					if (Localization.MainMenuLocalisation.TryGetValue(text.name, out string value))
					{
						text.text = value;
					}
				}
			}

			Globals.ToggleCursor(true);
			Disclamer.SetActive(ShowDisclaimer);
			if (ShowDisclaimer)
			{
				SplashScreen.StartVideo(0);
			}
			else
			{
				DisclaimerAgree();
			}

			VersionText.text = string.Format(Localization.ClientVersion, Application.version);
			StartingPointData = Resources.LoadAll<StartingPointOptionData>("StartingPoints").ToList();

			RichPresenceManager.UpdateStatus();

			if (ReturnToServerList)
			{
				ReturnToServerList = false;
				PlayButton();
			}
		}

		private void Update()
		{
			// Exiting the splash screen is handled by its own script.
			if (SplashScreen.VideoPlayer.isPlaying)
			{
				return;
			}

			if (Keyboard.current.anyKey.wasPressedThisFrame)
			{
				if (DisconnectScreen.activeInHierarchy)
				{
					DisconnectScreen.SetActive(false);
					return;
				}

				// Close the disclaimer.
				if (Disclamer.activeInHierarchy)
				{
					DisclaimerAgree();
					return;
				}
			}

			if (Keyboard.current.enterKey.wasPressedThisFrame)
			{
				if (MainMenu.activeInHierarchy)
				{
					PlayButton();
				}
				else if (CreateCharacterPanel.activeInHierarchy)
				{
					CreateCharacterButton();
				}
			}

			// If esc is clicked.
			if (Keyboard.current.escapeKey.wasPressedThisFrame)
			{
				if (StartingPointScreen.activeInHierarchy)
				{
					ExitStartingPointScreen();
				}
			}

			RichPresenceManager.Update();
		}

		public void DisclaimerAgree()
		{
			ShowDisclaimer = false;
			Disclamer.SetActive(ShowDisclaimer);
			//_world.AmbientSounds.SwitchAmbience("MainMenu"); TODO
			//_world.AmbientSounds.Play(0);
		}

		public void DiscordButton()
		{
			Application.OpenURL("https://discord.gg/9nGWgQ8Uyf");
		}

		public void GamepediaButton()
		{
			Application.OpenURL("https://hellion.gamepedia.com/Hellion_Wiki");
		}

		public void ShowFreshStartOptions()
		{
			SpawnOptions.gameObject.Activate(value: false);
			FreshStartSpawnOptions.gameObject.Activate(value: true);
		}

		private void InstantiateFreshStartOptions()
		{
			FreshStartSpawnOptions.DestroyAll<StartingPointOptionUI>();
			StartingPointOptionUI freshStartUI = Instantiate(StartingPointUI, FreshStartSpawnOptions);
			freshStartUI.Type = StartingPointOption.FreshStart;
			freshStartUI.Data = StartingPointData.FirstOrDefault(m => m.Type == StartingPointOption.FreshStart);
			freshStartUI.GetComponent<Button>().onClick.AddListener(delegate
			{
				SplashScreen.FreshStart(CreateFreshStartTask(SpawnSetupType.Start1));
			});

			bool achieved = RichPresenceManager.GetAchievement(AchievementID.quest_sound_of_silence);
			StartingPointOptionUI strandedMinerUI = Instantiate(StartingPointUI, FreshStartSpawnOptions);
			strandedMinerUI.Type = StartingPointOption.StrandedMiner;
			strandedMinerUI.Data = StartingPointData.FirstOrDefault(m => m.Type == StartingPointOption.StrandedMiner);
			strandedMinerUI.GetComponent<Button>().interactable = achieved;
			strandedMinerUI.GetComponent<Button>().onClick.AddListener(delegate
			{
				SplashScreen.FreshStart(CreateFreshStartTask(SpawnSetupType.Start2));
			});

			achieved = RichPresenceManager.GetAchievement(AchievementID.quest_shattered_dreams);
			StartingPointOptionUI evaUI = Instantiate(StartingPointUI, FreshStartSpawnOptions);
			evaUI.Type = StartingPointOption.Eva;
			evaUI.Data = StartingPointData.FirstOrDefault(m => m.Type == StartingPointOption.Eva);
			evaUI.GetComponent<Button>().interactable = achieved;
			evaUI.GetComponent<Button>().onClick.AddListener(delegate
			{
				SplashScreen.FreshStart(CreateFreshStartTask(SpawnSetupType.Start3));
			});

			achieved = RichPresenceManager.GetAchievement(AchievementID.quest_heart_of_stone);
			StartingPointOptionUI soeUI = Instantiate(StartingPointUI, FreshStartSpawnOptions);
			soeUI.Type = StartingPointOption.Soe;
			soeUI.Data = StartingPointData.FirstOrDefault(m => m.Type == StartingPointOption.Soe);
			soeUI.GetComponent<Button>().interactable = achieved;
			soeUI.GetComponent<Button>().onClick.AddListener(delegate
			{
				SplashScreen.FreshStart(CreateFreshStartTask(SpawnSetupType.Start4));
			});

#if UNITY_EDITOR || DEVELOPMENT_BUILD
			StartingPointOptionData testShipData = ScriptableObject.CreateInstance<StartingPointOptionData>();
			testShipData.Type = StartingPointOption.FreshStart;
			testShipData.Background = freshStartUI.Data.Background;
			testShipData.Title = "Test ship";
			testShipData.Description = "Development only. Crew quarters, command and life support modules, always spawned the same way.";
			StartingPointOptionUI testShipUI = Instantiate(StartingPointUI, FreshStartSpawnOptions);
			testShipUI.Type = StartingPointOption.FreshStart;
			testShipUI.Data = testShipData;
			testShipUI.GetComponent<Button>().onClick.AddListener(delegate
			{
				SplashScreen.FreshStart(CreateFreshStartTask(SpawnSetupType.TestShip));
			});
#endif
		}

		private Action CreateFreshStartTask(SpawnSetupType tip)
		{
			return new Action(delegate
			{
				ChooseSpawn(new SpawnPointDetails
				{
					SpawnSetupType = tip,
					IsPartOfCrew = false,
					PlayersOnShip = new List<string>()
				});
			});
		}

		public void ShowSpawnSelection(List<SpawnPointDetails> spawnPoints, bool canContinue,
			Action<SpawnPointDetails> onChosen)
		{
			_onSpawnChosen = onChosen;
			SelectScreen(Screen.StartingPoint);
			ShowStartingPoints(spawnPoints ?? new List<SpawnPointDetails>(), canContinue);
		}

		private void ChooseSpawn(SpawnPointDetails details)
		{
			// Every tile routes through here, so a second click cannot start a second spawn.
			if (_onSpawnChosen is null)
			{
				return;
			}

			Action<SpawnPointDetails> onChosen = _onSpawnChosen;
			_onSpawnChosen = null;
			SelectScreen(Screen.None);
			onChosen(details);
		}

		/// <summary>
		/// 	Choose a new spawn point while already in the world, after dying.
		/// </summary>
		public async UniTaskVoid SendAvailableSpawnPointsRequest()
		{
			var response = await NetworkController.SendReceiveAsync(new AvailableSpawnPointsRequest()) as AvailableSpawnPointsResponse;
			if (response?.SpawnPoints is null)
			{
				return;
			}

			ShowSpawnSelection(response.SpawnPoints, canContinue: false, static delegate(SpawnPointDetails details)
			{
				NetworkController.SendAndForget(new PlayerSpawnRequest
				{
					SpawnSetupType = details.SpawnSetupType,
					SpawnPointParentId = details.SpawnPointParentID
				});
			});
		}

		private void ShowStartingPoints(List<SpawnPointDetails> spawnPoints, bool canContinue)
		{
			FreshStartSpawnOptions.gameObject.Activate(value: false);
			SpawnOptions.DestroyAll<StartingPointOptionUI>();
			SpawnOptions.gameObject.Activate(value: true);
			SpawnPointScreen.SetActive(value: false);
			SpawnPointsHolder.DestroyAll();
			foreach (SpawnPointDetails spawnPoint in spawnPoints)
			{
				CreateInviteSpawnPoints(spawnPoint);
			}

			// Show fresh start. canContinue determines if we're able to open the next menu with spawn points.
			StartingPointOptionUI freshStartOptionUI = Instantiate(StartingPointUI, SpawnOptions);
			freshStartOptionUI.Type = StartingPointOption.NewGame;
			freshStartOptionUI.Data = StartingPointData.FirstOrDefault(m => m.Type == StartingPointOption.NewGame);
			if (canContinue)
			{
				freshStartOptionUI.GetComponent<Button>().onClick.AddListener(
					() => GlobalGUI.ShowConfirmMessageBox(Localization.FreshStartConfrimTitle, Localization.FreshStartConfrimText,
				Localization.Yes, Localization.No, ShowFreshStartOptions));
			}
			else
			{
				freshStartOptionUI.GetComponent<Button>().onClick.AddListener(ShowFreshStartOptions);
			}

			StartingPointOptionUI continueOptionUI = Instantiate(StartingPointUI, SpawnOptions);
			continueOptionUI.Type = StartingPointOption.Continue;
			continueOptionUI.Data = StartingPointData.FirstOrDefault(m => m.Type == StartingPointOption.Continue);
			SpawnPointDetails continueSpawnPoint = new SpawnPointDetails
			{
				SpawnSetupType = SpawnSetupType.Continue,
				IsPartOfCrew = false,
				PlayersOnShip = new List<string>()
			};
			continueOptionUI.GetComponent<Button>().onClick
				.AddListener(delegate { ChooseSpawn(continueSpawnPoint); });
			continueOptionUI.GetComponent<Button>().interactable = canContinue;

			// In single player mode add a custom starting point option, while in multiplayer add an invite starting point option.
			StartingPointOptionUI inviteCustomOptionUI = Instantiate(StartingPointUI, SpawnOptions);
			inviteCustomOptionUI.Type = StartingPointOption.Invite;
			inviteCustomOptionUI.Data = StartingPointData.FirstOrDefault(m => m.Type == StartingPointOption.Invite);
			inviteCustomOptionUI.GetComponent<Button>().interactable = spawnPoints.Count > 0;
			inviteCustomOptionUI.GetComponent<Button>().onClick.AddListener(delegate
			{
				SpawnPointScreen.SetActive(value: true);
			});
			InstantiateFreshStartOptions();
		}

		private void CreateInviteSpawnPoints(SpawnPointDetails spawnPoint)
		{
			string name = !spawnPoint.Name.IsNullOrEmpty()
				? spawnPoint.Name
				: spawnPoint.SpawnSetupType.ToLocalizedString();
			string crew = spawnPoint.PlayersOnShip is null || spawnPoint.PlayersOnShip.Count <= 0
				? string.Empty
				: string.Join(", ", spawnPoint.PlayersOnShip.ToArray());

			SpawnPointOptionUI option = Instantiate(SpawnPointOptionPrefab, SpawnPointsHolder);
			option.Initialise(name, crew, delegate
			{
				SpawnPointScreen.SetActive(value: false);
				ChooseSpawn(spawnPoint);
			});
		}

		/// <summary>
		/// 	Open one of the most important screens on the main menu.
		/// </summary>
		public void SelectScreen(Screen screen)
		{
			switch (screen)
			{
				case Screen.None:
					MainMenu.SetActive(true);
					CreateCharacterPanel.SetActive(false);
					StartingPointScreen.SetActive(false);
					GlobalGUI.CloseLoadingScreen();
					break;
				case Screen.CreateCharacter:
					MainMenu.SetActive(false);
					CreateCharacterPanel.SetActive(true);
					StartingPointScreen.SetActive(false);
					GlobalGUI.CloseLoadingScreen();
					break;
				case Screen.StartingPoint:
					MainMenu.SetActive(false);
					CreateCharacterPanel.SetActive(false);
					StartingPointScreen.SetActive(true);
					GlobalGUI.CloseLoadingScreen();
					break;
			}
		}

		/// <summary>
		///		Starts connecting to multiplayer assuming you are on the main menu screen.
		/// </summary>
		public void PlayButton()
		{
			SelectScreen(Screen.None);
			ServerList.Open();
		}

		/// <summary>
		/// 	The settings button in the main menu.
		/// </summary>
		public void SettingsButton()
		{
			GlobalGUI.OpenSettingsScreen();
		}

		/// <summary>
		/// 	The quit button in the main menu.
		/// </summary>
		public void QuitButton()
		{
			Application.Quit();
		}

		public void ShowCharacterCreation(string suggestedName, Action<CharacterData> onCreated)
		{
			_onCharacterCreated = onCreated;
			CharacterInputField.text = suggestedName;
			SwitchCurrentGender();
			SelectScreen(Screen.CreateCharacter);
		}

		public void CreateCharacterButton()
		{
			if (CharacterInputField.text.IsNullOrEmpty())
			{
				return;
			}

			Action<CharacterData> onCreated = _onCharacterCreated;
			_onCharacterCreated = null;
			SelectScreen(Screen.None);

			onCreated?.Invoke(new CharacterData
			{
				Name = CharacterInputField.text,
				Gender = _currentGenderGUI,
				HeadType = 1,
				HairType = 1
			});
		}

		public void SwitchCurrentGender()
		{
			_currentGenderGUI = _currentGenderGUI == Gender.Male ? Gender.Female : Gender.Male;
			CurrentGenderText.text = _currentGenderGUI.ToLocalizedString();
			//TODO: InventoryCharacterPreview.Instance.ChangeGender(_currentGenderGUI);
		}

		public void QuitGameButton()
		{
			GlobalGUI.ShowConfirmMessageBox(Localization.ExitGame, Localization.AreYouSureExitGame, Localization.Yes,
				Localization.No, Application.Quit);
		}

		/// <summary>
		/// 	Handle exiting the statring point screen, the menu where you select if you want to continue or start a new game, as well as what type of new game.
		/// </summary>
		public void ExitStartingPointScreen()
		{
			// Go back to general spawn options (new game, continue, invite).
			if (FreshStartSpawnOptions.gameObject.activeSelf)
			{
				SpawnOptions.gameObject.SetActive(true);
				FreshStartSpawnOptions.gameObject.SetActive(false);
			}
			else
			{
				// Backing out abandons the connection attempt, so drop the pending choice with it.
				_onSpawnChosen = null;
				SelectScreen(Screen.None);
			}
		}
	}
}
