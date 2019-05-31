using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Odbc;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tethr.AudioBroker.Model;
using Tethr.AudioBroker.Session;
using Path = System.IO.Path;

namespace Tethr.AudioBroker.DesktopApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public ObservableCollection<MetaDataItem> CallMetaDataItems = new ObservableCollection<MetaDataItem>();

		public MainWindow()
		{
			// Set the product info header, used to update the HTTP User-Agent for requests to Tethr.
			var type = typeof(MainWindow);
			TethrSession.SetProductInfoHeaderValue(type.Namespace, type.Assembly.GetName().Version.ToString());

			InitializeComponent();
			CallMetaData.ItemsSource = CallMetaDataItems;

			ApiUserName.Text = Properties.Settings.Default.ApiUser;
			UrlEndPoint.Text = Properties.Settings.Default.Uri;

			var fields = Properties.Settings.Default.MetaDataFields;
			if (!string.IsNullOrEmpty(fields))
			{
				foreach (var s in fields.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				{
					CallMetaDataItems.Add(new MetaDataItem { Key = s });
				}
			}
		}

		private void AddCallMetaDataClicked(object sender, RoutedEventArgs e)
		{
			CallMetaDataItems.Add(new MetaDataItem());
		}

		public class MetaDataItem
		{
			public string Key { get; set; }
			public string Value { get; set; }
		}

		private void StartUploadClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				var audioFilePath = this.AudioFileName.Text;
				if (!File.Exists(audioFilePath))
				{
					MessageBox.Show("Audio file not found.", "Error preping to send call", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				var audioExt = Path.GetExtension(audioFilePath).ToLowerInvariant();
				var audioType = (string)null;
				switch (audioExt)
				{
					case ".wav":
						audioType = "audio/x-wav";
						break;
					case ".mp3":
						audioType = "audio/mp3";
						break;
					case ".ogg":
						// TODO: we only support OPUS in Ogg, really should open the file and make sure it's OPUS.
						audioType = "audio/ogg";
						break;
					default:
						MessageBox.Show(audioExt + " is not a supported audio file.", "Error preping to send call", MessageBoxButton.OK, MessageBoxImage.Error);
						return;
				}

				var settings = CreateRecordingInfo();

				this.UploadBtn.IsEnabled = false;
				this.UploadProgress.Visibility = Visibility.Visible;
				var hostUri = new Uri(this.UrlEndPoint.Text);
				var apiUser = ApiUserName.Text;
				var apiPassword = ApiPassword.SecurePassword;

				Task.Run(async () =>
				{
					try
					{
						// Because we are doing this in a UI, and that the setting can change from run to run
						// we are creating a new session per request.  However, it is normaly recommended that session
						// be a singleton instanse per processes
						using (var session = new TethrSession(hostUri, apiUser, apiPassword))
						{
							var archiveRecorder = new TethrArchivedRecording(session);
							using (var audioStream = File.OpenRead(audioFilePath))
							{
								var result =await archiveRecorder.SendRecordingAsync(settings, audioStream, audioType);
								this.Dispatcher.Invoke(() => { this.CallId.Text = result.CallId; });
								MessageBox.Show($"Call Upload Successful.\r\n\r\nCall ID : {result.CallId}", "Call Uploaded", MessageBoxButton.OK, MessageBoxImage.Information);
							}
						}
					}
					catch (Exception exception)
					{
						MessageBox.Show(exception.Message, "Error sending call", MessageBoxButton.OK, MessageBoxImage.Error);
					}
					finally
					{
						this.Dispatcher.Invoke(() =>
						{
							this.UploadBtn.IsEnabled = true;
							this.UploadProgress.Visibility = Visibility.Collapsed;

						});
					}
				});
			}
			catch (Exception exception)
			{
				this.UploadBtn.IsEnabled = true;
				this.UploadProgress.Visibility = Visibility.Collapsed;
				MessageBox.Show(exception.Message, "Error preping to send call", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private RecordingInfo CreateRecordingInfo()
		{
			var metadata = new JObject();
			foreach (var callMetaDataItem in CallMetaDataItems)
			{
				metadata[callMetaDataItem.Key] = new JValue(callMetaDataItem.Value);
			}

			if (string.IsNullOrEmpty(this.SessionId.Text))
			{
				SessionId.Text = Guid.NewGuid().ToString();
			}

			var startTime = DateTimeOffset.Now;
			if (string.IsNullOrEmpty(StartTime.Text))
			{
				StartTime.Text = startTime.ToString("o");
			}
			else if (!DateTimeOffset.TryParse(StartTime.Text, out startTime))
			{
				throw new FormatException("Call Start Date and Time, is not a valid date time");
			}

			CallDirection callDirection;
			Enum.TryParse(this.CallDirection.SelectionBoxItem as string ?? "Unknown", out callDirection);

			int agentChan;
			int.TryParse((this.AgentChannel.SelectedItem as ListBoxItem)?.Tag as string ?? "0", out agentChan);

			var settings = new RecordingInfo
			{
				StartTime = startTime.UtcDateTime,
				Metadata = metadata,
				SessionId = this.SessionId.Text,
				MasterCallId = this.MasterCallId.TextValueOrNull(),
				NumberDialed = this.NumberDialed.TextValueOrNull(),
				Direction = callDirection,

				// TODO: Get the channel data from dropdown.
				Contacts = new List<Contact>
				{
					new Contact
					{
						Channel = agentChan == 1 ? 0 : 1,
						Type = "Customer",
						PhoneNumber = this.CustomerNumber.TextValueOrNull()
					}
					, new Contact()
					{
						Channel = agentChan,
						Type = "Agent",
						FirstName = this.AgentFirstName.TextValueOrNull(),
						LastName = this.AgentLastName.TextValueOrNull(),
						PhoneNumber = this.AgentExtention.TextValueOrNull(),
						ReferenceId = this.AgentRefId.TextValueOrNull()
					}
				}
			};

			// Save the settings for next time
			Properties.Settings.Default.ApiUser = ApiUserName.Text;
			Properties.Settings.Default.Uri = UrlEndPoint.Text;
			Properties.Settings.Default.MetaDataFields =
				string.Join(",", CallMetaDataItems.Select(d => d.Key).Where(s => !string.IsNullOrEmpty(s)));
			Properties.Settings.Default.Save();

			settings.EndTime = settings.StartTime.Add(CheckAudioFile());

			return settings;
		}

		private TimeSpan CheckAudioFile()
		{
			AudioInformation.Text = string.Empty;
			// Going to allow this call to work with out an audio file
			// As it's ok to generate the Preview.
			// But if there is a file selected, and it's not a valid format, we will throw an error.
			if (!string.IsNullOrEmpty(this.AudioFileName.Text))
			{
				// Open the Audio file to get it's length and set the end time.
				var ext = Path.GetExtension(this.AudioFileName.Text);
				if (string.Equals(ext, ".wav", StringComparison.OrdinalIgnoreCase))
				{
					using (var reader = new WaveFileReader(this.AudioFileName.Text))
					{
						var knownGoodEncoding = false;
						// This is the list of encoding that we have tested and fully support
						// other encodings may work, just haven't been tested.
						switch (reader.WaveFormat.Encoding)
						{
							case (WaveFormatEncoding)0xA100:
							case WaveFormatEncoding.IeeeFloat:
							case WaveFormatEncoding.Pcm:
							case WaveFormatEncoding.ALaw:
							case WaveFormatEncoding.MuLaw:
								knownGoodEncoding = true;
								break;
						}

						AudioInformation.Text =
							$"Encoding: {reader.WaveFormat.Encoding}{(knownGoodEncoding ? string.Empty : " (!!un-tested encoding!!)")}, Length: {reader.TotalTime}, Sample Size: {reader.WaveFormat.BitsPerSample} bits, Sample Rate: {reader.WaveFormat.SampleRate:N0}";
						if (reader.WaveFormat.Channels > 2)
							throw new InvalidOperationException(
								"While the Tethr API does support mulit-channel auido files, this application is limited to 2 channels at this time.");

						return reader.TotalTime;
					}
				}

				if (string.Equals(ext, ".mp3", StringComparison.OrdinalIgnoreCase))
				{
					using (var reader = new Mp3FileReader(this.AudioFileName.Text))
					{
						AudioInformation.Text =
							$"Encoding: {reader.Mp3WaveFormat.Encoding}, Length: {reader.TotalTime}, Avg Rate: {reader.Mp3WaveFormat.AverageBytesPerSecond / 1024:N0} KBps";

						if (reader.WaveFormat.Channels > 2)
							throw new InvalidOperationException(
								"While the Tethr API does support mulit-channel auido files, this application is limited to 2 channels at this time.");

						return reader.TotalTime;
					}
				}

				if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase))
				{
					// Naudio doesn't support ogg/opus out of the box, so we need to let Tethr tell us if something is wrong.
					AudioInformation.Text = $"Encoding: ogg, Length: ???, Sample Size: ???, Sample Rate: ???";
					return TimeSpan.FromMinutes(5);
				}

				throw new InvalidOperationException($"{ext} is not a supported audio file type.");
			}

			return TimeSpan.FromMinutes(5);
		}

		private void PreviewCallDataClick(object sender, RoutedEventArgs e)
		{
			try
			{
				CallDataPreview.Text = JsonConvert.SerializeObject(CreateRecordingInfo(), Formatting.Indented);
			}
			catch (Exception exception)
			{
				CallDataPreview.Text = string.Empty;
				MessageBox.Show(exception.Message, "Error generating call data preview", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void FileBrowseClick(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog()
			{
				CheckFileExists = true,
				Filter = "Audio files (*.wav, *.mp3)|*.wav;*.mp3|All files (*.*)|*.*",
				Multiselect = false,
				Title = "Select Audio file to send to Tethr"
			};

			if (ofd.ShowDialog(this) == true)
			{
				try
				{
					this.AudioFileName.Text = ofd.FileName;
					CheckAudioFile();
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.Message, "Error preping to send call", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void RefreshCallStateClick(object sender, RoutedEventArgs e)
		{
			var sessionId = this.SessionId.TextValueOrNull();

			if (sessionId != null)
			{
				var hostUri = new Uri(this.UrlEndPoint.Text);
				var apiUser = ApiUserName.Text;
				var apiPassword = ApiPassword.SecurePassword;
				var status = Task.Run(async () =>
				{
					try
					{
						// Because we are doing this in a UI, and that the setting can change from run to run
						// we are creating a new session per request.  However, it is normaly recommended that session
						// be a singleton instanse per processes
						using (var session = new TethrSession(hostUri, apiUser, apiPassword))
						{
							var archiveRecorder = new TethrArchivedRecording(session);
							return await archiveRecorder.GetRecordingStatusAsync(sessionId);
						}
					}
					catch (Exception exception)
					{
						MessageBox.Show(exception.Message, "Error sending call", MessageBoxButton.OK, MessageBoxImage.Error);
						return null;
					}
				}).GetAwaiter().GetResult();

				if (status != null)
				{
					CallId.Text = status.CallId;
				}

				CallState.Text = status?.Status.ToString() ?? "CALL NOT FOUND";
			}
		}
	}
}
