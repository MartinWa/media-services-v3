using media_services_v3.Common.Enums;

namespace media_services_v3.Data
{
    public interface IZeroPortalContainer
    {
        IZeroBlob GetPortalBlob(BlobFileType type, string filename);
    }
}