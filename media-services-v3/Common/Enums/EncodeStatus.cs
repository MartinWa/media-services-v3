namespace media_services_v3.Common.Enums
{
    public enum EncodeStatus
    {
        NotFound = 0,
        Queued,
        Scheduled,
        Processing,
        Finished,
        Error,
        Canceled,
        Canceling,
        Copying
    }
}