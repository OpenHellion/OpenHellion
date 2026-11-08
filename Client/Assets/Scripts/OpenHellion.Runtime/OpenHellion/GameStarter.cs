// GameStarter.cs
//
// Copyright (C) 2026, OpenHellion contributors
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
using OpenHellion.Net;
using OpenHellion.Social;
using OpenHellion.Social.Message;
using OpenHellion.Social.RichPresence;
using UnityEngine;
using UnityEngine.SceneManagement;
using OpenHellion.UI;
using ZeroGravity;
using ZeroGravity.Network;
using ZeroGravity.Objects;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;

namespace OpenHellion
{
	/// <summary>
	///		This class is to be instantiated when the we click the play button, and then destroyed when we finally connect.
	/// </summary>
	/// <remarks>
	///		Start a game by calling Create and FindAndConnectServer. It is expected that we are not calling this from the World scene.
	/// </remarks>
	public class GameStarter : MonoBehaviour
	{
		private InviteMessage _inviteMessage;

		private ServerConnectionInfo? _server;

		private CharacterData _character;

		private SpawnPointDetails _spawn;

		private string _password;

		private World _world;

		/// <summary>
		///		Creates a GameStarter instance.
		/// </summary>
		/// <returns>An instance of GameStarter.</returns>
		public static GameStarter Create(ServerConnectionInfo? server = null, CharacterData character = null,
			SpawnPointDetails spawn = null, string password = null, InviteMessage inviteMessage = null)
		{
			var gameObject = new GameObject();
			var gameStarter = gameObject.AddComponent<GameStarter>();
			gameStarter._server = server;
			gameStarter._character = character;
			gameStarter._spawn = spawn;
			gameStarter._password = password;
			gameStarter._inviteMessage = inviteMessage;

			DontDestroyOnLoad(gameStarter);

			return gameStarter;
		}

		/// <summary>
		/// 	Work out which server to join, then start the game.
		/// </summary>
		public async UniTaskVoid FindServerAndConnect(bool reconnecting = false)
		{
			GlobalGUI.ShowLoadingScreen(GlobalGUI.LoadingScreenType.ConnectingToMain);

			if (reconnecting)
			{
				_server = Globals.LastConnectedServer;
			}
			else if (_inviteMessage is not null)
			{
				// An invite carries the address directly, so joining a friend never needs a main server.
				_server = new ServerConnectionInfo
				{
					IpAddress = _inviteMessage.IpAddress,
					GamePort = _inviteMessage.GamePort,
					StatusPort = _inviteMessage.StatusPort
				};
			}

			if (_server is not { } server)
			{
				GlobalGUI.ShowMessageBox(Localization.ConnectionError, Localization.NoServerConnection);
				Debug.LogError("Asked to connect without a server to connect to.");
				GlobalGUI.CloseLoadingScreen();
				Destroy(gameObject);
				return;
			}

			await ConnectToServer(server);
		}

		/// <summary>
		/// 	Connect to a remote server.
		/// </summary>
		private async UniTask ConnectToServer(ServerConnectionInfo server)
		{
			GlobalGUI.ShowLoadingScreen(GlobalGUI.LoadingScreenType.ConnectingToGame);

			await SceneManager.LoadSceneAsync("WorldScene", LoadSceneMode.Single);

			_world = GameObject.Find("/World").GetComponent<World>();
			Debug.Assert(_world != null);

			try
			{
				await NetworkController.ConnectToGame(server.IpAddress, server.GamePort, _world.OnDisconnectedFromServer);
				Globals.LastConnectedServer = server;

				Debug.Log("Successfully established connection with server.");

				LogInRequest logInRequest = new LogInRequest
				{
					ClientHash = Globals.CombinedHash,
					IsOffline = Profile.OfflineMode,
					PlayerId = Profile.PlayerId,
					Password = _password,
					CharacterData = _character ?? (Profile.OfflineMode ? null : await NakamaClient.GetCharacterData())
				};

				var response = await NetworkController.SendReceiveAsync(logInRequest, 10000) as LogInResponse;

				if (response != null && response.Status == NetworkData.MessageStatus.Success)
				{
					Debug.Log("Received log in response.");

					GlobalGUI.ShowLoadingScreen(GlobalGUI.LoadingScreenType.LoadWorld);

					bool wasLoginSuccessful = await _world.OnLogin(response, _spawn, _inviteMessage?.SpawnPointId);

					if (wasLoginSuccessful)
					{
						AkSoundEngine.SetRTPCValue(SoundManager.InGameVolume, 1f);
						MyPlayer.Instance.InitializeCameraEffects();
						Globals.ToggleCursor(false);

						_world.ActivatePlayerDelegate();
						await NetworkController.SendAsync(new EnvironmentReadyMessage());
						MyPlayer.Instance.PlayerReady = true;
						RichPresenceManager.UpdateStatus(_world);

						_world.InGameGUI.HelmetHud.gameObject.Activate(true);
						GlobalGUI.CloseLoadingScreen();
					}

					Destroy(gameObject);
					return;
				}
				/*else if (response.Status == NetworkData.MessageStatus.VersionError)
				{
					Debug.LogWarning("Version error.");
					GlobalGUI.ShowMessageBox(Localization.ConnectionError, Localization.VersionError);
				}*/
				else
				{
					Debug.LogWarning("Error in login data.");
					GlobalGUI.ShowErrorMessage(Localization.ConnectionError,
						_password is not null ? Localization.WrongServerPassword : Localization.NoServerConnection);
				}
			}
			catch (SocketException)
			{
				GlobalGUI.ShowErrorMessage(Localization.ConnectionError, Localization.NoServerConnection);
				Debug.LogWarning("Server refused connection.");
			}
			catch (TimeoutException)
			{
				GlobalGUI.ShowErrorMessage(Localization.ConnectionError, Localization.ConnectionTimedOut);
			}
			catch (UnityException ex)
			{
				Debug.LogException(ex);
			}

			GlobalGUI.CloseLoadingScreen();
			MainMenuGUI.ReturnToServerList = true;
			SceneManager.LoadScene(1);
			Destroy(gameObject);
		}
	}
}
