using System.Collections.Generic;
using System.Threading.Tasks;
using ZeroGravity.Data;

namespace ZeroGravity.Objects;

public interface IDamageable
{
	float MaxHealth { get; set; }

	float Health { get; set; }

	float Armor { get; set; }

	bool Damageable { get; }

	bool Repairable { get; }

	Task TakeDamage(Dictionary<TypeOfDamage, float> damages, bool forceTakeDamage = false);
}
