using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class ResourceContainer : IResourceProvider, IResourceUser, ICargo, IPersistantObject
{
	public VesselObjectID ID;

	private float _QuantityChangeRate;

	private bool _IsInUse = true;

	private DistributionSystemType _OutputType;

	private HashSet<IResourceUser> _ConnectedConsumers = new HashSet<IResourceUser>();

	private Dictionary<DistributionSystemType, SortedSet<IResourceProvider>> _ConnectedProviders = new Dictionary<DistributionSystemType, SortedSet<IResourceProvider>>();

	private float _Output;

	private float _NominalOutput;

	private float _OutputRate = 1f;

	public float NominalInput;

	public bool StatusChanged = true;

	public SpaceObjectVessel ParentVessel;

	private List<CargoCompartmentData> _Compartments = new List<CargoCompartmentData>();

	public List<CargoCompartmentData> Compartments => _Compartments;

	public float QuantityChangeRate
	{
		get
		{
			return _QuantityChangeRate;
		}
		set
		{
			if (_QuantityChangeRate != value)
			{
				StatusChanged = true;
				_QuantityChangeRate = value;
			}
		}
	}

	public bool IsInUse
	{
		get
		{
			return _IsInUse;
		}
		set
		{
			if (_IsInUse != value)
			{
				StatusChanged = true;
				_IsInUse = value;
			}
		}
	}

	public HashSet<IResourceUser> ConnectedConsumers => _ConnectedConsumers;

	public Dictionary<DistributionSystemType, SortedSet<IResourceProvider>> ConnectedProviders => _ConnectedProviders;

	public float NominalOutput
	{
		get
		{
			return _NominalOutput;
		}
		set
		{
			if (_NominalOutput != value)
			{
				StatusChanged = true;
			}
			_NominalOutput = value;
		}
	}

	public float Output
	{
		get
		{
			return _Output;
		}
		set
		{
			if (_Output != value)
			{
				StatusChanged = true;
			}
			_Output = value;
		}
	}

	public DistributionSystemType OutputType
	{
		get
		{
			return _OutputType;
		}
		set
		{
			_OutputType = value;
		}
	}

	public float MaxOutput => _NominalOutput;

	public float OperationRate
	{
		get
		{
			return _OutputRate;
		}
		set
		{
			if (_OutputRate != value)
			{
				StatusChanged = true;
			}
			_OutputRate = value;
		}
	}

	public ResourceContainer(SpaceObjectVessel vessel, VesselObjectID id, ResourceContainerData rcData)
	{
		ResourceContainerData data = ObjectCopier.DeepCopy(rcData);
		ParentVessel = vessel;
		ID = id;
		OutputType = data.DistributionSystemType;
		CargoCompartmentData compartment = data.CargoCompartment;
		foreach (CargoResourceData resource in compartment.Resources)
		{
			ResourcesSpawnSettings[] spawnSettings = resource.SpawnSettings;
			foreach (ResourcesSpawnSettings rss in spawnSettings)
			{
				if (vessel.CheckTag(rss.Tag, rss.Case))
				{
					float qty = MathHelper.RandomRange(rss.MinQuantity, rss.MaxQuantity);
					resource.Quantity = 0f;
					float avail = compartment.Capacity - compartment.Resources.Sum((CargoResourceData m) => m.Quantity);
					resource.Quantity = MathHelper.Clamp(qty, 0f, avail);
					break;
				}
			}
		}
		_Compartments = new List<CargoCompartmentData> { compartment };
		NominalInput = data.NominalInput;
		NominalOutput = data.NominalOutput;
		IsInUse = data.IsInUse;
	}

	public async Task<float> ConsumeResource(float consumeQuantity)
	{
		if (!IsInUse)
		{
			return 0f;
		}
		return await ChangeQuantityByAsync(_Compartments[0].ID, _Compartments[0].Resources[0].ResourceType, 0f - consumeQuantity);
	}

	public CargoCompartmentData GetCompartment(int? id = null)
	{
		if (id.HasValue)
		{
			return _Compartments.Find((CargoCompartmentData m) => m.ID == id.Value);
		}
		return _Compartments[0];
	}

	public Task<float> ChangeQuantityByAsync(int compartmentID, ResourceType resourceType, float quantity, bool wholeAmount = false)
	{
		CargoCompartmentData compartment = Compartments.Find((CargoCompartmentData m) => m.ID == compartmentID);
		CargoResourceData res = compartment.Resources.Find((CargoResourceData m) => m.ResourceType == resourceType);
		if (res == null)
		{
			res = new CargoResourceData
			{
				ResourceType = resourceType,
				Quantity = 0f
			};
			compartment.Resources.Add(res);
		}
		float available = res.Quantity;
		float qty = quantity;
		float free = compartment.Capacity - res.Quantity;
		if (quantity > 0f && quantity > free)
		{
			qty = free;
		}
		else if (quantity < 0f && 0f - qty > available)
		{
			qty = 0f - available;
		}
		res.Quantity = available + qty;
		if (res.Quantity <= 0.01 && compartment.AllowOnlyOneType && compartment.AllowedResources.Count > 1)
		{
			compartment.Resources.Remove(res);
		}
		StatusChanged = true;
		return Task.FromResult(qty);
	}

	public virtual PersistenceObjectData GetPersistenceData()
	{
		return new PersistenceObjectDataCargo
		{
			InSceneID = ID.InSceneID,
			CargoCompartments = _Compartments
		};
	}

	public virtual Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		_Compartments = (persistenceData as PersistenceObjectDataCargo).CargoCompartments;
		foreach (CargoCompartmentData ccd in _Compartments)
		{
			if (ccd.AllowOnlyOneType && ccd.AllowedResources.Count > 1)
			{
				ccd.Resources.RemoveAll((CargoResourceData m) => m.Quantity <= 0.01);
			}
		}

		return Task.CompletedTask;
	}

	public virtual ResourceContainerDetails GetDetails()
	{
		return new ResourceContainerDetails
		{
			InSceneID = ID.InSceneID,
			Resources = new List<CargoResourceData>(GetCompartment().Resources),
			QuantityChangeRate = QuantityChangeRate,
			Output = Output,
			OutputRate = OperationRate,
			IsInUse = IsInUse,
			AuxDetails = GetAuxDetails()
		};
	}

	public virtual IAuxDetails GetAuxDetails()
	{
		return null;
	}
}
