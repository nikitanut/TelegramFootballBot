using MediatR;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using TelegramFootballBot.Queue;

namespace TelegramFootballBot.App.Workers
{
    public class QueueConsumerWorker : BackgroundService
    {
        private readonly IConsumer _consumer;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public QueueConsumerWorker(IConsumer consumer, IMediator mediator, ILogger logger)
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

                try
                {
                    await _mediator.Send(message, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.Error("An error occured while processing a queue message", ex);
                    throw;
                }
            }
        }
    }
}
