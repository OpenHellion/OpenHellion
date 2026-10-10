using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;

namespace ZeroGravity.Objects;

public class HandheldAsteroidScanner : Item
{
	private int penetrationLevel;

	public override async Task SetData(DynamicObjectAuxData data)
	{
		await base.SetData(data);
		HandheldAsteroidScannerData hasd = data as HandheldAsteroidScannerData;
		penetrationLevel = hasd.penetrationLevel;
	}

	public void UpdateResources(double dbl)
	{
	}

	public override PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataHandheldAsteroidScanner data = new PersistenceObjectDataHandheldAsteroidScanner();
		FillPersistenceData(data);
		data.ScannerData = new HandheldAsteroidScannerData();
		FillBaseAuxData(data.ScannerData);
		data.ScannerData.penetrationLevel = penetrationLevel;
		return data;
	}

	public override async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await base.LoadPersistenceData(persistenceData);
		if (persistenceData is not PersistenceObjectDataHandheldAsteroidScanner data)
		{
			Debug.LogWarning("PersistenceObjectDataHandheldAsteroidScanner data is null", GUID);
		}
		else
		{
			await SetData(data.ScannerData);
		}
	}

}
