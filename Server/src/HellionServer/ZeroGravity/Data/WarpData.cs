using System;

namespace ZeroGravity.Data;

[Serializable]
public class WarpData : ISceneData
{
	public long MinAcceleration;

	public long MaxAcceleration;

	public float ActivationCellConsumption;

	public float CellConsumption;

	public float PowerConsumption;

	public float EndPositionDeviation;
}
