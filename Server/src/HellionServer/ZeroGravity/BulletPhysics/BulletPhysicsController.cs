using System;
using System.Collections.Generic;
using System.Linq;
using BulletSharp;
using BulletSharp.Math;
using ZeroGravity.Data;
using ZeroGravity.Objects;

namespace ZeroGravity.BulletPhysics;

public class BulletPhysicsController
{
	private readonly BroadphaseInterface _broadphase;

	private readonly DefaultCollisionConfiguration _collisionConfiguration;

	private readonly CollisionDispatcher _dispatcher;

	private readonly DiscreteDynamicsWorld _dynamicsWorld;

	public double Restitution = 0.85;

	public double Friction = 0.7;

	private double prevUpdateTime;

	public BulletPhysicsController()
	{
		_collisionConfiguration = new DefaultCollisionConfiguration();
		_dispatcher = new CollisionDispatcher(_collisionConfiguration);
		_broadphase = new DbvtBroadphase();
		_dynamicsWorld = new DiscreteDynamicsWorld(_dispatcher, _broadphase, null, _collisionConfiguration);
		_dynamicsWorld.Gravity = Vector3.Zero;
		_dynamicsWorld.SetInternalTickCallback(InternalTickCallback);
		GImpactCollisionAlgorithm.RegisterAlgorithm(_dispatcher);
	}

	public void CreateAndAddRigidBody(SpaceObjectVessel vessel)
	{
		CompoundShape compound = new CompoundShape();
		CompoundShape baseCompound = new CompoundShape();
		foreach (VesselPrimitiveColliderData data4 in vessel.PrimitiveCollidersData)
		{
			CollisionShape shape4 = null;
			Matrix l = BulletHelper.AffineTransformation(1f, Quaternion.Identity, data4.CenterPosition.ToVector3());
			if (data4.Type == ColliderDataType.Box)
			{
				shape4 = new BoxShape(data4.Bounds.X / 2.0, data4.Bounds.Y / 2.0, data4.Bounds.Z / 2.0);
			}
			else if (data4.Type == ColliderDataType.Sphere)
			{
				shape4 = new SphereShape(data4.Bounds.X);
			}
			if (shape4 != null)
			{
				shape4.UserObject = data4;
				baseCompound.AddChildShape(l, shape4);
			}
		}
		foreach (VesselMeshColliderData data3 in vessel.MeshCollidersData)
		{
			Matrix k = BulletHelper.AffineTransformation(1f, data3.Rotation.ToQuaternion(), data3.CenterPosition.ToVector3());
			GImpactMeshShape shape3 = new GImpactMeshShape(new TriangleIndexVertexArray(data3.Indices, data3.Vertices));
			shape3.LocalScaling = data3.Scale.ToVector3();
			shape3.UpdateBound();
			if (shape3 != null)
			{
				shape3.UserObject = data3;
				baseCompound.AddChildShape(k, shape3);
			}
		}
		baseCompound.UserObject = vessel;
		Matrix offset = BulletHelper.AffineTransformation(1f, Quaternion.Identity, -vessel.CollidersCenterOffset.ToVector3D().ToVector3());
		compound.AddChildShape(offset, baseCompound);
		Quaternion qua = vessel.Rotation.ToQuaternion();
		Matrix position = BulletHelper.AffineTransformation(1f, qua, vessel.Position.ToVector3());
		double AdditionalMass = 0.0;
		if (vessel.AllDockedVessels.Count > 0)
		{
			foreach (Ship p in vessel.AllDockedVessels.Cast<Ship>())
			{
				AdditionalMass += p.Mass;
				CompoundShape newCompound = new CompoundShape();
				Matrix relative = BulletHelper.AffineTransformation(1f, p.RelativeRotationFromMainParent.ToQuaternion(), p.RelativePositionFromMainParent.ToVector3() - vessel.CollidersCenterOffset.ToVector3D().ToVector3());
				foreach (VesselPrimitiveColliderData data2 in p.PrimitiveCollidersData)
				{
					CollisionShape shape2 = null;
					Matrix j = BulletHelper.AffineTransformation(1f, Quaternion.Identity, data2.CenterPosition.ToVector3());
					if (data2.Type == ColliderDataType.Box)
					{
						shape2 = new BoxShape(data2.Bounds.X / 2.0, data2.Bounds.Y / 2.0, data2.Bounds.Z / 2.0);
					}
					else if (data2.Type == ColliderDataType.Sphere)
					{
						shape2 = new SphereShape(data2.Bounds.X);
					}
					if (shape2 != null)
					{
						shape2.UserObject = data2;
						newCompound.AddChildShape(j, shape2);
					}
				}
				foreach (VesselMeshColliderData data in p.MeshCollidersData)
				{
					GImpactMeshShape shape = null;
					Matrix i = BulletHelper.AffineTransformation(1f, data.Rotation.ToQuaternion(), data.CenterPosition.ToVector3());
					shape = new GImpactMeshShape(new TriangleIndexVertexArray(data.Indices, data.Vertices));
					shape.LocalScaling = data.Scale.ToVector3();
					shape.UpdateBound();
					if (shape != null)
					{
						shape.UserObject = data;
						newCompound.AddChildShape(i, shape);
					}
				}
				newCompound.UserObject = p;
				compound.AddChildShape(relative, newCompound);
			}
		}
		DefaultMotionState motionState = new DefaultMotionState(position);
		Vector3 inertiaTensor = compound.CalculateLocalInertia(vessel.Mass + AdditionalMass);
		RigidBodyConstructionInfo rigidBodyCI = new RigidBodyConstructionInfo(vessel.Mass + AdditionalMass, motionState, compound);
		rigidBodyCI.LocalInertia = inertiaTensor;
		RigidBody rigidBody = new RigidBody(rigidBodyCI);
		rigidBody.SetDamping(0.0, 0.0);
		rigidBody.SetSleepingThresholds(0.1, 0.1);
		rigidBody.Restitution = Restitution;
		rigidBody.Friction = Friction;
		rigidBody.ForceActivationState(ActivationState.DisableDeactivation);
		rigidBody.UserObject = vessel;
		lock (_dynamicsWorld)
		{
			_dynamicsWorld.AddRigidBody(rigidBody);
		}
		compound.GetBoundingSphere(out _, out var sphereRadius);
		vessel.SetRadius(sphereRadius);
		vessel.RigidBody = rigidBody;
	}

