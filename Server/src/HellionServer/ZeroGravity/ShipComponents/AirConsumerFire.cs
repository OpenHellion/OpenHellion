namespace ZeroGravity.ShipComponents;

public class AirConsumerFire : IAirConsumer
{
	public bool Persistent = false;

	private FireType _Type;

	private float _AirQualityDegradationRate;

	public float AirQualityDegradationRate => _AirQualityDegradationRate;

	public FireType Type
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
			case FireType.Small:
				_AirQualityDegradationRate = 0.5f;
				break;
			case FireType.Medium:
				_AirQualityDegradationRate = 2.5f;
				break;
			case FireType.Large:
				_AirQualityDegradationRate = 15f;
				break;
			}
		}
	}

	public float AirQuantityDecreaseRate => 0f;

	public bool AffectsQuality => true;

	public bool AffectsQuantity => false;

	public AirConsumerFire(FireType type)
	{
		Type = type;
	}
}
