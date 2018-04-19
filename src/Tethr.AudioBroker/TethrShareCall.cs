using System.Threading.Tasks;
using Tethr.AudioBroker.Model;
using Tethr.AudioBroker.Session;

namespace Tethr.AudioBroker
{
	/// <summary>
	/// Used to generate a link to a Tethr call for a guest user. This link expires in 8 hours by default but is configurable.
	/// </summary>
	public interface ITethrCallShare
	{
		/// <summary>
		/// Used to generate a link to a Tethr call for a guest user. This link expires in 8 hours by default but is configurable.
		/// </summary>
		Task<CallShareResponse> ShareCall(CallShare callShare);
	}

	/// <inheritdoc />
	public class TethrCallShare : ITethrCallShare
	{
		private readonly ITethrSession _tethrSession;

		public TethrCallShare(ITethrSession tethrSession)
		{
			_tethrSession = tethrSession;
		}

		/// <inheritdoc />
		public async Task<CallShareResponse> ShareCall(CallShare callShare)
		{
			return await _tethrSession.PostAsync<CallShareResponse>(@"callShare/v1/token", callShare);
		}
	}
}