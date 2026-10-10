using OpenHellion.State;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;

namespace ZeroGravity.Objects;

public class Magazine : Item
{
	public int BulletCount => State.Bullets(Row);

	public int MaxBulletCount { get; private set; }

	public bool HasAmmo => BulletCount > 0;

	public override async Task SetData(DynamicObjectAuxData data)
	{
		await base.SetData(data);
		MagazineData md = data as MagazineData;
		State.SetBullets(Row, md.BulletCount);
		MaxBulletCount = md.MaxBulletCount;
	}

	public async Task ChangeQuantity(int amount)
	{
		State.SetBullets(Row, BulletCount + amount);
		if (BulletCount <= 0)
		{
			await DestroyItem();
		}
	}

	public override DynamicObjectStats NewStats()
	{
		return new MagazineStats();
	}

	public override void FillStats(DynamicObjectStats stats, ItemChanges fields)
	{
		base.FillStats(stats, fields);
		if ((fields & ItemChanges.Bullets) != 0)
		{
			((MagazineStats)stats).BulletCount = BulletCount;
		}
	}

	public override PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataMagazine data = new PersistenceObjectDataMagazine();
		FillPersistenceData(data);
		data.MagazineData = new MagazineData();
		FillBaseAuxData(data.MagazineData);
		data.MagazineData.BulletCount = BulletCount;
		data.MagazineData.MaxBulletCount = MaxBulletCount;
		return data;
	}

	public override async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await base.LoadPersistenceData(persistenceData);
		if (persistenceData is not PersistenceObjectDataMagazine data)
		{
			Debug.LogWarning("PersistenceObjectDataMagazine data is null", GUID);
		}
		else
		{
			await SetData(data.MagazineData);
		}
	}
}
