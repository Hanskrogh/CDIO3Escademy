using System.Configuration;

namespace Escademy.Models
{
    public class ConnectionString
    {
        // Retrieves a connection string by name.
        // Returns null if the name is not found.
        public static string Get(string name)
        {
            name = "EscademyDB";
            // Assume failure.
            string returnValue = null;

            // Look for the name in the connectionStrings section.
            ConnectionStringSettings settings =
                ConfigurationManager.ConnectionStrings[name];

            // If found, return the connection string.
            if (settings != null)
                returnValue = settings.ConnectionString;

            return returnValue;
        }
    }
}