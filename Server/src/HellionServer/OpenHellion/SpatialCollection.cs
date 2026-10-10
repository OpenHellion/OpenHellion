// SpatialCollection.cs
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ZeroGravity.Math;

namespace OpenHellion;

/// <summary>
///		A spatial index designed for efficient querying of objects in 3D space. Adapted for double precision to allow indexing for the whole world of Hellion.
/// 	Built for multiple thread access.
/// </summary>
/// <remarks>
/// 	Every value is stored in a flat id-keyed registry so it can be retrieved directly by id. In addition, values
/// 	added with a position are spatially indexed, so they can be queried by location. The collection
/// 	owns the authoritative cell of every spatially indexed value (see <c>_index</c>), so callers never have to supply a
/// 	previous position when moving or removing an object. This is what prevents objects from being "lost" when the
/// 	caller's idea of the old position disagrees with where the object is actually indexed.
///	</remarks>
///
/// <typeparam name="TValue">Type of object to store.</typeparam>
public class SpatialCollection<TValue>
{
	private readonly struct Cell : IEquatable<Cell>
	{
		public readonly int X;
		public readonly int Y;
		public readonly int Z;

		public Cell(Vector3D worldPosition, double inverseCellSize)
		{
			X = (int)Math.Floor(worldPosition.X * inverseCellSize);
			Y = (int)Math.Floor(worldPosition.Y * inverseCellSize);
			Z = (int)Math.Floor(worldPosition.Z * inverseCellSize);
		}

		public Cell(int cellX, int cellY, int cellZ)
		{
			X = cellX;
			Y = cellY;
			Z = cellZ;
		}

		public bool Equals(Cell other) => X == other.X && Y == other.Y && Z == other.Z;

		public override bool Equals(object obj) => obj is Cell other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(X, Y, Z);

		public static bool operator ==(Cell lhs, Cell rhs) => lhs.Equals(rhs);

		public static bool operator !=(Cell lhs, Cell rhs) => !lhs.Equals(rhs);
	}

	private readonly int _cellSize;
	private readonly double _inverseCellSize;

	private readonly ConcurrentDictionary<Cell, ConcurrentDictionary<long, Vector3D>> _grid;

	private readonly ConcurrentDictionary<long, TValue> _storage;

	// Authoritative mapping of id -> the cell the object is currently indexed in. Only contains spatially indexed
	// objects (those added with a position). This is what lets us move/remove without trusting a caller-supplied
	// old position.
	private readonly ConcurrentDictionary<long, Cell> _index;

	public SpatialCollection(int cellSize)
	{
		_cellSize = cellSize;
		_inverseCellSize = 1.0 / cellSize;
		_grid = [];
		_storage = [];
		_index = [];
	}

	public ICollection<TValue> Values => _storage.Values;

	public int Count => _storage.Count;

	/// <summary>
	/// 	Add a value to the spatial collection and index it by position.
	/// </summary>
	/// <param name="id">Id of the object to use as key when storing.</param>
	/// <param name="value">Value to add to collection.</param>
	/// <param name="position">Position to index with.</param>
	/// <returns>If object was added successfully.</returns>
	public bool TryAdd(long id, TValue value, Vector3D position)
	{
		if (!_storage.TryAdd(id, value))
		{
			return false;
		}

		Cell cell = new Cell(position, _inverseCellSize);
		_grid.GetOrAdd(cell, static _ => new ConcurrentDictionary<long, Vector3D>())[id] = position;
		_index[id] = cell;
		return true;
	}

	/// <summary>
	/// 	Add a value to the collection without spatially indexing it. The value can still be looked up by id, but it
	/// 	will not be returned by any of the spatial queries. Use this for objects whose absolute world position is
	/// 	derived from a parent (and therefore changes without an explicit update).
	/// </summary>
	/// <param name="id">Id of the object to use as key when storing.</param>
	/// <param name="value">Value to add to collection.</param>
	/// <returns>If object was added successfully.</returns>
	public bool TryAdd(long id, TValue value)
	{
		return _storage.TryAdd(id, value);
	}

	/// <summary>
	///		Get a value without a position.
	/// </summary>
	/// <param name="id">Id for the value.</param>
	/// <param name="value">Outputs the value if found.</param>
	/// <returns>If value was found in collection.</returns>
	public bool TryGet(long id, out TValue value)
	{
		return _storage.TryGetValue(id, out value);
	}

	/// <summary>
	/// 	Checks if an object with this id is stored in this collection.
	/// </summary>
	/// <param name="id">The id to check.</param>
	/// <returns>If an object with this key could be found.</returns>
	public bool Contains(long id)
	{
		return _storage.ContainsKey(id);
	}

