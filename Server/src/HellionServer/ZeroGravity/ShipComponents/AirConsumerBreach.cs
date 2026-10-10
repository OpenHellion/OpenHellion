namespace ZeroGravity.ShipComponents;

public class AirConsumerBreach : IAirConsumer
{
	private BreachType _Type;

	private float _VolumeDecreaseRate;

	public float AirQualityDegradationRate => 0f;

	public BreachType Type
	{
		get
		{
			return _Type;
		}
		set
		{
			_Type = value;
			switch (value)
			{
			case BreachType.Micro:
				_VolumeDecreaseRate = 0.15f;
				break;
			case BreachType.Small:
				_VolumeDecreaseRate = 1.5f;
				break;
			case BreachType.Large:
				_VolumeDecreaseRate = 150f;
				break;
			}
		}
	}

	public float AirQuantityDecreaseRate => _VolumeDecreaseRate;

	public bool AffectsQuality => false;

	public bool AffectsQuantity => true;

	public AirConsumerBreach(BreachType type)
	{
		Type = type;
	}
}
