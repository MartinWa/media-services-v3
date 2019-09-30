namespace media_services_v3.Data
{
    public interface IZeroNotificationContainer
    {
        IZeroBlob GetNotificationBlob(string filename);
    }
}