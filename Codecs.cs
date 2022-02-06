using Konsole;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace Transcoder
{
	class Codecs
	{
		public static async Task Convert_H265_cuvid(string fileName, ProgressBar pbar, CancellationTokenSource cancellationTokenSource)
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
				.UseHardwareAcceleration("cuda", (codec == "hevc") ? "hevc" : "h264_cuvid", "hevc_nvenc")
				.AddParameter("-vsync 0", ParameterPosition.PreInput)
				.AddParameter("-map 0:s? -x265-params lossless -spatial_aq 1 -rc-lookahead 20 -c:s copy", ParameterPosition.PostInput) //crf=20
				.SetOutput(output)
				.SetOutputFormat(Format.matroska)
				.SetPreset(ConversionPreset.Slow)
				.SetAudioBitrate(224000)
				.SetOverwriteOutput(true);


			conversion.OnProgress += (sender, args) =>
			{
				pbar.Refresh(args.Percent, Path.GetFileNameWithoutExtension(fileName) + $" - [{ args.TotalLength - args.Duration}]");
			};

			await conversion.Start(cancellationTokenSource.Token);

		}
		public static async Task Convert_H264(string fileName, ProgressBar pbar, CancellationTokenSource cancellationTokenSource)
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
				pbar.Refresh(args.Percent, Path.GetFileNameWithoutExtension(fileName) + $" - [{ args.TotalLength - args.Duration}]");
			};

			await conversion.Start(cancellationTokenSource.Token);

		}
	
	//fmpeg -vsync 0 -hwaccel cuvid -i @file  -c:v hevc_nvenc -vf format=yuv420p -preset slow -c:a copy -map 0 @fname-8BIT.mkv

		public static async Task Convert_10bit_to_8bit(string fileName, ProgressBar pbar, CancellationTokenSource cancellationTokenSource)
		{
			IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(fileName);
			IStream videoStream = mediaInfo.VideoStreams.FirstOrDefault();
			IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault()
										?.SetCodec(AudioCodec.copy);

			string output = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "-8Bit.mkv");
			string codec = mediaInfo.VideoStreams.FirstOrDefault().Codec; //"hevc" - "h264"

			var conversion = FFmpeg.Conversions.New();
			conversion
				.AddStream(videoStream, audioStream)
				.UseHardwareAcceleration("cuda", (codec == "hevc") ? "hevc" : "h264_cuvid", "hevc_nvenc")
				.SetPixelFormat(PixelFormat.yuv420p)
				.AddParameter("-vsync 0", ParameterPosition.PreInput)
				.AddParameter("-map 0:s? -c:s copy", ParameterPosition.PostInput)
				.SetOutput(output)
				.SetOutputFormat(Format.matroska)
				.SetPreset(ConversionPreset.Slow)
				.SetOverwriteOutput(true);


			conversion.OnProgress += (sender, args) =>
			{
				pbar.Refresh(args.Percent, Path.GetFileNameWithoutExtension(fileName) + $" - [{ args.TotalLength - args.Duration}]");
			};

			await conversion.Start(cancellationTokenSource.Token);

		}
	
	}
}
