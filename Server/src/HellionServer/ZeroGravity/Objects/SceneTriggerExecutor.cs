using System.Collections.Generic;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;

namespace ZeroGravity.Objects;

public class SceneTriggerExecutor : IPersistantObject
{
	public int InSceneID;

	public Ship ParentShip;

	public int DefaultStateID;

	private int _stateID;

	public Dictionary<int, SceneTriggerExecutorState> States = new Dictionary<int, SceneTriggerExecutorState>();

	public long PlayerThatActivated;

	public ShipSpawnPoint SpawnPoint;

	public SceneTriggerExecutor Child;

	public SceneTriggerExecutor Parent;

	public Dictionary<int, SceneTriggerProximity> ProximityTriggers;

	public int StateID
	{
		get
		{
			return _stateID;
		}
		set
		{
			if (States.ContainsKey(value))
			{
				_stateID = value;
				if (Child != null)
				{
					Child._stateID = value;
				}
				else if (Parent != null)
				{
					Parent._stateID = value;
				}
			}
		}
	}

	public bool IsMerged => Child != null || Parent != null;

	public void MergeWith(SceneTriggerExecutor child)
	{
		if (child == null)
		{
			if (Child != null)
			{
				Child.Parent = null;
				Child = null;
			}
			if (Parent != null)
			{
				Parent.Child = null;
				Parent = null;
			}
		}
		else
		{
			Child = child;
			Child.Parent = this;
			Child.StateID = StateID;
		}
	}

	/// <summary>
	/// 	Populates the executor with states and sets the default state.
	/// </summary>
	/// <param name="states">A list of states to populate.</param>
	/// <param name="defaultStateID">StateID of the default state.</param>
	public void SetStates(List<SceneTriggerExecutorStateData> states, int defaultStateID)
	{
		foreach (SceneTriggerExecutorStateData st in states)
		{
			States.Add(st.StateID, new SceneTriggerExecutorState
			{
				StateID = st.StateID,
				PlayerDisconnectToStateID = st.PlayerDisconnectToStateID,
				PlayerDisconnectToStateImmediate = st.PlayerDisconnectToStateImmediate
			});
		}
		if (States.ContainsKey(defaultStateID))
		{
			DefaultStateID = defaultStateID;
			StateID = defaultStateID;
		}
	}

	/// <summary>
	/// 	Changes this executor's state if provided data is valid.
	/// </summary>
	/// <param name="sender">The GUID of the player that activated the trigger.</param>
	/// <param name="data">Data to fill the new state.</param>
	public SceneTriggerExecutorDetails ChangeState(long sender, SceneTriggerExecutorDetails data)
	{
		Player pl = null;
		try
		{
			pl = Server.Instance.GetPlayer(sender);
			data.PlayerThatActivated = pl.FakeGuid;
		}
		catch
		{
			data.PlayerThatActivated = 0L;
		}

		if (data.ProximityTriggerID.HasValue)
		{
			if (ProximityTriggers == null || !ProximityTriggers.TryGetValue(data.ProximityTriggerID.Value, out SceneTriggerProximity value))
			{
				data.IsFail = true;
				Debug.LogWarning($"SceneTriggerExecutor.ChangeState failed for player {sender} in scene {InSceneID} with state {StateID} to new state {data.NewStateID}. Proximity trigger not found.");
				return data;
			}
			bool containsGUID = value.ObjectsInTrigger.Contains(sender);
			if (data.ProximityIsEnter.Value)
			{
				if (!containsGUID)
				{
					value.ObjectsInTrigger.Add(sender);
				}
				if (value.ObjectsInTrigger.Count == 0)
				{
					data.IsFail = true;
				}
			}
			else
			{
				if (containsGUID)
				{
					value.ObjectsInTrigger.Remove(sender);
				}
				if (value.ObjectsInTrigger.Count > 0)
				{
					data.IsFail = true;
				}
			}
		}

		if (StateID == data.NewStateID && (!data.IsImmediate.HasValue || !data.IsImmediate.Value))
		{
			data.IsFail = true;
		}
		if (!States.ContainsKey(data.NewStateID))
		{
			data.IsFail = true;
		}
		if (SpawnPoint != null && ((SpawnPoint.Type == SpawnPointType.WithAuthorization && SpawnPoint.State == SpawnPointState.Locked) || (SpawnPoint.Player != null && SpawnPoint.Player != pl) || SpawnPoint.Player == null))
		{
			data.IsFail = true;
		}

		if (!data.IsFail)
		{
			StateID = data.NewStateID;
			if (SpawnPoint is { Player: not null })
			{
				if (StateID == SpawnPoint.ExecutorStateID || (SpawnPoint.ExecutorOccupiedStateIDs != null && SpawnPoint.ExecutorOccupiedStateIDs.Contains(StateID)))
				{
					SpawnPoint.Player.IsInsideSpawnPoint = true;
					SpawnPoint.IsPlayerInSpawnPoint = true;
				}
				else
				{
					SpawnPoint.Player.IsInsideSpawnPoint = false;
					SpawnPoint.IsPlayerInSpawnPoint = false;
					if (SpawnPoint.Type == SpawnPointType.SimpleSpawn)
					{
						SpawnPoint.Player.SetSpawnPoint(null);
						SpawnPoint.Player = null;
					}
				}
			}
			try
			{
				PlayerThatActivated = Server.Instance.GetPlayer(sender).FakeGuid;
			}
			catch
			{
				PlayerThatActivated = 0L;
			}
		}
		else
		{
			Debug.LogWarning($"SceneTriggerExecutor.ChangeState failed for player {sender} in scene {InSceneID} with state {StateID} to new state {data.NewStateID}. IsFail: {data.IsFail}");
		}

		return data;
	}

