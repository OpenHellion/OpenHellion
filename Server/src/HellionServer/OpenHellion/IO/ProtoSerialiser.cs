using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;
using ZeroGravity.Network;

namespace OpenHellion.IO;

/// <summary>
/// 	Class for deserialisation of network messages. Handles safe deserialisation of packets from the client.
/// </summary>
public static class ProtoSerialiser
{
	/// <summary>
	/// 	The peer closed the connection. An <see cref="IOException" /> so the transport's existing handling
	/// 	holds it, and public so a caller can tell a close apart from a broken pipe.
	/// </summary>
	public sealed class ZeroDataException : IOException
	{
		public ZeroDataException(string message)
			: base(message)
		{
		}
	}

	/// <summary>
	/// 	Unpack data sent by the server.
	/// 	Reads the size of the message, then reads the message itself.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	/// <param name="maxMessageSize">Max number of bytes to accept.</param>
	/// <param name="guid">Client the message came from, for tracing. Zero before a client has one.</param>
	/// <param name="cancellationToken"></param>
	/// <returns>The message, or null if the frame arrived intact but could not be read.</returns>
	/// <exception cref="ArgumentException">If message is too large.</exception>
	/// <exception cref="ZeroDataException">If the peer closed the connection.</exception>
	public static async Task<NetworkData> Unpack(Stream stream, int maxMessageSize, long guid = 0, CancellationToken cancellationToken = default)
	{
		int dataRead = 0;
		int readSize;

		byte[] dataLengthBuffer = new byte[4];
		do
		{
			readSize = await stream.ReadAsync(dataLengthBuffer.AsMemory(dataRead, dataLengthBuffer.Length - dataRead), cancellationToken);
			if (readSize == 0)
			{
				throw new ZeroDataException(dataRead == 0
					? "Peer closed the connection between messages."
					: $"Peer closed the connection inside a message header, after {dataRead} of 4 bytes.");
			}

			dataRead += readSize;
		} while (dataRead < dataLengthBuffer.Length);

		uint dataLength = BinaryPrimitives.ReadUInt32LittleEndian(dataLengthBuffer);
		if (dataLength > maxMessageSize)
		{
				await SkipAsync(stream, dataLength, cancellationToken);

				throw new ArgumentException($"Message too large. Declared {dataLength}, maximum allowed is {maxMessageSize}.");
		}

		// Read following contents.
		byte[] buffer = new byte[dataLength];
		dataRead = 0;
		do
		{
			readSize = await stream.ReadAsync(buffer.AsMemory(dataRead, buffer.Length - dataRead), cancellationToken);
			if (readSize == 0)
			{
				throw new ZeroDataException(
					$"Peer closed the connection inside a message body, after {dataRead} of {dataLength} bytes.");
			}

			dataRead += readSize;
		} while (dataRead < buffer.Length);

		// Make the stream into NetworkData.
		MemoryStream ms = new MemoryStream(buffer, 0, buffer.Length);

		NetworkData networkData;
		try
		{
			networkData = Serializer.Deserialize<NetworkData>(ms);
		}
		catch (Exception ex)
		{
			// The frame was whole, so the connection is still in sync; only this message is lost.
			Debug.LogWarning($"Discarding an unreadable {dataLength} byte message from client {guid}: {ex.GetType().Name}: {ex.Message}");
			return null;
		}

		return networkData;
	}

	/// <summary>
	/// 	Pack NetworkData into a binary array.
	/// </summary>
	/// <param name="data">NetworkData to serialise.</param>
	/// <param name="cancellationToken"></param>
	/// <returns>Data as a binary array, or null if it could not be serialised.</returns>
	public static async Task<byte[]> Pack(NetworkData data, CancellationToken cancellationToken = default)
	{
		await using MemoryStream ms = new MemoryStream();

		try
		{
			Serializer.Serialize(ms, data);
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			return null;
		}

		await using MemoryStream outMs = new MemoryStream();
		await outMs.WriteAsync(BitConverter.GetBytes((uint)ms.Length).AsMemory(0, 4), cancellationToken);
		await outMs.WriteAsync(ms.ToArray().AsMemory(0, (int)ms.Length), cancellationToken);
		await outMs.FlushAsync(cancellationToken);
		return outMs.ToArray();
	}

	/// <summary>
	/// 	Skips a specified number of bytes in the stream asynchronously.
	/// </summary>
	/// <param name="stream">Stream to skip on.</param>
	/// <param name="bytesToSkip">Bytes to skip.</param>
	/// <param name="cancellationToken"></param>
	/// <exception cref="EndOfStreamException">Stream ended unexpectedly.</exception>
	public static async Task SkipAsync(Stream stream, long bytesToSkip, CancellationToken cancellationToken = default)
	{
		if (bytesToSkip <= 0) return;

		// If the stream supports seeking, do it in one go
		if (stream.CanSeek)
		{
			long toSkip = Math.Min(stream.Length - stream.Position, bytesToSkip);
			stream.Seek(toSkip, SeekOrigin.Current);
			return;
		}

		// Otherwise, read-and-discard in chunks
		const int discardBufferSize = 8192;
		byte[] discardBuffer = new byte[discardBufferSize];
		long remaining = bytesToSkip;
		while (remaining > 0)
		{
			int chunk = (int)Math.Min(discardBufferSize, remaining);
			int read = await stream.ReadAsync(discardBuffer.AsMemory(0, chunk), cancellationToken);
			if (read == 0)
			{
				// Stream ended prematurely
				throw new EndOfStreamException(
					$"Stream ended while skipping {bytesToSkip} bytes (skipped {bytesToSkip - remaining}).");
			}
			remaining -= read;
		}
	}
}
