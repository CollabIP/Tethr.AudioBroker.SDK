using System;
using System.Data.Common;

namespace Tethr.AudioBroker.Session
{
	/// <summary>
	/// Configuration options for <see cref="ITethrSession"/>.
	/// </summary>
	public class TethrSessionOptions
	{
		/// <summary>
		/// The URI to the Tethr Audio API server.
		/// </summary>
		public string Uri { get; set; }
		
		/// <summary>
		/// The Audio ingestor API user name, will use the configurations set on this API user for any calls sent to Tethr.
		/// </summary>
		public string ApiUser { get; set; }
		
		/// <summary>
		/// The Audio ingestor API user password provided from Tethr.
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// Initializes a new instance from the specified connection string.
		/// </summary>
		/// <remarks>
		/// The connection string should contain a URI, an ApiUser, and a Password for connecting to Tethr.
		/// For example, uri=https://YourCompanyNameHere.Audio.Tethr.io/;ApiUser=YourUserNameHere;Password=YourPasswordHere
		/// </remarks>
		/// <param name="connectionString">The Tethr connection string.</param>
		/// <returns></returns>
		public static TethrSessionOptions InitializeFromConnectionString(string connectionString)
		{
			var builder = new DbConnectionStringBuilder();
			builder.ConnectionString = connectionString;
			
			if (!builder.TryGetValue("uri", out var uri))
				throw new InvalidOperationException("Could not find the URI value in the connection string.");
			
			if (!builder.TryGetValue("ApiUser", out var apiUser))
				throw new InvalidOperationException("Could not find the ApiUser value in the connection string.");
			
			if (!builder.TryGetValue("Password", out var password))
				throw new InvalidOperationException("Could not find the Password value in the connection string.");
			
			return new TethrSessionOptions
			{
				Uri = uri as string ?? uri.ToString(),
				ApiUser = apiUser as string ?? apiUser.ToString(),
				Password = password as string ?? password.ToString()
			};
		}
	}
}