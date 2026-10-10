using ZeroGravity.ShipComponents;

namespace ZeroGravity.Data;

public class ResourceContainerData : ISceneData
{
	public int InSceneID;

	public DistributionSystemType DistributionSystemType;

	public CargoCompartmentData CargoCompartment;

	public float NominalInput;

	public float NominalOutput;

	public bool IsInUse;

	public SystemAuxData AuxData;
}
