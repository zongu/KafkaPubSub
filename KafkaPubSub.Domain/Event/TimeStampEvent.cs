
namespace KafkaPubSub.Domain.Event
{
    public class TimeStampEvent
    {
        public int SN { get; set; }

        public long UtcDateTimeStamp { get; set; }
    }
}
