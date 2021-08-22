using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Transcoder
{
	class FileManagerUtils
	{
		public static List<string> getVideoFiles(string[] args, bool recurse = false)
		{
			List<string> videos = new List<string>();
			string path;
			string videoFilter = @"$(?<=\.(mkv|mp4|avi|mk3d|flv|wmv|m4v|webm))";
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(Figgle.FiggleFonts.Standard.Render("Transcoder"));
			Console.ResetColor();

			if (!args.Any())
			{
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


			return videos;
		}
		private static string cleanPath(string path)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				path = path.Replace("\'", "");
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				path = path.Trim('"');
			}
			return path;
		}

	}
}
