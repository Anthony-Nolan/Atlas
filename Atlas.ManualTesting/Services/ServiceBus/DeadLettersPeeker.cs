using Atlas.Common.Debugging;

namespace Atlas.ManualTesting.Services.ServiceBus
{
    internal interface IDeadLettersPeeker<T> : IServiceBusPeeker<T>
    {
    }

    internal class DeadLettersPeeker<T> : ServiceBusPeeker<T>, IDeadLettersPeeker<T>
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public DeadLettersPeeker(IDeadLetterReceiverFactory factory, string topicName, string subscriptionName)
            : base(factory, topicName, subscriptionName)
        {
        }
    }
}
