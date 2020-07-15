using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Konsole;
using McMaster.Extensions.CommandLineUtils;

namespace Transcoder
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("Enter path:");
			string path = Console.ReadLine();

			Console.Clear();

			if (Path.HasExtension(path) && (Path.GetExtension(path) == ".mkv"))
			{

				await StartConverting(path);
				Console.WriteLine("Finished All.");

			}

			else
			{
				bool recurse = true;
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
				if (Prompt.GetYesNo("Look Good?", true))
				{
					Console.Clear();
					for (int i = 0; i < videos.Count; i++, i++)
					{

						if ((i + 1) < videos.Count)
						{
							await Task.WhenAll(StartConverting(videos[i]), StartConverting(videos[i + 1]));
						}

						else
							await StartConverting(videos[i]);

					}

					Console.WriteLine("All Done!");
				}

			}
		}


		////ffmpeg - stats - vsync 0 - hwaccel cuvid - c:v h264_cuvid -i @file - c:v hevc_nvenc -x265 -params crf = 20 - spatial_aq 1 - rc - lookahead 20 - preset slow - c:a aac -b:a 224k - map 0 @fname - TRANSCODED.mkv

		private static async Task StartConverting(string fileName)
		{
			IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(fileName);
			IStream videoStream = mediaInfo.VideoStreams.FirstOrDefault()
				?.SetCodec("hevc_nvenc");
			IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault()
				?.SetCodec(AudioCodec.aac);


			string output = Path.Combine(Path.GetDirectoryName(fileName), "out", Path.GetFileNameWithoutExtension(fileName) + "-trans.mkv");

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

			await conversion.Start();

		}
	}
}
