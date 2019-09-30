namespace media_services_v3.Data
{
    public interface IZeroContainer
    {
        IZeroBlob GetBlob(string filename);
    }
}