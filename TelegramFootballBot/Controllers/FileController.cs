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
        public static void UpdatePlayers(IEnumerable<Player> players)
        {
            using (var sw = new StreamWriter(FILE_NAME))
            {
                var serializer = new XmlSerializer(typeof(List<Player>));
                serializer.Serialize(sw, new List<Player>(players));
                sw.Flush();
            }
        }

        /// <summary>
        /// Gets list of players from file
        /// </summary>
        /// <returns>List of players</returns>
        public static List<Player> GetPlayers()
        {
            using (var sr = new StreamReader(FILE_NAME))
            {
                var serializer = new XmlSerializer(typeof(List<Player>));
                return (List<Player>)serializer.Deserialize(sr);
            }
        }
    }
}
