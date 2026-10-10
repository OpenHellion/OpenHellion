using System.Net;
using System.Net.Sockets;
using System.Threading;
using ZeroGravity;

namespace OpenHellion.Net;

public class StatusConnectionListener
{
	private volatile bool _runThread;

	private Thread _listeningThread;

	private TcpListener _tcpListener;

	public void Stop()
	{
		_runThread = false;
		_tcpListener.Stop();
	}

	public void Start(int port)
	{
		_runThread = true;
		_tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
		_tcpListener.Start();
		_listeningThread = new Thread(Listen)
		{
			IsBackground = true,
			Priority = ThreadPriority.AboveNormal
		};
		_listeningThread.Start();
	}

	private void Listen()
	{
		Debug.Log("Started server status thread.");
		while (Server.IsRunning && _runThread)
		{
			try
			{
				Socket soc = _tcpListener.AcceptSocket();
				if (!_runThread)
				{
					break;
				}
				StatusConnection connection = new StatusConnection(soc);
				connection.Start();
			}
			catch (SocketException)
			{
				break;
			}
			catch
			{
				// Ignored
			}
			Thread.Sleep(20);
		}

		Debug.Log("Shutting down status connection listener.");
	}
}
