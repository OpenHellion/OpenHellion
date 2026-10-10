using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenHellion.Net;
using OpenHellion.State;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Spawn;

namespace ZeroGravity.Objects;

public abstract class SpaceObject
{
	public long Guid;

	public readonly HashSet<long> Corpses = [];

	private const double BaseSunHeatTransferPerSec = 9.4E+21;

	public bool IsExposedToSunlight;

	private double _sqrDistanceFromSun;

	public bool IsPartOfSpawnSystem;

	public virtual SpaceObjectType ObjectType => SpaceObjectType.None;

	public virtual SpaceObject Parent { get; set; }

	public long Key => this switch
	{
		Player player => player.FakeGuid,
		Pivot => 0,
		_ => Guid
	};

	public virtual Vector3D Position => Vector3D.Zero;

	public virtual QuaternionD Rotation
	{
		get => QuaternionD.Identity;
		set => Debug.LogError("Tried to rotate an object that has no orientation of its own", Guid);
	}

	public virtual Vector3D Velocity => Vector3D.Zero;

	public SpaceObject(long guid)
	{
		Guid = guid;
	}

	public virtual Task UpdateTimers(double deltaTime)
	{
		return Task.CompletedTask;
	}

	public void RefreshSunlightExposure()
	{
		IsExposedToSunlight = CalculateSunlightExposure(out _sqrDistanceFromSun);
	}

	public virtual async Task Destroy()
	{
		SolarSystemState state = Server.Instance.SolarSystem.State;
		List<long> items = [];
		for (ItemId item = state.FirstChild(Key); item.IsValid; item = state.NextSibling(item))
		{
			items.Add(state.Guid(item));
		}
		foreach (long item in items)
		{
			await Server.Instance.GetDynamicObject(item).Destroy();
		}

		foreach (long corpse in new List<long>(Corpses))
		{
			if (Server.Instance.SpaceObjects.TryRemove(corpse, out SpaceObject spaceObject))
			{
				await spaceObject.Destroy();
			}
		}

		DestroyObjectMessage message = new DestroyObjectMessage
		{
			ID = Guid,
			ObjectType = ObjectType
		};

		if (this is SpaceObjectVessel)
		{
			await NetworkController.SendToAllAsync(message);
		}
		else
		{
			await NetworkController.SendToClientsSubscribedToParents(message, this, -1L);
		}

		if (this is Player)
		{
			Server.Instance.Remove(this as Player);
		}
		else if (this is SpaceObjectVessel)
		{
			Server.Instance.Remove(this as SpaceObjectVessel);
		}
		else if (this is DynamicObject)
		{
			Server.Instance.Remove(this as DynamicObject);
		}
		else if (this is Corpse)
		{
			Server.Instance.Remove(this as Corpse);
		}

		if (Parent is Pivot pivot)
		{
			pivot.Child = null;
			Server.Instance.SolarSystem.RemoveArtificialBody(pivot);
		}

		Parent = null;
		if (IsPartOfSpawnSystem)
		{
			SpawnManager.RemoveSpawnSystemObject(this, checkChildren: false);
		}
	}

	/// <summary>
	/// 	Calculates if this vessel is exposed to sunlight.
	/// </summary>
	/// <param name="sqrDistFromSun">Square magnitude of our relative position to the sun.</param>
	/// <returns>If the vessel is exposed to sunlight.</returns>
	private bool CalculateSunlightExposure(out double sqrDistFromSun)
	{
		sqrDistFromSun = 0.0;
		CelestialBody sun = Server.Instance.SolarSystem.GetCelestialBodies()[0];
		while (sun.Parent != null)
		{
			sun = sun.Parent;
		}
		if (this is not SpaceObjectVessel && (this is not SpaceObjectTransferable || Parent is not Pivot))
		{
			return false;
		}
		Vector3D position = Position;
		Vector3D vSunPos = sun.Position - position;
		sqrDistFromSun = vSunPos.SqrMagnitude;
		foreach (CelestialBody cb in Server.Instance.SolarSystem.GetCelestialBodies())
		{
			if (cb.Parent != sun)
			{
				continue;
			}
			Vector3D vCBPos = cb.Position - position;
			if (vCBPos.SqrMagnitude < vSunPos.SqrMagnitude && Vector3D.Project(vCBPos, vSunPos).Normalized == vSunPos.Normalized)
			{
				double x = Vector3D.ProjectOnPlane(vCBPos, vSunPos).Magnitude;
				if (cb.Radius > x)
				{
					return false;
				}
			}
		}
		return true;
	}

	public float SpaceExposureTemperature(float currentTemperature, float heatCollectionFactor, float heatDissipationFactor, float mass, double deltaTime)
	{
		double hCol = 0.0;
		double hDis = 0.0;
		if (IsExposedToSunlight)
		{
			hCol = BaseSunHeatTransferPerSec * heatCollectionFactor / mass / _sqrDistanceFromSun;
		}
		hDis = heatDissipationFactor / mass * (currentTemperature + 273.15);
		return (float)(currentTemperature + (hCol - hDis) * deltaTime);
	}

	public List<SpaceObject> GetParents(bool includeMe, int depth = 10)
	{
		List<SpaceObject> retVal = new List<SpaceObject>();
		SpaceObject tmpParent = includeMe ? this : Parent;
		while (tmpParent != null && depth > 0)
		{
			retVal.Add(tmpParent);
			tmpParent = tmpParent.Parent;
			depth--;
		}
		return retVal;
	}

	public static T GetParent<T>(SpaceObject parent) where T : SpaceObject
	{
		while (parent is not null and not T)
		{
			parent = parent.Parent;
		}
		return parent as T;
	}
}
