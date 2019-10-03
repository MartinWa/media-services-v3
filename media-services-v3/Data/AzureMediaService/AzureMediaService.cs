using media_services_v3.Common.Dto;
using media_services_v3.Common.Enums;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace media_services_v3.Data.AzureMediaService
{
    class AzureMediaService : IMediaService
    {
        private readonly ConfigWrapper _settings;
        private readonly IAzureStorage _storage;
        private readonly IAzureMediaServicesClient _client;

        public AzureMediaService(ConfigWrapper settings, IAzureStorage storage, IAzureMediaServicesClient client)
        {
            _settings = settings;
            _storage = storage;
            _client = client;
        }

        public async Task<Job> CreateEncodeJobAsync(IZeroBlob original, string encodedFileName, int contentId, CancellationToken cancellationToken)
        {
            var extension = Path.GetExtension(original.GetName());
            if (extension == null || !_settings.SupportedVideoTypes.Contains(extension.ToLower()))
            {
                throw new NotSupportedException("Video type not supported");
            }
            string uniqueness = $"{contentId}-{Guid.NewGuid().ToString("N").Substring(0, 10)}";
            string jobName = $"job-{uniqueness}";
            string outputAssetName = $"output-{uniqueness}";
            var output = new Asset(description: $"{contentId}-{encodedFileName}");
            var outPutAsset = await _client.Assets.CreateOrUpdateAsync(_settings.ResourceGroup, _settings.AccountName, outputAssetName, output, cancellationToken);
            JobOutput[] jobOutputs =
            {
                new JobOutputAsset(outPutAsset.Name)
            };
            var jobInput = new JobInputHttp
            {
                Files = new[] { original.GetReadSharedAccessUrl("*") }
            };
            IDictionary<string, string> correlationData = null; // Add custom data sent with the job. Can then be used when processing it.
            var job = await _client.Jobs.CreateAsync(
                _settings.ResourceGroup,
                _settings.AccountName,
                _settings.MediaServicesTransform,
                jobName,
                new Job
                {
                    Input = jobInput,
                    Outputs = jobOutputs,
                    CorrelationData = correlationData
                }
                , cancellationToken);
            return job;
        }

        public async Task FinishEncodeJobAsync(string jobName, int contentId, string newFilename, CancellationToken cancellationToken)
        {
            var job = await _client.Jobs.GetAsync(_settings.ResourceGroup, _settings.AccountName, _settings.MediaServicesTransform, jobName);
            if (job == null)
            {
                throw new InvalidOperationException($"No job with id {jobName} was found");
            }
            var encoded = _storage.GetContentContainer(contentId).GetContentBlob(newFilename);
            var firstOutput = job.Outputs.FirstOrDefault() as JobOutputAsset;
            if (job.State == JobState.Error)
            {
                // TODO Look at old code, was it better messages?
                var error = firstOutput == null ? string.Empty : string.Concat(firstOutput.Error.Details.Select(ed => ed.Message));
                throw new Exception(error);
            }
            if (firstOutput == null)
            {
                throw new Exception("Asset not found");
            }
            var assetName = firstOutput.AssetName;
            var asset = await _client.Assets.GetAsync(_settings.ResourceGroup, _settings.AccountName, assetName);
            var assetContainer = _storage.GetContainer(asset.Container);
            await encoded.CopyBlobAsync(assetContainer.GetBlob(encoded.GetName()));
            await _client.Assets.DeleteAsync(_settings.ResourceGroup, _settings.AccountName, assetName);
            await _client.Jobs.DeleteAsync(_settings.ResourceGroup, _settings.AccountName, _settings.MediaServicesTransform, jobName);
        }

        public async Task<MediaEncodeProgressDto> GetEncodeProgressAsync(string jobName, IZeroBlob resultingFile)
        {
            var job = await _client.Jobs.GetAsync(_settings.ResourceGroup, _settings.AccountName, _settings.MediaServicesTransform, jobName);
            if (job == null)
            {
                return new MediaEncodeProgressDto
                {
                    Status = EncodeStatus.NotFound,
                    ProgressPercentage = 0,
                    Errors = "Not found"
                };
            }
            var status = ConvertToEncodeStatus(job.State);
            var firstOutput = job.Outputs.FirstOrDefault() as JobOutputAsset;
            switch (status)
            {
                case EncodeStatus.Processing:
                    return new MediaEncodeProgressDto
                    {
                        Status = status,
                        ProgressPercentage = firstOutput.Progress
                    };
                case EncodeStatus.Finished:
                    var exists = await resultingFile.ExistsAsync();
                    var size = await resultingFile.GetSizeAsync();
                    if (exists && size < 1)
                    {
                        status = EncodeStatus.Copying;
                    }
                    return new MediaEncodeProgressDto
                    {
                        Status = status
                    };
                case EncodeStatus.Error:
                    return new MediaEncodeProgressDto
                    {
                        Status = status,
                        ProgressPercentage = firstOutput?.Progress ?? 0,
                        Errors = firstOutput == null ? string.Empty : string.Concat(firstOutput.Error.Details.Select(ed => ed.Message))
                    };
                default:
                    return new MediaEncodeProgressDto
                    {
                        Status = status
                    };
            }
        }

        private static EncodeStatus ConvertToEncodeStatus(JobState state)
        {
            if (state == JobState.Queued)
                return EncodeStatus.Queued;
            if (state == JobState.Scheduled)
                return EncodeStatus.Scheduled;
            if (state == JobState.Processing)
                return EncodeStatus.Processing;
            if (state == JobState.Finished)
                return EncodeStatus.Finished;
            if (state == JobState.Error)
                return EncodeStatus.Error;
            if (state == JobState.Canceled)
                return EncodeStatus.Canceled;
            if (state == JobState.Canceling)
                return EncodeStatus.Canceling;
            return EncodeStatus.NotFound;
        }
    }
}