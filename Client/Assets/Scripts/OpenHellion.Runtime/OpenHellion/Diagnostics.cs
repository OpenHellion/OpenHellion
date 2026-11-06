using System;
using System.IO;
using System.Linq;
using OpenHellion.Net;
using UnityEngine;
using ZeroGravity.LevelDesign;
using ZeroGravity.Math;
using ZeroGravity.Objects;
using Conditional = System.Diagnostics.ConditionalAttribute;

namespace OpenHellion
{
	public static class Diagnostics
	{
		public sealed class DiagnosticsState
		{
			public string Parent;
			public bool ParentKnown;
			public bool ParentIsVessel;
			public float ParentHealth;
			public float ParentMaxHealth;
			public long ParentDockedTo;
			public bool ParentLoaded;
			public Vector3 ParentVelocity;
			public Vector3 ParentAngularVelocity;
			public string Anchor;
			public string Room;
			public bool HasRoom;
			public float RoomAirPressure;
			public float RoomAirQuality;
			public string RoomPressurization;
			public bool RoomBreathable;
			public bool RoomFireCanBurn;
			public bool RoomUseGravity;
			public Vector3 RoomGravity;
			public Vector3 Gravity;
			public bool ZeroG;
			public bool Grounded;
			public bool Ragdolled;
			public bool GravityChangeRagdolled;
			public string ControlMode;
			public int Health;
			public Vector3 LocalPosition;
			public Vector3D WorldPosition;
			public Vector3 Velocity;
			public int LatencyMs;
			public int MessagesReceived;
			public int MessagesDropped;
			public int Vessels;
			public int VesselsNotLoaded;
			public int DynamicObjects;
			public int Players;
		}

		public const int ParentRow = 0;
		public const int AnchorRow = 1;
		public const int RoomRow = 2;
		public const int StateRow = 3;
		public const int ControlRow = 4;
		public const int HealthRow = 5;
		public const int GravityRow = 6;
		public const int LocalRow = 7;
		public const int WorldRow = 8;
		public const int VelocityRow = 9;
		public const int ParentHealthRow = 10;
		public const int ParentDockingRow = 11;
		public const int ParentSceneRow = 12;
		public const int ParentVelocityRow = 13;
		public const int ParentSpinRow = 14;
		public const int RoomAirRow = 15;
		public const int RoomPressurizationRow = 16;
		public const int RoomGravityRow = 17;
		public const int LatencyRow = 18;
		public const int MessagesRow = 19;
		public const int ObjectsRow = 20;
		public const int RowCount = 21;

		private static StreamWriter _log;

		private static (long Guid, Vector3 Offset, float RelativeSpeed)[] _rebaseSnapshot;

		private static long _rebaseFromAnchor;

