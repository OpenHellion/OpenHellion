using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenHellion.Net;
using OpenHellion.Net.Message;
using ZeroGravity.BulletPhysics;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;
using ZeroGravity.Spawn;

namespace ZeroGravity.Objects;

public class Asteroid : SpaceObjectVessel, IPersistantObject
{
	public int ColliderIndex = 1;

	public ConcurrentDictionary<int, AsteroidMiningPoint> MiningPoints = new();

	public override SpaceObjectType ObjectType => SpaceObjectType.Asteroid;

	public Asteroid(long guid, bool initializeOrbit, Vector3D position, Vector3D velocity, QuaternionD rotation)
		: base(guid, initializeOrbit, position, velocity, rotation)
	{
		Mass = 100000000000.0;
	}

	public static Asteroid CreateNewAsteroid(GameScenes.SceneId sceneID, string registration = "", long asteroidGUID = -1L, List<long> nearArtificialBodyGUIDs = null, List<long> celestialBodyGUIDs = null, Vector3D? positionOffset = null, Vector3D? velocityAtPosition = null, QuaternionD? localRotation = null, string vesselTag = "", bool checkPosition = true, float? AsteroidResourcesMultiplier = null, double distanceFromSurfacePercMin = 0.03, double distanceFromSurfacePercMax = 0.3, SpawnRuleOrbit spawnRuleOrbit = null, double celestialBodyDeathDistanceMultiplier = 1.5, double artificialBodyDistanceCheck = 100.0, bool isDebrisFragment = false)
	{
		Vector3D astPos = Vector3D.Zero;
		Vector3D astVel = Vector3D.Zero;
		QuaternionD astRotation = QuaternionD.Identity;
		OrbitParameters orbit = null;
		Asteroid asteroid = new Asteroid(asteroidGUID < 0 ? GUIDFactory.NextVesselGUID() : asteroidGUID, initializeOrbit: false, astPos, astVel, astRotation)
		{
			SceneId = sceneID,
			VesselRegistration = registration,
			VesselName = "",
			VesselTag = vesselTag,
			CollidersCenterOffset = Vector3D.Zero.ToFloatArray(),
			IsDebrisFragment = isDebrisFragment,
			CreationSolarSystemTime = Server.SolarSystemTime
		};
		asteroid.ReadInfoFromJson();
		Server.Instance.PhysicsController.CreateAndAddRigidBody(asteroid);
		Server.Instance.SolarSystem.GetSpawnPosition(asteroid.Radius, checkPosition, out astPos, out astVel, out astRotation, nearArtificialBodyGUIDs, celestialBodyGUIDs, positionOffset, velocityAtPosition, localRotation, distanceFromSurfacePercMin, distanceFromSurfacePercMax, spawnRuleOrbit, celestialBodyDeathDistanceMultiplier, artificialBodyDistanceCheck, out orbit);
		asteroid.InitializeOrbit(astPos, astVel, astRotation, orbit);
		if (registration.IsNullOrEmpty())
		{
			asteroid.VesselRegistration = Server.NameGenerator.GenerateObjectRegistration(SpaceObjectType.Asteroid, asteroid.Orbit.Parent.CelestialBody, sceneID);
		}
		if (asteroid.MiningPoints.Count > 0)
		{
			CelestialBody cb = asteroid.Orbit.Parent.CelestialBody;
			while (cb == null || cb.AsteroidResources == null || cb.AsteroidResources.Count == 0)
			{
				cb = cb.Orbit.Parent.CelestialBody;
			}
			List<ResourceMinMax> asteroidResources = cb.AsteroidResources;
			if (asteroidResources is { Count: > 0 })
			{
				List<KeyValuePair<ResourceType, float>> resources = (from m in cb.AsteroidResources
					select new KeyValuePair<ResourceType, float>(m.Type, MathHelper.RandomRange(m.Min, m.Max) * (AsteroidResourcesMultiplier.HasValue ? AsteroidResourcesMultiplier.Value : 1f)) into m
					orderby 0f - m.Value
					where m.Value > 0f
					select m).ToList();
				if (resources.Count > 0)
				{
					int index = 0;
					foreach (AsteroidMiningPoint amp in from m in asteroid.MiningPoints.Values
						orderby 0f - m.Size, MathHelper.RandomNextDouble()
						select m)
					{
						if (index >= resources.Count)
						{
							index = 0;
						}
						amp.ResourceType = resources.ElementAt(index++).Key;
						amp.GasBurstTimeMin = cb.AsteroidGasBurstTimeMin;
						amp.GasBurstTimeMax = cb.AsteroidGasBurstTimeMax;
					}
					foreach (ResourceType rt in asteroid.MiningPoints.Values.Select((AsteroidMiningPoint m) => m.ResourceType).Distinct())
					{
						float avail = resources.FirstOrDefault((KeyValuePair<ResourceType, float> m) => m.Key == rt).Value;
						List<AsteroidMiningPoint> miningPoints = asteroid.MiningPoints.Values.Where((AsteroidMiningPoint m) => m.ResourceType == rt).ToList();
						while (avail > 0f)
						{
							foreach (AsteroidMiningPoint mp in miningPoints)
							{
								float qty = 10f * mp.Size;
								mp.Quantity += qty;
								mp.MaxQuantity = mp.Quantity;
								avail -= qty;
								if (avail <= 0f)
								{
									break;
								}
							}
						}
					}
				}
			}
		}
		Server.Instance.Add(asteroid);
		asteroid.SetPhysicsParameters();
		return asteroid;
	}

