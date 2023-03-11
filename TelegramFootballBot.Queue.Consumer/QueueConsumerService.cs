using MediatR;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TelegramFootballBot.Queue.Consumer
{
    public class QueueConsumerService : BackgroundService
    {
        private readonly IConsumer _consumer;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public QueueConsumerService(IConsumer consumer, IMediator mediator, ILogger logger)
        {
            _consumer = consumer;
            _mediator = mediator;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var message = _consumer.Consume();
                var type = Type.GetType($"TelegramFootballBot.Queue.Messages.{message?.Type}");

                if (type is null)
                {
                    _logger.Information("Unknown message type: {MessageType}", message?.Type);
                    continue;
                }

                try
                {
                    await _mediator.Send(message, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.Error("An error occured while processing a queue message", ex);
                    throw;
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