		public static bool TryGather(out DiagnosticsState state)
		{
			state = null;
			MyPlayer player = MyPlayer.Instance;
			if (player == null || !player.PlayerReady)
			{
				return false;
			}

			World world = player.World;
			SpaceObject parent = player.Parent;
			state = new DiagnosticsState
			{
				Parent = Describe(parent),
				ParentKnown = parent != null && world.TryGetSpaceObject(parent.Guid, out SpaceObject _),
				Anchor = Describe(world.TryGetSpaceObject(world.AnchorGuid, out ArtificialBody anchor) ? anchor : null) +
				         (anchor == null ? $" ({world.AnchorGuid})" : ""),
				Gravity = parent != null ? Quaternion.Inverse(parent.transform.rotation) * player.Gravity : player.Gravity,
				ZeroG = player.FpsController.IsZeroG,
				Grounded = player.FpsController.IsGrounded,
				Ragdolled = player.IsRagdolled,
				GravityChangeRagdolled = player.FpsController.IsGravityChangeRagdolled,
				ControlMode = player.ShipControlMode.ToString(),
				Health = player.Health,
				LocalPosition = player.transform.localPosition,
				WorldPosition = world.LocalToWorldPosition(player.transform.position),
				Velocity = player.FpsController.Velocity,
				LatencyMs = world.LatencyMs,
				MessagesReceived = NetworkController.MessagesReceived,
				MessagesDropped = NetworkController.MessagesDropped,
				DynamicObjects = world.AllDynamicObjects.Count(),
				Players = world.AllPlayers.Count()
			};

			if (parent is SpaceObjectVessel parentVessel)
			{
				state.ParentIsVessel = true;
				state.ParentHealth = parentVessel.Health;
				state.ParentMaxHealth = parentVessel.MaxHealth;
				state.ParentDockedTo = parentVessel.DockedToVessel != null ? parentVessel.DockedToVessel.Guid : 0;
				state.ParentLoaded = parentVessel.SceneObjectsLoaded;
			}

			if (parent is ArtificialBody parentBody)
			{
				state.ParentVelocity = parentBody.Velocity;
				state.ParentAngularVelocity = parentBody.AngularVelocity;
			}

			SceneTriggerRoom room = player.CurrentRoomTrigger;
			state.HasRoom = room != null;
			state.Room = room != null ? $"{room.RoomName} ({room.InSceneID})" : "none";
			if (room != null)
			{
				state.RoomAirPressure = room.AirPressure;
				state.RoomAirQuality = room.AirQuality;
				state.RoomPressurization = room.PressurizationStatus.ToString();
				state.RoomBreathable = room.IsAirOk;
				state.RoomFireCanBurn = room.FireCanBurn;
				state.RoomUseGravity = room.UseGravity;
				state.RoomGravity = room.GravityForce;
			}

			foreach (SpaceObjectVessel vessel in world.AllArtificialBodies.OfType<SpaceObjectVessel>())
			{
				state.Vessels++;
				if (!vessel.SceneObjectsLoaded)
				{
					state.VesselsNotLoaded++;
				}
			}

			return true;
		}

		private static string Describe(SpaceObject spaceObject) => spaceObject switch
		{
			null => "none",
			Pivot pivot => $"Pivot of {pivot.ChildType} ({pivot.Guid})",
			SpaceObjectVessel { CustomName: { Length: > 0 } customName } vessel => $"{vessel.GetType().Name} '{customName}' ({vessel.Guid})",
			_ => $"{spaceObject.GetType().Name} '{spaceObject.name}' ({spaceObject.Guid})"
		};

		public static string Row(DiagnosticsState state, int row) => row switch
		{
			ParentRow => $"parent   {state.Parent}",
			AnchorRow => $"anchor   {state.Anchor}",
			RoomRow => $"room     {state.Room}",
			StateRow => $"state    {(state.ZeroG ? "zero-g" : "1g")} {(state.Grounded ? "grounded" : "airborne")}{(state.Ragdolled ? " ragdolled" : "")}{(state.GravityChangeRagdolled ? " gravity-change-ragdoll" : "")}",
			ControlRow => $"control  {state.ControlMode}",
			HealthRow => $"health   {state.Health}",
			GravityRow => $"gravity  {state.Gravity:F2} in parent frame",
			LocalRow => $"local    {state.LocalPosition:F2}",
			WorldRow => $"world    {state.WorldPosition.ToString("F1")}",
			VelocityRow => $"velocity {state.Velocity:F2} |{state.Velocity.magnitude:F2}|",
			ParentHealthRow => state.ParentIsVessel ? $"health   {state.ParentHealth:F0}/{state.ParentMaxHealth:F0}" : "health   n/a",
			ParentDockingRow => !state.ParentIsVessel ? "docking  n/a" : state.ParentDockedTo != 0 ? $"docking  docked to {state.ParentDockedTo}" : "docking  not docked",
			ParentSceneRow => !state.ParentIsVessel ? "scene    n/a" : state.ParentLoaded ? "scene    loaded" : "scene    not loaded",
			ParentVelocityRow => $"velocity {state.ParentVelocity:F2} |{state.ParentVelocity.magnitude:F2}|",
			ParentSpinRow => $"spin     {state.ParentAngularVelocity:F3} |{state.ParentAngularVelocity.magnitude:F3}|",
			RoomAirRow => state.HasRoom ? $"air      pressure {state.RoomAirPressure:F2} quality {state.RoomAirQuality:F2}" : "air      no room",
			RoomPressurizationRow => state.HasRoom
				? $"status   {state.RoomPressurization}{(state.RoomBreathable ? " breathable" : " unbreathable")}{(state.RoomFireCanBurn ? " fire-can-burn" : "")}"
				: "status   no room",
			RoomGravityRow => !state.HasRoom ? "gravity  no room" : state.RoomUseGravity ? $"gravity  on {state.RoomGravity:F2}" : "gravity  off",
			LatencyRow => $"latency  {state.LatencyMs} ms",
			MessagesRow => $"messages {state.MessagesReceived} received, {state.MessagesDropped} dropped",
			ObjectsRow => $"objects  {state.Vessels} vessels ({state.VesselsNotLoaded} not loaded), {state.DynamicObjects} items, {state.Players} players",
			_ => string.Empty
		};

