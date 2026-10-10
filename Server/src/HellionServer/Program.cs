using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using OpenHellion;
using OpenHellion.IO;
using ZeroGravity;
using ZeroGravity.Network;

public static class Program
{
	public static readonly CancellationTokenSource CancelToken = new CancellationTokenSource();

	private static void ProcessExit(object sender, EventArgs e)
	{
		Debug.Log("Exiting safely...");
		CancelToken.Cancel();
		Server.IsRunning = false;
		if (Server.PersistenceSaveInterval > 0.0)
		{
			Server.SavePersistenceDataOnShutdown = true;
		}
		Server.MainLoopEnded.WaitOne(10000);
	}

	private static async Task<int> Main(string[] args)
	{
		// Exit handlers.
		AppDomain.CurrentDomain.ProcessExit += new EventHandler(ProcessExit);
		Console.CancelKeyPress += new ConsoleCancelEventHandler(ProcessExit);

		// Parse arguments.
		Server.InitProperties(args);

		// Enable debugger.
		Debug.OutputDir = Server.ConfigDir;
		Debug.Initialize();

		Debug.Log("Starting server with args:", string.Join(" ", args));

		for (int i = 0; i < args.Length; i++)
		{
			if (args[i].ToLower() == "-shutdown")
			{
				await ShutdownServerInstance();
				return 0;
			}
			else if (args[i].ToLower() == "-scan")
			{
				ScanInstances();
				Environment.Exit(0);
			}
			else if (args[i].ToLower() == "-cleanup")
			{
				CleanupAllInstances();
				Environment.Exit(0);
			}
		}

		if (!File.Exists(Server.ConfigDir + "GameServer.ini"))
		{
			Debug.LogInfo("GameServer.ini not found in folder " + Server.ConfigDir);
			return -1;
		}

		CheckIniFields();

		AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
		// Start the server.
		Server server = new Server();
		HiResTime.Start();
		await server.MainLoop();
		HiResTime.Stop();
		return 0;
	}

	private static void CheckIniFields()
	{
		if (!Server.Properties.TryGetProperty<int>("game_port", out var _))
		{
			Debug.LogError("Missing or invalid 'game_port' field.");
			Environment.Exit(1);
		}
		if (!Server.Properties.TryGetProperty<int>("status_port", out var _))
		{
			Debug.LogError("Missing or invalid 'status_port' field.");
			Environment.Exit(1);
		}
		// The main server keys only matter when we are in online mode.
		if (!Server.Properties.TryGetProperty<bool>("offline_mode", out var offlineMode) || !offlineMode)
		{
			if (!Server.Properties.TryGetProperty<string>("http_key", out var _))
			{
				Debug.LogError("Missing or invalid 'http_key' field. Set 'offline_mode=true' to run without a main server.");
				Environment.Exit(1);
			}
		}

	}

	private static void CleanupAllInstances()
	{
		DirectoryInfo dinfo = new DirectoryInfo(".");
		foreach (FileInfo file2 in dinfo.EnumerateFiles("*.save"))
		{
			file2.Delete();
		}
		string[] directories = Directory.GetDirectories(".");
		foreach (string dir in directories)
		{
			string configDir = Path.GetFileName(dir);
			string path = configDir + "/GameServer.ini";
			if (!File.Exists(path))
			{
				continue;
			}
			dinfo = new DirectoryInfo(configDir);
			foreach (FileInfo file in dinfo.EnumerateFiles("*.save"))
			{
				file.Delete();
			}
		}
	}

	private static void ScanInstances()
	{
		DirectoryInfo dinfo = new DirectoryInfo(".");
		foreach (FileInfo file2 in dinfo.EnumerateFiles("Start_*.bat"))
		{
			file2.Delete();
		}
		foreach (FileInfo file in dinfo.EnumerateFiles("Stop_*.bat"))
		{
			file.Delete();
		}
		string exe = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
		string startAll = "start cmd /c call \"Start_DEFAULT.bat\"\r\n";
		string stopAll = "start cmd /c call \"Stop_DEFAULT.bat\"\r\n";
		File.WriteAllText("Start_DEFAULT.bat", "@TITLE Game Server: DEFAULT \r\n@" + exe);
		File.WriteAllText("Stop_DEFAULT.bat", exe + " -shutdown");
		string[] directories = Directory.GetDirectories(".");
		foreach (string dir in directories)
		{
			string configDir = Path.GetFileName(dir);
			string path = configDir + "/GameServer.ini";
			if (File.Exists(path))
			{
				string start = "@TITLE Game Server: " + configDir + "\r\n@" + exe + " -configdir \"" + configDir + "\"";
				string stop = start + " -shutdown";
				string startBatFile = "Start_" + configDir + ".bat";
				string stopBatFile = "Stop_" + configDir + ".bat";
				File.WriteAllText(startBatFile, start);
				File.WriteAllText(stopBatFile, stop);
				startAll = startAll + "start cmd /c call \"" + startBatFile + "\"\r\n";
				stopAll = stopAll + "start cmd /c call \"" + stopBatFile + "\"\r\n";
			}
		}
		File.WriteAllText("Start_ALL.bat", startAll);
		File.WriteAllText("Stop_ALL.bat", stopAll);
	}

	private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
	{
		try
		{
			Debug.LogException((Exception)args.ExceptionObject);
		}
		catch
		{
			File.WriteAllText(Server.ConfigDir + "unhandled_exception.txt", ((Exception)args.ExceptionObject).Message + ", " + ((Exception)args.ExceptionObject).StackTrace);
		}
	}

	// Sends a request to the status listener that we should shutdown.
	private static async Task ShutdownServerInstance()
	{
		if (!Server.Properties.TryGetProperty("status_port", out int StatusPort))
		{
			return;
		}
		try
		{
			TcpClient tcpClient = new TcpClient();
			IAsyncResult ar = tcpClient.BeginConnect("127.0.0.1", StatusPort, null, null);
			WaitHandle wh = ar.AsyncWaitHandle;
			try
			{
				if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2.0), exitContext: false))
				{
					tcpClient.Close();
					throw new TimeoutException();
				}
				tcpClient.EndConnect(ar);
			}
			finally
			{
				wh.Close();
			}
			NetworkStream ns = null;
			try
			{
				ns = tcpClient.GetStream();
			}
			catch
			{
				return;
			}
			ns.ReadTimeout = 1000;
			ns.WriteTimeout = 1000;
			ServerShutDownMessage msg = new ServerShutDownMessage();
			byte[] buffer = await ProtoSerialiser.Pack(msg);
			ns.Write(buffer, 0, buffer.Length);
			ns.Flush();
			ns.Close();
			tcpClient.Close();
		}
		catch
		{
		}
	}
}
