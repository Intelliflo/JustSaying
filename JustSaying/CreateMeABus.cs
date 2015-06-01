using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;

namespace JustSaying
{
    public static class CreateMeABus
    {
        public static IMayWantOptionalSettings InRegion(params string[] regions)
        {
            var config = new MessagingConfig();

            foreach (var region in regions)
            {
                config.Regions.Add(region);
            }

            config.Validate();

            var messageSerialisationRegister = new MessageSerialisationRegister();
            var justSayingBus = new JustSayingBus(config, messageSerialisationRegister);

            var amazonQueueCreator = new AmazonQueueCreator();
            var bus = new JustSayingFluently(justSayingBus, amazonQueueCreator);

            bus
                .WithMonitoring(new NullOpMessageMonitor())
                .WithSerialisationFactory(new NewtonsoftSerialisationFactory());

            return bus;
        }
    }
}