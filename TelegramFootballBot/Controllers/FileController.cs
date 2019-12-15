using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
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
        /// <returns>Is serialization successfull</returns>
        public static async Task<bool> UpdatePlayersAsync(List<Player> players)
        {
            // TODO: MultiAccess

            try
            {                
                using (var sw = new StreamWriter(FILE_NAME))
                {
                    var serializer = new XmlSerializer(typeof(List<Player>));
                    await Task.Run(() => { serializer.Serialize(sw, players); });
                    await sw.FlushAsync();
                    return true;
                }
            }
            catch (SerializationException)
            {
                // TODO log
                return false;
            }
        }

        public static async Task<List<Player>> GetPlayersAsync()
        {
            try
            {
                using (var sr = new StreamReader(FILE_NAME))
                {
                    var serializer = new XmlSerializer(typeof(List<Player>));
                    return await Task.Run(() => { return (List<Player>)serializer.Deserialize(sr); });
                }
            }
            catch (FileNotFoundException)
            {
                // TODO log
                throw;
            }
            catch (SerializationException)
            {
                // TODO log
                throw;
            }
        }
    }
}
