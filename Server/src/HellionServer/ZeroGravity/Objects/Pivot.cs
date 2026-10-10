using System.Threading.Tasks;
using ZeroGravity.Math;

namespace ZeroGravity.Objects;

public class Pivot : ArtificialBody
{
	public SpaceObjectTransferable Child;

	public override SpaceObjectType ObjectType
	{
		get
		{
			if (Child == null)
			{
				return SpaceObjectType.None;
			}
			if (Child.ObjectType == SpaceObjectType.Player)
			{
				return SpaceObjectType.PlayerPivot;
			}
			if (Child.ObjectType == SpaceObjectType.DynamicObject)
			{
				return SpaceObjectType.DynamicObjectPivot;
			}
			if (Child.ObjectType == SpaceObjectType.Corpse)
			{
				return SpaceObjectType.CorpsePivot;
			}
			return SpaceObjectType.None;
		}
	}

	public Pivot(Player child, SpaceObjectVessel vessel)
		: base(child.FakeGuid, initializeOrbit: true, vessel.Position, vessel.Velocity, QuaternionD.Identity)
	{
		Child = child;
	}

	public Pivot(SpaceObjectTransferable child, ArtificialBody abody)
		: base(child.Guid, initializeOrbit: true, abody.Position, abody.Velocity, QuaternionD.Identity)
	{
		Child = child;
	}

	public Pivot(SpaceObjectTransferable child, Vector3D position, Vector3D velocity)
		: base(child.Guid, initializeOrbit: true, position, velocity, QuaternionD.Identity)
	{
		Child = child;
	}

	// A player is known to the world by its fake guid, so its pivot must share that instead.
	public Pivot(Player child, Vector3D position, Vector3D velocity)
		: base(child.FakeGuid, initializeOrbit: true, position, velocity, QuaternionD.Identity)
	{
		Child = child;
	}

	public void AdjustPositionAndVelocity(Vector3D positionAddition, Vector3D velocityAddition)
	{
		Orbit.RelativePosition += positionAddition;
		Orbit.RelativeVelocity += velocityAddition;
		Orbit.InitFromCurrentStateVectors(Server.Instance.SolarSystem.CurrentTime);
	}

	public override async Task Destroy()
	{
		await base.Destroy();
	}
}
