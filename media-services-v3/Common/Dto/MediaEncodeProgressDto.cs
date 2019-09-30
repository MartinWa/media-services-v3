using media_services_v3.Common.Enums;

namespace media_services_v3.Common.Dto
{
    public class MediaEncodeProgressDto
    {
        public EncodeStatus Status { get; set; }
        public double ProgressPercentage { get; set; }
        public string Errors { get; set; }
    }
}