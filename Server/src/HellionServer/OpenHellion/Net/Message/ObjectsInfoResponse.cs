// ObjectsInfoResponse.cs
//
// Copyright (C) 2026, OpenHellion contributors
//
// SPDX-License-Identifier: GPL-3.0-or-later
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using ProtoBuf;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace OpenHellion.Net.Message;

/// <summary>
/// 	See also <seealso cref="ObjectsInfoRequest"/>.
/// </summary>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ObjectsInfoResponse : NetworkData
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public struct ShipData
	{
		public long Guid;

		public float[] Position;

		public float[] Rotation;

		public string VesselRegistration;

		public string VesselName;

		public string Tag;

		public GameScenes.SceneId SceneId;

		public float[] CollidersCenterOffset;

		public bool IsDebrisFragment;

		public double RadarSignature;

		public bool IsDistressSignalActive;

		public bool IsAlwaysVisible;

		public bool DockingControlsDisabled;

		public bool SecurityPanelsLocked;

		public VesselObjects VesselObjects;

		public DockedVesselData[] DockedVessels;
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public struct AsteroidData
	{
		public long Guid;

		public float[] Position;

		public float[] Rotation;

		public double Radius;

		public string VesselRegistration;

		public string VesselName;

		public string Tag;

		public GameScenes.SceneId SceneId;

		public bool IsDebrisFragment;

		public bool IsAlwaysVisible;

		public AsteroidMiningPointDetails[] MiningPoints;
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public struct PivotData
	{
		public long Guid;

		public float[] Position;

		public float[] Rotation;

		public SpaceObjectType PivotType;
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public struct CorpseData
	{
		public long Guid;

		public float[] Position;

		public float[] Rotation;

		public long ParentGUID;

		public Gender Gender;

		public DynamicObjectDetails[] DynamicObjects;
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public struct PlayerData
	{
		public long Guid;

		public float[] Position;

		public float[] Rotation;

		public long ParentId;

		public string PlayerId;

		public int SpawnPointId;

		public Gender Gender;

		public byte HeadType;

		public byte HairType;

		public string Name;

		public DynamicObjectDetails[] DynamicObjects;

		public int AnimationStatsMask;

		public VesselObjectID LockedToTriggerID;
	}

	public ShipData[] ShipObjects;

	public AsteroidData[] AsteroidObjects;

	public PivotData[] PivotObjects;

	public DynamicObjectDetails[] DynamicObjects;

	public CorpseData[] CorpseObjects;

	public PlayerData[] Players;
}
