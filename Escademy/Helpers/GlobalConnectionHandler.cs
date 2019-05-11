using Escademy.Models;
using MySql.Data.MySqlClient;

namespace Escademy.Helpers
{
    public static class GlobalConnectionHandler
    {
        public readonly static object mutex = new object();
        private static MySqlConnection currentConnection;

        public static MySqlConnection GetActiveConnection()
        {
            lock (mutex) // ensures that the connection cant be disposed if in use.
            {
                if (currentConnection == null || !currentConnection.Ping())
                {
                    if (currentConnection != null) currentConnection.Dispose();
                    currentConnection = CreateNewConnection();                   
                }  
            }

            return currentConnection;
        }
        

        private static MySqlConnection CreateNewConnection()
        {
            var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB"));
            conn.Open();
            return conn;
        }
    }
}