using System.Collections.Generic;
using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
[ProtoInclude(10000, typeof(HelmetStats))]
[ProtoInclude(10002, typeof(JetpackStats))]
[ProtoInclude(10003, typeof(MagazineStats))]
[ProtoInclude(10004, typeof(WeaponStats))]
[ProtoInclude(10005, typeof(BatteryStats))]
[ProtoInclude(10006, typeof(CanisterStats))]
[ProtoInclude(10007, typeof(HandDrillStats))]
[ProtoInclude(10008, typeof(GlowStickStats))]
[ProtoInclude(10009, typeof(MedpackStats))]
[ProtoInclude(10010, typeof(DisposableHackingToolStats))]
[ProtoInclude(10011, typeof(MachineryPartStats))]
[ProtoInclude(10013, typeof(LogItemStats))]
[ProtoInclude(10014, typeof(GenericItemStats))]
[ProtoInclude(10015, typeof(GrenadeStats))]
[ProtoInclude(10016, typeof(PortableTurretStats))]
[ProtoInclude(10017, typeof(RepairToolStats))]
public class DynamicObjectStats
{
	public int? Tier;

	public float? Health;

	public float? MaxHealth;

	public float? Armor;

	public Dictionary<TypeOfDamage, float> Damages;
}
