using System;
using System.Collections.Generic;
using System.Linq;
using OpenHellion;
using OpenHellion.Net;
using OpenHellion.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;
using ZeroGravity.Objects;
using ZeroGravity.ShipComponents;
using Cysharp.Threading.Tasks;

namespace ZeroGravity.UI
{
	public class SecurityScreen : MonoBehaviour
	{
		public SecuritySystem SecuritySystem;

		public GameObject FreeTerminal;

		public GameObject RegistredTerminal;

		public Button ClaimButton;

		public Button InviteButton;

		public Button ResignButton;

		public Button ChangeNameButton;

		public Button ChangeEmblemButton;

		public Button ShipCrewButton;

		[Space(20f)] public Text PlayerName;

		public RawImage PlayerImage;

		public Texture DefaultTexture;

		public Text CustomShipName;

		public Text DefaultShipName;

		public GameObject EnterCustomNamePopUp;

		public InputField CustomShipNameInputField;

		public GameObject ResignAlertBox;

		public GameObject CrewMemberPref;

		private List<CrewMembersUI> _crewMembersList = new List<CrewMembersUI>();

		public GameObject CrewMemberPanel;

		public GameObject PromotePlayerBox;

		public GameObject ShipCrewList;

		public GameObject PlayerToInvitePref;

		public GameObject InviteList;

		public Dictionary<string, InvitePlayerToPod> AvailablePlayersForInvite =
			new Dictionary<string, InvitePlayerToPod>();

		private AuthorizedPerson _selectedCrewman;

		public GameObject SelfDestructBox;

		public GameObject SelfDestructWarning;

		public Button SelfDestructButton;

		public Text SelfDestructState;

		public Text SelfDestructTimer;

		public GameObject ChooseEmblemPanel;

		public Transform EmblemParent;

		public GameObject EmblemObject;

		public Texture DefaultEmblemTexture;

		[FormerlySerializedAs("_worldState")] [SerializeField] private World _world;

		private void Start()
		{
			UpdateUI();
			ClaimButton.onClick.AddListener(delegate { ClaimSecurityTerminal(); });
			ResignButton.onClick.AddListener(delegate { Resign(); });
			ChangeNameButton.onClick.AddListener(delegate { ChangeShipName(); });
			ChangeEmblemButton.onClick.AddListener(delegate { ShowEmblemPanel(status: true); });
			InviteButton.onClick.AddListener(delegate { GetPlayerList().Forget(); });
			ShipCrewButton.onClick.AddListener(delegate { ShowShipCrew(status: true); });
			SelfDestructButton.onClick.AddListener(delegate { SelfDestructAction(); });
		}

		private void OnDestroy()
		{
			ClaimButton.onClick.RemoveAllListeners();
			ResignButton.onClick.RemoveAllListeners();
			ChangeNameButton.onClick.RemoveAllListeners();
			ChangeEmblemButton.onClick.RemoveAllListeners();
			InviteButton.onClick.RemoveAllListeners();
			ShipCrewButton.onClick.RemoveAllListeners();
		}

