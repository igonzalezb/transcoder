using PCPerformance;
using System;
using System.Threading.Tasks;

namespace Testing
{
	class Testing
	{
		static async Task Main(string[] args)
		{
			var per = new PerformanceMonitor();

			await per.StartPerformanceBoxAsync();
		}
	}
}
