namespace TelegramFootballBot.Queue
{
    public interface IProducerFactory
    {
        IProducer Create();
    }
}
