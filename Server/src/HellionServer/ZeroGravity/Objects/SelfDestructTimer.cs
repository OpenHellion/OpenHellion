using ZeroGravity.Math;

namespace ZeroGravity.Objects;

public class SelfDestructTimer
{
	public class SelfDestructTimerData
	{
		public float SetTime;

		public double DestructionSolarSystemTime;
	}

	public double CheckPlayersDistance;

	private float setTime;

	private SpaceObjectVessel parentVessel;

	private double _DestructionSolarSystemTime;

	public double DestructionSolarSystemTime => _DestructionSolarSystemTime;

	public float Time
	{
		get
		{
			return MathHelper.Clamp((float)(DestructionSolarSystemTime - Server.SolarSystemTime), 0f, float.MaxValue);
		}
		set
		{
			setTime = value;
			Reset();
		}
	}

	public SelfDestructTimer(SpaceObjectVessel vessel, float time)
	{
		parentVessel = vessel;
		Time = time;
	}

	public SelfDestructTimer(SpaceObjectVessel vessel, SelfDestructTimerData data)
	{
		parentVessel = vessel;
		setTime = data.SetTime;
		_DestructionSolarSystemTime = data.DestructionSolarSystemTime;
	}

	public void Reset()
	{
		_DestructionSolarSystemTime = Server.SolarSystemTime + setTime;
	}

	public SelfDestructTimerData GetData()
	{
		return new SelfDestructTimerData
		{
			SetTime = setTime,
			DestructionSolarSystemTime = DestructionSolarSystemTime
		};
	}

	public void Update()
	{
		if (!(CheckPlayersDistance <= 0.0) && parentVessel != null)
		{
			double dist;
			Player pl = parentVessel.GetNearestPlayer(out dist);
			if (pl != null && dist < CheckPlayersDistance)
			{
				Reset();
			}
		}
	}
}
