namespace ZeroGravity.ShipComponents;

public class StructureConnection
{
	public enum ConnectionType
	{
		AltCorp_Connection1 = 100,
		ScienceCorp_Connection1 = 200,
		ScienceCorp_Connection2 = 201
	}

	public ConnectionType Type;

	public bool IsFemale;
}