		public static bool IsLogged(int row) => row is <= GravityRow or ParentHealthRow or ParentDockingRow or ParentSceneRow
			or RoomAirRow or RoomPressurizationRow or RoomGravityRow or ObjectsRow;

		public static bool IsInvalid(DiagnosticsState state, int row) => row switch
		{
			ParentRow => !state.ParentKnown,
			GravityRow => !float.IsFinite(state.Gravity.sqrMagnitude),
			LocalRow => !float.IsFinite(state.LocalPosition.sqrMagnitude),
			WorldRow => !double.IsFinite(state.WorldPosition.X + state.WorldPosition.Y + state.WorldPosition.Z),
			VelocityRow => !float.IsFinite(state.Velocity.sqrMagnitude),
			ParentVelocityRow => !float.IsFinite(state.ParentVelocity.sqrMagnitude),
			ParentSpinRow => !float.IsFinite(state.ParentAngularVelocity.sqrMagnitude),
			_ => false
		};

		[Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
		public static void Log(string message)
		{
			_log ??= new StreamWriter(Path.Combine(Application.persistentDataPath,
				$"Diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.log")) { AutoFlush = true };
			_log.WriteLine($"{DateTime.UtcNow:HH:mm:ss.ffff} f{Time.frameCount} {message}");
		}

		[Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
		public static void SnapshotBeforeRebase(World world, MyPlayer player)
		{
			Vector3 playerVelocity = player.FpsController.Velocity;
			_rebaseFromAnchor = world.AnchorGuid;
			_rebaseSnapshot = world.AllArtificialBodies
				.Select(body => (body.Guid, body.transform.position - player.transform.position, (body.Velocity - playerVelocity).magnitude))
				.ToArray();
		}

		[Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
		public static void AssertRebaseKeptRelativePositions(World world, MyPlayer player)
		{
			if (_rebaseSnapshot == null)
			{
				return;
			}

			foreach ((long guid, Vector3 offset, float relativeSpeed) in _rebaseSnapshot)
			{
				if (world.TryGetSpaceObject(guid, out ArtificialBody body))
				{
					float error = (body.transform.position - player.transform.position - offset).magnitude;
					Assert(error <= 1f + relativeSpeed * 0.5f,
						$"anchor {_rebaseFromAnchor} -> {world.AnchorGuid} moved {guid} by {error:F2} m relative to the player");
				}
			}

			_rebaseSnapshot = null;
		}

		[Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
		public static void Assert(bool condition, string message)
		{
			if (condition)
			{
				return;
			}

			UnityEngine.Debug.LogError("Assertion failed: " + message);
			Log("assertion failed: " + message);
			if (TryGather(out DiagnosticsState state))
			{
				for (int row = 0; row < RowCount; row++)
				{
					Log("  " + Row(state, row));
				}
			}
		}
	}
}
