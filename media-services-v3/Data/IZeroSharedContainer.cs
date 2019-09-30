using media_services_v3.Common.Enums;

namespace media_services_v3.Data
{
    public interface IZeroSharedContainer
    {
        IZeroBlob GetBlob(BlobFileType type, string filename);
    }
}