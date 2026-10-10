using System.Collections.Generic;

namespace ZeroGravity.Data;

public class WeaponData : DynamicObjectAuxData
{
	public int CurrentMod;

	public List<WeaponModData> weaponMods;
}
