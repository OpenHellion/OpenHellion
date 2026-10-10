using OpenHellion.State;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;

namespace ZeroGravity.Objects;

public class GenericItem : Item
{
	public GenericItemSubType SubType;

	public string Look;

	public override async Task SetData(DynamicObjectAuxData data)
	{
		await base.SetData(data);
		GenericItemData i = data as GenericItemData;
		SubType = i.SubType;
		Look = i.Look;
	}

	public override DynamicObjectStats NewStats()
	{
		return new GenericItemStats();
	}

	public override void FillStats(DynamicObjectStats stats, ItemChanges fields)
	{
		base.FillStats(stats, fields);
		if (fields == ItemChanges.All)
		{
			((GenericItemStats)stats).Look = Look;
		}
	}

	public override PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataGenericItem data = new PersistenceObjectDataGenericItem();
		FillPersistenceData(data);
		data.GenericData = new GenericItemData();
		FillBaseAuxData(data.GenericData);
		data.GenericData.SubType = SubType;
		data.GenericData.Look = Look;
		return data;
	}

	public override async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await base.LoadPersistenceData(persistenceData);
		if (persistenceData is not PersistenceObjectDataGenericItem data)
		{
			Debug.LogWarning("PersistenceObjectDataGenericItem data is null", GUID);
		}
		else
		{
			await SetData(data.GenericData);
		}
	}
}
