// SocialServerConnection.cs
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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using OpenHellion.Exceptions;
using OpenHellion.IO;
using OpenHellion.Social.Message;

namespace OpenHellion.Social;

/// <summary>
/// 	Handles connections to the main server. This is the Nakama repository located at: https://github.com/OpenHellion/Nakama
/// </summary>
public static class SocialServerConnection
{
	public static string IpAddress = "127.0.0.1";
	public static ushort Port = 7350;
	public static string HttpKey;

	private static HttpClient _httpClient;

	/// <summary>
	/// 	Send a message to the Nakama main server.
	/// </summary>
	/// <exception cref="MainServerException" />
	/// <exception cref="HttpRequestException" />
	/// <exception cref="TimeoutException" />
	/// <exception cref="ArgumentNullException" />
	public static async Task<T> Send<T>(NakamaMessage message)
	{
		if (HttpKey is null)
		{
			throw new MainServerException("No http_key is configured, so the main server cannot be reached.");
		}

		if (HttpKey.Contains('&'))
		{
			throw new MainServerException("HttpKey contains &.");
		}

		ArgumentNullException.ThrowIfNull(message);

		// Create new client if one doesn't exist.
		_httpClient ??= new HttpClient();

		Debug.Log("Sending data to main server:", message.GetType(), message.ToString());
		Debug.Log($"http://{IpAddress}:{Port}/v2/rpc/{message.GetDestination()}?http_key={HttpKey}&unwrap");

		byte[] jsonBytes = Encoding.UTF8.GetBytes(message.ToString());

		var request = new HttpRequestMessage
		{
			RequestUri = new Uri($"http://{IpAddress}:{Port}/v2/rpc/{message.GetDestination()}?http_key={HttpKey}&unwrap"),
			Method = HttpMethod.Post,
			Content = new ByteArrayContent(jsonBytes, 0, jsonBytes.Length),
			Headers =
			{
				Accept = { new MediaTypeWithQualityHeaderValue("application/json") }
			},
			Version = new Version("2.0")
		};

		HttpResponseMessage response = await _httpClient.SendAsync(request, Program.CancelToken.Token);

		// Read data as string.
		string str = await response.Content.ReadAsStringAsync(Program.CancelToken.Token);
		response.Content.Dispose();

		Debug.Log("Response with data:", str);

		// Clean up.
		response.Dispose();

		var json = JsonSerialiser.Deserialize<T>(str);

		if (json is null)
		{
			throw new MainServerException("Received no response from MsConnection.Send.");
		}

		if (json is NakamaResponse nakamaResponse)
		{
			if (nakamaResponse.Code != 0)
			{
				throw new MainServerException(nakamaResponse.Message, (StatusCode)nakamaResponse.Code);
			}
		}

		return json;
	}

	/// <summary>
	/// 	Send a message to the Nakama main server.
	/// </summary>
	/// <exception cref="MainServerException" />
	/// <exception cref="HttpRequestException" />
	/// <exception cref="TimeoutException" />
	/// <exception cref="ArgumentNullException" />
	public static async Task Send(NakamaMessage message)
	{
		if (HttpKey is null)
		{
			throw new MainServerException("No http_key is configured, so the main server cannot be reached.");
		}

		if (HttpKey.Contains('&'))
		{
			throw new MainServerException("HttpKey contains &.");
		}

		// Create new client if one doesn't exist.
		_httpClient ??= new HttpClient();

		Debug.Log("Sending data to main server:", message.GetType(), message.ToString());
		Debug.Log($"http://{IpAddress}:{Port}/v2/rpc/{message.GetDestination()}?http_key={HttpKey}&unwrap");

		byte[] jsonBytes = Encoding.UTF8.GetBytes(message.ToString());

		var request = new HttpRequestMessage
		{
			RequestUri = new Uri($"http://{IpAddress}:{Port}/v2/rpc/{message.GetDestination()}?http_key={HttpKey}&unwrap"),
			Method = HttpMethod.Post,
			Content = new ByteArrayContent(jsonBytes, 0, jsonBytes.Length),
			Headers =
			{
				Accept = { new MediaTypeWithQualityHeaderValue("application/json") }
			},
			Version = new Version("2.0")
		};

		await _httpClient.SendAsync(request);
	}
}
