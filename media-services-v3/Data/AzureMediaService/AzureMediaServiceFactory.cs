using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest.Azure.Authentication;

namespace media_services_v3.Data.AzureMediaService
{
    public class AzureMediaServiceFactory
    {
        private const string EncodedFileExtension = ".mp4";
        private readonly ConfigWrapper _settings;

        public AzureMediaServiceFactory(ConfigWrapper settings)
        {
            _settings = settings;
        }

        public async Task<IAzureMediaServicesClient> GetAzureMediaServicesClientAsync()
        {
            var clientCredential = new ClientCredential(_settings.AadClientId, _settings.AadSecret);
            var credentials = await ApplicationTokenProvider.LoginSilentAsync(_settings.AadTenantId, clientCredential, ActiveDirectoryServiceSettings.Azure);
            var client = new AzureMediaServicesClient(credentials)
            {
                SubscriptionId = _settings.SubscriptionId
            };
            // Set the polling interval for long running operations to 2 seconds.
            // The default value is 30 seconds for the .NET client SDK
            client.LongRunningOperationRetryTimeout = 2;
            return client;
        }

        public async Task<Transform> EnsureTransformExistsAsync(CancellationToken cancellationToken)
        {
            var client = await GetAzureMediaServicesClientAsync();
            var transform = await client.Transforms.GetAsync(_settings.ResourceGroup, _settings.AccountName, _settings.MediaServicesTransform, cancellationToken);
            if (transform == null)
            {
                // https://docs.microsoft.com/en-us/rest/api/media/transforms/createorupdate
                TransformOutput[] outputs = new TransformOutput[]
                {
                  new TransformOutput(
                        new StandardEncoderPreset
                        {
                            Codecs = new Codec[]
                            {
                                // AAC Audio layer for the audio encoding
                                new AacAudio
                                {
                                    Channels= 2,
                                    SamplingRate= 48000,
                                    Bitrate= 128000,
                                    Profile= AacAudioProfile.AacLc
                                },
                                // H264Video for the video encoding
                               new H264Video
                               {
                                    Complexity =H264Complexity.Quality,

                                    KeyFrameInterval = TimeSpan.FromSeconds(2),
                                    Layers =  new H264Layer[]
                                    {
                                        new H264Layer
                                        {
                                            Bitrate = 1000000, // Units are in bits per second
                                            Width= "1280",
                                            Height= "720"
                                        }
                                    }
                                }
                            },
                            Formats= new Format[]
                            {
                                new Mp4Format
                                {
                                    FilenamePattern="{Basename}{Extension}"
                                }
                            }
                        },
                        OnErrorType.StopProcessingJob,
                        Priority.Normal
                   )
                };
                string description = "A simple custom encoding transform with 2 MP4 bitrates";
                transform = await client.Transforms.CreateOrUpdateAsync(_settings.ResourceGroup, _settings.AccountName, _settings.MediaServicesTransform, outputs, description, cancellationToken);
            }
            return transform;
        }

        public string GetEncodedFileExtension()
        {
            return EncodedFileExtension;
        }
    }
}