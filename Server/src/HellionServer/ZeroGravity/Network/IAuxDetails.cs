using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
[ProtoInclude(1001, typeof(MachineryPartSlotAuxDetails))]
[ProtoInclude(1002, typeof(GeneratorCapacitorAuxDetails))]
[ProtoInclude(1003, typeof(RefineryAuxDetails))]
[ProtoInclude(1004, typeof(FTLAuxDetails))]
[ProtoInclude(1005, typeof(GeneratorSolarAuxDetails))]
[ProtoInclude(1006, typeof(RCSAuxDetails))]
[ProtoInclude(1007, typeof(AirTankAuxDetails))]
[ProtoInclude(1008, typeof(FabricatorAuxDetails))]
public interface IAuxDetails
{
}
