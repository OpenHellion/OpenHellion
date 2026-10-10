// AABBTests.cs
//
// Copyright (C) 2026, OpenHellion contributors
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
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using OpenHellion;
using ZeroGravity.Math;

namespace HellionServer.Tests;

public class AABBTests
{
	[Test]
	public void DefaultBoxIsEmpty()
	{
		var box = new AABB();

		Assert.Multiple(() =>
		{
			Assert.That(box.IsEmpty, Is.True);
			Assert.That(box.Center, Is.EqualTo(Vector3D.Zero));
			Assert.That(box.Size, Is.EqualTo(Vector3D.Zero));
			Assert.That(box.Contains(new Vector3D(0.0, 0.0, 0.0)), Is.False);
		});
	}

	[Test]
	public void CornerConstructorSortsAxes()
	{
		var box = new AABB(new Vector3D(4.0, -1.0, 6.0), new Vector3D(-2.0, 3.0, 0.0));

		Assert.Multiple(() =>
		{
			Assert.That(box.IsEmpty, Is.False);
			Assert.That(box.Min, Is.EqualTo(new Vector3D(-2.0, -1.0, 0.0)));
			Assert.That(box.Max, Is.EqualTo(new Vector3D(4.0, 3.0, 6.0)));
			Assert.That(box.Center, Is.EqualTo(new Vector3D(1.0, 1.0, 3.0)));
			Assert.That(box.Size, Is.EqualTo(new Vector3D(6.0, 4.0, 6.0)));
		});
	}

	[Test]
	public void ExtentsConstructorUsesAbsoluteExtents()
	{
		var box = new AABB(new Vector3D(1.0, 2.0, 3.0), new Vector3D(-1.0, 2.0, -3.0), true);

		Assert.Multiple(() =>
		{
			Assert.That(box.Min, Is.EqualTo(new Vector3D(0.0, 0.0, 0.0)));
			Assert.That(box.Max, Is.EqualTo(new Vector3D(2.0, 4.0, 6.0)));
		});
	}

	[Test]
	public void SizeConstructorBuildsCube()
	{
		var box = new AABB(new Vector3D(0.0, 0.0, 0.0), 2.0);

		Assert.Multiple(() =>
		{
			Assert.That(box.Min, Is.EqualTo(new Vector3D(-2.0, -2.0, -2.0)));
			Assert.That(box.Size, Is.EqualTo(new Vector3D(4.0, 4.0, 4.0)));
		});
	}

	[Test]
	public void SetReplacesBounds()
	{
		var box = new AABB();

		box.Set(new Vector3D(5.0, 5.0, 5.0), new Vector3D(1.0, 1.0, 1.0));

		Assert.Multiple(() =>
		{
			Assert.That(box.IsEmpty, Is.False);
			Assert.That(box.Min, Is.EqualTo(new Vector3D(1.0, 1.0, 1.0)));
			Assert.That(box.Max, Is.EqualTo(new Vector3D(5.0, 5.0, 5.0)));
		});

		box.Set(new Vector3D(0.0, 0.0, 0.0), 1.0);

		Assert.Multiple(() =>
		{
			Assert.That(box.Min, Is.EqualTo(new Vector3D(-1.0, -1.0, -1.0)));
			Assert.That(box.Max, Is.EqualTo(new Vector3D(1.0, 1.0, 1.0)));
		});
	}

	[Test]
	public void ContainsPointIsInclusiveOnBounds()
	{
		var box = new AABB(new Vector3D(0.0, 0.0, 0.0), new Vector3D(10.0, 10.0, 10.0));

		Assert.Multiple(() =>
		{
			Assert.That(box.Contains(new Vector3D(5.0, 5.0, 5.0)), Is.True);
			Assert.That(box.Contains(new Vector3D(0.0, 10.0, 0.0)), Is.True);
			Assert.That(box.Contains(new Vector3D(10.001, 5.0, 5.0)), Is.False);
			Assert.That(box.Contains(new Vector3D(5.0, -0.001, 5.0)), Is.False);
		});
	}