	/// <summary>
	/// 	Attempt to remove an object from this collection by its id. The object's indexed cell is looked up
	/// 	internally, so no position needs to be supplied.
	/// </summary>
	/// <param name="id">Id of object to remove.</param>
	/// <param name="value">Value of the object removed.</param>
	/// <returns>If object was removed successfully.</returns>
	public bool TryRemove(long id, out TValue value)
	{
		if (!_storage.TryRemove(id, out value))
		{
			return false;
		}

		if (_index.TryRemove(id, out Cell cell) && _grid.TryGetValue(cell, out ConcurrentDictionary<long, Vector3D> cellContents))
		{
			cellContents.TryRemove(id, out _);
			if (cellContents.IsEmpty)
			{
				_grid.TryRemove(new KeyValuePair<Cell, ConcurrentDictionary<long, Vector3D>>(cell, cellContents));
			}
		}

		return true;
	}

	/// <summary>
	/// 	Search for objects within a bounding box.
	/// </summary>
	/// <param name="boundingBox">Dimensions of bounding box to search for objects within.</param>
	/// <param name="ignoreId">Id of object to ignore, usually the object calling this method.</param>
	/// <returns>List of objects found within the bounding box.</returns>
	public List<TValue> QueryAABB(AABB boundingBox, long ignoreId)
	{
		Cell minCell = new Cell(boundingBox.Min, _inverseCellSize);
		Cell maxCell = new Cell(boundingBox.Max, _inverseCellSize);

		List<TValue> results = [];
		for (int x = minCell.X; x <= maxCell.X; x++)
		{
			for (int y = minCell.Y; y <= maxCell.Y; y++)
			{
				for (int z = minCell.Z; z <= maxCell.Z; z++)
				{
					if (_grid.TryGetValue(new Cell(x, y, z), out ConcurrentDictionary<long, Vector3D> cellContents))
					{
						foreach ((long key, Vector3D pos) in cellContents)
						{
							if (key == ignoreId || !boundingBox.Contains(pos))
							{
								continue;
							}

							if (_storage.TryGetValue(key, out TValue output))
							{
								results.Add(output);
							}
						}
					}
				}
			}
		}

		return results;
	}

	/// <summary>
	/// 	Search for objects within a sphere.
	/// </summary>
	/// <param name="centre">Centre of sphere to search.</param>
	/// <param name="radius">Extents of the sphere.</param>
	/// <param name="ignoreId">Id of object to ignore, usually the object calling this method.</param>
	/// <returns>List of objects found within the sphere.</returns>
	public List<TValue> QueryRadius(Vector3D centre, double radius, long? ignoreId = null)
	{
		List<TValue> results = [];
		if (radius == 0)
		{
			return results;
		}

		int extent = (int)Math.Ceiling(Math.Abs(radius) / _cellSize);
		Cell centerCell = new Cell(centre, _inverseCellSize);
		double radiusSquared = radius * radius;

		for (int dx = -extent; dx <= extent; dx++)
		{
			for (int dy = -extent; dy <= extent; dy++)
			{
				for (int dz = -extent; dz <= extent; dz++)
				{
					Cell cell = new Cell(centerCell.X + dx, centerCell.Y + dy, centerCell.Z + dz);
					if (_grid.TryGetValue(cell, out ConcurrentDictionary<long, Vector3D> cellContents))
					{
						foreach ((long id, Vector3D pos) in cellContents)
						{
							if ((ignoreId.HasValue && id == ignoreId.Value) || Vector3D.DistanceSquared(centre, pos) > radiusSquared)
							{
								continue;
							}

							if (_storage.TryGetValue(id, out TValue output))
							{
								results.Add(output);
							}
						}
					}
				}
			}
		}

		return results;
	}

	/// <summary>
	/// 	Search for objects of a specific subtype within a sphere. Filters while scanning the
	/// 	grid, so no intermediate list of mismatching objects is built.
	/// </summary>
	/// <param name="centre">Centre of sphere to search.</param>
	/// <param name="radius">Extents of the sphere.</param>
	/// <param name="ignoreId">Id of object to ignore, usually the object calling this method.</param>
	/// <returns>List of objects of type <typeparamref name="TFilter"/> found within the sphere.</returns>
	public List<TFilter> QueryRadius<TFilter>(Vector3D centre, double radius, long? ignoreId = null) where TFilter : class
	{
		List<TFilter> results = [];
		if (radius == 0)
		{
			return results;
		}

		int extent = (int)Math.Ceiling(Math.Abs(radius) / _cellSize);
		Cell centerCell = new Cell(centre, _inverseCellSize);
		double radiusSquared = radius * radius;

		for (int dx = -extent; dx <= extent; dx++)
		{
			for (int dy = -extent; dy <= extent; dy++)
			{
				for (int dz = -extent; dz <= extent; dz++)
				{
					Cell cell = new Cell(centerCell.X + dx, centerCell.Y + dy, centerCell.Z + dz);
					if (_grid.TryGetValue(cell, out ConcurrentDictionary<long, Vector3D> cellContents))
					{
						foreach ((long id, Vector3D pos) in cellContents)
						{
							if ((ignoreId.HasValue && id == ignoreId.Value) || Vector3D.DistanceSquared(centre, pos) > radiusSquared)
							{
								continue;
							}

							if (_storage.TryGetValue(id, out TValue output) && output is TFilter typed)
							{
								results.Add(typed);
							}
						}
					}
				}
			}
		}

		return results;
	}