	public Dictionary<ResourceType, float> GetAllResources()
	{
		Dictionary<ResourceType, float> retVal = new Dictionary<ResourceType, float>();
		foreach (AsteroidMiningPoint mp in MiningPoints.Values)
		{
			if (retVal.ContainsKey(mp.ResourceType))
			{
				retVal[mp.ResourceType] += mp.Quantity;
			}
			else
			{
				retVal.Add(mp.ResourceType, mp.Quantity);
			}
		}
		return retVal;
	}

	public override void AddPlayerToCrew(Player pl)
	{
		if (!VesselCrew.Contains(pl))
		{
			VesselCrew.Add(pl);
		}
	}

	public override void RemovePlayerFromCrew(Player pl, bool checkDetails = false)
	{
		VesselCrew.Remove(pl);
	}

	public override bool HasPlayerInCrew(Player pl)
	{
		return VesselCrew.Contains(pl);
	}

	public void ReadInfoFromJson()
	{
		CelestialSceneData asd = StaticData.AsteroidDataList.Find((CelestialSceneData x) => x.ItemID == (short)SceneId);
		Radius = asd.Radius;
		RadarSignature = asd.RadarSignature;
		foreach (AsteroidMiningPointData ampd in asd.MiningPoints)
		{
			MiningPoints[ampd.InSceneID] = new AsteroidMiningPoint(this, ampd);
		}
		if (asd.Colliders == null)
		{
			return;
		}
		if (asd.Colliders.PrimitiveCollidersData is { Count: > 0 })
		{
			foreach (PrimitiveColliderData data2 in asd.Colliders.PrimitiveCollidersData)
			{
				if (data2.Type == ColliderDataType.Box)
				{
					PrimitiveCollidersData.Add(new VesselPrimitiveColliderData
					{
						Type = data2.Type,
						CenterPosition = data2.Center.ToVector3D(),
						Bounds = data2.Size.ToVector3D(),
						AffectingCenterOfMass = data2.AffectingCenterOfMass
					});
				}
				else if (data2.Type == ColliderDataType.Sphere)
				{
					PrimitiveCollidersData.Add(new VesselPrimitiveColliderData
					{
						Type = data2.Type,
						CenterPosition = data2.Center.ToVector3D(),
						Bounds = data2.Size.ToVector3D(),
						AffectingCenterOfMass = data2.AffectingCenterOfMass
					});
				}
			}
		}
		if (asd.Colliders.MeshCollidersData == null)
		{
			return;
		}
		foreach (MeshData data in asd.Colliders.MeshCollidersData)
		{
			MeshCollidersData.Add(new VesselMeshColliderData
			{
				AffectingCenterOfMass = data.AffectingCenterOfMass,
				ColliderID = ColliderIndex++,
				Indices = data.Indices,
				Vertices = data.Vertices.GetVertices(),
				CenterPosition = data.CenterPosition.ToVector3D(),
				Bounds = data.Bounds.ToVector3D(),
				Rotation = data.Rotation.ToQuaternionD(),
				Scale = data.Scale.ToVector3D()
			});
		}
	}

