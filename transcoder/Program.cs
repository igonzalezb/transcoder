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
using System.Diagnostics;

namespace Transcoder
{
	class Program
	{
		enum options
		{
			H265, H264, Cancel
		}
		static async Task Main(string[] args)
		{
			Console.Title = "Transcoder";

			//FFmpeg.SetExecutablesPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FFmpeg"));
			//FFmpeg.SetExecutablesPath(".");
			 
            //Get latest version of FFmpeg. It's great idea if you don't know if you had installed FFmpeg.
           // await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
			List<string> videos = new List<string>();
			bool recurse = false;
			string videoFilter = @"$(?<=\.(mkv|mp4|avi|mk3d|flv|wmv|m4v|webm))";
			string path;
			Console.WriteLine(Figgle.FiggleFonts.Standard.Render("Transcoder"));
			if (!args.Any())
			{				
				//Console.WriteLine("Welcome to Transcoder!");
				Console.WriteLine();
				Console.WriteLine("Enter path:");
				path = Console.ReadLine();
				path = cleanPath(path);
				if (Path.HasExtension(path) && Regex.IsMatch(path, videoFilter, RegexOptions.IgnoreCase))
				{
					videos.Add(path);					
				}

				else
				{					
					videos = Directory
						.GetFiles(path, "*.*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
						.Where(x => Regex.IsMatch(x, videoFilter, RegexOptions.IgnoreCase))
						.ToList();				

				}

				Console.Clear();
			}
			else
			{
				foreach (var item in args)
				{
					path = cleanPath(item.ToString());
					if (Path.HasExtension(path) && Regex.IsMatch(path, videoFilter, RegexOptions.IgnoreCase))
					{
						videos.Add(path);					
					}
					else
					{
						videos = Directory
							.GetFiles(path, "*.*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
							.Where(x => Regex.IsMatch(x, videoFilter, RegexOptions.IgnoreCase))
							.ToList();
					}
					
				}
			}

			
			
			/////////////////////////////////////////////////////////////////////////////////////////////
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
				try
				{
					await StartConverting(videos, option);
					Console.WriteLine("Finished All.");	
				}
				catch (System.Exception e)
				{
					Console.WriteLine("Cancelled by user");
					Debug.WriteLine(e);
				}								
			}

			Console.WriteLine("Press Any Key to Exit");
			Console.ReadKey();
		}


		
		private static async Task StartConverting(List<string> videos, options _codec)
		{
			var pbar_opt = new ProgressBarOptions
			{
				BackgroundCharacter = '\u2593',
				BackgroundColor = ConsoleColor.DarkGray,
				ProgressBarOnBottom = false,
        		ForegroundColorDone = ConsoleColor.Green,
        		ForegroundColor = ConsoleColor.DarkYellow,
				CollapseWhenFinished = false,
				DisplayTimeInRealTime = false,
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
			var cancellationTokenSource = new CancellationTokenSource();
			Console.CancelKeyPress += (sender, args) =>
			{
				args.Cancel = true;
				pbar.Dispose();
				cancellationTokenSource.Cancel();
			};

			var pbarList = new List<ShellProgressBar.ChildProgressBar>();

			foreach (string video in videos)
			{
				pbarList.Add(pbar.Spawn(100, Path.GetFileNameWithoutExtension(video) + " - [00:00:00]", _options));
				
			}
			

			if(_codec == options.H265)
			{
				for (int i = 0; i < videos.Count; i++, i++)
				{
					if ((i + 1) < videos.Count)
					{
						await Task.WhenAll(
							Convert_H265_cuvid(videos[i], pbarList[i], cancellationTokenSource), 
							Convert_H265_cuvid(videos[i + 1], pbarList[i+1], cancellationTokenSource));
						
						pbar.Tick();
						pbar.Tick($"Finished: {i + 2} of {videos.Count} - (Press ctrl+c to cancel)");
					}

					else
					{
						await Convert_H265_cuvid(videos[i], pbarList[i], cancellationTokenSource);
						pbar.Tick($"Finished: {i + 1} of {videos.Count} - (Press ctrl+c to cancel)");
					}

				}
			}
			else if(_codec == options.H264)
			{
				for (int i = 0; i < videos.Count; i++, i++)
				{
					if ((i + 1) < videos.Count)
					{
						await Task.WhenAll(
							Convert_H264(videos[i], pbarList[i], cancellationTokenSource), 
							Convert_H264(videos[i + 1], pbarList[i+1], cancellationTokenSource));
						
						pbar.Tick();
						pbar.Tick($"Finished: {i + 2} of {videos.Count} - (Press ctrl+c to cancel)");
					}

					else
					{
						await Convert_H264(videos[i], pbarList[i], cancellationTokenSource);
						pbar.Tick($"Finished: {i + 1} of {videos.Count} - (Press ctrl+c to cancel)");
					}

				}
			}

			cancellationTokenSource.Cancel(true);

		}

		private static async Task Convert_H265_cuvid(string fileName, ShellProgressBar.ChildProgressBar pbar, CancellationTokenSource cancellationTokenSource)
		{
			IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(fileName);
			IStream videoStream = mediaInfo.VideoStreams.FirstOrDefault()
				?.SetCodec("hevc_nvenc");
			IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault()
				?.SetCodec(AudioCodec.aac);
				
			string codec = mediaInfo.VideoStreams.FirstOrDefault().Codec; //"hevc" - "h264"

			string output = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "-H265.mkv");

			var conversion = FFmpeg.Conversions.New();
			conversion
				.AddStream(videoStream, audioStream)
				.UseHardwareAcceleration("cuda", (codec=="hevc") ? "hevc" : "h264_cuvid", "hevc_nvenc")
				.AddParameter("-vsync 0", ParameterPosition.PreInput)
				.AddParameter("-map 0:s? -x265-params crf=20 -spatial_aq 1 -rc-lookahead 20 -c:s copy", ParameterPosition.PostInput)
				.SetOutput(output)
				.SetOutputFormat(Format.matroska)
				.SetPreset(ConversionPreset.Slow)
				.SetAudioBitrate(224000)
				.SetOverwriteOutput(true);

			
			conversion.OnProgress += (sender, args) =>
			{
				pbar.Tick(args.Percent, Path.GetFileNameWithoutExtension(fileName) + $" - [{ args.TotalLength - args.Duration}]");
			};

			

			await conversion.Start(cancellationTokenSource.Token);

		}

		private static async Task Convert_H264(string fileName, ShellProgressBar.ChildProgressBar pbar, CancellationTokenSource cancellationTokenSource)
		{
			IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(fileName);
			IStream videoStream = mediaInfo.VideoStreams.FirstOrDefault();
			IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault()
				?.SetCodec(AudioCodec.aac);


			string output = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "-H264.mkv");

			var conversion = FFmpeg.Conversions.New();
			conversion
				.AddStream(videoStream, audioStream)
				.UseHardwareAcceleration(HardwareAccelerator.cuvid, VideoCodec.h264_cuvid, VideoCodec.h264_nvenc)
				.AddParameter("-vsync 0", ParameterPosition.PreInput)
				.AddParameter("-map 0:s? -spatial_aq 1 -rc-lookahead 20 -c:s copy", ParameterPosition.PostInput)
				.SetOutput(output)
				.SetOutputFormat(Format.matroska)
				.SetPreset(ConversionPreset.Slow)
				.SetAudioBitrate(224000)
				.SetOverwriteOutput(true);

			
			conversion.OnProgress += (sender, args) =>
			{
				pbar.Tick(args.Percent, Path.GetFileNameWithoutExtension(fileName) + $" - [{ args.TotalLength - args.Duration}]");
			};

			await conversion.Start(cancellationTokenSource.Token);

		}

		private static string cleanPath (string path)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
  				path = path.Replace("\'", "");
			}
			else if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				path = path.Trim('"');
			}
			return path;
		}
	}
}
