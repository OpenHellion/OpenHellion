using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class SubSystemEngine : SubSystem
{
	private float _Acceleration;

	private float _AccelerationBuildup;

	private float _ReverseAcceleration;

	private float accelerationFactor = 1f;

	private float accelerationBuildupFactor = 1f;

	public bool ThrustActive;

	public bool ReverseThrust;

	public float RequiredThrust;

	public float Acceleration => _Acceleration * accelerationFactor;

	public float ReverseAcceleration => _ReverseAcceleration * accelerationFactor;

	public float AccelerationBuildup => _AccelerationBuildup * accelerationBuildupFactor;

	public override bool AutoReactivate => false;

	public override SubSystemType Type => SubSystemType.Engine;

	public SubSystemEngine(SpaceObjectVessel vessel, VesselObjectID id, SubSystemData ssData)
		: base(vessel, id, ssData)
	{
	}

	public override async Task Update(double duration)
	{
		await base.Update(duration);
		if (ThrustActive && RequiredThrust != 0f)
		{
			if (OperationRate != RequiredThrust)
			{
				OperationRate = RequiredThrust;
			}
		}
		else
		{
			OperationRate = 0f;
		}
		accelerationFactor = GetScopeMultiplier(MachineryPartSlotScope.Output);
	}

	public override void SetAuxData(SystemAuxData auxData)
	{
		SubSystemEngineAuxData aux = auxData as SubSystemEngineAuxData;
		_Acceleration = aux.Acceleration;
		_ReverseAcceleration = aux.ReverseAcceleration;
		_AccelerationBuildup = aux.AccelerationBuildup;
	}
}