	/// <summary>
	/// 	Find the spatially indexed value closest to a position.
	/// </summary>
	/// <param name="position">Position to search around.</param>
	/// <param name="radius">Maximum search radius in world units.</param>
	/// <returns>The nearest value, or <c>default</c> if the collection holds no spatially indexed objects nearby.</returns>
	public TValue FindNearestNeighbour(Vector3D position, double radius)
	{
		Cell centerCell = new Cell(position, _inverseCellSize);

		long? nearestId = null;
		double nearestDistanceSquared = radius * radius;

		int maxExtent = (int)Math.Ceiling(radius * _inverseCellSize);
		for (int extent = 0; extent <= maxExtent; extent++)
		{
			if (extent == 0)
			{
				ScanCell(0, 0, 0);
			}
			else
			{
				// Enumerate only the shell cells at Chebyshev distance == extent by iterating each
				// pair of opposite faces, trimming edges and corners already covered by earlier faces.
				for (int dx = -extent; dx <= extent; dx++)
					for (int dy = -extent; dy <= extent; dy++)
					{
						ScanCell(dx, dy, -extent);
						ScanCell(dx, dy,  extent);
					}
				for (int dx = -extent; dx <= extent; dx++)
					for (int dz = -extent + 1; dz <= extent - 1; dz++)
					{
						ScanCell(dx, -extent, dz);
						ScanCell(dx,  extent, dz);
					}
				for (int dy = -extent + 1; dy <= extent - 1; dy++)
					for (int dz = -extent + 1; dz <= extent - 1; dz++)
					{
						ScanCell(-extent, dy, dz);
						ScanCell( extent, dy, dz);
					}
			}

			// Any point in the next shell is at least extent * cellSize away. Once our best is within
			// that bound, nothing further out can be closer.
			if (nearestId.HasValue)
			{
				double safeDistance = (double)extent * _cellSize;
				if (nearestDistanceSquared <= safeDistance * safeDistance)
				{
					break;
				}
			}
		}

		if (nearestId.HasValue && _storage.TryGetValue(nearestId.Value, out TValue output))
		{
			return output;
		}

		return default;

		void ScanCell(int dx, int dy, int dz)
		{
			Cell cell = new Cell(centerCell.X + dx, centerCell.Y + dy, centerCell.Z + dz);
			if (!_grid.TryGetValue(cell, out ConcurrentDictionary<long, Vector3D> cellContents))
			{
				return;
			}

			foreach ((long id, Vector3D pos) in cellContents)
			{
				double d2 = Vector3D.DistanceSquared(position, pos);
				if (d2 < nearestDistanceSquared)
				{
					nearestId = id;
					nearestDistanceSquared = d2;
				}
			}
		}
	}

