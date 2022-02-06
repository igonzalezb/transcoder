using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Konsole;

namespace Transcoder
{
	class Program
	{
		enum options
		{
			H265, H264, _10Bit_to_8Bit,Cancel
		}
		static async Task Main(string[] args)
		{
			Console.Clear();
			Console.Title = "Transcoder";

			//FFmpeg.SetExecutablesPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FFmpeg"));
			//FFmpeg.SetExecutablesPath(".");
			//Get latest version of FFmpeg. It's great idea if you don't know if you had installed FFmpeg.
			// await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
			if (args.Length > 0 && args[0] == "-contextmenu")
			{
				ContextMenu.setContextMenu(args[1]);
			}
			else
			{
				List<string> videos = FileManagerUtils.getVideoFiles(args);

				/////////////////////////////////////////////////////////////////////////////////////////////
				Console.WriteLine($"Found {videos.Count} files:");
				foreach (string video in videos)
				{
					Console.WriteLine(Path.GetFileName(video));
				}
				Console.WriteLine("");
					
					
				options option;
				bool wrongInput = true;

				do
				{
					Console.WriteLine("Select Option:");
					string[] array = Enum.GetNames(typeof(options));
					for (int i = 0; i < array.Length; i++)
					{
						Console.WriteLine($"{i}) {array[i]}");
					}
					
					string value = Console.ReadLine();
					option = (options)Enum.Parse(typeof(options), value);

					if(Enum.IsDefined(typeof(options), option))
					{
						wrongInput = false;
					}
					else
						Console.WriteLine("Error try again\n");
				}while(wrongInput);
				
				if(option != options.Cancel)
				{
					Console.CursorVisible = false;
					Console.Clear();
					try
					{
						await StartConverting(videos, option);
						Console.WriteLine("Finished All.");
						Console.Beep();
					}
					catch (System.Exception e)
					{
						Console.WriteLine("Cancelled by user");
						Debug.WriteLine(e);
					}								
				}
			}
			Console.WriteLine("Press Any Key to Exit");
			Console.ReadKey();
		}


		
		private static async Task StartConverting(List<string> videos, options _codec)
		{					
			var cancellationTokenSource = new CancellationTokenSource();
			Console.CancelKeyPress += (sender, args) =>
			{
				args.Cancel = true;
				cancellationTokenSource.Cancel();
			};

			var bars = new List<ProgressBar>();

			//var pbarList = new List<ShellProgressBar.ChildProgressBar>();

			//List<Task> jobsList = new();
			//List<Action> actionList = new();
			Console.Title = $"Transcoder - {0}/{videos.Count}";
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.WriteLine($"Transcoding {videos.Count} videos - (Press ctrl+c to cancel)\n");
			Console.ResetColor();
			
			for (int i = 0; i < videos.Count; i++)
			{
				//var length = videos.Max(s => s.Length);
				bars.Add(new ProgressBar(100));
				bars[i].Refresh(0, $"{Path.GetFileNameWithoutExtension(videos[i])} - [00:00:00]");
				//pbarList.Add(pbar.Spawn(100, Path.GetFileNameWithoutExtension(video) + " - [00:00:00]", _options)); 
				//jobsList.Add(new Task(async () => await Codecs.Convert_H265_cuvid(video, pbarList.Last(), cancellationTokenSource)));
				//actionList.Add(new Action(async () => await Codecs.Convert_H265_cuvid(video, pbarList.Last(), cancellationTokenSource)));
				//jobsList.Add(Codecs.Convert_H265_cuvid(video, pbarList.Last(), cancellationTokenSource));
			}
			//await TasksUtilities.StartAndWaitAllThrottledAsync(jobsList, 2, cancellationTokenSource.Token);		

			Func<string, ProgressBar, CancellationTokenSource, Task>[] funcArray = {Codecs.Convert_H265_cuvid, Codecs.Convert_H264, Codecs.Convert_10bit_to_8bit };

			for (int i = 0; i < videos.Count; i++, i++)
			{
				if ((i + 1) < videos.Count)
				{
					await Task.WhenAll(
						funcArray[(int)_codec](videos[i], bars[i], cancellationTokenSource),
						funcArray[(int)_codec](videos[i + 1], bars[i + 1], cancellationTokenSource));

					Console.Title = $"Transcoder - {i + 2}/{videos.Count}";
				}

				else
				{
					await funcArray[(int)_codec](videos[i], bars[i], cancellationTokenSource);
					Console.Title = $"Transcoder - {i + 1}/{videos.Count}";
				}

			}

			cancellationTokenSource.Cancel(true);

		}


		
	}
}
