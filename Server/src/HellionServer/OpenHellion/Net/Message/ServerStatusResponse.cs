// ServerStatusResponse.cs
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
using ZeroGravity.Network;

namespace OpenHellion.Net.Message;

/// <summary>
/// 	See also <seealso cref="ServerStatusRequest"/>.
/// </summary>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ServerStatusResponse : NetworkData
{
	public string Name;

	public string Description;

	public short CurrentPlayers;

	public short AlivePlayers;

	public short MaxPlayers;

	public uint Hash;

	public bool IsOffline;

	public bool IsPrivate;

	public string CharacterName;
}
