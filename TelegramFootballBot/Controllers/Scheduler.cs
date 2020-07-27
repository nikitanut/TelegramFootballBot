using Serilog;
using System;
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
        private bool _firstLaunch = true;

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
            if (_firstLaunch)
            {
                _firstLaunch = false;
                await ClearPlayersMessages();
            }
            
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
            await UpdateTeamPollMessagesAsync();            
        }

        private async Task ClearGameAttrsAsync()
        {
            try
            {
                await SheetController.GetInstance().ClearApproveCellsAsync();
                await ClearPlayersMessages();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Clearing game attrs error");
                await _messageController.SendTextMessageToBotOwnerAsync("Ошибка при очищении полей");
            }
        }

        private async Task ClearPlayersMessages()
        {
            var playersToUpdate = await _playerRepository.GetAllAsync();
            foreach (var player in playersToUpdate)
            {
                player.PollMessageId = 0;               
                player.ApprovedPlayersMessageId = 0;                
                player.IsGoingToPlay = false;
            };

            await _playerRepository.UpdateMultipleAsync(playersToUpdate);
        }

        private async Task UpdateTotalPlayersMessagesAsync()
        {
            try
            {
                await _messageController.UpdateTotalPlayersMessagesAsync();
            }
            catch (TaskCanceledException)
            {
                _logger.Error($"The operation was canceled for {nameof(UpdateTotalPlayersMessagesAsync)}.");                
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on updating total players messages, {nameof(UpdateTotalPlayersMessagesAsync)}");
                await _messageController.SendTextMessageToBotOwnerAsync($"Ошибка при обновлении сообщений с отметившимися игроками: {ex.Message}");
            }
        }

        private async Task UpdateTeamPollMessagesAsync()
        {
            try
            {
                await _messageController.UpdatePollMessagesAsync();
            }
            catch (TaskCanceledException)
            {
                _logger.Error($"The operation was canceled for {nameof(UpdateTeamPollMessagesAsync)}.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on updating total players messages, {nameof(UpdateTeamPollMessagesAsync)}");
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
                _logger.Error(ex, $"Error on {nameof(SendQuestionToAllUsersAsync)}");
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
                _logger.Error(ex, $"Error on {nameof(SendGeneratedTeamsMessageAsync)}");
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
