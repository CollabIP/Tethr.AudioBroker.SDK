using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tethr.AudioBroker.Model;
using Tethr.AudioBroker.Session;

namespace Tethr.AudioBroker
{
	/// <summary>
	/// BETA CLIENT for sending Chat sessions to Tethr.
	/// </summary>
	public class TethrChat
	{
		private readonly ITethrSession _tethrSession;

		public TethrChat(ITethrSession tethrSession)
		{
			_tethrSession = tethrSession;
		}

		public async Task<ArchiveCallResponse> SendChatSessionAsync(ChatSession chatSession)
		{
			var result = await
				_tethrSession.PostAsync<ArchiveCallResponse>("/chatCapture/v1", chatSession);

			return result;
		}
	}
}
