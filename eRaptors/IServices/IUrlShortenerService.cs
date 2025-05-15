
namespace eRaptors.IServices
{
    public interface IUrlShortenerService
    {
        Task<ShortenedUrl> ShortenUrlAsync(string longUrl);
        Task<ShortenedUrl?> GetByCodeAsync(string code);
        Task<UrlVisit> TrackVisitAsync(ShortenedUrl url, string ipAddress);
        Task<ShortenedUrl?> GetUrlHistoryAsync(string code);

        Task<VisitorAddress?> GetVisitorDetails(string ipAddress);
    }
}
