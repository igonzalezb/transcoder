using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Konsole;
using McMaster.Extensions.CommandLineUtils;
using System.Collections.Generic;
using System.Threading;

using System.Diagnostics;
using System.Runtime.InteropServices;

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
				Console.CursorVisible = false;
			}
			else
			{
				path = args[0].ToString();
			}
			
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
				Console.WriteLine(Path.GetFileName(path) + " => " +Path.GetFileNameWithoutExtension(path) + "-trans.mkv");
				Console.WriteLine("");
				
				if (Prompt.GetYesNo("Look Good?", true))
				{
					Console.Clear();
					await H265_cuvid(path);
					Console.WriteLine("Finished All.");				
				}
							
			}

			else
			{
				bool recurse = false;
				string videoFilter = @"$(?<=\.(mkv|mp4|avi|mk3d|flv|wmv|m4v|webm))";
				//string subsFilter = @"$(?<=\.(srt|idx|sub))";

				var videos = Directory
					.GetFiles(path, "*.*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
					.Where(x => Regex.IsMatch(x, videoFilter, RegexOptions.IgnoreCase))
					.ToList();

				foreach (var video in videos)
				{
					Console.WriteLine(Path.GetFileName(video) + " => " +Path.GetFileNameWithoutExtension(video) + "-trans.mkv");
				}
				Console.WriteLine("");
				if (Prompt.GetYesNo("Look Good?", true))
				{
					Console.Clear();
					await StartConverting(videos);					
				}

			}

			Console.WriteLine("Press Any Key to Exit");
			Console.ReadKey();
		}


		
		private static async Task StartConverting(List<string> videos)
		{
			for (int i = 0; i < videos.Count; i++, i++)
			{

				if ((i + 1) < videos.Count)
				{
					await Task.WhenAll(H265_cuvid(videos[i]), H265_cuvid(videos[i + 1]));
				}

				else
					await H265_cuvid(videos[i]);

			}

			Console.WriteLine("All Done!");
		}

		private static async Task H265_cuvid(string fileName)
		{
			IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(fileName);
			IStream videoStream = mediaInfo.VideoStreams.FirstOrDefault()
				?.SetCodec("hevc_nvenc");
			IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault()
				?.SetCodec(AudioCodec.aac);

			var cancellationTokenSource = new CancellationTokenSource();

			string output = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "-trans.mkv");

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

			var p = new ProgressBar(PbStyle.SingleLine, 100);

			conversion.OnProgress += (sender, args) =>
			{
				p.Refresh(args.Percent, Path.GetFileNameWithoutExtension(fileName) + $" - [{ args.TotalLength - args.Duration}]");
			};

			Console.CancelKeyPress += (sender, args) =>
			{
				args.Cancel = true;
				Console.WriteLine("Stopped by user");
				cancellationTokenSource.Cancel(true);
			};
			await conversion.Start(cancellationTokenSource.Token);

		}
	}
}
