using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ValueRange = Google.Apis.Sheets.v4.Data.ValueRange;

namespace TelegramFootballBot.Controllers
{
    public class SheetController
    {
        private readonly string[] _scopes = { SheetsService.Scope.Spreadsheets };
        private readonly string _sheetName = "Участие в играх";
        private readonly SheetsService _sheetsService;

        public SheetController()
        {
            _sheetsService = GetSheetsService();
        }

        private SheetsService GetSheetsService()
        {
            GoogleCredential credential;
            var binPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(_scopes);
            }

            return new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "TelegramFootballBot"
            });
        }

        public async Task<string> UpdateData(IList<IList<object>> data)
        {
            var range = $"{_sheetName}!C33";

            var dataValueRange = new ValueRange();
            dataValueRange.Range = range;
            dataValueRange.Values = data;
            
            var request = _sheetsService.Spreadsheets.Values.Append(dataValueRange, AppSettings.GoogleDocUrl, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

            var response = await request.ExecuteAsync();

            return JsonConvert.SerializeObject(response);
        }
    }
}

