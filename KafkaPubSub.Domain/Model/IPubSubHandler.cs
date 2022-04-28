
namespace KafkaPubSub.Domain.Model
{
    public interface IPubSubHandler<TEventStream>
        where TEventStream : EventStream
    {
        void Handle(TEventStream stream);
    }
}
