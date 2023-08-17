using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tethr.AudioBroker
{
	public static class MimeAudioExtensions
	{
		private class MimeDescription
		{
			public readonly AudioMimeType MimeType;
			public readonly string MimeName;
			public readonly string Extension;

			public MimeDescription(AudioMimeType mimeType, string mimeName, string extension)
			{
				MimeType = mimeType;
				MimeName = mimeName;
				Extension = extension;
			}
		}

		private static readonly List<MimeDescription> Types = new List<MimeDescription>
		{
			new MimeDescription(AudioMimeType.Wav, "audio/wav", "wav"),
			new MimeDescription(AudioMimeType.Wav, "audio/x-wav", "wav"),
			new MimeDescription(AudioMimeType.Wav, "audio/wave", "wav"),
			new MimeDescription(AudioMimeType.Wav, "audio/vnd.wav", "wav"),
			new MimeDescription(AudioMimeType.Wav, "audio/x-wave", "wav"),
			new MimeDescription(AudioMimeType.Mp3, "audio/mp3", "mp3"),
			new MimeDescription(AudioMimeType.Opus, "audio/ogg", "opus"),
			new MimeDescription(AudioMimeType.Mp4, "audio/mp4", "mp4"),
			new MimeDescription(AudioMimeType.Mp4, "audio/m4a", "mp4"),
			new MimeDescription(AudioMimeType.Mp4Helium, "audio/mp4-helium", "mp4helium"),
			new MimeDescription(AudioMimeType.Mp4Helium, "audio/m4a-helium", "mp4helium"),
			new MimeDescription(AudioMimeType.Wma, "audio/wma", "wma"),
			new MimeDescription(AudioMimeType.WmaHelium, "audio/wma-helium", "wmahelium"),
		};

		public static AudioMimeType ToAudioMimeType(this string mimeType)
		{
			var searchType = mimeType.ToLower();
			var type = Types.FirstOrDefault(p => p.MimeName == searchType);
			return type?.MimeType ?? AudioMimeType.Unknown;
		}

		public static string MimeTypeToAudioExtension(this string mimeType)
		{
			var searchType = mimeType.ToLower();
			var type = Types.FirstOrDefault(p => p.MimeName == searchType);
			return type?.Extension;
		}

		public static string AudioExtensionToMimeType(this string extension)
		{
			var searchType = extension.ToLower();
			var type = Types.FirstOrDefault(p => p.Extension == searchType);
			return type?.MimeName;
		}

		public static string SupportedMimeTypes()
		{
			var s = new StringBuilder();
			foreach (var type in Types)
			{
				s.Append(type.MimeName);
				s.Append(", ");
			}

			var types = s.ToString();
			return types.Substring(0, types.Length - 2);
		}

		public static string SupportedAudioExtensions()
		{
			var s = new StringBuilder();
			foreach (var extension in Types.Select(p => p.Extension).Distinct())
			{
				s.Append(extension);
				s.Append(", ");
			}

			var extensions = s.ToString();
			return extensions.Substring(0, extensions.Length - 2);
		}
	}
}