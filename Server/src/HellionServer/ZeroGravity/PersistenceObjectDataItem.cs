using ZeroGravity.Data;

namespace ZeroGravity;

public class PersistenceObjectDataItem : PersistenceObjectDataDynamicObject
{
	public int Tier;

	public float Health;

	public float Armor;

	public AttachPointType AttachPointType;

	public int? AttachPointID;

	public short? SlotID;

	public short? ItemSlotID;
}