	[Test]
	public void ContainsBoxRequiresFullEnclosure()
	{
		var outer = new AABB(new Vector3D(0.0, 0.0, 0.0), new Vector3D(10.0, 10.0, 10.0));
		var inner = new AABB(new Vector3D(1.0, 1.0, 1.0), new Vector3D(9.0, 9.0, 9.0));
		var overlapping = new AABB(new Vector3D(5.0, 5.0, 5.0), new Vector3D(15.0, 5.0, 5.0));

		Assert.Multiple(() =>
		{
			Assert.That(outer.Contains(inner), Is.True);
			Assert.That(inner.Contains(outer), Is.False);
			Assert.That(outer.Contains(overlapping), Is.False);
			Assert.That(outer.Contains(new AABB()), Is.False);
			Assert.That(new AABB().Contains(outer), Is.False);
		});
	}

	[Test]
	public void UnionExpandsToCoverBothBoxes()
	{
		var box = new AABB(new Vector3D(0.0, 0.0, 0.0), new Vector3D(1.0, 1.0, 1.0));

		box.Union(new AABB(new Vector3D(-3.0, 0.0, 0.0), new Vector3D(0.0, 2.0, 0.0)));

		Assert.Multiple(() =>
		{
			Assert.That(box.Min, Is.EqualTo(new Vector3D(-3.0, 0.0, 0.0)));
			Assert.That(box.Max, Is.EqualTo(new Vector3D(1.0, 2.0, 1.0)));
		});
	}

	[Test]
	public void UnionWithEmptyLeavesBoxUnchanged()
	{
		var box = new AABB(new Vector3D(0.0, 0.0, 0.0), new Vector3D(1.0, 1.0, 1.0));

		box.Union(new AABB());
		box.Union(null);

		Assert.Multiple(() =>
		{
			Assert.That(box.Min, Is.EqualTo(new Vector3D(0.0, 0.0, 0.0)));
			Assert.That(box.Max, Is.EqualTo(new Vector3D(1.0, 1.0, 1.0)));
		});
	}

	[Test]
	public void UnionOntoEmptyAdoptsOtherBounds()
	{
		var box = new AABB();

		box.Union(new AABB(new Vector3D(2.0, 2.0, 2.0), new Vector3D(4.0, 4.0, 4.0)));

		Assert.Multiple(() =>
		{
			Assert.That(box.IsEmpty, Is.False);
			Assert.That(box.Min, Is.EqualTo(new Vector3D(2.0, 2.0, 2.0)));
			Assert.That(box.Max, Is.EqualTo(new Vector3D(4.0, 4.0, 4.0)));
		});
	}

	[Test]
	public void AdditionOperatorDoesNotMutateOperands()
	{
		var a = new AABB(new Vector3D(0.0, 0.0, 0.0), new Vector3D(1.0, 1.0, 1.0));
		var b = new AABB(new Vector3D(-1.0, -1.0, -1.0), new Vector3D(0.0, 0.0, 0.0));

		var sum = a + b;

		Assert.Multiple(() =>
		{
			Assert.That(sum.Min, Is.EqualTo(new Vector3D(-1.0, -1.0, -1.0)));
			Assert.That(sum.Max, Is.EqualTo(new Vector3D(1.0, 1.0, 1.0)));
			Assert.That(a.Min, Is.EqualTo(new Vector3D(0.0, 0.0, 0.0)));
			Assert.That(b.Max, Is.EqualTo(new Vector3D(0.0, 0.0, 0.0)));
		});
	}

	[Test]
	public void AdditionOperatorPassesThroughEmptyOperands()
	{
		var box = new AABB(new Vector3D(0.0, 0.0, 0.0), new Vector3D(1.0, 1.0, 1.0));

		Assert.Multiple(() =>
		{
			Assert.That((new AABB() + box).Max, Is.EqualTo(box.Max));
			Assert.That((box + new AABB()).Min, Is.EqualTo(box.Min));
			Assert.That((new AABB() + new AABB()).IsEmpty, Is.True);
		});
	}
}
