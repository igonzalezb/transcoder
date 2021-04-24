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
		enum myCodecs
		{
			H265, H264
		}
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
			
			List<string> videos = new List<string>();

			if (Path.HasExtension(path) && (Path.GetExtension(path) == ".mkv"))
			{
				videos.Add(path);					
			}

			else
			{
				bool recurse = false;
				string videoFilter = @"$(?<=\.(mkv|mp4|avi|mk3d|flv|wmv|m4v|webm))";

				videos = Directory
					.GetFiles(path, "*.*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
					.Where(x => Regex.IsMatch(x, videoFilter, RegexOptions.IgnoreCase))
					.ToList();				

			}
			foreach (string video in videos)
			{
				Console.WriteLine(Path.GetFileName(video));
			}
			Console.WriteLine("");

			
			if (Prompt.GetYesNo("Look Good?", false))
			{
				myCodecs codec;
				while(true)
				{
					codec = (myCodecs)Prompt.GetInt("[0] h265, [1] h264");
					if(codec == myCodecs.H264 || codec == myCodecs.H265)
					{
						Console.Clear();
						break;
					}
					else
					{
						Console.WriteLine("Input must be 0 or 1");
					}
				}
				
				try
				{
					await StartConverting(videos, codec);
					Console.WriteLine("Finished All.");	
				}
				catch (System.Exception)
				{
					Console.WriteLine("Stopped by user");
				}
								
			}

			Console.WriteLine("Press Any Key to Exit");
			Console.ReadKey();
		}


		
		private static async Task StartConverting(List<string> videos, myCodecs _codec)
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
			if(_codec == myCodecs.H265)
			{
				for (int i = 0; i < videos.Count; i++, i++)
				{
					if ((i + 1) < videos.Count)
					{
						await Task.WhenAll(
							Convert_H265_cuvid(videos[i], pbarList[i]), 
							Convert_H265_cuvid(videos[i + 1], pbarList[i+1]));
						
						pbar.Tick();
						pbar.Tick($"Finished: {i + 2} of {videos.Count} - (Press ctrl+c to cancel)");
					}

					else
					{
						await Convert_H265_cuvid(videos[i], pbarList[i]);
						pbar.Tick($"Finished: {i + 1} of {videos.Count} - (Press ctrl+c to cancel)");
					}

				}
			}
			else if(_codec == myCodecs.H264)
			{
				for (int i = 0; i < videos.Count; i++, i++)
				{
					if ((i + 1) < videos.Count)
					{
						await Task.WhenAll(
							Convert_H264(videos[i], pbarList[i]), 
							Convert_H264(videos[i + 1], pbarList[i+1]));
						
						pbar.Tick();
						pbar.Tick($"Finished: {i + 2} of {videos.Count} - (Press ctrl+c to cancel)");
					}

					else
					{
						await Convert_H264(videos[i], pbarList[i]);
						pbar.Tick($"Finished: {i + 1} of {videos.Count} - (Press ctrl+c to cancel)");
					}

				}
			}
		}

		private static async Task Convert_H265_cuvid(string fileName, ShellProgressBar.ChildProgressBar pbar)
		{
			IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(fileName);
			IStream videoStream = mediaInfo.VideoStreams.FirstOrDefault()
				?.SetCodec("hevc_nvenc");
			IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault()
				?.SetCodec(AudioCodec.aac);

			string codec = mediaInfo.VideoStreams.FirstOrDefault().Codec; //"hevc" - "h264"

			var cancellationTokenSource = new CancellationTokenSource();

			string output = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "-H265.mkv");

			var conversion = FFmpeg.Conversions.New();
			conversion
				.AddStream(videoStream, audioStream)
				.AddStream(mediaInfo.SubtitleStreams)
				.UseHardwareAcceleration("cuda", (codec=="hevc") ? "hevc" : "h264_cuvid", "hevc_nvenc")
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

				private static async Task Convert_H264(string fileName, ShellProgressBar.ChildProgressBar pbar)
		{
			IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(fileName);
			IStream videoStream = mediaInfo.VideoStreams.FirstOrDefault();
			IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault()
				?.SetCodec(AudioCodec.aac);

			var cancellationTokenSource = new CancellationTokenSource();

			string output = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "-H264.mkv");

			var conversion = FFmpeg.Conversions.New();
			conversion
				.AddStream(videoStream, audioStream)
				.AddStream(mediaInfo.SubtitleStreams)
				.UseHardwareAcceleration(HardwareAccelerator.cuvid, VideoCodec.h264_cuvid, VideoCodec.h264_nvenc)
				.AddParameter("-vsync 0", ParameterPosition.PreInput)
				.AddParameter("-spatial_aq 1 -rc-lookahead 20", ParameterPosition.PostInput)
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
