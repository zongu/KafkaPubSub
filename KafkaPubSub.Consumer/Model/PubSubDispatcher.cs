
namespace KafkaPubSub.Consumer.Model
{
    using System;
    using KafkaPubSub.Domain.Model;

    public class PubSubDispatcher<TEventStream> : IPubSubDispatcher<TEventStream>
        where TEventStream : EventStream
    {
        public bool DispatchMessage(TEventStream stream)
        {
            Console.WriteLine($"{this.GetType().Name} DispatchMessage Type:{stream.Type}, Data:{stream.Data}");
            return true;
        }
    }
}
