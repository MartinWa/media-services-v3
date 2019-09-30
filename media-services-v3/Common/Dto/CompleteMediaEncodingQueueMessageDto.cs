namespace media_services_v3.Common.Dto
{
    public class CompleteMediaEncodingQueueMessageDto
    {
        public string JobIdentifier { get; set; }
        public int ContentId { get; set; }
        public string NewFileName { get; set; }
    }
}