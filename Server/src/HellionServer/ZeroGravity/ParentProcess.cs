using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ZeroGravity;

public class ParentProcess
{
	public struct PROCESSENTRY32
	{
		public uint dwSize;

		public uint cntUsage;

		public uint th32ProcessID;

		public IntPtr th32DefaultHeapID;

		public uint th32ModuleID;

		public uint cntThreads;

		public uint th32ParentProcessID;

		public int pcPriClassBase;

		public uint dwFlags;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szExeFile;
	}

	private static Process parentProcess;

	private static uint TH32CS_SNAPPROCESS = 2u;

	public static string ProcessName => GetParentProcess().ProcessName;

	public static int ProcessId => GetParentProcess().Id;

	public static string FullPath => GetParentProcess().MainModule.FileName;

	public static string FileName => Path.GetFileName(GetParentProcess().MainModule.FileName);

	public static string DirectoryName => Path.GetDirectoryName(GetParentProcess().MainModule.FileName);

	public static Process GetParentProcess()
	{
		if (parentProcess != null)
		{
			return parentProcess;
		}
		int iParentPid = 0;
		int iCurrentPid = Process.GetCurrentProcess().Id;
		IntPtr oHnd = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0u);
		if (oHnd == IntPtr.Zero)
		{
			return null;
		}
		PROCESSENTRY32 oProcInfo = default(PROCESSENTRY32);
		oProcInfo.dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32));
		if (!Process32First(oHnd, ref oProcInfo))
		{
			return null;
		}
		do
		{
			if (iCurrentPid == oProcInfo.th32ProcessID)
			{
				iParentPid = (int)oProcInfo.th32ParentProcessID;
			}
		}
		while (iParentPid == 0 && Process32Next(oHnd, ref oProcInfo));
		if (iParentPid > 0)
		{
			return parentProcess = Process.GetProcessById(iParentPid);
		}
		return null;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

	[DllImport("kernel32.dll")]
	private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

	[DllImport("kernel32.dll")]
	private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);
}
