using media_services_v3.Common.Dto;
using media_services_v3.Common.Enums;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest.Azure.Authentication;
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
            if (job.State == JobState.Error)
            {
                var firstOutput = job.Outputs[0];
                // TODO Look at old code, was it better messages?
                var error = firstOutput == null ? string.Empty : string.Concat(firstOutput.Error.Details.Select(ed => ed.Message));
                throw new Exception(error);
            }

            var output = job.Outputs.FirstOrDefault() as JobOutputAsset;
            if (output == null)
            {
                throw new Exception("Asset not found");
            }
            var assetName = output.AssetName;
            var asset = await _client.Assets.GetAsync(_settings.ResourceGroup, _settings.AccountName, assetName);
            var assetContainer = _storage.GetContainer(asset.Container);
            await encoded.CopyBlobAsync(assetContainer.GetBlob(encoded.GetName()));
            await _client.Assets.DeleteAsync(_settings.ResourceGroup, _settings.AccountName, assetName);
            await _client.Jobs.DeleteAsync(_settings.ResourceGroup, _settings.AccountName, _settings.MediaServicesTransform, jobName);
        }

        public async Task<MediaEncodeProgressDto> GetEncodeProgressAsync(string jobName, IZeroBlob resultingFile)
        {
            var clientCredential = new ClientCredential(_settings.AadClientId, _settings.AadSecret);
            var credentials = await ApplicationTokenProvider.LoginSilentAsync(_settings.AadTenantId, clientCredential, ActiveDirectoryServiceSettings.Azure);
            var client = new AzureMediaServicesClient(_settings.ArmEndpoint, credentials)
            {
                SubscriptionId = _settings.SubscriptionId
            };
            // Set the polling interval for long running operations to 2 seconds.
            // The default value is 30 seconds for the .NET client SDK
            client.LongRunningOperationRetryTimeout = 2;

            var job = await client.Jobs.GetAsync(_settings.ResourceGroup, _settings.AccountName, _settings.MediaServicesTransform, jobName);
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
            switch (status)
            {
                case EncodeStatus.Processing:
                    var progress = job.Outputs.Average(x => x.Progress);
                    return new MediaEncodeProgressDto
                    {
                        Status = status,
                        ProgressPercentage = progress
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
                    var firstOutput = job.Outputs[0];
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


//        private async Task<Job> CreateEncodeJobAsync()
//        {
//            var credentials = await GetCredentialsAsync(config);
//            var client = new AzureMediaServicesClient(config.ArmEndpoint, credentials)
//            {
//                SubscriptionId = config.SubscriptionId,
//            };
//            // Set the polling interval for long running operations to 2 seconds.
//            // The default value is 30 seconds for the .NET client SDK
//            client.LongRunningOperationRetryTimeout = 2;
//            //// Ensure that you have customized transforms for the VideoAnalyzer.  This is really a one time setup operation.
//            //Transform videoAnalyzerTransform = EnsureTransformExists(client, config.ResourceGroup, config.AccountName, VideoAnalyzerTransformName, new VideoAnalyzerPreset());

//            // Creating a unique suffix so that we don't have name collisions if you run the sample
//            // multiple times without cleaning up.
//            string uniqueness = Guid.NewGuid().ToString().Substring(0, 13);
//            string jobName = "job-" + uniqueness;
//            string inputAssetName = "input-" + uniqueness;
//            string outputAssetName = "output-" + uniqueness;

//            CreateInputAsset(client, config.ResourceGroup, config.AccountName, inputAssetName, inputMP4FileName).Wait();

//            JobInput input = new JobInputAsset(assetName: inputAssetName);

//            Asset outputAsset = CreateOutputAsset(client, config.ResourceGroup, config.AccountName, outputAssetName);

//            // Note that you can now pass custom correlation data Dictionary into the job to use via EventGrid or other Job polling listeners.
//            // this is handy for passing tenant ID, or custom workflow data as part of your job.
//            var correlationData = new Dictionary<string, string>();
//            correlationData.Add("customData1", "some custom data to pass through the job");
//            correlationData.Add("custom ID", "some GUID here");

//            Job job = SubmitJob(client, config.ResourceGroup, config.AccountName, VideoAnalyzerTransformName, jobName, input, outputAsset.Name, correlationData);

//            return job;
//            //DateTime startedTime = DateTime.Now;

//            //job = WaitForJobToFinish(client, config.ResourceGroup, config.AccountName, VideoAnalyzerTransformName, jobName);

//            //TimeSpan elapsed = DateTime.Now - startedTime;

//            //if (job.State == JobState.Finished)
//            //{
//            //    Console.WriteLine("Job finished.");
//            //    if (!Directory.Exists(outputFolder))
//            //        Directory.CreateDirectory(outputFolder);
//            //    DownloadResults(client, config.ResourceGroup, config.AccountName, outputAsset.Name, outputFolder).Wait();
//            //}
//            //else if (job.State == JobState.Error)
//            //{
//            //    Console.WriteLine($"ERROR: Job finished with error message: {job.Outputs[0].Error.Message}");
//            //    Console.WriteLine($"ERROR:                   error details: {job.Outputs[0].Error.Details[0].Message}");
//            //}
//        }

//        private static async Task<ServiceClientCredentials> GetCredentialsAsync(ConfigWrapper config)
//        {
//            // Use ApplicationTokenProvider.LoginSilentWithCertificateAsync or UserTokenProvider.LoginSilentAsync to get a token using service principal with certificate
//            //// ClientAssertionCertificate
//            //// ApplicationTokenProvider.LoginSilentWithCertificateAsync

//            // Use ApplicationTokenProvider.LoginSilentAsync to get a token using a service principal with symetric key
//            ClientCredential clientCredential = new ClientCredential(config.AadClientId, config.AadSecret);
//            return await ApplicationTokenProvider.LoginSilentAsync(config.AadTenantId, clientCredential, ActiveDirectoryServiceSettings.Azure);
//        }

//        private static async Task<IAzureMediaServicesClient> CreateMediaServicesClientAsync(ConfigWrapper config)
//        {
//            var credentials = await GetCredentialsAsync(config);

//            return new AzureMediaServicesClient(config.ArmEndpoint, credentials)
//            {
//                SubscriptionId = config.SubscriptionId,
//            };
//        }

//private static Transform EnsureTransformExists(IAzureMediaServicesClient client, string resourceGroupName, string accountName, string transformName, Preset preset)
//{
//    Transform transform = client.Transforms.Get(resourceGroupName, accountName, transformName);

//    if (transform == null)
//    {
//        TransformOutput[] outputs = new TransformOutput[]
//        {
//                    new TransformOutput(preset),
//        };

//        transform = client.Transforms.CreateOrUpdate(resourceGroupName, accountName, transformName, outputs);
//    }

//    return transform;
//}

//        private static async Task<Asset> CreateInputAsset(IAzureMediaServicesClient client, string resourceGroupName, string accountName, string assetName, string fileToUpload)
//        {
//            Console.WriteLine("Creating Input Asset");
//            Asset asset = client.Assets.CreateOrUpdate(resourceGroupName, accountName, assetName, new Asset());



//            ListContainerSasInput input = new ListContainerSasInput()
//            {
//                Permissions = AssetContainerPermission.ReadWrite,
//                ExpiryTime = DateTime.Now.AddHours(2).ToUniversalTime()
//            };

//            var response = client.Assets.ListContainerSasAsync(resourceGroupName, accountName, assetName, input.Permissions, input.ExpiryTime).Result;

//            string uploadSasUrl = response.AssetContainerSasUrls.First();

//            string filename = Path.GetFileName(fileToUpload);
//            Console.WriteLine("Uploading file: {0}", filename);

//            var sasUri = new Uri(uploadSasUrl);
//            CloudBlobContainer container = new CloudBlobContainer(sasUri);
//            var blob = container.GetBlockBlobReference(filename);
//            blob.Properties.ContentType = "video/mp4";
//            Console.WriteLine("Uploading File to container: {0}", sasUri);
//            await blob.UploadFromFileAsync(fileToUpload);

//            return asset;
//        }

//        private static Asset CreateOutputAsset(IAzureMediaServicesClient client, string resourceGroupName, string accountName, string assetName)
//        {
//            Asset input = new Asset();

//            return client.Assets.CreateOrUpdate(resourceGroupName, accountName, assetName, input);
//        }

//        private static Job SubmitJob(IAzureMediaServicesClient client, string resourceGroupName, string accountName, string transformName, string jobName, JobInput jobInput, string outputAssetName, Dictionary<string, string> correlationData)
//        {
//            JobOutput[] jobOutputs =
//            {
//                new JobOutputAsset(outputAssetName),
//            };

//            Job job = client.Jobs.Create(
//                resourceGroupName,
//                accountName,
//                transformName,
//                jobName,
//                new Job
//                {
//                    Input = jobInput,
//                    Outputs = jobOutputs,
//                    CorrelationData = correlationData
//                });

//            return job;
//        }


//        private static Job WaitForJobToFinish(IAzureMediaServicesClient client, string resourceGroupName, string accountName, string transformName, string jobName)
//        {
//            const int SleepInterval = 10 * 1000;

//            Job job = null;
//            bool exit = false;

//            do
//            {
//                job = client.Jobs.Get(resourceGroupName, accountName, transformName, jobName);

//                if (job.State == JobState.Finished || job.State == JobState.Error || job.State == JobState.Canceled)
//                {
//                    exit = true;
//                }
//                else
//                {
//                    Console.WriteLine($"Job is {job.State}.");

//                    for (int i = 0; i < job.Outputs.Count; i++)
//                    {
//                        JobOutput output = job.Outputs[i];

//                        Console.Write($"\tJobOutput[{i}] is {output.State}.");

//                        if (output.State == JobState.Processing)
//                        {
//                            Console.Write($"  Progress: {output.Progress}");
//                        }

//                        Console.WriteLine();
//                    }

//                    System.Threading.Thread.Sleep(SleepInterval);
//                }
//            }
//            while (!exit);

//            return job;
//        }


//        /// <summary>
//        ///  Downloads the results from the specified output asset, so you can see what you got.
//        /// </summary>
//        /// <param name="client">The Media Services client.</param>
//        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
//        /// <param name="accountName"> The Media Services account name.</param>
//        /// <param name="assetName">The output asset.</param>
//        /// <param name="outputFolderName">The name of the folder into which to download the results.</param>
//        private static async Task DownloadResults(IAzureMediaServicesClient client,
//            string resourceGroup,
//            string accountName,
//            string assetName,
//            string outputFolderName)
//        {
//            if (!Directory.Exists(outputFolderName))
//            {
//                Directory.CreateDirectory(outputFolderName);
//            }

//            AssetContainerSas assetContainerSas = await client.Assets.ListContainerSasAsync(
//                resourceGroup,
//                accountName,
//                assetName,
//                permissions: AssetContainerPermission.Read,
//                expiryTime: DateTime.UtcNow.AddHours(1).ToUniversalTime());

//            Uri containerSasUrl = new Uri(assetContainerSas.AssetContainerSasUrls.FirstOrDefault());
//            CloudBlobContainer container = new CloudBlobContainer(containerSasUrl);

//            string directory = Path.Combine(outputFolderName, assetName);
//            Directory.CreateDirectory(directory);

//            Console.WriteLine($"Downloading output results to '{directory}'...");

//            BlobContinuationToken continuationToken = null;
//            IList<Task> downloadTasks = new List<Task>();

//            do
//            {
//                // A non-negative integer value that indicates the maximum number of results to be returned at a time,
//                // up to the per-operation limit of 5000. If this value is null, the maximum possible number of results
//                // will be returned, up to 5000.
//                int? ListBlobsSegmentMaxResult = null;

//                BlobResultSegment segment = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.None, ListBlobsSegmentMaxResult, continuationToken, null, null);

//                foreach (IListBlobItem blobItem in segment.Results)
//                {
//                    if (blobItem is CloudBlockBlob blob)
//                    {
//                        string path = Path.Combine(directory, blob.Name);

//                        downloadTasks.Add(blob.DownloadToFileAsync(path, FileMode.Create));
//                    }
//                }

//                continuationToken = segment.ContinuationToken;
//            }
//            while (continuationToken != null);

//            await Task.WhenAll(downloadTasks);

//            Console.WriteLine("Download complete.");
//        }


//    }
//}
