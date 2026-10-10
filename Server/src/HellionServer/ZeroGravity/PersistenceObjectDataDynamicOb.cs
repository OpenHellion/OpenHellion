using System.Collections.Generic;
using ZeroGravity.Data;

namespace ZeroGravity;

public class PersistenceObjectDataDynamicObject : PersistenceObjectData
{
	public short ItemID;

	public float[] LocalPosition;

	public float[] LocalRotation;

	public double[] Velocity;

	public double[] AngularVelocity;

	public float? RespawnTime;

	public float? MaxHealth;

	public float? MinHealth;

	public float? WearMultiplier;

	public float[] RespawnPosition;

	public float[] RespawnRotation;

	public DynamicObjectAuxData RespawnAuxData;

	public List<PersistenceObjectData> ChildObjects;
}
