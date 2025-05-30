namespace BackBuddy.Api.Service.V1.Database.Redis
{

    public interface Consumer<TMessage> where TMessage : class
    {
        Task Consume(TMessage message);
    }

    public class Consumer
    {
    }
}
