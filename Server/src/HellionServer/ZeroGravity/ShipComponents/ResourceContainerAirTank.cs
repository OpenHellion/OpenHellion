using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class ResourceContainerAirTank : ResourceContainer
{
	public float AirQuality = 1f;

	public ResourceContainerAirTank(SpaceObjectVessel vessel, VesselObjectID id, ResourceContainerData rcData)
		: base(vessel, id, rcData)
	{
	}

	public override PersistenceObjectData GetPersistenceData()
	{
		return new PersistenceObjectDataCargo
		{
			InSceneID = ID.InSceneID,
			CargoCompartments = Compartments,
			AuxData = new PersistenceObjectAuxDataAirTank
			{
				AirQuality = AirQuality
			}
		};
	}

	public override async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await base.LoadPersistenceData(persistenceData);
		AirQuality = ((persistenceData as PersistenceObjectDataCargo).AuxData as PersistenceObjectAuxDataAirTank).AirQuality;
	}

	public override IAuxDetails GetAuxDetails()
	{
		return new AirTankAuxDetails
		{
			AirQuality = Compartments[0].Resources[0].Quantity > float.Epsilon ? AirQuality : 0f
		};
	}
}