	/// <summary>
	/// 	Find the spatially indexed value closest to a position, restricted to a specific subtype.
	/// 	Only performs a storage lookup when a candidate beats the current typed best, keeping
	/// 	dictionary access proportional to improvements rather than total candidates scanned.
	/// </summary>
	/// <param name="position">Position to search around.</param>
	/// <param name="radius">Maximum search radius in world units.</param>
	/// <returns>The nearest value of type <typeparamref name="TFilter"/>, or <c>null</c> if none found nearby.</returns>
	public TFilter FindNearestNeighbour<TFilter>(Vector3D position, double radius) where TFilter : class
	{
		Cell centerCell = new Cell(position, _inverseCellSize);

		long? nearestId = null;
		double nearestDistanceSquared = radius * radius;

		int maxExtent = (int)Math.Ceiling(radius * _inverseCellSize);
		for (int extent = 0; extent <= maxExtent; extent++)
		{
			if (extent == 0)
			{
				ScanCell(0, 0, 0);
			}
			else
			{
				for (int dx = -extent; dx <= extent; dx++)
					for (int dy = -extent; dy <= extent; dy++)
					{
						ScanCell(dx, dy, -extent);
						ScanCell(dx, dy,  extent);
					}
				for (int dx = -extent; dx <= extent; dx++)
					for (int dz = -extent + 1; dz <= extent - 1; dz++)
					{
						ScanCell(dx, -extent, dz);
						ScanCell(dx,  extent, dz);
					}
				for (int dy = -extent + 1; dy <= extent - 1; dy++)
					for (int dz = -extent + 1; dz <= extent - 1; dz++)
					{
						ScanCell(-extent, dy, dz);
						ScanCell( extent, dy, dz);
					}
			}

			if (nearestId.HasValue)
			{
				double safeDistance = (double)extent * _cellSize;
				if (nearestDistanceSquared <= safeDistance * safeDistance)
				{
					break;
				}
			}
		}

		if (nearestId.HasValue && _storage.TryGetValue(nearestId.Value, out TValue output) && output is TFilter result)
		{
			return result;
		}

		return default;

		void ScanCell(int dx, int dy, int dz)
		{
			Cell cell = new Cell(centerCell.X + dx, centerCell.Y + dy, centerCell.Z + dz);
			if (!_grid.TryGetValue(cell, out ConcurrentDictionary<long, Vector3D> cellContents))
			{
				return;
			}

			foreach ((long id, Vector3D pos) in cellContents)
			{
				double d2 = Vector3D.DistanceSquared(position, pos);
				if (d2 >= nearestDistanceSquared)
				{
					continue;
				}

				if (_storage.TryGetValue(id, out TValue value) && value is TFilter)
				{
					nearestId = id;
					nearestDistanceSquared = d2;
				}
			}
		}
	}

	public bool AnyPointInRadius(Vector3D centre, double radius)
	{
		if (radius == 0)
		{
			return false;
		}

		int extent = (int)Math.Ceiling(Math.Abs(radius) / _cellSize);
		Cell centerCell = new Cell(centre, _inverseCellSize);
		double radiusSquared = radius * radius;

		for (int dx = -extent; dx <= extent; dx++)
		{
			for (int dy = -extent; dy <= extent; dy++)
			{
				for (int dz = -extent; dz <= extent; dz++)
				{
					Cell cell = new Cell(centerCell.X + dx, centerCell.Y + dy, centerCell.Z + dz);
					if (_grid.TryGetValue(cell, out ConcurrentDictionary<long, Vector3D> cellContents))
					{
						foreach ((long id, Vector3D pos) in cellContents)
						{
							if (Vector3D.DistanceSquared(pos, centre) <= radiusSquared)
							{
								return true;
							}
						}
					}
				}
			}
		}

		return false;
	}

	/// <summary>
	/// 	Spatially index a stored value at the given position, or move it if it is already indexed.
	/// 	Ids without a stored value are ignored.
	/// </summary>
	/// <param name="id">Id of the value to index.</param>
	/// <param name="position">Position to index at.</param>
	public void SetPosition(long id, Vector3D position)
	{
		if (!_storage.ContainsKey(id))
		{
			return;
		}

		Cell newCell = new Cell(position, _inverseCellSize);

		if (_index.TryGetValue(id, out Cell oldCell))
		{
			if (oldCell == newCell)
			{
				// Still in the same cell, but refresh the stored position so spatial queries stay accurate.
				if (_grid.TryGetValue(oldCell, out ConcurrentDictionary<long, Vector3D> sameCell))
				{
					sameCell[id] = position;
				}
				return;
			}

			if (_grid.TryGetValue(oldCell, out ConcurrentDictionary<long, Vector3D> oldSet))
			{
				oldSet.TryRemove(id, out _);
				if (oldSet.IsEmpty)
				{
					_grid.TryRemove(new KeyValuePair<Cell, ConcurrentDictionary<long, Vector3D>>(oldCell, oldSet));
				}
			}
		}

		_grid.GetOrAdd(newCell, static _ => new ConcurrentDictionary<long, Vector3D>())[id] = position;
		_index[id] = newCell;
	}

	/// <summary>
	/// 	Remove a value from the spatial index while keeping it retrievable by id. Spatial queries
	/// 	no longer return it. Values that were never indexed are ignored.
	/// </summary>
	/// <param name="id">Id of the value to remove from the index.</param>
	public void ClearPosition(long id)
	{
		if (_index.TryRemove(id, out Cell cell) && _grid.TryGetValue(cell, out ConcurrentDictionary<long, Vector3D> cellContents))
		{
			cellContents.TryRemove(id, out _);
			if (cellContents.IsEmpty)
			{
				_grid.TryRemove(new KeyValuePair<Cell, ConcurrentDictionary<long, Vector3D>>(cell, cellContents));
			}
		}
	}

	public void Clear()
	{
		_storage.Clear();
		_grid.Clear();
		_index.Clear();
	}
}
