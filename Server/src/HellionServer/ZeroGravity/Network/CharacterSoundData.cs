using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class CharacterSoundData
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class SoundEventParams
	{
		public bool? State;

		public RTPCParameter[] RTPCParams;

		public SwitchCombo[] SwitchCombos;
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class SwitchCombo
	{
		public byte? SwitchGroup;

		public byte? SwitchItem;

		public SwitchCombo()
		{
		}

		public SwitchCombo(byte swGroup, byte swItem)
		{
			SwitchGroup = swGroup;
			SwitchItem = swItem;
		}
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class RTPCParameter
	{
		public byte? ParamName;

		public float? ParamValue;

		public RTPCParameter()
		{
		}

		public RTPCParameter(byte paramName, float paramValue)
		{
			ParamName = paramName;
			ParamValue = paramValue;
		}
	}

	public enum SoundEvents : byte
	{
		Drop = 1,
		Pickup,
		SwitchStance,
		JetpackToggle,
		OxygenSupply
	}

	public Dictionary<SoundEvents, SoundEventParams> SoundEventData = new Dictionary<SoundEvents, SoundEventParams>();

	public void AddCharacterSoundData(SoundEvents eventID, SoundEventParams param)
	{
		if (eventID != 0)
		{
			SoundEventData.Add(eventID, param);
		}
	}
}
