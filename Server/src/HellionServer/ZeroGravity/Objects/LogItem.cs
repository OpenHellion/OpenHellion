using OpenHellion.State;
using System;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;

namespace ZeroGravity.Objects;

internal class LogItem : Item
{
	public int LogID;

	public override async Task SetData(DynamicObjectAuxData data)
	{
		await base.SetData(data);
		LogItemData i = data as LogItemData;
		int tmpInt = i.logID;
		if (tmpInt == -1)
		{
			tmpInt = MathHelper.RandomRange(0, Enum.GetValues(typeof(LogItemTypes)).Length);
		}
		LogID = tmpInt;
	}

	public override DynamicObjectStats NewStats()
	{
		return new LogItemStats();
	}

	public override void FillStats(DynamicObjectStats stats, ItemChanges fields)
	{
		base.FillStats(stats, fields);
		if (fields == ItemChanges.All)
		{
			((LogItemStats)stats).LogID = LogID;
		}
	}

	public override PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataLogItem data = new PersistenceObjectDataLogItem();
		FillPersistenceData(data);
		data.LogItemData = new LogItemData();
		FillBaseAuxData(data.LogItemData);
		data.LogItemData.logID = LogID;
		return data;
	}

	public override async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await base.LoadPersistenceData(persistenceData);
		if (persistenceData is not PersistenceObjectDataLogItem data)
		{
			Debug.LogWarning("PersistenceObjectDataLogItem data is null", GUID);
		}
		else
		{
			await SetData(data.LogItemData);
		}
	}
}
