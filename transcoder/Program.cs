using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using ShellProgressBar;
using McMaster.Extensions.CommandLineUtils;

namespace Transcoder
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.Title = "Transcoder";
			string path;

			if (!args.Any())
			{
				Console.WriteLine("Welcome to Transcoder!");
				Console.WriteLine();
				Console.WriteLine("The HEVC settings are:");
				Console.WriteLine("ffmpeg -vsync 0 -hwaccel cuvid -c:v h264_cuvid -i video -c:v hevc_nvenc -x265-params crf=20 -spatial_aq 1 -rc-lookahead 20 -preset slow -c:a aac -b:a 224k -map 0 video-trans.mkv");
				Console.WriteLine();
				Console.WriteLine("Enter path:");
				path = Console.ReadLine();

				Console.Clear();
			}
			else
			{
				path = args[0].ToString();
			}
			Console.CursorVisible = false;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
  				path = path.Replace("\'", "");
			}
			else if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				path = path.Trim('"');
			}
			
			
			if (Path.HasExtension(path) && (Path.GetExtension(path) == ".mkv"))
			{
				Console.WriteLine(Path.GetFileName(path));
				Console.WriteLine("");
				
				if (Prompt.GetYesNo("Look Good?", false))
				{
					Console.Clear();
					try
					{
						var pbar_opt = new ProgressBarOptions
						{
							BackgroundCharacter = '\u2593',
							ProgressBarOnBottom = false,
							ForegroundColorDone = ConsoleColor.Green,
							ForegroundColor = ConsoleColor.DarkYellow,
							CollapseWhenFinished = false,
							DisplayTimeInRealTime = true,
							ShowEstimatedDuration = false,
							
						};
						var _options = new ProgressBarOptions
						{
							ProgressCharacter = '_',
							ProgressBarOnBottom = true,
							ForegroundColorDone = ConsoleColor.Green,
							ForegroundColor = ConsoleColor.White,
							CollapseWhenFinished = false,
							DisplayTimeInRealTime = true,				
						};
						using var pbar = new ShellProgressBar.ProgressBar(1, "(Press ctrl+c to cancel)", pbar_opt);
						await H265_cuvid(path, pbar.Spawn(100, Path.GetFileNameWithoutExtension(path) + " - [00:00:00]", _options));
						Console.WriteLine("Finished All.");	
					}
					catch (System.Exception)
					{
						Console.WriteLine("Stopped by user");
					}
					
					
								
				}
							
			}

			else
			{
				bool recurse = false;
				string videoFilter = @"$(?<=\.(mkv|mp4|avi|mk3d|flv|wmv|m4v|webm))";

				var videos = Directory
					.GetFiles(path, "*.*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
					.Where(x => Regex.IsMatch(x, videoFilter, RegexOptions.IgnoreCase))
					.ToList();

				foreach (var video in videos)
				{
					Console.WriteLine(Path.GetFileName(video));
				}
				Console.WriteLine("");
				if (Prompt.GetYesNo("Look Good?", false))
				{
					Console.Clear();
					try
					{
						await StartConverting(videos);
						Console.WriteLine("Finished All.");	
					}
					catch (System.Exception)
					{
						Console.WriteLine("Stopped by user");
					}
									
				}

			}

			Console.WriteLine("Press Any Key to Exit");
			Console.ReadKey();
		}


		
		private static async Task StartConverting(List<string> videos)
		{
			var pbar_opt = new ProgressBarOptions
			{
				BackgroundCharacter = '\u2593',
				BackgroundColor = ConsoleColor.DarkGray,
				ProgressBarOnBottom = false,
        		ForegroundColorDone = ConsoleColor.Green,
        		ForegroundColor = ConsoleColor.DarkYellow,
				CollapseWhenFinished = false,
				DisplayTimeInRealTime = true,
				ShowEstimatedDuration = false,
				
			};
			var _options = new ProgressBarOptions
			{
				ProgressCharacter = '_',
				ProgressBarOnBottom = true,
        		ForegroundColorDone = ConsoleColor.Green,
        		ForegroundColor = ConsoleColor.White,
				CollapseWhenFinished = false,
				DisplayTimeInRealTime = true,				
			};
			
			using var pbar = new ShellProgressBar.ProgressBar(videos.Count, $"Finished: 0 of {videos.Count} - (Press ctrl+c to cancel)", pbar_opt);

			var pbarList = new List<ShellProgressBar.ChildProgressBar>();

			foreach (string video in videos)
			{
				pbarList.Add(pbar.Spawn(100, Path.GetFileNameWithoutExtension(video) + " - [00:00:00]", _options));
				
			}
			for (int i = 0; i < videos.Count; i++, i++)
			{

				if ((i + 1) < videos.Count)
				{
					await Task.WhenAll(
						H265_cuvid(videos[i], pbarList[i]), 
						H265_cuvid(videos[i + 1], pbarList[i+1]));
					
					pbar.Tick($"Finished: {i +1} of {videos.Count}");
				}

				else
				{
					await H265_cuvid(videos[i], pbarList[i]);
					pbar.Tick($"Finished: {i} of {videos.Count}");
				}

			}
		}

		private static async Task H265_cuvid(string fileName, ShellProgressBar.ChildProgressBar pbar)
		{
			IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(fileName);
			IStream videoStream = mediaInfo.VideoStreams.FirstOrDefault()
				?.SetCodec("hevc_nvenc");
			IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault()
				?.SetCodec(AudioCodec.aac);

			var cancellationTokenSource = new CancellationTokenSource();

			string output = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "-CODED.mkv");

			var conversion = FFmpeg.Conversions.New();
			conversion
				.AddStream(videoStream, audioStream)
				.AddStream(mediaInfo.SubtitleStreams)
				.UseHardwareAcceleration("cuda", "h264_cuvid", "hevc_nvenc")
				.AddParameter("-vsync 0", ParameterPosition.PreInput)
				.AddParameter("-x265-params crf=20 -spatial_aq 1 -rc-lookahead 20", ParameterPosition.PostInput)
				.SetOutput(output)
				.SetOutputFormat(Format.matroska)
				.SetPreset(ConversionPreset.Slow)
				.SetAudioBitrate(224000)
				.SetOverwriteOutput(true);

			
			conversion.OnProgress += (sender, args) =>
			{
				pbar.Tick(args.Percent, Path.GetFileNameWithoutExtension(fileName) + $" - [{ args.TotalLength - args.Duration}]");
			};

			Console.CancelKeyPress += (sender, args) =>
			{
				args.Cancel = true;
				cancellationTokenSource.Cancel(true);
				pbar.Dispose();
			};

			await conversion.Start(cancellationTokenSource.Token);

		}
	}
}
