
namespace KafkaPubSub.Domain.Model
{
    public class EventStream
    {
        public long UtcTimeStamp { get; set; }

        public string Type { get; set; }

        public string Data { get; set; }
    }
}