	public static void ComplexBoundCalculation(SpaceObjectVessel vessel, out Vector3 minValue, out Vector3 maxValue)
	{
		CompoundShape compound = new CompoundShape();
		foreach (VesselPrimitiveColliderData data4 in vessel.PrimitiveCollidersData)
		{
			if (data4.AffectingCenterOfMass)
			{
				CollisionShape shape4 = null;
				Matrix l = BulletHelper.AffineTransformation(1f, Quaternion.Identity, data4.CenterPosition.ToVector3());
				if (data4.Type == ColliderDataType.Box)
				{
					shape4 = new BoxShape(data4.Bounds.X / 2.0, data4.Bounds.Y / 2.0, data4.Bounds.Z / 2.0);
				}
				else if (data4.Type == ColliderDataType.Sphere)
				{
					shape4 = new SphereShape(data4.Bounds.X);
				}
				if (shape4 != null)
				{
					compound.AddChildShape(l, shape4);
				}
			}
		}
		foreach (VesselMeshColliderData data3 in vessel.MeshCollidersData)
		{
			if (data3.AffectingCenterOfMass)
			{
				Matrix k = BulletHelper.AffineTransformation(1f, data3.Rotation.ToQuaternion(), data3.CenterPosition.ToVector3());
				GImpactMeshShape shape3 = new GImpactMeshShape(new TriangleIndexVertexArray(data3.Indices, data3.Vertices));
				shape3.LocalScaling = data3.Scale.ToVector3();
				shape3.UpdateBound();
				if (shape3 != null)
				{
					compound.AddChildShape(k, shape3);
				}
			}
		}
		if (vessel.AllDockedVessels.Count > 0)
		{
			foreach (Ship p in vessel.AllDockedVessels.Cast<Ship>())
			{
				CompoundShape newCompound = new CompoundShape();
				Matrix relative = BulletHelper.AffineTransformation(1f, p.RelativeRotationFromMainParent.ToQuaternion(), p.RelativePositionFromMainParent.ToVector3());
				foreach (VesselPrimitiveColliderData data2 in p.PrimitiveCollidersData)
				{
					CollisionShape shape2 = null;
					Matrix j = BulletHelper.AffineTransformation(1f, Quaternion.Identity, data2.CenterPosition.ToVector3());
					if (data2.Type == ColliderDataType.Box)
					{
						shape2 = new BoxShape(data2.Bounds.X / 2.0, data2.Bounds.Y / 2.0, data2.Bounds.Z / 2.0);
					}
					else if (data2.Type == ColliderDataType.Sphere)
					{
						shape2 = new SphereShape(data2.Bounds.X);
					}
					if (shape2 != null)
					{
						newCompound.AddChildShape(j, shape2);
					}
				}
				foreach (VesselMeshColliderData data in p.MeshCollidersData)
				{
					Matrix i = BulletHelper.AffineTransformation(1f, data.Rotation.ToQuaternion(), data.CenterPosition.ToVector3());
					GImpactMeshShape shape = new GImpactMeshShape(new TriangleIndexVertexArray(data.Indices, data.Vertices));
					shape.LocalScaling = data.Scale.ToVector3();
					shape.UpdateBound();
					if (shape != null)
					{
						newCompound.AddChildShape(i, shape);
					}
				}
				compound.AddChildShape(relative, newCompound);
			}
		}
		Matrix identity = BulletHelper.AffineTransformation(1f, Quaternion.Identity, Vector3.Zero);
		compound.GetAabb(identity, out minValue, out maxValue);
	}

