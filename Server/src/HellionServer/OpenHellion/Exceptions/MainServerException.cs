// MainServerException.cs
//
// Copyright (C) 2025, OpenHellion contributors
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
using OpenHellion.Social;

namespace OpenHellion.Exceptions;

public class MainServerException : Exception
{
	public MainServerException(string message) : base(message) { }
	public MainServerException(string message, StatusCode statusCode) : base($"Didn't get valid response from main server. Perhaps you should check if your http_key config is correct?: {message}. Error code: {statusCode}") { }
}
