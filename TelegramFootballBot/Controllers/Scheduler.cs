using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TelegramFootballBot.Data;
using TelegramFootballBot.Helpers;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Controllers
{
    public class Scheduler
    {
        private readonly Timer _timer;
        private readonly MessageController _messageController;
        private readonly TeamsController _teamsController;
        private readonly IPlayerRepository _playerRepository;
        private readonly ILogger _logger;
        int i = 0;

        public Scheduler(MessageController messageController, TeamsController teamSet, IPlayerRepository playerRepository, ILogger logger)
        {            
            _messageController = messageController;
            _playerRepository = playerRepository;
            _logger = logger;
            _timer = new Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            _teamsController = teamSet;
            _teamsController.OnDislike += SendGeneratedTeamsMessage;
        }

        public void Run()
        {
            var minuteInterval = 60 * 1000;
            _timer.Change(0, minuteInterval); 
        }

        private async void OnTimerElapsed(object state)
        {
            var now = DateTime.UtcNow;

            if (DistributionTimeHasCome(now))
                await SendQuestionToAllUsersAsync();

            if (TeamsGenerationTimeHasCome(now))
            {
                await _teamsController.GenerateNewTeams();
                await SendGeneratedTeamsMessageAsync();
            }

            if (GameStarted(now))
            {
                await ClearGameAttrsAsync();
                _teamsController.ClearGeneratedTeams();
            }

            await UpdateTotalPlayersMessagesAsync();
        }

        private async Task ClearGameAttrsAsync()
        {
            try
            {
                await SheetController.GetInstance().ClearApproveCellsAsync();

                var playersToUpdate = (await _playerRepository.GetAllAsync()).Where(p => p.IsGoingToPlay || p.ApprovedPlayersMessageId != 0).ToList();
                foreach (var player in playersToUpdate)
                {
                    player.IsGoingToPlay = false;
                    player.ApprovedPlayersMessageId = 0;
                };

                await _playerRepository.UpdateMultipleAsync(playersToUpdate);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Clearing game attrs error");
                await _messageController.SendTextMessageToBotOwnerAsync("Ошибка при очищении полей");
            }
        }

        private async Task UpdateTotalPlayersMessagesAsync()
        {
            try
            {
                await _messageController.UpdateTotalPlayersMessagesAsync();
            }
            catch (TaskCanceledException)
            {
                _logger.Error("The operation was canceled for UpdateTotalPlayersMessagesAsync.");                
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on updating total players messages");
                await _messageController.SendTextMessageToBotOwnerAsync($"Ошибка при обновлении сообщений с отметившимися игроками: {ex.Message}");
            }
        }

        private async Task SendQuestionToAllUsersAsync()
        {
            try
            {
                await _messageController.SendDistributionQuestionAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error on StartPlayersSetDeterminationAsync");
                await _messageController.SendTextMessageToBotOwnerAsync($"Ошибка при определении списка игроков: {ex.Message}");
            }
        }

        private async void SendGeneratedTeamsMessage(object sender, EventArgs args)
        {
            await SendGeneratedTeamsMessageAsync();
        }

        private async Task SendGeneratedTeamsMessageAsync()
        {
            try
            {
                await _messageController.SendTeamPollMessageAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error on SendGeneratedTeamsMessageAsync");
                await _messageController.SendTextMessageToBotOwnerAsync($"Ошибка при отправке сообщения с командами: {ex.Message}");
            }
        }

        private bool DistributionTimeHasCome(DateTime currentDate)
        {
            var distributionDate = GetNearestDistributionDateMoscowTime(currentDate);
            return GetDayOfWeek(currentDate.ToMoscowTime()) == GetDayOfWeek(distributionDate)
                && currentDate.ToMoscowTime().TimeOfDay.Hours == distributionDate.TimeOfDay.Hours
                && currentDate.ToMoscowTime().TimeOfDay.Minutes == distributionDate.TimeOfDay.Minutes;
        }

        private bool TeamsGenerationTimeHasCome(DateTime currentDate)
        {
            var gameDate = GetNearestGameDateMoscowTime(currentDate);
            return currentDate.ToMoscowTime().Year == gameDate.Year
                && currentDate.ToMoscowTime().Month == gameDate.Month
                && currentDate.ToMoscowTime().Day == gameDate.Day
                && currentDate.ToMoscowTime().Hour == gameDate.Hour - 2
                && currentDate.ToMoscowTime().Minute == gameDate.Minute;
        }

        private bool GameStarted(DateTime currentDate)
        {
            var gameDate = GetNearestGameDateMoscowTime(currentDate);
            return currentDate.ToMoscowTime().Year == gameDate.Year 
                && currentDate.ToMoscowTime().Month == gameDate.Month 
                && currentDate.ToMoscowTime().Day == gameDate.Day
                && currentDate.ToMoscowTime().Hour == gameDate.Hour 
                && currentDate.ToMoscowTime().Minute == gameDate.Minute;
        }
        
        public static DateTime GetNearestGameDateMoscowTime(DateTime currentDate)
        {
            return GetNearestDate(currentDate, 
                AppSettings.GameDay.Days, 
                AppSettings.GameDay.Hours, 
                AppSettings.GameDay.Minutes);
        }

        public static DateTime GetNearestDistributionDateMoscowTime(DateTime currentDate)
        {
            return GetNearestDate(currentDate,
                AppSettings.DistributionTime.Days,
                AppSettings.DistributionTime.Hours,
                AppSettings.DistributionTime.Minutes);
        }
        
        private static DateTime GetNearestDate(DateTime currentDate, int eventDayOfWeek, int eventHour, int eventMinutes)
        {
            if (eventDayOfWeek > 7)
                throw new ArgumentOutOfRangeException("eventDayOfWeek");

            var eventDate = currentDate.ToMoscowTime().Date;
            var dayOfWeek = GetDayOfWeek(eventDate);

            while (eventDayOfWeek != dayOfWeek)
            {
                eventDate = eventDate.AddDays(1);
                dayOfWeek = GetDayOfWeek(eventDate);
            }

            eventDate = eventDate.AddHours(eventHour).AddMinutes(eventMinutes);
            return eventDate;
        }

        private static int GetDayOfWeek(DateTime date)
        {
            return date.DayOfWeek != 0 ? (int)date.DayOfWeek : 7;
        }
    }
}
