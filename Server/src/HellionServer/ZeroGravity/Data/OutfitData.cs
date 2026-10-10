using System.Collections.Generic;

namespace ZeroGravity.Data;

public class OutfitData : DynamicObjectAuxData
{
	public List<InventorySlotData> InventorySlots;

	public float DamageReductionTorso = 0f;

	public float DamageReductionAbdomen = 0f;

	public float DamageReductionArms = 0f;

	public float DamageReductionLegs = 0f;

	public float DamageResistanceTorso = 1f;

	public float DamageResistanceAbdomen = 1f;

	public float DamageResistanceArms = 1f;

	public float DamageResistanceLegs = 1f;

	public float CollisionResistance = 1f;
}
