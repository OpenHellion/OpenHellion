using OpenHellion.State;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;

namespace ZeroGravity.Objects;

public class Medpack : Item
{
	public float RegenRate;

	public float MaxHp;

	public override async Task SetData(DynamicObjectAuxData data)
	{
		await base.SetData(data);
		MedpackData md = data as MedpackData;
		RegenRate = md.RegenRate;
		MaxHp = md.MaxHP;
	}

	public override PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataMedpack data = new PersistenceObjectDataMedpack();
		FillPersistenceData(data);
		data.MedpackData = new MedpackData();
		FillBaseAuxData(data.MedpackData);
		data.MedpackData.MaxHP = MaxHp;
		data.MedpackData.RegenRate = RegenRate;
		return data;
	}

	public override async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await base.LoadPersistenceData(persistenceData);
		if (persistenceData is not PersistenceObjectDataMedpack data)
		{
			Debug.LogWarning("PersistenceObjectDataMedpack data is null", GUID);
		}
		else
		{
			await SetData(data.MedpackData);
		}
	}
}
