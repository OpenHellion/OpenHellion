using ZeroGravity.Data;
using ZeroGravity.Objects;

namespace ZeroGravity;

public class PersistenceObjectDataRespawnObject : PersistenceObjectData
{
	public short ItemID;

	public long ParentGUID;

	public SpaceObjectType ParentType;

	public float[] Position;

	public float[] Rotation;

	public int? AttachPointID;

	public DynamicObjectAuxData AuxData;

	public float RespawnTime;

	public double Timer;
}
