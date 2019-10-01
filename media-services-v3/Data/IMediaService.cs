using media_services_v3.Common.Dto;
using Microsoft.Azure.Management.Media.Models;
using System.Threading;
using System.Threading.Tasks;

namespace media_services_v3.Data
{
    public interface IMediaService
    {
        Task<Job> CreateEncodeJobAsync(IZeroBlob original, string encodedFileName, int contentId, CancellationToken cancellationToken);
        Task FinishEncodeJobAsync(string jobIdentifier, int contentId, string newFilename,CancellationToken cancellationToken);
        Task<MediaEncodeProgressDto> GetEncodeProgressAsync(string jobIdentifier, IZeroBlob resultingFile);
        string EncodedFileExtension();
    }
}