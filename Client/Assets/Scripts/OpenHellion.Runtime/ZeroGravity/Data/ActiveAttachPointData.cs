using OpenHellion.State;

namespace ZeroGravity.Data
{
	public class ActiveAttachPointData : BaseAttachPointData
	{
		public override AttachPointType AttachPointType
		{
			get { return AttachPointType.Active; }
		}
	}
}
