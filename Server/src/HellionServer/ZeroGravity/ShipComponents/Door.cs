using System.Threading.Tasks;
using Newtonsoft.Json;
using ZeroGravity.Math;
using ZeroGravity.Network;

namespace ZeroGravity.ShipComponents;

public class Door : IAirConsumer, IPersistantObject
{
	public VesselObjectID ID;

	public bool IsSealable;

	public bool LockedAutoToggle;

	public bool HasPower;

	private bool _IsOpen;

	private bool _IsLocked;

	private Room _Room1;

	private Room _Room2;

	[JsonIgnore]
	public Vector3D PositionRelativeToDockingPort;

	public float PassageArea;

	public VesselObjectID PairedDoorID = null;

	public bool StatusChanged = true;

	public bool IsSealed => IsSealable && !IsOpen;

	public bool isExternal => Room1 == null || Room2 == null;

	public bool IsOpen
	{
		get
		{
			return _IsOpen;
		}
		set
		{
			if (value != _IsOpen)
			{
				StatusChanged = true;
				_IsOpen = value;
			}
		}
	}

	public bool IsLocked
	{
		get
		{
			return _IsLocked;
		}
		set
		{
			if (value != _IsLocked)
			{
				StatusChanged = true;
				_IsLocked = value;
			}
		}
	}

	[JsonIgnore]
	public Room Room1
	{
		get
		{
			return _Room1;
		}
		set
		{
			if (value != _Room1)
			{
				StatusChanged = true;
				_Room1 = value;
			}
		}
	}

	[JsonIgnore]
	public Room Room2
	{
		get
		{
			return _Room2;
		}
		set
		{
			if (value != _Room2)
			{
				StatusChanged = true;
				_Room2 = value;
			}
		}
	}

	public float AirQualityDegradationRate => 0f;

	public float AirQuantityDecreaseRate
	{
		get
		{
			if (isExternal && !IsSealed)
			{
				float pDiff = 0f;
				if (Room1 != null && Room2 == null)
				{
					pDiff = Room1.AirPressure;
				}
				else if (Room1 == null && Room2 != null)
				{
					pDiff = Room2.AirPressure;
				}
				else if (Room1 != null && Room2 != null)
				{
					pDiff = System.Math.Abs(Room1.AirPressure - Room2.AirPressure);
				}
				return (float)(0.61 * PassageArea * System.Math.Sqrt(2f * pDiff * 100000f / 1.225));
			}
			return 0f;
		}
	}

	public bool AffectsQuality => false;

	public bool AffectsQuantity => isExternal && !IsSealed;

	public DoorDetails GetDetails()
	{
		return new DoorDetails
		{
			InSceneID = ID.InSceneID,
			HasPower = HasPower,
			IsLocked = IsLocked,
			IsOpen = IsOpen,
			Room1ID = Room1 != null ? Room1.ID : null,
			Room2ID = Room2 != null ? Room2.ID : null
		};
	}

	public PersistenceObjectData GetPersistenceData()
	{
		return new PersistenceObjectDataDoor
		{
			InSceneID = ID.InSceneID,
			HasPower = HasPower,
			IsLocked = IsLocked,
			IsOpen = IsOpen
		};
	}

	public Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		if (persistenceData is not PersistenceObjectDataDoor data)
		{
			Debug.LogWarning("PersistenceObjectDataDoor data is null");
			return Task.CompletedTask;
		}
		HasPower = data.HasPower;
		IsLocked = data.IsLocked;
		IsOpen = data.IsOpen;

		return Task.CompletedTask;
	}
}
