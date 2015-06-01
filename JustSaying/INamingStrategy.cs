namespace JustSaying
{
    public interface INamingStrategy
    {
        string GetTopicName(string messageType);
        string GetQueueName(string queueName, string messageType);
    }
}