// HiResTime.cs
//
// Copyright (C) 2023, OpenHellion contributors
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
using System.Diagnostics;

namespace OpenHellion;

/// <summary>
/// 	High-Resolution time. Counts milliseconds and uses a long.
/// 	This should make it be able to run continuously for 300 000 000 years.
/// 	Perhaps slightly overkill.
/// </summary>
public static class HiResTime
{
	private static Stopwatch _stopwatch;

	public static ulong Milliseconds
	{
		get
		{
			if (_stopwatch == null)
			{
				throw new InvalidOperationException("The HiResTime class has not been started. Call the Start() method first.");
			}

			return (ulong)_stopwatch.ElapsedMilliseconds;
		}
	}

	public static void Start()
	{
		_stopwatch = Stopwatch.StartNew();
	}

	public static void Stop()
	{
		_stopwatch?.Stop();
		_stopwatch = null;
	}
}
