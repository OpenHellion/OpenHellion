namespace ZeroGravity.Data;

public class MachineryPartSlotData : BaseAttachPointData
{
	public MachineryPartSlotScope Scope;

	public int MinTier;

	public int MaxTier;

	public float PartDecay;

	public int SlotIndex;

	public override AttachPointType AttachPointType => AttachPointType.MachineryPartSlot;

	public bool IsActive
	{
		get
		{
			return true;
		}
		set
		{
		}
	}
}
