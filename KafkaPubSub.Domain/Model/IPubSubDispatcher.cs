
namespace KafkaPubSub.Domain.Model
{
    public interface IPubSubDispatcher<TEventStream>
            where TEventStream : EventStream
    {
        void DispatchMessage(TEventStream stream);

        void WriteInfoLog(string info);

        void WriteWarnLog(string warn);

        void WriteErrorLog(string error);
    }
}
