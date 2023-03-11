using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TelegramFootballBot.App.Workers;
using TelegramFootballBot.Core.Clients;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Helpers;
using TelegramFootballBot.Core.Services;
using TelegramFootballBot.Queue;

namespace TelegramFootballBot.App
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

                    services.AddSingleton<ILogger, Logger>(s => new LoggerConfiguration()
                        .WriteTo.File("logs.txt", outputTemplate: "{Timestamp:dd.MM HH:mm:ss} {Level:u3} - {Message:lj}{NewLine}{Exception}")
                        .CreateLogger());

                    services.AddSingleton<IPlayerRepository>(s => 
                        new PlayerRepository(new DbContextOptionsBuilder<FootballBotDbContext>().UseSqlite("Filename=./BotDb.db").Options));

                    services.AddSingleton<ISheetService>(s =>
                    {
                        using (var credentialsFile = new FileStream("sheetcredentials.json", FileMode.Open, FileAccess.Read))
                        {
                            return new SheetService(credentialsFile, configuration["googleDocSheetId"]);
                        };
                    });
                                        
                    services.AddSingleton<ITelegramBotClient>(s => new TelegramBotClient(configuration["botToken"]));
                    services.AddSingleton<IBotClient, BotClient>();
                    services.AddSingleton<IMessageService, MessageService>();
                    services.AddSingleton<CommandFactory>();
                    services.AddScoped<IUpdateHandler, UpdateHandler>();
                    services.AddScoped<IReceiverService, ReceiverService>();

                    var topic = "telegram-football-bot-events";

                    services.AddSingleton(new ConsumerConfig
                    {
                        BootstrapServers = "localhost:9092"
                    });

                    services.AddSingleton<IConsumer>(s => new KafkaConsumer(
                        new ConsumerConfig
                        {
                            BootstrapServers = "localhost:9092",
                            GroupId = "test"
                        }, topic));

                    services.AddSingleton<IProducerFactory>(s => new KafkaProducerFactory(
                        new ProducerConfig
                        {
                            BootstrapServers = "localhost:9092"
                        }, 
                        topic));

                    services.AddMediatR(s => s.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
                    services.AddHostedService<SchedulerWorker>();
                    //services.AddHostedService<MessageProcessingWorker>();
                    services.AddHostedService<QueueConsumerWorker>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
