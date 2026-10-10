namespace ZeroGravity.Objects;

public interface IBatteryConsumer
{
	ItemSlot BatterySlot { get; set; }

	Battery Battery { get; }

	float BatteryPower { get; }
}
