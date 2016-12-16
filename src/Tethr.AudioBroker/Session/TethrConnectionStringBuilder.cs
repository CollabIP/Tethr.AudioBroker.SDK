using System;
using System.ComponentModel;
using System.Configuration;
using System.Data.Common;

namespace Tethr.AudioBroker.Session
{
    public class TethrConnectionStringBuilder : DbConnectionStringBuilder
    {
        public static TethrConnectionStringBuilder Read(string connectionStringName = "Tethr")
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringName];
            if (connectionStringSettings == null)
            {
                throw new InvalidOperationException(
                    $"Could not find a connection string for Tethr. Please add a connection string in the <ConnectionStrings> section of the application's configuration file. For example: <add name=\"{connectionStringName}\" connectionString=\"uri=https://YourCompanyNameHere.Tethr.io/;ApiUser=YourUserNameHere;Password=YourPasswordHere\" />");
            }

            return new TethrConnectionStringBuilder { ConnectionString = connectionStringSettings.ConnectionString };
        }

        [Category("Tethr Audio Connection")]
        [RefreshProperties(RefreshProperties.All)]
        [Description("The URI to the Tethr Audio API server")]
        [DisplayName("Tethr Server URI")]
        public string Uri
        {
            get
            {
                var v = base["uri"];
                return v as string ?? v?.ToString();
            }
            set
            {
                base["uri"] = value;
            }
        }

        [Category("Tethr Audio Connection")]
        [RefreshProperties(RefreshProperties.All)]
        [Description("The Audio ingestor API user name, will use the configurations set on this API user for any calls sent to Tethr.")]
        [DisplayName("Tethr API User")]
        public string ApiUser
        {
            get
            {
                var v = base["apiUser"];
                return v as string ?? v?.ToString();
            }
            set
            {
                base["apiUser"] = value;
            }
        }

        [Category("Tethr Audio Connection")]
        [RefreshProperties(RefreshProperties.All)]
        [Description("The Audio ingestor API user password provided from Tethr")]
        [DisplayName("Tethr API Password")]
        [PasswordPropertyText(true)]
        public string Password
        {
            get
            {
                var v = base["password"];
                return v as string ?? v?.ToString();
            }
            set
            {
                base["password"] = value;
            }
        }
    }
}
