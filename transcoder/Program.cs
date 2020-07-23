using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Konsole;
using McMaster.Extensions.CommandLineUtils;
using System.Collections.Generic;
using PCPerformance;
using System.Threading;

using System.Diagnostics;

namespace Transcoder
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.Title = "Transcoder";

			Console.WriteLine("Enter path:");
			string path = Console.ReadLine();

			Console.Clear();
			Console.CursorVisible = false;

			//await DownloadM3U8(path, @"C:\Users\inaki\Videos\downloads\test.mp4");
			path = path.Trim('"');

			
			if (Path.HasExtension(path) && (Path.GetExtension(path) == ".mkv"))
			{
				Console.Clear();
				//var per = new PerformanceMonitor();

				//per.StartPerformanceBoxAsync();
				await H265_cuvid(path);
				//await Task.WhenAny(H265_cuvid(path), per.StartPerformanceBoxAsync());
				//per.Working = false;
				Console.WriteLine("Finished All.");
				
			}

			else
			{
				bool recurse = false;
				string videoFilter = @"$(?<=\.(mkv|mp4|avi|mk3d|flv|wmv|m4v|webm))";
				string subsFilter = @"$(?<=\.(srt|idx|sub))";

				var videos = Directory
					.GetFiles(path, "*.*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
					.Where(x => Regex.IsMatch(x, videoFilter, RegexOptions.IgnoreCase))
					.ToList();
				var subtitlesSRT = Directory
					.GetFiles(path, "*.*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
					.Where(x => Regex.IsMatch(x, subsFilter, RegexOptions.IgnoreCase))
					.ToList();

				foreach (var video in videos)
				{
					Console.WriteLine(Path.GetFileName(video));
				}
				Console.WriteLine("");
				if (Prompt.GetYesNo("Look Good?", true))
				{
					Console.Clear();
					//var per = new PerformanceMonitor();

					//per.StartPerformanceBoxAsync();
					await StartConverting(videos);
					//await Task.WhenAny(per.StartPerformanceBoxAsync(), StartConverting(videos));
					//per.Working = false;
					
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

		private static async Task DownloadM3U8(string url, string outputPath)
		{
			var _url = new Uri(url);

			IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(url);

			//var conversion = FFmpeg.Conversions.New();
			var conversion = await FFmpeg.Conversions.FromSnippet.SaveM3U8Stream(_url, outputPath);
			conversion
			//	.AddStream(mediaInfo.VideoStreams.FirstOrDefault().CopyStream())
			//	.AddStream(mediaInfo.AudioStreams.FirstOrDefault().CopyStream())
			//	.AddStream(mediaInfo.SubtitleStreams)
				.SetOverwriteOutput(true)
			;

			var p = new ProgressBar(PbStyle.SingleLine, 100);


			conversion.OnProgress += (sender, args) =>
			{
				p.Refresh(args.Percent, Path.GetFileNameWithoutExtension(outputPath) + $" - [{ args.TotalLength - args.Duration}]");
			};


			await conversion.Start();
		}
	}
}
