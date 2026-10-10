using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;

namespace ZeroGravity.Objects;

public class DisposableHackingTool : Item
{
	public override async Task SetData(DynamicObjectAuxData data)
	{
		await base.SetData(data);
		ApplyTierMultiplier();
	}

	public Task Use()
	{
		return TakeDamage(TypeOfDamage.None, 1f);
	}

	public override PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataHackingTool data = new PersistenceObjectDataHackingTool();
		FillPersistenceData(data);
		data.HackingToolData = new DisposableHackingToolData();
		FillBaseAuxData(data.HackingToolData);
		return data;
	}

	public override async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await base.LoadPersistenceData(persistenceData);
		if (persistenceData is not PersistenceObjectDataHackingTool data)
		{
			Debug.LogWarning("PersistenceObjectDataHackingTool data is null", GUID);
		}
		else
		{
			await SetData(data.HackingToolData);
			ApplyTierMultiplier();
		}
	}

	public override void ApplyTierMultiplier()
	{
		if (!TierMultiplierApplied)
		{
			MaxHealth *= TierMultiplier;
			Health *= TierMultiplier;
		}
		base.ApplyTierMultiplier();
	}
}
