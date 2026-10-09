// RichPresenceManager.cs
//
// Copyright (C) 2026, OpenHellion contributors
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
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using UnityEngine;
using ZeroGravity;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace OpenHellion.Social.RichPresence
{
	/// <summary>
	/// 	Manages rich presence on Steam and Discord.
	/// </summary>
	/// <seealso cref="DiscordProvider"/>
	/// <seealso cref="SteamProvider"/>
	public static class RichPresenceManager
	{
		public struct ActivityStatus
		{
			public string State;
			public string Details;
			public string LargeImageId;
			public string LargeText;
			public string SmallImageId;
			public string SmallText;
			public string JoinSecret;
			public int PlayerCount;
			public int MaxPlayers;
		}

		private static readonly Dictionary<long, string> Planets = new()
		{
			{ 1L, "Hellion" },
			{ 2L, "Nimath" },
			{ 3L, "Athnar" },
			{ 4L, "Ulgorat" },
			{ 5L, "Tasciana" },
			{ 6L, "Hirath" },
			{ 7L, "Calipso" },
			{ 8L, "Iblith" },
			{ 9L, "Enigma" },
			{ 10L, "Eridil" },
			{ 11L, "Arhlan" },
			{ 12L, "Teiora" },
			{ 13L, "Sinha" },
			{ 14L, "Bethyr" },
			{ 15L, "Burner" },
			{ 16L, "Broken marble" },
			{ 17L, "Everest station" },
			{ 18L, "Askatar" },
			{ 19L, "Ia" }
		};

		private static readonly List<string> Descriptions = new()
		{
			"Building station", "Mining asteroids", "In a salvaging mission", "Doing a piracy job",
			"Repairing a hull breach"
		};

		public static bool HasSteam { get; private set; }

		public static bool HasDiscord { get; private set; }

		private static SteamProvider _steam;

		private static DiscordProvider _discord;

		public static void Initialise()
		{
			_steam = new SteamProvider();
			_discord = new DiscordProvider();

			HasSteam = _steam.Initialise();
			HasDiscord = _discord.Initialise();

			if (HasSteam)
			{
				_steam.Enable();
			}
		}

		public static void Update()
		{
			if (HasSteam)
			{
				_steam.Update();
			}

			if (HasDiscord)
			{
				_discord.Update();
			}
		}

		// Because of Steam, this should be called on either OnDestroy or OnDisable.
		public static void Shutdown()
		{
			if (HasSteam)
			{
				_steam.Shutdown();
				HasSteam = false;
			}

			if (HasDiscord)
			{
				_discord.Shutdown();
				HasDiscord = false;
			}
		}

		/// <summary>
		/// 	Used to update rich presence.
		/// </summary>
		public static void UpdateStatus(World world = null)
		{
			ActivityStatus activityStatus;
			if (MyPlayer.Instance != null && MyPlayer.Instance.PlayerReady)
			{
				activityStatus = new()
				{
					JoinSecret = Globals.GetInviteString(null),
					LargeText = Localization.InGameDescription,
					Details = Descriptions[Random.Range(0, Descriptions.Count - 1)],
					SmallImageId = Gender.Male.ToLocalizedString().ToLower(),
					SmallText = MyPlayer.Instance.PlayerName,
					//PlayerCount = 0, // TODO: Get player count and max players from server.
					//MaxPlayers = 0
				};

				if (world != null && MyPlayer.Instance.Parent
				    && world.SolarSystem.GetParentCelestialBody(
					    world.LocalToWorldPosition(MyPlayer.Instance.Parent.transform.position)) is var parentCelestialBody)
				{
					if (Planets.TryGetValue(parentCelestialBody.Guid, out var value))
					{
						activityStatus.LargeImageId = parentCelestialBody.Guid.ToString();
					}
					else
					{
						activityStatus.LargeImageId = "default";
						value = parentCelestialBody.Name;
					}

					if (MyPlayer.Instance.Parent is Ship { IsWarpOnline: true })
					{
						activityStatus.State = Localization.WarpingNear + " " + value.ToUpper();
					}
					else if (MyPlayer.Instance.Parent is Ship Pivot)
					{
						activityStatus.State = Localization.FloatingFreelyNear + " " + value.ToUpper();
					}
					else
					{
						activityStatus.State = Localization.OrbitingNear + " " + value.ToUpper();
					}
				}
			}
			else
			{
				activityStatus = new()
				{
					State = "In Menus",
					Details = "Launch Sequence Initiated",
					LargeImageId = "cover"
				};
			}

			if (HasSteam)
			{
				_steam.UpdateStatus(activityStatus);
			}

			if (HasDiscord)
			{
				_discord.UpdateStatus(activityStatus);
			}
		}

		/// <summary>
		/// 	Get if we have achieved a specific achievement.
		/// </summary>
		public static bool GetAchievement(AchievementID id)
		{
			return true;
			/* Achievements are not implemented.
			if (HasSteam)
			{
				return _steam.GetAchievement(id);
			}

			return false;*/
		}

		/// <summary>
		/// 	Award the player an achievement.
		/// </summary>
		public static void SetAchievement(AchievementID id)
		{
			if (HasSteam)
			{
				_steam.SetAchievement(id);
			}
		}

		public static string GetUsername()
		{
			if (HasSteam)
			{
				return _steam.GetUsername();
			}

			if (HasDiscord)
			{
				return _discord.GetUsername();
			}

			return null;
		}

		public static void InviteUser(ulong steamId, long discordId, VesselObjectID spawnPointId)
		{
			string inviteString = Globals.GetInviteString(spawnPointId);
			if (HasSteam)
			{
				_steam.InviteUser(steamId, inviteString);
			}

			if (HasDiscord)
			{
				_discord.InviteUser(discordId, inviteString);
			}
		}

		public static List<ServerConnectionInfo> GetFriendServers()
		{
			if (HasSteam)
			{
				return _steam.GetFriendServers();
			}

			return new List<ServerConnectionInfo>();
		}

		/// <summary>
		/// 	Get the avatar of a specified user as a texture.
		/// </summary>
		public static Texture2D GetAvatar(string id)
		{
			return Resources.Load<Texture2D>("UI/default_avatar");
		}
	}
}
