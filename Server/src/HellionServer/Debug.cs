using System;
using System.Diagnostics;
using System.IO;

public static class Debug
{
	internal static string OutputDir = "";

	public const string TimestampFormat = "HH:mm:ss.ffff";

	public static void Initialize()
	{
		string fileName = OutputDir + "log.txt";
		try
		{
			if (File.Exists(fileName))
			{
				string backupFileName = OutputDir + "log_backup.txt";
				File.Move(fileName, backupFileName, true);
			}
		}
		catch
		{
			// ignored
		}

		Trace.Listeners.Clear();

		ConsoleTraceListener consoleListener = new ConsoleTraceListener(useErrorStream: false)
		{
			TraceOutputOptions = TraceOptions.Callstack
		};
		Trace.Listeners.Add(consoleListener);

		TextWriterTraceListener writerListener = new TextWriterTraceListener(new StreamWriter(fileName, append: false))
		{
			TraceOutputOptions = TraceOptions.Callstack
		};
		Trace.Listeners.Add(writerListener);

		Trace.AutoFlush = true;
	}

	public static void Destroy()
	{
		Trace.Listeners.Clear();
	}

	private static void Write(string message, string category)
	{
		try
		{
			Trace.WriteLine(DateTime.UtcNow.ToString(TimestampFormat + " - " ) + message, category);
		}
		catch
		{
			// ignored
		}
	}

	private static string GetString(object value)
	{
		return value != null ? value.ToString() : "NULL";
	}

	[Conditional("DEBUG")]
	[Conditional("SHOW_ALL_LOGS")]
	public static void Log(string message)
	{
		Write(message, "Debug");
	}

	[Conditional("DEBUG")]
	[Conditional("SHOW_ALL_LOGS")]
	public static void LogFormat(string message, params object[] args)
	{
		Log(string.Format(message, args));
	}

	[Conditional("DEBUG")]
	[Conditional("SHOW_ALL_LOGS")]
	public static void Log(params object[] values)
	{
		if (values.Length == 1)
		{
			Log(GetString(values[0]));
		}
		else
		{
			Log(string.Join(" ", values));
		}
	}

	public static void UnformattedMessage(string message)
	{
		Trace.WriteLine(message);
	}

	public static void LogInfo(string message)
	{
		Write(message, "Info");
	}

	public static void LogInfoFormat(string message, params object[] values)
	{
		LogInfo(string.Format(message, values));
	}

	public static void LogInfo(params object[] values)
	{
		if (values.Length == 1)
		{
			LogInfo(GetString(values[0]));
		}
		else
		{
			LogInfo(string.Join(" ", values));
		}
	}

	public static void LogWarning(string message)
	{
		Write(message, "Warning");
	}

	public static void LogWarningFormat(string message, params object[] values)
	{
		LogWarning(string.Format(message, values));
	}

	public static void LogWarning(params object[] values)
	{
		if (values.Length == 1)
		{
			LogWarning(GetString(values[0]));
		}
		else
		{
			LogWarning(string.Join(" ", values));
		}
	}

	public static void LogError(string message)
	{
		Write(message, "Error");
	}

	public static void LogErrorFormat(string message, params object[] values)
	{
		LogError(string.Format(message, values));
	}


	public static void LogError(params object[] values)
	{
		if (values.Length == 1)
		{
			LogError(GetString(values[0]));
		}
		else
		{
			LogError(string.Join(" ", values));
		}
	}

	public static void LogException(Exception ex)
	{
		Write(ex.ToString(), "Exception");
	}

	/// <summary>
	/// 	When condition is false, break the program and log an error message.
	/// </summary>
	/// <param name="condition">Condition to assert</param>
	[Conditional("DEBUG")]
	public static void Assert(bool condition)
	{
		Trace.Assert(condition);
	}

	/// <summary>
	/// 	When condition is false, break the program and log the provided message.
	/// </summary>
	/// <param name="condition">Condition to check.</param>
	/// <param name="message">Message to log on fail.</param>
	[Conditional("DEBUG")]
	public static void Assert(bool condition, string message)
	{
		Trace.Assert(condition, message);
	}
}
