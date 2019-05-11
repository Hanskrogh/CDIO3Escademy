using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace Escademy.Models
{
    public class UserLanguage
    {
        public int Language { get; set; }
        public int Level { get; set; }

        public static string GetLanguageString(int Language)
        {
            LoadLanguagesFromDB();
            return cachedLanguages[Language];
        }
        public static string[][] GetCachedList()
        {
            LoadLanguagesFromDB();

            lock (mutex)
            {
                return cachedLanguageList.ToArray();
            }
        }

        public static string GetLevelText(int Level)
        {
            switch (Level)
            {
                case 1: return "Basic";
                case 2: return "Conversational";
                case 3: return "Fluent";
                case 4: return "Native/Bilingual";
            }
            return "Undefined";
        }

        private static object mutex = new object();
        private static Dictionary<int, string> cachedLanguages = null;
        private static List<string[]> cachedLanguageList = null;
        private static DateTime cacheExpiration = DateTime.UtcNow;
        private static void LoadLanguagesFromDB() {
            lock (mutex)
            {
                if (cachedLanguages == null || cacheExpiration < DateTime.UtcNow)
                {
                    cachedLanguages = new Dictionary<int, string>();
                    cachedLanguageList = new List<string[]>();

                    using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT * FROM esc_languages";
                            var reader = cmd.ExecuteReader();

                            while (reader.Read())
                            {
                                cachedLanguages.Add(reader.GetInt32("Id"), reader.GetString("Language"));
                                cachedLanguageList.Add(new string[] {
                                    reader.GetInt32("Id").ToString(),
                                    reader.GetString("Language")
                                });
                            }
                        }
                        conn.Close();
                    }

                    cacheExpiration = DateTime.UtcNow.AddHours(10);// Refresh rate!
                }
            }
        }
    }
}