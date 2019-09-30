using System.Collections.Generic;
using System.Threading.Tasks;

namespace media_services_v3.Data
{
    public interface IZeroContentContainer
    {
        IZeroBlob GetContentBlob(string filename);
        Task<IEnumerable<IZeroBlob>> GetAllContentBlobs();
    }
}