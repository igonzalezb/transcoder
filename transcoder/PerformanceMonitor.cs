using System;
using Konsole;
using System.Threading.Tasks;
using static System.ConsoleColor;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PCPerformance
{
	public class PerformanceMonitor
	{
		private static readonly Task<PerformanceCounter> cpuCounter = GetCounter();
		private readonly IConsole box;
		public bool Working { get; set; }
		public PerformanceMonitor()
		{ 
			box = Window.OpenBox("Performance", 20, 4, new BoxStyle()
			{
				ThickNess = LineThickNess.Single,
				Title = new Colors(White, Blue)
			});
			Working = true;
		}
		public async Task StartPerformanceBoxAsync()
		{
			while (Working)
			{
				float cpuPerc = await GetCpuTimeInPercentAsync();
				Tick(box, "CPU", cpuPerc.ToString("f0") + "%", DarkGreen);
				Tick(box, "GPU", GetGpuLoad().ToString() + "%", Red);
				await Task.Delay(700);
			}
		}

		private void Tick(IConsole con, string device, string usage, ConsoleColor color)
		{
			con.WriteLine("");
			con.Write(White, $"{device,-10}");
			con.Write(color, $"{usage}");
		}



		private static async Task<PerformanceCounter> GetCounter()
		{
			PerformanceCounter pc = new PerformanceCounter("Processor Information", "% Processor Time", "_Total");
			// "warm up"
			pc.NextValue();
			await Task.Delay(1000);
			// return ready-to-use instance
			return pc;
		}

		public static async Task<float> GetCpuTimeInPercentAsync()
		{
			var counter = await cpuCounter;
			return counter.NextValue();
		}

		// Handle error loading nvGpuLoad.dll (e.g. missing Microsoft Visual C++ 2015 Redistributable  (x86) or missing dll)
		class NvGpuLoad
		{
			// dll with code from http://eliang.blogspot.de/2011/05/getting-nvidia-gpu-usage-in-c.html

			#if WIN64
			[DllImport("nvGpuLoad_x64.dll")]
			public static extern int getGpuLoad();
			#else
			[DllImport("nvGpuLoad_x86.dll")]
			public static extern int getGpuLoad();
			#endif
			internal static int GetGpuLoad()
			{
				int a = new int();
				a = getGpuLoad();
				return a;
			}
		}

		private int GetGpuLoad()
		{
			try
			{
				return NvGpuLoad.GetGpuLoad();
			}
			catch (DllNotFoundException e)
			{
				Console.WriteLine(e);
				return 0;
			}
		}


	}
}
