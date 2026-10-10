namespace ZeroGravity.ShipComponents;

public interface IAirConsumer
{
	float AirQualityDegradationRate { get; }

	float AirQuantityDecreaseRate { get; }

	bool AffectsQuality { get; }

	bool AffectsQuantity { get; }
}
