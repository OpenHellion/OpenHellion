using OpenHellion.State;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;

namespace ZeroGravity.Objects;

public class Weapon : Item
{
	private ItemSlot magazineSlot;

	private List<WeaponModData> weaponMods = new List<WeaponModData>();

	private double lastShotTime;

	public float Damage
	{
		get
		{
			return CurrentMod.Damage;
		}
		set
		{
		}
	}

	public bool HasAmmo => Magazine is { HasAmmo: true };

	public Magazine Magazine => magazineSlot != null ? magazineSlot.Item as Magazine : null;

	public int CurrentModIndex
	{
		get => State.WeaponMod(Row);
		set => State.SetWeaponMod(Row, MathHelper.Clamp(value, 0, System.Math.Max(0, weaponMods.Count - 1)));
	}

	public WeaponModData CurrentMod => weaponMods[CurrentModIndex];

	public float ChargeAmount => 1f;

	public override async Task SetData(DynamicObjectAuxData data)
	{
		await base.SetData(data);
		WeaponData wd = data as WeaponData;
		weaponMods = ObjectCopier.DeepCopy(wd.weaponMods);
		foreach (WeaponModData wmod in weaponMods)
		{
			wmod.RateOfFire *= 0.95f;
		}
		if (wd.CurrentMod < 0)
		{
			CurrentModIndex = 0;
		}
		else
		{
			CurrentModIndex = wd.CurrentMod;
		}
		magazineSlot = Slots?.Values.FirstOrDefault(m => m.ItemTypes.Any(ItemTypeRange.IsAmmo));
	}

	public override DynamicObjectStats NewStats()
	{
		return new WeaponStats();
	}

	public override void FillStats(DynamicObjectStats stats, ItemChanges fields)
	{
		base.FillStats(stats, fields);
		if ((fields & ItemChanges.WeaponMod) != 0)
		{
			((WeaponStats)stats).CurrentMod = CurrentModIndex;
		}
	}

	public void ConsumePower(double amount)
	{
	}

	public async Task<bool> CanShoot()
	{
		if (Magazine.BulletCount > 0 && Server.Instance.SolarSystem.CurrentTime - lastShotTime > CurrentMod.RateOfFire)
		{
			await Magazine.ChangeQuantity(-1);
			lastShotTime = Server.Instance.SolarSystem.CurrentTime;
			return true;
		}
		return false;
	}

	public override PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataWeapon data = new PersistenceObjectDataWeapon();
		FillPersistenceData(data);
		data.WeaponData = new WeaponData();
		FillBaseAuxData(data.WeaponData);
		data.WeaponData.CurrentMod = CurrentModIndex;
		data.WeaponData.weaponMods = weaponMods;
		return data;
	}

	public override async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await base.LoadPersistenceData(persistenceData);
		if (persistenceData is not PersistenceObjectDataWeapon data)
		{
			Debug.LogWarning("PersistenceObjectDataWeapon data is null", GUID);
		}
		else
		{
			await SetData(data.WeaponData);
		}
	}
}
