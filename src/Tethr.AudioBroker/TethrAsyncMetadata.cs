using System.Threading.Tasks;
using Tethr.AudioBroker.Model;
using Tethr.AudioBroker.Session;

namespace Tethr.AudioBroker
{
	/// <summary>
	/// Interface for updating metadata for interactions.
	/// </summary>
	public interface ITethrAsyncMetadata
	{
		/// <summary>
		/// Update metadata for the interaction with the specified session Id.
		/// </summary>
		/// <param name="metadata"></param>
		/// <returns></returns>
		Task SendInteractionMetadataBySessionIdAsync(SessionInteractionMetadata metadata);

		/// <summary>
		/// Update metadata for all interactions with the specified master call Id.
		/// </summary>
		/// <param name="metadata"></param>
		/// <returns></returns>
		Task SendInteractionMetadataByMasterCallIdAsync(MasterInteractionMetadata metadata);

		/// <summary>
		/// Update metadata for the case with the specified case reference Id.
		/// </summary>
		/// <param name="metadata"></param>
		/// <returns></returns>
		Task SendInteractionMetadataByCaseReferenceIdAsync(CaseInteractionMetadata metadata);
	}

	public class TethrAsyncMetadata : ITethrAsyncMetadata
	{
		private readonly ITethrSession _tethrSession;

		public TethrAsyncMetadata(ITethrSession tethrSession)
		{
			_tethrSession = tethrSession;
		}

		/// <inheritdoc/>
		public async Task SendInteractionMetadataBySessionIdAsync(SessionInteractionMetadata metadata)
		{
			await _tethrSession.PostAsync("/callEvent/v1/outofband/event", metadata);
		}

		/// <inheritdoc/>
		public async Task SendInteractionMetadataByMasterCallIdAsync(MasterInteractionMetadata metadata)
		{
			await _tethrSession.PostAsync("/callEvent/v1/outofband/mastercall", metadata);
		}

		/// <inheritdoc/>
		public async Task SendInteractionMetadataByCaseReferenceIdAsync(CaseInteractionMetadata metadata)
		{
			await _tethrSession.PostAsync("/callEvent/v1/outofband/case", metadata);
		}
	}
}