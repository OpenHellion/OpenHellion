using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class AuthorizedVesselsResponse : NetworkData
{
	public long[] GUIDs;
}
