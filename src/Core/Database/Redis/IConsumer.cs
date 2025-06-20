namespace BackBuddy.Core.Library.Database.Redis
{
    public interface IConsumer<in TMessage> where TMessage : class
    {
        Task Consume(TMessage message);
    }
}
