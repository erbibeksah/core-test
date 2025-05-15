namespace eRaptors.Models
{
    public class ShortenedUrl
    {
        public Guid Id { get; set; }
        public string LongUrl { get; set; } = string.Empty;
        public string ShortUrl { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int VisitCount { get; set; }
        public List<UrlVisit> Visits { get; set; } = new();
    }

    public class UrlVisit
    {
        public Guid Id { get; set; }
        public Guid ShortenedUrlId { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string VirtualLocation { get; set; } = string.Empty;
        public DateTime VisitedAt { get; set; }
    }

    public class VisitorAddress
    {
        public string IpAddress { get; set; } = string.Empty;
        public string VirtualLocation { get; set; } = string.Empty;
    }
}
