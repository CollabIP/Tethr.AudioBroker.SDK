using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
			InitializeComponent();
			CallMetaData.ItemsSource = CallMetaDataItems;
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
					case "wav":
						audioType = "audio/x-wav";
						break;
					case "mp3":
						audioType = "audio/mp3";
						break;
					case "ogg":
						// TODO: we only support OPUS in Ogg, really should open the file and make sure it's OPUS.
						audioType = "audio/ogg";
						break;
					default:
						MessageBox.Show(audioExt + " is not a supported audio file.", "Error preping to send call", MessageBoxButton.OK, MessageBoxImage.Error);
						return;
				}

				var settings = CreateRecordingInfo();

				this.IsEnabled = false;
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
								await archiveRecorder.SendRecordingAsync(settings, audioStream, audioType);
						}
					}
					catch (Exception exception)
					{
						MessageBox.Show(exception.Message, "Error sending call", MessageBoxButton.OK, MessageBoxImage.Error);
					}
					finally
					{
						this.Dispatcher.Invoke(() => { this.IsEnabled = true; });
					}
				});
			}
			catch (Exception exception)
			{
				this.IsEnabled = true;
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

			var settings = new RecordingInfo
			{
				StartTime = DateTime.Now.ToUniversalTime(),
				Metadata = metadata,
				SessionId = string.IsNullOrEmpty(this.SessionId.Text) ? Guid.NewGuid().ToString() : this.SessionId.Text,
				MasterCallId = string.IsNullOrEmpty(this.MasterCallId.Text) ? null : this.MasterCallId.Text,
				NumberDialed = this.NumberDialed.Text,

				// TODO: Get call Direction from dropdown.
				//Direction = this.CallDirection.SelectionBoxItem == null ? Model.CallDirection.Unknown 

				// TODO: Get the channel data from dropdown.
				Contacts = new List<Contact>
				{
					new Contact
					{
						Channel = 0,
						Type = "Customer",
						PhoneNumber = this.CustomerNumber.Text
					}
					, new Contact()
					{
						Channel = 1,
						Type = "Agent",
						FirstName = this.AgentFirstName.Text,
						LastName = this.AgentLastName.Text,
						PhoneNumber = this.AgentExtention.Text,
						ReferenceId = this.AgentRefId.Text
					}
				}
			};
			return settings;
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
	}
}