	public override async Task Destroy()
	{
		Server.Instance.PhysicsController.RemoveRigidBody(this);
		Server.Instance.Remove(this);
		DisconectListener();
		await base.Destroy();
	}

	private void DisconectListener()
	{
	}

	public ObjectsInfoResponse.AsteroidData GetAsteroidData(Vector3D anchorPosition)
	{
		return new ObjectsInfoResponse.AsteroidData
		{
			Guid = Guid,
			Position = (Position - anchorPosition).ToFloatArray(),
			Rotation = Rotation.ToFloatArray(),
			Radius = Radius,
			VesselRegistration = VesselRegistration,
			VesselName = VesselName,
			Tag = VesselTag,
			SceneId = SceneId,
			IsDebrisFragment = IsDebrisFragment,
			IsAlwaysVisible = IsAlwaysVisible,
			MiningPoints = MiningPoints.Values.Select((AsteroidMiningPoint m) => m.GetDetails()).ToArray()
		};
	}

	public PersistenceObjectData GetPersistenceData()
	{
		var orbitData = new OrbitData();
		Orbit.FillOrbitData(ref orbitData);
		return new PersistenceObjectDataAsteroid
		{
			GUID = Guid,
			OrbitData = orbitData,
			Name = VesselRegistration,
			Tag = VesselTag,
			SceneID = SceneId,
			IsAlwaysVisible = IsAlwaysVisible,
			Rotation = Rotation.ToArray(),
			AngularVelocity = AngularVelocity.ToArray(),
			MiningPoints = MiningPoints.Values.Select((AsteroidMiningPoint m) => m.GetDetails()).ToList()
		};
	}

	public async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		PersistenceObjectDataAsteroid data = persistenceData as PersistenceObjectDataAsteroid;
		CollidersCenterOffset = Vector3D.Zero.ToFloatArray();
		VesselRegistration = data.Name;
		VesselTag = data.Tag;
		SceneId = data.SceneID;
		IsAlwaysVisible = data.IsAlwaysVisible;
		ReadInfoFromJson();
		Server.Instance.PhysicsController.CreateAndAddRigidBody(this);
		InitializeOrbit(Vector3D.Zero, Vector3D.One, data.Rotation.ToQuaternionD());
		if (data.OrbitData != null)
		{
			Orbit.ParseNetworkData(data.OrbitData, resetOrbit: true);
		}
		AngularVelocity = data.AngularVelocity.ToVector3D();
		await Task.Run(() => {
			foreach (var det in data.MiningPoints)
			{
				if (MiningPoints.TryGetValue(det.InSceneID, out var mp))
				{
					mp.ResourceType = det.ResourceType;
					mp.Quantity = det.Quantity;
					mp.MaxQuantity = det.MaxQuantity;
				}
			}
		});
		Server.Instance.Add(this);
		SetPhysicsParameters();
	}

	public override async Task UpdateVesselSystems()
	{
		foreach (AsteroidMiningPoint amp in MiningPoints.Values)
		{
			if (amp.CheckGasBurst())
			{
				await NetworkController.SendToClientsSubscribedTo(new MiningPointStatsMessage
				{
					ID = amp.ID,
					GasBurst = true
				}, -1L, this);
				break;
			}
			if (amp.StatusChanged)
			{
				await NetworkController.SendToClientsSubscribedTo(new MiningPointStatsMessage
				{
					ID = amp.ID,
					Quantity = amp.Quantity
				}, -1L, this);
				amp.StatusChanged = false;
			}
		}
	}
}
