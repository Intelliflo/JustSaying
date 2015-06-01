using JustSaying.AwsTools.QueueCreation;

namespace JustSaying
{
    class DefaultNamingStrategy : INamingStrategy
    {
        private readonly SqsReadConfiguration sqsReadConfiguration;
        private readonly SqsWriteConfiguration publishConfig;

        public DefaultNamingStrategy(SqsReadConfiguration sqsReadConfiguration, SqsWriteConfiguration publishConfig)
        {
            this.sqsReadConfiguration = sqsReadConfiguration;
            this.publishConfig = publishConfig;
        }

        public string GetTopicName(string messageType)
        {
            return messageType;
        }

        public string GetQueueName(string queueName, string messageType)
        {
            return queueName;
        }
    }
}