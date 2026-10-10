using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Spawn;

namespace ZeroGravity.Objects;

public class QuestTrigger
{
	public class QuestTriggerID
	{
		public long PlayerGUID;

		public uint QuestID;

		public uint ID;

		public override bool Equals(object obj)
		{
			return this == obj as QuestTriggerID;
		}

		public override int GetHashCode()
		{
			return new object[(int)checked((nint)PlayerGUID), QuestID, ID].GetHashCode();
		}

		public static bool operator ==(QuestTriggerID x, QuestTriggerID y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}
			if (x is null || y is null)
			{
				return false;
			}
			return x.PlayerGUID == y.PlayerGUID && x.QuestID == y.QuestID && x.ID == y.ID;
		}

		public static bool operator !=(QuestTriggerID x, QuestTriggerID y)
		{
			return !(x == y);
		}
	}

	public uint ID;

	public uint BatchID;

	public QuestTriggerType Type;

	public string Station;

	public string Tag;

	public CelestialBodyGUID Celestial = CelestialBodyGUID.None;

	public uint DependencyBatchID;

	public QuestTriggerDependencyTpe DependencyTpe;

	public string SpawnRuleName;

	public Quest Quest;

	private QuestStatus _Status;

	public QuestStatus Status
	{
		get
		{
			return _Status;
		}
	}

	private QuestTrigger()
	{
	}

	public static async Task<QuestTrigger> CreateQuestTriggerAsync(Quest quest, QuestTriggerData data)
	{
		var questTrigger = new QuestTrigger
		{
			ID = data.ID,
			BatchID = data.BatchID,
			Type = data.Type,
			Station = data.Station,
			Tag = data.Tag,
			Celestial = data.Celestial,
			DependencyBatchID = data.DependencyBatchID,
			DependencyTpe = data.DependencyTpe,
			SpawnRuleName = data.SpawnRuleName,
			Quest = quest
		};
		if (questTrigger.Type == QuestTriggerType.Activate)
		{
			await questTrigger.SetQuestStatusAsync(QuestStatus.Active);
		}
		return questTrigger;
	}

	public static async Task<List<QuestTrigger>> CreateQuestTriggersAsync(Quest quest, List<QuestTriggerData> data)
	{
		List<QuestTrigger> triggers = [];
		foreach (QuestTriggerData element in data)
		{
			QuestTrigger trigger = new QuestTrigger
			{
				ID = element.ID,
				BatchID = element.BatchID,
				Type = element.Type,
				Station = element.Station,
				Tag = element.Tag,
				Celestial = element.Celestial,
				DependencyBatchID = element.DependencyBatchID,
				DependencyTpe = element.DependencyTpe,
				SpawnRuleName = element.SpawnRuleName,
				Quest = quest
			};

			triggers.Add(trigger);

			if (trigger.Type == QuestTriggerType.Activate)
			{
				await trigger.SetQuestStatusAsync(QuestStatus.Active);
			}
		}

		return triggers;
	}

	public async Task SetQuestStatusAsync(QuestStatus status)
	{
		if (_Status != status)
		{
			_Status = status;
			if (_Status == QuestStatus.Active && !SpawnRuleName.IsNullOrEmpty())
			{
				await SpawnManager.SpawnQuestSetup(this);
			}
		}
	}

	public QuestTriggerDetails GetDetails()
	{
		long locGUID = SpawnManager.GetStationMainVesselGUID(Station);
		return new QuestTriggerDetails
		{
			ID = ID,
			Status = Status,
			StationMainVesselGUID = locGUID
		};
	}

	public async Task UpdateDependentTriggers(Quest quest)
	{
		if (BatchID == 0)
		{
			return;
		}
		List<QuestTrigger> batch = quest.QuestTriggers.Where((QuestTrigger m) => m.BatchID == BatchID).ToList();
		int completedCount = batch.Count((QuestTrigger m) => m.Status == QuestStatus.Completed);
		bool fixIncomplete = false;
		foreach (QuestTrigger qt2 in quest.QuestTriggers.Where((QuestTrigger m) => m.DependencyBatchID == BatchID))
		{
			if (qt2.DependencyTpe == QuestTriggerDependencyTpe.Any)
			{
				fixIncomplete = true;
			}
			if ((qt2.DependencyTpe == QuestTriggerDependencyTpe.Any && completedCount > 0) || (qt2.DependencyTpe == QuestTriggerDependencyTpe.All && completedCount == batch.Count))
			{
				await qt2.SetQuestStatusAsync(QuestStatus.Active);
			}
		}
		if (!fixIncomplete)
		{
			return;
		}
		foreach (QuestTrigger qt in batch)
		{
			await qt.SetQuestStatusAsync(QuestStatus.Completed);
		}
	}

	public QuestTriggerID GetQuestTriggerID()
	{
		return new QuestTriggerID
		{
			PlayerGUID = Quest.Player.Guid,
			QuestID = Quest.ID,
			ID = ID
		};
	}
}