	public SceneTriggerExecutorDetails RemovePlayerFromExecutor(Player pl)
	{
		if (PlayerThatActivated != pl.FakeGuid || !States.TryGetValue(_stateID, out SceneTriggerExecutorState value) || value.PlayerDisconnectToStateID == 0 || !States.ContainsKey(value.PlayerDisconnectToStateID))
		{
			return null;
		}
		return new SceneTriggerExecutorDetails
		{
			InSceneID = InSceneID,
			IsFail = false,
			CurrentStateID = StateID,
			NewStateID = value.PlayerDisconnectToStateID,
			IsImmediate = value.PlayerDisconnectToStateImmediate,
			PlayerThatActivated = 0L
		};
	}

	public SceneTriggerExecutorDetails RemovePlayerFromProximity(Player pl)
	{
		if (ProximityTriggers == null || ProximityTriggers.Count == 0)
		{
			return null;
		}
		SceneTriggerProximity proxEmptied = null;
		bool hasProxWithPlayer = false;
		foreach (KeyValuePair<int, SceneTriggerProximity> prox in ProximityTriggers)
		{
			if (prox.Value.ObjectsInTrigger.Contains(pl.Guid))
			{
				prox.Value.ObjectsInTrigger.Remove(pl.Guid);
				if (prox.Value.ObjectsInTrigger.Count == 0)
				{
					proxEmptied = prox.Value;
				}
			}
			if (prox.Value.ObjectsInTrigger.Count > 0)
			{
				hasProxWithPlayer = true;
			}
		}
		if (hasProxWithPlayer || proxEmptied == null || StateID != proxEmptied.ActiveStateID)
		{
			return null;
		}
		return new SceneTriggerExecutorDetails
		{
			InSceneID = InSceneID,
			IsFail = false,
			CurrentStateID = StateID,
			NewStateID = proxEmptied.InactiveStateID,
			PlayerThatActivated = 0L,
			ProximityTriggerID = proxEmptied.TriggerID,
			ProximityIsEnter = false
		};
	}

	public bool AreStatesEqual(SceneTriggerExecutor other)
	{
		return States.Count == other.States.Count;
	}

	public PersistenceObjectData GetPersistenceData()
	{
		return new PersistenceObjectDataExecutor
		{
			InSceneID = InSceneID,
			StateID = StateID,
			PlayerThatActivated = PlayerThatActivated
		};
	}

	public Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		if (persistenceData is not PersistenceObjectDataExecutor data)
		{
			Debug.LogError("PersistenceObjectDataExecutor data is null");
			return Task.CompletedTask;
		}
		StateID = data.StateID;
		PlayerThatActivated = data.PlayerThatActivated;

		return Task.CompletedTask;
	}
}