	public bool RayCast(Vector3 from, Vector3 to, out ClosestRayResultCallback result)
	{
		lock (_dynamicsWorld)
		{
			result = new ClosestRayResultCallback(ref from, ref to);
			_dynamicsWorld.RayTest(from, to, result);
		}
		return result.HasHit;
	}

	public bool RayCastAll(Vector3 from, Vector3 to, out AllHitsRayResultCallback result)
	{
		lock (_dynamicsWorld)
		{
			result = new AllHitsRayResultCallback(from, to);
			_dynamicsWorld.RayTest(from, to, result);
		}
		return result.HasHit;
	}

	private async void InternalTickCallback(DynamicsWorld world, double timeStep)
	{
		try
		{
			foreach (SpaceObjectVessel vess in Server.Instance.AllVessels)
			{
				vess.ReadPhysicsParameters();
			}
			int k = world.Dispatcher.NumManifolds;
			for (int i = 0; i < k; i++)
			{
				PersistentManifold contactManifold = world.Dispatcher.GetManifoldByIndexInternal(i);
				RigidBody obA = contactManifold.Body0 as RigidBody;
				RigidBody obB = contactManifold.Body1 as RigidBody;
				Vector3 vel = obA.LinearVelocity - obB.LinearVelocity;
				List<long> shipsGUID = new List<long>();
				if (obA.UserObject is Ship ship)
				{
					shipsGUID.Add(ship.Guid);
				}
				if (obB.UserObject is Ship o)
				{
					shipsGUID.Add(o.Guid);
				}
				int numContacts = contactManifold.NumContacts;
				for (int j = 0; j < numContacts; j++)
				{
					ManifoldPoint pt = contactManifold.GetContactPoint(j);
					if (pt.AppliedImpulse.IsNotEpsilonZeroD() && shipsGUID.Count > 0)
					{
						await (Server.Instance.GetVessel(shipsGUID[0]) as Ship).SendCollision(vel.Length, pt.AppliedImpulse, pt.LifeTime, shipsGUID.Count > 1 ? shipsGUID[1] : -1);
					}
				}
				contactManifold.ClearManifold();
			}
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
		}
	}

	public void Update()
	{
		double deltaTime = Server.SolarSystemTime - prevUpdateTime;
		prevUpdateTime = Server.SolarSystemTime;
		lock (_dynamicsWorld)
		{
			_dynamicsWorld.StepSimulation(deltaTime);
		}
	}

	public bool RemoveRigidBody(SpaceObjectVessel ship)
	{
		try
		{
			lock (_dynamicsWorld)
			{
				if (ship.RigidBody != null && _dynamicsWorld.CollisionObjectArray.Contains(ship.RigidBody))
				{
					_dynamicsWorld.RemoveRigidBody(ship.RigidBody);
					ship.RigidBody = null;
					return true;
				}
			}
			if (ship.IsDocked)
			{
				return RemoveRigidBody(ship.DockedToMainVessel as Ship);
			}
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			return false;
		}
	}
}
