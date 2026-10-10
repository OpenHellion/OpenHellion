using System.Threading.Tasks;

namespace ZeroGravity.Objects;

public interface IUpdateable
{
	Task Update(double deltaTime);
}
