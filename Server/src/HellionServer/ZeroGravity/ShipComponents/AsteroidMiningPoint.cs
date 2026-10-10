using System;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class AsteroidMiningPoint : IPersistantObject
{
	public VesselObjectID ID;

	public Vector3D LocalPosition;

	public float Size;

	public ResourceType ResourceType;

	public float MaxQuantity;

	public float GasBurstTimeMin = 30f;

	public float GasBurstTimeMax = 60f;

	public bool StatusChanged;

	public Asteroid Parent;

	private double gasBurstSolarSystemTime;

	public float _Quantity;

	public float Quantity
	{
		get
		{
			return _Quantity;
		}
		set
		{
			if (_Quantity != value)
			{
				_Quantity = value;
				StatusChanged = true;
			}
		}
	}

	public AsteroidMiningPoint(Asteroid parent, AsteroidMiningPointData data)
	{
		Parent = parent;
		ID = new VesselObjectID(parent.Guid, data.InSceneID);
		LocalPosition = data.Position.ToVector3D();
		Size = data.Size;
		gasBurstSolarSystemTime = Server.SolarSystemTime + MathHelper.RandomRange(0f, GasBurstTimeMax);
	}

	public AsteroidMiningPointDetails GetDetails()
	{
		return new AsteroidMiningPointDetails
		{
			InSceneID = ID.InSceneID,
			ResourceType = ResourceType,
			MaxQuantity = MaxQuantity,
			Quantity = Quantity
		};
	}

	public PersistenceObjectData GetPersistenceData()
	{
		throw new NotImplementedException();
	}

	public Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		throw new NotImplementedException();
	}

	public bool CheckGasBurst()
	{
		if (Quantity > float.Epsilon && gasBurstSolarSystemTime < Server.SolarSystemTime)
		{
			double angle = Vector3D.Angle(-Parent.Position, Parent.Rotation * LocalPosition);
			if (angle > 90.0)
			{
				return false;
			}
			gasBurstSolarSystemTime = Server.SolarSystemTime + MathHelper.RandomRange(GasBurstTimeMin, GasBurstTimeMax);
			return true;
		}
		return false;
	}
}