		private void Update()
		{
			if (EnterCustomNamePopUp.activeInHierarchy)
			{
				if (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
				{
					CancelChangeShipName();
				}

				if (Keyboard.current.enterKey.wasPressedThisFrame)
				{
					ChangeCustomShipName();
				}
			}
		}

		public void SetSecurityStatus()
		{
			AuthorizedPerson commandingOfficer = SecuritySystem.AuthorizedPlayers.Find((AuthorizedPerson m) =>
				m.Rank == AuthorizedPersonRank.CommandingOfficer);
			FreeTerminal.SetActive(commandingOfficer == null);
			RegistredTerminal.SetActive(commandingOfficer != null);


			if (commandingOfficer == null)
			{
				PlayerName.text = "-";
				PlayerImage.texture = DefaultTexture;
			}
			else
			{
				PlayerName.text = commandingOfficer.Name;
				PlayerImage.texture = Player.GetAvatar(commandingOfficer.PlayerId);
				if (!SecuritySystem.ParentShip.SelfDestructTimer.HasValue)
				{
					ResignButton.gameObject.SetActive(SecuritySystem.GetPlayerRank(MyPlayer.Instance) !=
					                                  AuthorizedPersonRank.None);
					InviteButton.gameObject.SetActive(SecuritySystem.GetPlayerRank(MyPlayer.Instance) ==
					                                  AuthorizedPersonRank.CommandingOfficer);
					ChangeNameButton.gameObject.SetActive(SecuritySystem.GetPlayerRank(MyPlayer.Instance) ==
					                                      AuthorizedPersonRank.CommandingOfficer);
					ChangeEmblemButton.gameObject.SetActive(
						SecuritySystem.GetPlayerRank(MyPlayer.Instance) == AuthorizedPersonRank.CommandingOfficer &&
						SecuritySystem.ParentShip.Emblems.Count > 0);
					ShipCrewButton.gameObject.SetActive(SecuritySystem.GetPlayerRank(MyPlayer.Instance) !=
					                                    AuthorizedPersonRank.None);
				}
				else
				{
					ResignButton.gameObject.SetActive(false);
					InviteButton.gameObject.SetActive(false);
					ChangeNameButton.gameObject.SetActive(false);
					ChangeEmblemButton.gameObject.SetActive(false);
					ShipCrewButton.gameObject.SetActive(false);
				}

				SelfDestructState.text =
					((!SecuritySystem.ParentShip.SelfDestructTimer.HasValue)
						? Localization.Activate.ToUpper()
						: Localization.Cancel.ToUpper()) + " " + Localization.SelfDestruct.ToUpper();
			}

			SelfDestructButton.gameObject.SetActive(SecuritySystem.GetPlayerRank(MyPlayer.Instance) ==
			                                        AuthorizedPersonRank.CommandingOfficer);
			RefreshSelfDestructTimer();
		}

		public void UpdateUI()
		{
			if (SecuritySystem != null)
			{
				SetShipName();
				SetSecurityStatus();
				UpdateSecurityList();
				UpdateEmblems();
			}
		}

		private void SetShipName()
		{
			if (SecuritySystem.ParentShip.VesselName.IsNullOrEmpty())
			{
				CustomShipName.text = Localization.UnnamedVessel.ToUpper();
			}
			else
			{
				CustomShipName.text = SecuritySystem.ParentShip.VesselName.ToUpper();
			}

			if (SecuritySystem.ParentShip.VesselRegistration != null)
			{
				DefaultShipName.text = SecuritySystem.ParentShip.VesselRegistration.ToUpper();
			}

			UpdateShipNameTags();
		}

		private void ChangeShipName()
		{
			EnterCustomNamePopUp.SetActive(value: true);
			CustomShipNameInputField.text = CustomShipName.text;
			CustomShipNameInputField.Select();
			_world.InGameGUI.IsInputFieldIsActive = true;
		}

		public void CancelChangeShipName()
		{
			_world.InGameGUI.IsInputFieldIsActive = false;
			EnterCustomNamePopUp.SetActive(value: false);
		}

		public void ChangeEmblem(string id)
		{
			SecuritySystem.ParentShip.ChangeStats(null, null, null, null, null, null, null, null, null, null, null,
				null, null, null, id);
			ShowEmblemPanel(status: false);
		}

		public void ChangeCustomShipName()
		{
			string text = CustomShipName.text.ToUpper();
			if (CustomShipNameInputField.text != null)
			{
				CustomShipName.text = CustomShipNameInputField.text.ToUpper();
				SecuritySystem.ChangeVesselName(CustomShipName.text);
			}
			else
			{
				CustomShipName.text = text;
			}

			SetShipName();
			_world.InGameGUI.IsInputFieldIsActive = false;
			EnterCustomNamePopUp.SetActive(value: false);
		}

		private void UpdateShipNameTags()
		{
			SceneNameTag[] shipNameTags = SecuritySystem.ShipNameTags;
			foreach (SceneNameTag sceneNameTag in shipNameTags)
			{
				sceneNameTag.SetNameTagText(CustomShipName.text);
			}
		}

		private async UniTaskVoid GetPlayerList()
		{
			PurgeList();
			await SecuritySystem.GetPlayersForAuthorization(getFriends: true, getPlayerFromServer: true, UpdatePlayerList);
		}

		public void UpdateSecurityList()
		{
			foreach (CrewMembersUI crewMembers in _crewMembersList)
			{
				Destroy(crewMembers.gameObject);
			}

			_crewMembersList.Clear();
			AuthorizedPerson ourPerson =
				SecuritySystem.AuthorizedPlayers.Find((AuthorizedPerson m) => m.PlayerId == MyPlayer.Instance.PlayerId);
			foreach (AuthorizedPerson crewman in SecuritySystem.AuthorizedPlayers.FindAll((AuthorizedPerson m) =>
				         m.Rank == AuthorizedPersonRank.Crewman))
			{
				GameObject gameObject = Instantiate(CrewMemberPref, CrewMemberPref.transform.parent);
				gameObject.SetActive(value: true);
				CrewMembersUI component = gameObject.GetComponent<CrewMembersUI>();
				component.Player = crewman;
				_crewMembersList.Add(component);
				component.PlayerNameText.text = crewman.Name;
				component.Avatar.texture = Player.GetAvatar(crewman.PlayerId);
				if (ourPerson != null && ourPerson.Rank == AuthorizedPersonRank.CommandingOfficer)
				{
					component.GetComponent<Button>().interactable = true;
					component.GetComponent<Button>().onClick.AddListener(delegate { CrewMemberActions(crewman); });
				}
				else
				{
					component.GetComponent<Button>().interactable = false;
					component.GetComponent<Button>().onClick.RemoveAllListeners();
				}
			}
		}

		public void CrewMemberActions(AuthorizedPerson crewman)
		{
			CrewMemberPanel.SetActive(value: true);
			CrewMemberPanel.GetComponentInChildren<CrewMembersUI>().Avatar.texture = Player.GetAvatar(crewman.PlayerId);
			CrewMemberPanel.GetComponentInChildren<CrewMembersUI>().PlayerNameText.text = crewman.Name;
			_selectedCrewman = crewman;
		}

		public void CloseCrewMemberPanel()
		{
			_selectedCrewman = null;
			CrewMemberPanel.Activate(value: false);
			PromotePlayerBox.Activate(value: false);
		}

		public void PromotePlayer()
		{
			PromotePlayerBox.SetActive(value: true);
		}

		public void CancelPromote()
		{
			PromotePlayerBox.SetActive(value: false);
		}

		public void ConfirmPlayerPromotion()
		{
			NetworkController.SendAndForget(new VesselSecurityRequest
			{
				VesselGUID = SecuritySystem.ParentShip.Guid,
				AddPlayerId = _selectedCrewman.PlayerId,
				AddPlayerRank = AuthorizedPersonRank.CommandingOfficer,
				AddPlayerName = _selectedCrewman.Name
			});
			CloseCrewMemberPanel();
		}

		public void RemoveCrewMember()
		{
			RemovePlayer(_selectedCrewman);
			CloseCrewMemberPanel();
		}

		public void ClaimSecurityTerminal()
		{
			// Check if nobody owns the terminal, or if we are the owner.
			AuthorizedPerson commandingOfficer = SecuritySystem.AuthorizedPlayers.Find((AuthorizedPerson m) =>
				m.Rank == AuthorizedPersonRank.CommandingOfficer);
			if (commandingOfficer == null || commandingOfficer.PlayerId == MyPlayer.Instance.PlayerId)
			{
				NetworkController.SendAndForget(new VesselSecurityRequest
				{
					VesselGUID = SecuritySystem.ParentShip.Guid,
					AddPlayerId = MyPlayer.Instance.PlayerId,
					AddPlayerRank = AuthorizedPersonRank.CommandingOfficer,
					AddPlayerName = MyPlayer.Instance.PlayerName
				});
			}

			UpdateUI();
		}

		public void Resign()
		{
			AuthorizedPerson ourPerson =
				SecuritySystem.AuthorizedPlayers.Find((AuthorizedPerson m) => m.PlayerId == MyPlayer.Instance.PlayerId);
			if (ourPerson.Rank == AuthorizedPersonRank.CommandingOfficer &&
			    SecuritySystem.AuthorizedPlayers.Find((AuthorizedPerson m) => m.Rank == AuthorizedPersonRank.Crewman) !=
			    null)
			{
				ResignAlertBox.SetActive(value: true);
			}
			else
			{
				SecuritySystem.RemovePerson(ourPerson);
			}
		}

		public void CancelResign()
		{
			ResignAlertBox.SetActive(value: false);
		}

		public void ConfirmCommanderResign()
		{
			AuthorizedPerson ourPerson =
				SecuritySystem.AuthorizedPlayers.Find((AuthorizedPerson m) => m.PlayerId == MyPlayer.Instance.PlayerId);
			SecuritySystem.RemovePerson(ourPerson);
			AuthorizedPerson crewman =
				SecuritySystem.AuthorizedPlayers.Find((AuthorizedPerson m) => m.Rank == AuthorizedPersonRank.Crewman);
			NetworkController.SendAndForget(new VesselSecurityRequest
			{
				VesselGUID = SecuritySystem.ParentShip.Guid,
				AddPlayerId = crewman.PlayerId,
				AddPlayerRank = AuthorizedPersonRank.CommandingOfficer,
				AddPlayerName = crewman.Name
			});
			ResignAlertBox.SetActive(value: false);
			UpdateUI();
		}

		private void UpdatePlayerList(List<AuthorizedPerson> availablePlayers)
		{
			InviteList.SetActive(value: true);
			InviteList.GetComponentInChildren<Scrollbar>(includeInactive: true).value = 1f;
			foreach (AuthorizedPerson pl in availablePlayers)
			{
				if (AvailablePlayersForInvite.ContainsKey(pl.PlayerId) || pl.Rank != 0)
				{
					continue;
				}

				if (SecuritySystem.AuthorizedPlayers.FirstOrDefault((AuthorizedPerson m) =>
					    m.PlayerId == pl.PlayerId) == null)
				{
					GameObject gameObject = Instantiate(PlayerToInvitePref, PlayerToInvitePref.transform.parent);
					gameObject.SetActive(value: true);
					InvitePlayerToPod component = gameObject.GetComponent<InvitePlayerToPod>();
					component.PlayerName.text = pl.Name;
					if (pl.IsFriend)
					{
						component.IsFriend.SetActive(value: false);
						component.Avatar.texture = Player.GetAvatar(pl.PlayerId);
					}
					else
					{
						component.IsFriend.SetActive(value: true);
						component.Avatar.gameObject.SetActive(value: false);
					}

					component.InvitePlayerButton.onClick.AddListener(delegate { AddToCrew(pl); });
					AvailablePlayersForInvite.Add(pl.PlayerId, component);
				}
			}
		}

		private void AddToCrew(AuthorizedPerson player)
		{
			SecuritySystem.AddPerson(player, AuthorizedPersonRank.Crewman);
			InviteList.SetActive(value: false);
			UpdateSecurityList();
		}

		private void RemovePlayer(AuthorizedPerson player)
		{
			SecuritySystem.RemovePerson(player);
		}

		public void SelfDestructAction()
		{
			float? selfDestructTimer = SecuritySystem.ParentShip.SelfDestructTimer;
			if (selfDestructTimer.HasValue)
			{
				ToggleSelfDestruct();
			}
			else
			{
				SelfDestructBox.SetActive(value: true);
			}
		}

		public void ToggleSelfDestruct()
		{
			float? selfDestructTimer = SecuritySystem.ParentShip.SelfDestructTimer;
			if (selfDestructTimer.HasValue)
			{
				SecuritySystem.ParentShip.CancelSelfDestruct();
				SelfDestructWarning.SetActive(value: false);
			}
			else
			{
				SecuritySystem.ParentShip.ActivateSelfDestruct(300f);
				SelfDestructWarning.SetActive(value: true);
				SelfDestructBox.SetActive(value: false);
			}

			RefreshSelfDestructTimer();
			Text selfDestructState = SelfDestructState;
			float? selfDestructTimer2 = SecuritySystem.ParentShip.SelfDestructTimer;
			selfDestructState.text =
				((!selfDestructTimer2.HasValue) ? Localization.Activate.ToUpper() : Localization.Cancel.ToUpper()) +
				" " + Localization.SelfDestruct.ToUpper();
			SetSecurityStatus();
		}

		public void RefreshSelfDestructTimer()
		{
			float? selfDestructTimer = SecuritySystem.ParentShip.SelfDestructTimer;
			if (selfDestructTimer.HasValue)
			{
				TimeSpan timeSpan = TimeSpan.FromSeconds(SecuritySystem.ParentShip.SelfDestructTimer.Value);
				string text = $"{timeSpan.TotalHours:n0} : {timeSpan.Minutes:n0} : {timeSpan.Seconds:n0}";
				SelfDestructTimer.text = text;
			}
			else
			{
				SelfDestructWarning.SetActive(value: false);
				SelfDestructTimer.text = string.Empty;
			}
		}

		private void PurgeList()
		{
			foreach (KeyValuePair<string, InvitePlayerToPod> item in AvailablePlayersForInvite)
			{
				Destroy(item.Value.gameObject);
			}

			AvailablePlayersForInvite.Clear();
		}

		public void ToggleCanvas(bool toggle)
		{
			if (toggle)
			{
				_world.InGameGUI.QuickTipHolder.Activate(value: false);
				UpdateUI();
				SelfDestructBox.Activate(value: false);
				InviteList.Activate(value: false);
				ShowShipCrew(status: false);
				EnterCustomNamePopUp.Activate(value: false);
				ResignAlertBox.Activate(value: false);
				ShowEmblemPanel(status: false);
				CloseCrewMemberPanel();
				RefreshSelfDestructTimer();
				_world.InGameGUI.IsInputFieldIsActive = false;
				UpdateEmblems();
				gameObject.Activate(value: true);
			}
			else
			{
				gameObject.Activate(value: false);
			}
		}

		public void ShowShipCrew(bool status)
		{
			ShipCrewList.SetActive(status);
		}

		public void ShowEmblemPanel(bool status)
		{
			ChooseEmblemPanel.SetActive(status);
			ChooseEmblemPanel.GetComponentInChildren<Scrollbar>(includeInactive: true).value = 1f;
		}

		public void UpdateEmblems()
		{
			EmblemObjectUI[] componentsInChildren =
				EmblemParent.GetComponentsInChildren<EmblemObjectUI>(includeInactive: true);
			foreach (EmblemObjectUI emblemObjectUI in componentsInChildren)
			{
				DestroyImmediate(emblemObjectUI.gameObject);
			}

			if (SecuritySystem.ParentShip.Emblems.Count == 0)
			{
				return;
			}

			string currentEmblemId = SecuritySystem.ParentShip.Emblems[0].EmblemId;
			GameObject gameObject = Instantiate(EmblemObject, EmblemParent);
			gameObject.transform.localScale = Vector3.one;
			EmblemObjectUI component = gameObject.GetComponent<EmblemObjectUI>();
			component.Panel = this;
			component.EmblemId = string.Empty;
			component.Texture = null;
			component.Image.texture = DefaultEmblemTexture;
			component.IsSelected.SetActive(component.EmblemId == currentEmblemId);
			foreach (Texture2D value in SceneVesselEmblem.Textures.Values)
			{
				GameObject gameObject2 = Instantiate(EmblemObject, EmblemParent);
				gameObject2.transform.localScale = Vector3.one;
				EmblemObjectUI component2 = gameObject2.GetComponent<EmblemObjectUI>();
				component2.Panel = this;
				component2.EmblemId = value.name;
				component2.Texture = value;
				component2.Image.texture = component2.Texture;
				component2.IsSelected.SetActive(component2.EmblemId == currentEmblemId);
			}
		}

		public void OnInteract()
		{
			ToggleCanvas(toggle: true);
		}

		public void OnDetach()
		{
			ToggleCanvas(toggle: false);
		}

		public void ExitButton()
		{
			MyPlayer.Instance.LockedToTrigger.CancelInteract(MyPlayer.Instance);
		}
	}
}
