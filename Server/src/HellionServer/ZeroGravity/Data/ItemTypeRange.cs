namespace ZeroGravity.Data;

public static class ItemTypeRange
{
	public const int WeaponFrom = 100;

	public const int WeaponTo = 199;

	public const int AmmoFrom = 200;

	public const int AmmoTo = 299;

	public const int OutfitFrom = 300;

	public const int OutfitTo = 399;

	public const int HelmetFrom = 400;

	public const int HelmetTo = 499;

	public const int JetpackFrom = 500;

	public const int JetpackTo = 599;

	public const int MachineryPartFrom = 600;

	public const int MachineryPartTo = 699;

	public const int DrillFrom = 700;

	public const int DrillTo = 799;

	public const int DockingItemFrom = 800;

	public const int DockingItemTo = 899;

	public const int BatteryFrom = 900;

	public const int BatteryTo = 999;

	public const int CanisterFrom = 1000;

	public const int CanisterTo = 1099;

	public const int MeleeFrom = 1100;

	public const int MeleeTo = 1199;

	public const int GlowStickFrom = 1200;

	public const int GlowStickTo = 1299;

	public const int MedpackFrom = 1300;

	public const int MedpackTo = 1399;

	public const int HackingToolFrom = 1400;

	public const int HackingToolTo = 1499;

	public const int AsteroidScanningToolFrom = 1500;

	public const int AsteroidScanningToolTo = 1599;

	public const int LogItemFrom = 1600;

	public const int LogItemTo = 1699;

	public const int GenericItemFrom = 1700;

	public const int GenericItemTo = 1799;

	public const int GrenadeFrom = 1800;

	public const int GrenadeTo = 1899;

	public const int PortableTurretFrom = 1900;

	public const int PortableTurretTo = 1999;

	public const int RepairToolsFrom = 2000;

	public const int RepairToolsTo = 2099;

	public static bool IsWeapon(ItemType type)
	{
		return type is >= ItemType.AltairRifle and <= (ItemType)199;
	}

	public static bool IsAmmo(ItemType type)
	{
		return type is >= ItemType.AltairRifleAmmo and <= (ItemType)299;
	}

	public static bool IsOutfit(ItemType type)
	{
		return type is >= ItemType.AltairPressurisedSuit and <= (ItemType)399;
	}

	public static bool IsHelmet(ItemType type)
	{
		return type is >= ItemType.AltairPressurisedHelmet and <= (ItemType)499;
	}

	public static bool IsJetpack(ItemType type)
	{
		return type is >= ItemType.AltairPressurisedJetpack and <= (ItemType)599;
	}

	public static bool IsMachineryPart(ItemType type)
	{
		return type is >= ItemType.MachineryPart and <= (ItemType)699;
	}

	public static bool IsDrill(ItemType type)
	{
		return type is >= ItemType.AltairHandDrill and <= (ItemType)799;
	}

	public static bool IsDockingItem(ItemType type)
	{
		return type is >= ItemType.AltairDockingItem and <= (ItemType)899;
	}

	public static bool IsBattery(ItemType type)
	{
		return type is >= ItemType.AltairHandDrillBattery and <= (ItemType)999;
	}

	public static bool IsCanister(ItemType type)
	{
		return type is >= ItemType.AltairHandDrillCanister and <= (ItemType)1099;
	}

	public static bool IsMelee(ItemType type)
	{
		return type is >= ItemType.AltairCrowbar and <= (ItemType)1199;
	}

	public static bool IsGlowStick(ItemType type)
	{
		return type is >= ItemType.AltairGlowStick and <= (ItemType)1299;
	}

	public static bool IsMedpack(ItemType type)
	{
		return type is >= ItemType.AltairMedpackSmall and <= (ItemType)1399;
	}

	public static bool IsHackingTool(ItemType type)
	{
		return type is >= ItemType.AltairDisposableHackingTool and <= (ItemType)1499;
	}

	public static bool IsAsteroidScanningTool(ItemType type)
	{
		return type is >= ItemType.AltairHandheldAsteroidScanningTool and <= (ItemType)1599;
	}

	public static bool IsLogItem(ItemType type)
	{
		return type is >= ItemType.LogItem and <= (ItemType)1699;
	}

	public static bool IsGenericItem(ItemType type)
	{
		return type is >= ItemType.GenericItem and <= (ItemType)1799;
	}

	public static bool IsGrenade(ItemType type)
	{
		return type is >= ItemType.APGrenade and <= (ItemType)1899;
	}

	public static bool IsPortableTurret(ItemType type)
	{
		return type is >= ItemType.PortableTurret and <= (ItemType)1999;
	}

	public static bool IsRepairTool(ItemType type)
	{
		return type is >= ItemType.Welder and <= (ItemType)2099;
	}
}
