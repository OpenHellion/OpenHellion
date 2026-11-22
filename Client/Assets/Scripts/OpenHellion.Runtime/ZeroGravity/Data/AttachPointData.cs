using OpenHellion.State;

namespace ZeroGravity.Data
{
	public class AttachPointData : BaseAttachPointData
	{
		public override AttachPointType AttachPointType
		{
			get { return AttachPointType.Simple; }
		}
	}
}
