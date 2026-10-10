using System.Threading.Tasks;

namespace ZeroGravity;

public interface IPersistantObject
{
	PersistenceObjectData GetPersistenceData();

	Task LoadPersistenceData(PersistenceObjectData persistenceData);
}
