using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class SubSystemRCS : SubSystem
{
	private double lastSwitchOnTime;

	private float accelerationFactor = 1f;

	private float _Acceleration;

	private float _RotationAcceleration;

	private float _RotationStabilization;

	private float _MaxOperationRate = 1f;

	public float Acceleration => _Acceleration * accelerationFactor;

	public float RotationAcceleration => _RotationAcceleration * accelerationFactor;

	public float RotationStabilization => _RotationStabilization * accelerationFactor;

	public override SubSystemType Type => SubSystemType.RCS;

	public override float PowerUpTime => 0f;

	public override float CoolDownTime => 0f;

	public override bool AutoReactivate => false;

	public float MaxOperationRate
	{
		get
		{
			return _MaxOperationRate;
		}
		set
		{
			if (_MaxOperationRate != value)
			{
				StatusChanged = true;
			}
			_MaxOperationRate = value;
		}
	}

	public override SystemStatus Status
	{
		get
		{
			if (Server.Instance.RunTime.TotalSeconds - lastSwitchOnTime > 1.0 && _Status != SystemStatus.OffLine)
			{
				SetStatus(SystemStatus.OffLine);
			}
			return base.Status;
		}
	}

	protected override async Task SetStatusAsync(SystemStatus status)
	{
		if (status == SystemStatus.OnLine)
		{
			lastSwitchOnTime = Server.Instance.RunTime.TotalSeconds;
		}
		await base.SetStatusAsync(status);
	}

	protected override void SetStatus(SystemStatus status)
	{
		if (status == SystemStatus.OnLine)
		{
			lastSwitchOnTime = Server.Instance.RunTime.TotalSeconds;
		}
		base.SetStatus(status);
	}

	public override async Task Update(double duration)
	{
		await base.Update(duration);
		accelerationFactor = GetScopeMultiplier(MachineryPartSlotScope.Output);
	}

	public SubSystemRCS(SpaceObjectVessel vessel, VesselObjectID id, SubSystemData ssData)
		: base(vessel, id, ssData)
	{
	}

	public override void SetAuxData(SystemAuxData auxData)
	{
		SubSystemRCSAuxData aux = auxData as SubSystemRCSAuxData;
		_Acceleration = aux.Acceleration;
		_RotationAcceleration = aux.RotationAcceleration;
		_RotationStabilization = aux.RotationStabilization;
	}

	public override IAuxDetails GetAuxDetails()
	{
		return new RCSAuxDetails
		{
			MaxOperationRate = MaxOperationRate
		};
	}
}
