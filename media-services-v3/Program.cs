using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using media_services_v3.Common.Dto;
using media_services_v3.Common.Enums;
using media_services_v3.Data.AzureBlob;
using media_services_v3.Data.AzureMediaService;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Extensions.Configuration;

namespace media_services_v3
{
    public class Program
    {
        const string inputMP4FileName = @"sample.mp4";
        const int SleepInterval = 1000;

        public static async Task Main()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();
            var config = new ConfigWrapper(builder.Build());


            // Injection mocking
            var _storage = new AzureStorage(null, new AzureBlobFactory(config.StorageConnectionString));
            var _mediaService = new AzureMediaService(config, _storage);

            // Example specific code
            var fileGuid = Guid.NewGuid().ToString("N").ToLower();
            string newFileName;
            var contentDisposition = $"filename=\"{HttpUtility.UrlEncode(inputMP4FileName)}\""; // Set filename of downloaded file to original name
            var fileExtension = Path.GetExtension(inputMP4FileName.ToLower());
            var buffer = new FileStream(inputMP4FileName, FileMode.Open);
            var contentId = 0;

            try
            {
                var originalBackupFileName = $"{fileGuid}_original{fileExtension}";
                newFileName = $"{fileGuid}{_mediaService.EncodedFileExtension()}";
                var originalBlob = _storage.GetContentContainer(contentId).GetContentBlob(originalBackupFileName);
                await originalBlob.UploadFromStreamAsync(buffer);
                var encodedBlob = _storage.GetContentContainer(contentId).GetContentBlob(newFileName);
                await encodedBlob.UploadTextAsync(""); // Create empty blob
                var job = await _mediaService.CreateEncodeJobAsync(originalBlob, encodedBlob.GetName(), contentId, CancellationToken.None);
                var message = new CompleteMediaEncodingQueueMessageDto
                {
                    JobName = job.Name,
                    ContentId = contentId,
                    NewFileName = newFileName
                };
                var blob = _storage.GetContentContainer(contentId).GetContentBlob(newFileName);
                await blob.SetContentDispositionAsync(contentDisposition);
                var fileUri = blob.GetReadSharedAccessUrl("1.1.1.1");
                Console.WriteLine($"Uploaded original at {fileUri}");

                // External API call:
                bool exit = false;
                bool startedCopying = false;
                do
                {
                    var progress = await _mediaService.GetEncodeProgressAsync(message.JobName, encodedBlob);
                    Console.WriteLine($"Status {progress.Status}, progress {progress.ProgressPercentage}%");
                    // Azure Function call:
                    if (progress.Status == EncodeStatus.Copying && startedCopying == false)
                    {
                            await _mediaService.FinishEncodeJobAsync(message.JobName, message.ContentId, message.NewFileName, CancellationToken.None);
                    }
                    if (progress.Status == EncodeStatus.Finished || progress.Status == EncodeStatus.Error || progress.Status == EncodeStatus.NotFound)
                    {
                        exit = true;
                    }
                    Thread.Sleep(SleepInterval);
                }
                while (!exit);


            }
            catch (ApiErrorException ex)
            {
                string code = ex.Body.Error.Code;
                string message = ex.Body.Error.Message;

                Console.WriteLine("ERROR:API call failed with error code: {0} and message: {1}", code, message);

            }
            catch (Exception exception)
            {
                if (exception.Source.Contains("ActiveDirectory"))
                {
                    Console.Error.WriteLine("TIP: Make sure that you have filled out the appsettings.json file before running this sample.");
                }

                Console.Error.WriteLine($"{exception.Message}");

                ApiErrorException apiException = exception.GetBaseException() as ApiErrorException;
                if (apiException != null)
                {
                    Console.Error.WriteLine(
                        $"ERROR: API call failed with error code '{apiException.Body.Error.Code}' and message '{apiException.Body.Error.Message}'.");
                }
            }

            buffer.Close();
            Console.WriteLine("Press Enter to continue.");
            Console.ReadLine();

        }
    }
}