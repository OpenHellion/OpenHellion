using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZeroGravity.Data;

public abstract class DynamicObjectAuxData
{
	public ItemCategory Category;

	public ItemType ItemType;

	public int Tier;

	public float[] TierMultipliers;

	public float[] AuxValues;

	public float MaxHealth;

	public float Health;

	public float Armor;

	public bool Damageable;

	public bool Repairable;

	public float UsageWear;

	public float MeleeDamage;

	public List<ItemSlotData> Slots;

	public float ExplosionDamage;

	public float ExplosionRadius;

	public TypeOfDamage ExplosionDamageType;

	public static object GetJsonData(JObject jo, JsonSerializer serializer)
	{
		ItemType type = (ItemType)(int)jo["ItemType"];
		if (ItemTypeRange.IsMachineryPart(type))
		{
			return jo.ToObject<MachineryPartData>(serializer);
		}
		if (ItemTypeRange.IsHelmet(type))
		{
			return jo.ToObject<HelmetData>(serializer);
		}
		if (ItemTypeRange.IsJetpack(type))
		{
			return jo.ToObject<JetpackData>(serializer);
		}
		if (ItemTypeRange.IsWeapon(type))
		{
			return jo.ToObject<WeaponData>(serializer);
		}
		if (ItemTypeRange.IsMelee(type))
		{
			return jo.ToObject<MeleeWeaponData>(serializer);
		}
		if (ItemTypeRange.IsAmmo(type))
		{
			return jo.ToObject<MagazineData>(serializer);
		}
		if (ItemTypeRange.IsOutfit(type))
		{
			return jo.ToObject<OutfitData>(serializer);
		}
		if (ItemTypeRange.IsBattery(type))
		{
			return jo.ToObject<BatteryData>(serializer);
		}
		if (ItemTypeRange.IsCanister(type))
		{
			return jo.ToObject<CanisterData>(serializer);
		}
		if (ItemTypeRange.IsDrill(type))
		{
			return jo.ToObject<HandDrillData>(serializer);
		}
		if (ItemTypeRange.IsGlowStick(type))
		{
			return jo.ToObject<GlowStickData>(serializer);
		}
		if (ItemTypeRange.IsMedpack(type))
		{
			return jo.ToObject<MedpackData>(serializer);
		}
		if (ItemTypeRange.IsHackingTool(type))
		{
			return jo.ToObject<DisposableHackingToolData>(serializer);
		}
		if (ItemTypeRange.IsAsteroidScanningTool(type))
		{
			return jo.ToObject<HandheldAsteroidScannerData>(serializer);
		}
		if (ItemTypeRange.IsLogItem(type))
		{
			return jo.ToObject<LogItemData>(serializer);
		}
		if (ItemTypeRange.IsGenericItem(type))
		{
			return jo.ToObject<GenericItemData>(serializer);
		}
		if (ItemTypeRange.IsGrenade(type))
		{
			return jo.ToObject<GrenadeData>(serializer);
		}
		if (ItemTypeRange.IsPortableTurret(type))
		{
			return jo.ToObject<PortableTurretData>(serializer);
		}
		if (ItemTypeRange.IsRepairTool(type))
		{
			return jo.ToObject<RepairToolData>(serializer);
		}
		throw new Exception("Json deserializer was not implemented for item type " + type);
	}
}
