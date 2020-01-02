using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Controllers
{
    public static class FileController
    {
        private const string FILE_NAME = "Players.xml";

        /// <summary>
        /// Saves list of known players to file
        /// </summary>
        /// <param name="players">List of known players</param>
        public static void UpdatePlayersAsync(List<Player> players)
        {
            // TODO: MultiAccess

            using (var sw = new StreamWriter(FILE_NAME))
            {
                var serializer = new XmlSerializer(typeof(List<Player>));
                serializer.Serialize(sw, players);
                sw.Flush();
            }
        }

        public static List<Player> GetPlayersAsync()
        {
            using (var sr = new StreamReader(FILE_NAME))
            {
                var serializer = new XmlSerializer(typeof(List<Player>));
                return (List<Player>)serializer.Deserialize(sr);
            }
        }
    }
}
