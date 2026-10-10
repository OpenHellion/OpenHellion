namespace ZeroGravity.Spawn;

public struct SpawnRange<T>
{
	public T Min;

	public T Max;

	public SpawnRange(T min, T max)
	{
		Min = min;
		Max = max;
	}
}
