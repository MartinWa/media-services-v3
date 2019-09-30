namespace media_services_v3.Data
{
    public interface IAzureStorage
    {
        IZeroPortalContainer GetPortalContainer(int portalId);
        IZeroSharedContainer GetSharedContainer();
        IZeroContainer GetContainer(string containerName);
        IZeroContentContainer GetContentContainer(int contentId);
        IZeroNotificationContainer GetNotificationContainer();
        string BaseUri();
    }
}