// AABB.cs
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

using ZeroGravity.Math;

namespace OpenHellion;

/// <summary>
/// 	Axis-Aligned Bounding Box in 3D.
/// </summary>
/// <remarks>
/// 	Represents an inclusive AABB with Min <= Max on each axis.
/// 	An empty box is represented by Min = +Infinity, Max = -Infinity.
/// </remarks>
public class AABB
{
	public Vector3D Min { get; private set; }
	public Vector3D Max { get; private set; }

	public AABB()
	{
		Min = new Vector3D(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
		Max = new Vector3D(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);
	}

	public AABB(Vector3D cornerA, Vector3D cornerB)
	{
		Min = Vector3D.Min(cornerA, cornerB);
		Max = Vector3D.Max(cornerA, cornerB);
	}

	public AABB(Vector3D centre, Vector3D extents, bool _)
	{
		var e = Vector3D.Abs(extents);
		Min = centre - e;
		Max = centre + e;
	}

	public AABB(Vector3D centre, double size)
	{
		var e = new Vector3D(size, size, size);
		Min = centre - e;
		Max = centre + e;
	}

	public bool IsEmpty
	{
		get
		{
			return Min.X > Max.X || Min.Y > Max.Y || Min.Z > Max.Z;
		}
	}

	public Vector3D Center
	{
		get
		{
			if (IsEmpty) return Vector3D.Zero;
			return (Min + Max) * 0.5f;
		}
	}

	public Vector3D Size
	{
		get
		{
			if (IsEmpty) return Vector3D.Zero;
			return Max - Min;
		}
	}

	public void Set(Vector3D min, Vector3D max)
	{
		Min = Vector3D.Min(min, max);
		Max = Vector3D.Max(min, max);
	}

	public void Set(Vector3D centre, Vector3D extents, bool _)
	{

		var e = Vector3D.Abs(extents);
		Min = centre - e;
		Max = centre + e;
	}

	public void Set(Vector3D centre, double size)
	{
		var e = new Vector3D(size, size, size);
		Min = centre - e;
		Max = centre + e;
	}

	public bool Contains(Vector3D point)
	{
		if (IsEmpty) return false;
		return point.X >= Min.X && point.X <= Max.X
			&& point.Y >= Min.Y && point.Y <= Max.Y
			&& point.Z >= Min.Z && point.Z <= Max.Z;
	}

	public bool Contains(AABB other)
	{
		if (IsEmpty || other == null || other.IsEmpty) return false;
		return other.Min.X >= Min.X && other.Max.X <= Max.X
			&& other.Min.Y >= Min.Y && other.Max.Y <= Max.Y
			&& other.Min.Z >= Min.Z && other.Max.Z <= Max.Z;
	}

	public void Union(AABB other)
	{
		if (other == null || other.IsEmpty) return;
		if (IsEmpty)
		{
			Min = other.Min;
			Max = other.Max;
			return;
		}
		Min = Vector3D.Min(Min, other.Min);
		Max = Vector3D.Max(Max, other.Max);
	}

	public static AABB operator +(AABB a, AABB b)
	{
		if (a.IsEmpty) return b;
		if (b.IsEmpty) return a;
		return new AABB(Vector3D.Min(a.Min, b.Min), Vector3D.Max(a.Max, b.Max));
	}
}
