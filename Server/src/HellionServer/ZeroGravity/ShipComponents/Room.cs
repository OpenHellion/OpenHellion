using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class Room : IPersistantObject, IResourceUser
{
	public VesselObjectID ID;

	private float _AirPressure = 1f;

	private float _AirQuality = 1f;

	private float _AirPressureChangeRate;

	private float _AirQualityChangeRate;

	private float _Temperature = 25f;

	private bool _UseGravity;

	private bool _AirFiltering = true;

	private bool _GravityMalfunction;

	public bool GravityAutoToggle;

	public SpaceObjectVessel ParentVessel;

	[JsonIgnore]
	public HashSet<Room> LinkedRooms = new HashSet<Room>();

	public HashSet<Door> Doors = new HashSet<Door>();

	[JsonIgnore]
	public List<VesselComponent> VesselComponents = new List<VesselComponent>();

	[JsonIgnore]
	public List<ILifeSupportDevice> LifeSupportDevices = new List<ILifeSupportDevice>();

	public float Volume;

	public bool StatusChanged = true;

	[JsonIgnore]
	public DistributionManager.CompoundRoom CompoundRoom = null;

	public float PressurizeSpeed;

	public float DepressurizeSpeed;

	public float VentSpeed;

	private Dictionary<DistributionSystemType, SortedSet<IResourceProvider>> _ConnectedProviders = new Dictionary<DistributionSystemType, SortedSet<IResourceProvider>>();

	public Room EquilizePressureRoom;

	public float? TargetPressure;

	public List<IAirConsumer> AirConsumers = new List<IAirConsumer>();

	public float Breathability => MathHelper.Clamp(AirQuality * AirPressure / 0.2f, 0f, 1f);

	public bool FireCanBurn => AirQuality * AirPressure < 0.25f;

	public bool GravityMalfunction
	{
		get
		{
			return _GravityMalfunction;
		}
		set
		{
			if (_GravityMalfunction != (_GravityMalfunction = value))
			{
				StatusChanged = true;
			}
		}
	}

	public float AirQuality
	{
		get
		{
			return _AirQuality;
		}
		set
		{
			float newValue = !float.IsNaN(value) ? MathHelper.Clamp(value, 0f, 1f) : 0f;
			if (_AirQuality != newValue)
			{
				StatusChanged = true;
				_AirQuality = newValue;
			}
		}
	}

	public float AirPressure
	{
		get
		{
			return _AirPressure;
		}
		set
		{
			float newValue = !float.IsNaN(value) ? MathHelper.Clamp(value, 0f, 1f) : 0f;
			if (_AirPressure != newValue)
			{
				StatusChanged = true;
				_AirPressure = newValue;
			}
		}
	}

	public float AirQualityChangeRate
	{
		get
		{
			return _AirQualityChangeRate;
		}
		set
		{
			if (_AirQualityChangeRate != value)
			{
				StatusChanged = true;
				_AirQualityChangeRate = value;
			}
		}
	}

	public float AirPressureChangeRate
	{
		get
		{
			return _AirPressureChangeRate;
		}
		set
		{
			if (_AirPressureChangeRate != value)
			{
				StatusChanged = true;
				_AirPressureChangeRate = value;
			}
		}
	}

	public bool UseGravity
	{
		get
		{
			return _UseGravity;
		}
		set
		{
			if (value && GravityAutoToggle && HasExternalDoor)
			{
				value = false;
			}
			if (_UseGravity != value)
			{
				StatusChanged = true;
				_UseGravity = value;
			}
		}
	}

	public bool AirFiltering
	{
		get
		{
			return _AirFiltering;
		}
		set
		{
			if (_AirFiltering != value)
			{
				StatusChanged = true;
				_AirFiltering = value;
			}
		}
	}

	public float Temperature
	{
		get
		{
			return _Temperature;
		}
		set
		{
			if (_Temperature != value)
			{
				StatusChanged = true;
				_Temperature = value;
			}
		}
	}

	public bool HasExternalDoor
	{
		get
		{
			foreach (Door door in Doors)
			{
				if (door.isExternal)
				{
					return true;
				}
			}
			return false;
		}
	}

	public Dictionary<DistributionSystemType, SortedSet<IResourceProvider>> ConnectedProviders => _ConnectedProviders;

	public PersistenceObjectData GetPersistenceData()
	{
		return new PersistenceObjectDataRoom
		{
			GUID = ParentVessel.Guid,
			InSceneID = ID.InSceneID,
			AirPressure = AirPressure,
			AirQuality = AirQuality,
			AirPressureChangeRate = AirPressureChangeRate,
			AirQualityChangeRate = AirQualityChangeRate,
			Temperature = Temperature,
			UseGravity = UseGravity,
			GravityMalfunction = GravityMalfunction,
			AirFiltering = AirFiltering
		};
	}

	public Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		if (persistenceData is not PersistenceObjectDataRoom data)
		{
			Debug.LogWarning("PersistenceObjectDataRoom data is null");
			return Task.CompletedTask;
		}
		_AirPressure = data.AirPressure;
		_AirQuality = data.AirQuality;
		_AirPressureChangeRate = data.AirPressureChangeRate;
		_AirQualityChangeRate = data.AirQualityChangeRate;
		_Temperature = data.Temperature;
		_UseGravity = data.UseGravity;
		GravityMalfunction = data.GravityMalfunction;
		AirFiltering = data.AirFiltering;

		return Task.CompletedTask;
	}

	public RoomDetails GetDetails()
	{
		return new RoomDetails
		{
			InSceneID = ID.InSceneID,
			AirPressure = AirPressure,
			AirQuality = AirPressure <= float.Epsilon ? 0f : AirQuality,
			CompoundRoomID = CompoundRoom.ID,
			UseGravity = UseGravity,
			AirFiltering = AirFiltering,
			Temperature = Temperature,
			AirPressureChangeRate = AirPressureChangeRate.IsNotEpsilonZero(1E-05f) ? AirPressureChangeRate : 0f,
			AirQualityChangeRate = AirQualityChangeRate.IsNotEpsilonZero(1E-05f) ? AirQualityChangeRate : 0f,
			PressurizationStatus = GetPressurizationStatus(),
			Fire = AirConsumers.Count((IAirConsumer m) => m is AirConsumerFire) > 0,
			Breach = AirConsumers.Count((IAirConsumer m) => m is AirConsumerBreach) > 0,
			GravityMalfunction = GravityMalfunction
		};
	}

	private RoomPressurizationStatus GetPressurizationStatus()
	{
		if (TargetPressure.HasValue)
		{
			if (TargetPressure.Value > AirPressure)
			{
				return RoomPressurizationStatus.Pressurize;
			}
			if (TargetPressure.Value < AirPressure)
			{
				return TargetPressure.Value < 0f ? RoomPressurizationStatus.Vent : RoomPressurizationStatus.Depressurize;
			}
		}
		else if (EquilizePressureRoom != null)
		{
			if (EquilizePressureRoom.AirPressure > AirPressure)
			{
				return RoomPressurizationStatus.Pressurize;
			}
			if (EquilizePressureRoom.AirPressure < AirPressure)
			{
				return RoomPressurizationStatus.Depressurize;
			}
		}
		return RoomPressurizationStatus.None;
	}

	public void AddAirConsumer(IAirConsumer consumer)
	{
		AirConsumers.Add(consumer);
		if (CompoundRoom != null)
		{
			CompoundRoom.AirConsumers.Add(consumer);
		}
	}

	public void RemoveAirConsumer(IAirConsumer consumer)
	{
		AirConsumers.Remove(consumer);
		if (CompoundRoom != null)
		{
			CompoundRoom.AirConsumers.Remove(consumer);
		}
	}
}
