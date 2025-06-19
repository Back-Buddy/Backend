namespace BackBuddy.Api.Service.V1.Database.Redis
{
    public interface IConsumer<TMessage> where TMessage : class
    {
        Task Consume(TMessage message);
    }
}
