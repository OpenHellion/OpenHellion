using OpenHellion.State;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenHellion.Net;
using ZeroGravity.Data;
using ZeroGravity.Network;

namespace ZeroGravity.Objects;

internal class PortableTurret : Item
{
	public bool IsActive
	{
		get => State.Active(Row);
		set => State.SetActive(Row, value);
	}

	public float Damage;

	private Player targetPlayer;

	public bool isStunned
	{
		get => State.Stunned(Row);
		set => State.SetStunned(Row, value);
	}

	private double unstunTime;

	public PortableTurret()
	{
		EventSystem.AddListener<PortableTurretShootingMessage>(PortableTurretShootingMessageListener);
	}

	public override PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataPortableTurret data = new PersistenceObjectDataPortableTurret();
		FillPersistenceData(data);
		data.PortableTurretData = new PortableTurretData();
		FillBaseAuxData(data.PortableTurretData);
		data.PortableTurretData.IsActive = IsActive;
		data.PortableTurretData.Damage = Damage;
		return data;
	}

	public override async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await base.LoadPersistenceData(persistenceData);
		if (persistenceData is not PersistenceObjectDataPortableTurret data)
		{
			Debug.LogWarning("PersistenceObjectDataPortableTurret data is null", GUID);
		}
		else
		{
			await SetData(data.PortableTurretData);
			ApplyTierMultiplier();
		}
	}

	public override async Task SetData(DynamicObjectAuxData data)
	{
		await base.SetData(data);
		PortableTurretData ptd = data as PortableTurretData;
		IsActive = ptd.IsActive;
		Damage = ptd.Damage;
		ApplyTierMultiplier();
	}

	private async void PortableTurretShootingMessageListener(NetworkData data)
	{
		var message = data as PortableTurretShootingMessage;
		if (message.TurretGUID == GUID && !isStunned)
		{
			targetPlayer = Server.Instance.GetPlayer(message.Sender);
			await NetworkController.SendToClientsSubscribedTo(message, -1L, targetPlayer.Parent);
			if (message.IsShooting)
			{
				Server.Instance.SubscribeToTimer(UpdateTimer.TimerStep.Step_0_1_sec, DamagePlayer);
			}
			else
			{
				Server.Instance.UnsubscribeFromTimer(UpdateTimer.TimerStep.Step_0_1_sec, DamagePlayer);
			}
		}
	}

	private Task CheckStun(double deltaTime)
	{
		if (!State.IsAlive(Row) || Server.SolarSystemTime >= unstunTime)
		{
			Server.Instance.UnsubscribeFromTimer(UpdateTimer.TimerStep.Step_0_1_sec, CheckStun);
			if (State.IsAlive(Row))
			{
				isStunned = false;
			}
		}
		return Task.CompletedTask;
	}

	public override async Task TakeDamage(Dictionary<TypeOfDamage, float> damages, bool forceTakeDamage = false)
	{
		await base.TakeDamage(damages, forceTakeDamage);
		if (damages.ContainsKey(TypeOfDamage.EMP))
		{
			if (!isStunned)
			{
				Server.Instance.SubscribeToTimer(UpdateTimer.TimerStep.Step_0_1_sec, CheckStun);
			}
			isStunned = true;
			unstunTime = Server.SolarSystemTime + 10.0;
		}
	}

	public override DynamicObjectStats NewStats()
	{
		return new PortableTurretStats();
	}

	public override void FillStats(DynamicObjectStats stats, ItemChanges fields)
	{
		base.FillStats(stats, fields);
		PortableTurretStats turret = (PortableTurretStats)stats;
		if ((fields & ItemChanges.Active) != 0)
		{
			turret.IsActive = IsActive;
		}
		if ((fields & ItemChanges.Stunned) != 0)
		{
			turret.IsStunned = isStunned;
		}
	}

	private async Task DamagePlayer(double deltaTime)
	{
		if (targetPlayer is { IsAlive: true })
		{
			await targetPlayer.TakeHitDamage(Damage * TierMultiplier * (float)deltaTime, Player.HitBoxType.Torso, isMelee: false, null, (float)deltaTime, "turret");
		}
		else
		{
			Server.Instance.UnsubscribeFromTimer(UpdateTimer.TimerStep.Step_0_1_sec, DamagePlayer);
		}
	}

	public override void ApplyTierMultiplier()
	{
		if (!TierMultiplierApplied)
		{
			Armor = AuxValue;
		}
		base.ApplyTierMultiplier();
	}

	~PortableTurret()
	{
		EventSystem.RemoveListener<PortableTurretShootingMessage>(PortableTurretShootingMessageListener);
	}
}
