using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class VesselBaseSystem : SubSystem
{
	public float DecayDamageMultiplier;

	public float DebrisFieldDamageMultiplier;

	public override SubSystemType Type => SubSystemType.VesselBasePowerConsumer;

	public override float CoolDownTime => 0f;

	public override float PowerUpTime => 0f;

	public override bool AutoReactivate => true;

	public VesselBaseSystem(SpaceObjectVessel vessel, VesselObjectID id, SubSystemData ssData)
		: base(vessel, id, ssData)
	{
	}

	public override void SetAuxData(SystemAuxData auxData)
	{
		VesselBaseSystemAuxData data = auxData as VesselBaseSystemAuxData;
		DecayDamageMultiplier = data.DecayDamageMultiplier;
		DebrisFieldDamageMultiplier = data.DebrisFieldDamageMultiplier;
	}

	public override async Task Update(double duration)
	{
		await base.Update(duration);
		float armor = 0f;
		foreach (KeyValuePair<VesselObjectID, MachineryPartSlotData> kv in MachineryPartSlots.Where((KeyValuePair<VesselObjectID, MachineryPartSlotData> m) => m.Value.Scope == MachineryPartSlotScope.Armor))
		{
			MachineryPart mp = MachineryParts[kv.Key];
			if (mp is { Health: > float.Epsilon })
			{
				armor += mp.AuxValue;
			}
		}
		ParentVessel.AddedArmor = armor;
	}

	public async Task<float> ConsumeArmor(float damage, double time = 1.0)
	{
		if (damage <= 0f)
		{
			return 0f;
		}
		float armorLeft = damage;
		float admorConsumed = 0f;
		List<MachineryPart> list = (from m in MachineryPartSlots
			where m.Value.Scope == MachineryPartSlotScope.Armor
			select MachineryParts[m.Key] into m
			where m is { Health: > float.Epsilon }
			orderby m.AuxValue
			select m).ToList();
		foreach (MachineryPart mp in list)
		{
			if (armorLeft > float.Epsilon)
			{
				float armor = (float)(mp.AuxValue * time);
				if (armorLeft > armor)
				{
					mp.Health -= armor;
					admorConsumed += armor;
				}
				else
				{
					mp.Health -= armorLeft;
					admorConsumed += armorLeft;
				}
				armorLeft -= armor;
				continue;
			}
			break;
		}
		return admorConsumed;
	}
}
