using System;
using Telegram.Bot;

namespace TelegramFootballBot.Services
{
    public class MessageWorker
    {
        private readonly ITelegramBotClient _client;
        private readonly MessageCallbackService _messageCallbackService;

        private bool _isRunning = false;
        
        public MessageWorker(ITelegramBotClient client, MessageCallbackService messageCallbackService)
        {
            _client = client;
            _messageCallbackService = messageCallbackService;
        }

        public void Run()
        {
            if (_isRunning)
                throw new ApplicationException("MessageService is already running");

            _client.OnMessage += _messageCallbackService.OnMessageRecievedAsync;
            _client.OnCallbackQuery += _messageCallbackService.OnCallbackQueryAsync;
            _client.StartReceiving();
            _isRunning = true;
        }

        public void Stop()
        {
            _client.OnMessage -= _messageCallbackService.OnMessageRecievedAsync;
            _client.OnCallbackQuery -= _messageCallbackService.OnCallbackQueryAsync;
            _client.StopReceiving();
            _isRunning = false;
        }
    }
}
