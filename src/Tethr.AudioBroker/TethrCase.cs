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
	/// BETA CLIENT for sending Case sessions to Tethr.
	/// </summary>
	public class TethrCase
	{
		private readonly ITethrSession _tethrSession;

		public TethrCase(ITethrSession tethrSession)
		{
			_tethrSession = tethrSession;
		}

		public async Task<CaseResponse> SendCaseSessionAsync(CaseSession caseSession)
		{
			var result = await
				_tethrSession.PostAsync<CaseResponse>("/caseCapture/v1", caseSession);

			return result;
		}
	}
}
