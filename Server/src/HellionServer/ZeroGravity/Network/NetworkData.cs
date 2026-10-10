using System;
using OpenHellion.IO;
using OpenHellion.Net.Message;
using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
[ProtoInclude(100, typeof(LogInRequest))]
[ProtoInclude(101, typeof(LogOutRequest))]
[ProtoInclude(103, typeof(PlayerSpawnRequest))]
[ProtoInclude(108, typeof(ManeuverCourseRequest))]
[ProtoInclude(109, typeof(DistressCallRequest))]
[ProtoInclude(110, typeof(SuicideRequest))]
[ProtoInclude(111, typeof(PlayersOnServerRequest))]
[ProtoInclude(112, typeof(AvailableSpawnPointsRequest))]
[ProtoInclude(113, typeof(VesselSecurityRequest))]
[ProtoInclude(114, typeof(VesselRequest))]
[ProtoInclude(115, typeof(AuthorizedVesselsRequest))]
[ProtoInclude(200, typeof(LogInResponse))]
[ProtoInclude(201, typeof(LogOutResponse))]
[ProtoInclude(203, typeof(PlayerSpawnResponse))]
[ProtoInclude(206, typeof(ManeuverCourseResponse))]
[ProtoInclude(209, typeof(PlayersOnServerResponse))]
[ProtoInclude(210, typeof(AvailableSpawnPointsResponse))]
[ProtoInclude(211, typeof(VesselSecurityResponse))]
[ProtoInclude(212, typeof(VesselRequestResponse))]
[ProtoInclude(213, typeof(AuthorizedVesselsResponse))]
[ProtoInclude(300, typeof(CheckConnectionMessage))]
[ProtoInclude(301, typeof(EnvironmentReadyMessage))]
[ProtoInclude(303, typeof(DestroyObjectMessage))]
[ProtoInclude(304, typeof(PlayerStatsMessage))]
[ProtoInclude(305, typeof(PlayerShootingMessage))]
[ProtoInclude(309, typeof(ShipStatsMessage))]
[ProtoInclude(311, typeof(KillPlayerMessage))]
[ProtoInclude(313, typeof(PlayerRoomMessage))]
[ProtoInclude(317, typeof(TurretShootingMessage))]
[ProtoInclude(318, typeof(DestroyVesselMessage))]
[ProtoInclude(319, typeof(ResetServer))]
[ProtoInclude(320, typeof(TextChatMessage))]
[ProtoInclude(321, typeof(PlayerDrillingMessage))]
[ProtoInclude(325, typeof(TransferResourceMessage))]
[ProtoInclude(326, typeof(RefineResourceMessage))]
[ProtoInclude(327, typeof(ShipCollisionMessage))]
[ProtoInclude(328, typeof(ResetBaseBuilding))]
[ProtoInclude(330, typeof(VoiceCommDataMessage))]
[ProtoInclude(332, typeof(ServerShutDownMessage))]
[ProtoInclude(333, typeof(LatencyTestMessage))]
[ProtoInclude(334, typeof(ServerUpdateMessage))]
[ProtoInclude(335, typeof(NameTagMessage))]
[ProtoInclude(336, typeof(PortableTurretShootingMessage))]
[ProtoInclude(337, typeof(ExplosionMessage))]
[ProtoInclude(338, typeof(FabricateItemMessage))]
[ProtoInclude(339, typeof(RepairItemMessage))]
[ProtoInclude(340, typeof(RepairVesselMessage))]
[ProtoInclude(341, typeof(HurtPlayerMessage))]
[ProtoInclude(343, typeof(RoomPressureMessage))]
[ProtoInclude(344, typeof(RecycleItemMessage))]
[ProtoInclude(345, typeof(CancelFabricationMessage))]
[ProtoInclude(346, typeof(LockToTriggerMessage))]
[ProtoInclude(347, typeof(ConsoleMessage))]
[ProtoInclude(348, typeof(QuestTriggerMessage))]
[ProtoInclude(349, typeof(QuestStatsMessage))]
[ProtoInclude(350, typeof(SkipQuestMessage))]
[ProtoInclude(351, typeof(MiningPointStatsMessage))]
[ProtoInclude(352, typeof(UpdateBlueprintsMessage))]
[ProtoInclude(354, typeof(UpdateVesselDataMessage))]
[ProtoInclude(406, typeof(DeleteCharacterRequest))]
[ProtoInclude(506, typeof(DeleteCharacterResponse))]
// OpenHellion messages below this line. (100x, state inform; 101x, movement; 102x, map; 103x, status messages)
[ProtoInclude(1000, typeof(ObjectsInfoRequest))]
[ProtoInclude(1001, typeof(ObjectsInfoResponse))]
[ProtoInclude(1002, typeof(StateUpdateMessage))]
[ProtoInclude(1003, typeof(StateUpdateRequest))]
[ProtoInclude(1010, typeof(MovementMessage))]
[ProtoInclude(1011, typeof(MoveObjectRequest))]
[ProtoInclude(1012, typeof(CharacterAnimationMessage))]
[ProtoInclude(1013, typeof(ChangeParentMessage))]
[ProtoInclude(1014, typeof(ShipThrustMessage))]
[ProtoInclude(1015, typeof(ShipThrustStateMessage))]
[ProtoInclude(1020, typeof(MapDataRequest))]
[ProtoInclude(1021, typeof(MapDataResponse))]
[ProtoInclude(1022, typeof(ScanForObjectsRequest))]
[ProtoInclude(1030, typeof(ServerStatusRequest))]
[ProtoInclude(1031, typeof(ServerStatusResponse))]
[ProtoInclude(1032, typeof(JoinInfoRequest))]
[ProtoInclude(1033, typeof(JoinInfoResponse))]
public abstract class NetworkData
{
		public enum MessageStatus : byte
		{
			Normal = 0,
			Success = 1,
			Failure = 2,
			Removed = 7,
			Shutdown = 8,
			Heartbeat = 9,
			Timeout = 10,
		}

		public long Sender;

		public MessageStatus Status;

		public Guid ConversationGuid { get; set; } = Guid.NewGuid();

		public bool SyncRequest;

		public bool SyncResponse;

		public DateTime ExpirationUtc;

	public override string ToString()
	{
		return JsonSerialiser.Serialize(this);
	}
}
