
using System;
using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;

namespace eRaptors.Services
{
    public class UrlShortenerService : IUrlShortenerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGeoLocationService _geoLocationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICacheService _cache;
        private readonly ILogger<UrlShortenerService> _logger;
        private const string CacheKeyPrefix = "url_";
        private const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public UrlShortenerService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, ICacheService cache,ILogger<UrlShortenerService> logger, IGeoLocationService geoLocationService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
            _logger = logger;
            _geoLocationService = geoLocationService;
        }

        public async Task<ShortenedUrl> ShortenUrlAsync(string longUrl)
        {
            // Validate URL            
            if(!IsValidHttpUrl(longUrl))
                throw new ArgumentException("Please enter a valid URL");

            // Generate unique code            
            var existingUrlKey = $"{CacheKeyPrefix}long_{longUrl.GetHashCode()}";
            var existingUrl = _cache.Get<ShortenedUrl>(existingUrlKey);            

            if (existingUrl == null)
            {
                // Check database if not in cache
                existingUrl = await _context.ShortenedUrls
                    .FirstOrDefaultAsync(u => u.LongUrl == longUrl);

                if (existingUrl != null)
                {
                    // Cache the found URL
                    _cache.Set(existingUrlKey, existingUrl, TimeSpan.FromHours(24));
                    _cache.Set($"{CacheKeyPrefix}{existingUrl.Code}", existingUrl, TimeSpan.FromHours(24));
                    return existingUrl;
                }
            }
            else
            {
                return existingUrl;
            }
            var _httpContext = _httpContextAccessor.HttpContext;
            var code = await GenerateUniqueCodeAsync();

            var shortenedUrl = new ShortenedUrl
            {
                Id = Guid.NewGuid(),
                LongUrl = longUrl,
                Code = code,
                ShortUrl = $"{_httpContext?.Request.Scheme}://{_httpContext?.Request.Host}/api/{code}",
                CreatedAt = DateTime.UtcNow
            };

            _context.ShortenedUrls.Add(shortenedUrl);
            await _context.SaveChangesAsync();

            _cache.Set($"{CacheKeyPrefix}{code}", shortenedUrl, TimeSpan.FromHours(24));
            _cache.Set(existingUrlKey, shortenedUrl, TimeSpan.FromHours(24));

            return shortenedUrl;
        }

        public async Task<ShortenedUrl?> GetByCodeAsync(string code)
        {
            var cacheKey = $"{CacheKeyPrefix}{code}";

            // Try to get from cache first
            var cachedUrl = _cache.Get<ShortenedUrl>(cacheKey);
            if (cachedUrl != null)
            {
                return cachedUrl;
            }

            // If not in cache, get from database
            var url = await _context.ShortenedUrls
                .FirstOrDefaultAsync(u => u.Code == code);

            if (url != null)
            {
                // Cache the result for 24 hours
                _cache.Set(cacheKey, url, TimeSpan.FromHours(24));
            }

            return url;
        }

        public async Task<UrlVisit> TrackVisitAsync(ShortenedUrl url, string ipAddress)
        {
            var location = await _geoLocationService.GetLocationFromIpAsync(ipAddress);
            var visit = new UrlVisit
            {
                ShortenedUrlId = url.Id,
                IpAddress = ipAddress,
                VirtualLocation = FormatLocation(location),
                VisitedAt = DateTime.UtcNow
            };

            url.VisitCount++;
            try
            {
                _context.UrlVisits.Add(visit);
                _context.ShortenedUrls.Update(url);
                await _context.SaveChangesAsync();                
            }
            catch (Exception)
            {
                return new UrlVisit();
            }
            return visit?? new UrlVisit();
        }

        public async Task<ShortenedUrl?> GetUrlHistoryAsync(string code)
        {
            var url = await _context.ShortenedUrls
                .FirstOrDefaultAsync(u => u.Code == code);

            if (url == null) return null;
            
            var cacheKey = $"history_{code}_visits_{url.VisitCount}";

            return await _cache.GetOrCreate(cacheKey, async () =>
            {
                var urlWithVisits = await _context.ShortenedUrls
                    .Include(u => u.Visits)
                    .FirstOrDefaultAsync(u => u.Code == code);

                if (urlWithVisits == null) return null;

                return new ShortenedUrl
                {
                    Code = urlWithVisits.Code,
                    LongUrl = urlWithVisits.LongUrl,
                    ShortUrl = urlWithVisits.ShortUrl,
                    CreatedAt = urlWithVisits.CreatedAt,
                    VisitCount = urlWithVisits.VisitCount,
                    Visits = urlWithVisits.Visits
                        .OrderByDescending(v => v.VisitedAt)
                        .Select(v => new UrlVisit
                        {
                            IpAddress = v.IpAddress,
                            VisitedAt = v.VisitedAt,
                            VirtualLocation = v.VirtualLocation
                        })
                        .ToList()
                };
            }, TimeSpan.FromMinutes(5));
        }

        public async Task<VisitorAddress?> GetVisitorDetails(string ipAddress)
        {
            var cacheKey = $"details_{ipAddress}_visits";

            return await _cache.GetOrCreate(cacheKey, async () =>
            {
                var location = await _geoLocationService.GetLocationFromIpAsync(ipAddress);

                return new VisitorAddress
                {
                    IpAddress = ipAddress,
                    VirtualLocation = FormatLocation(location)
                };
            }, TimeSpan.FromMinutes(20));
        }

        #region methods

        private async Task<string> GenerateUniqueCodeAsync()
        {
            var random = new Random();
            string code = string.Empty;
            do
            {
                code = new string(Enumerable.Repeat(AllowedChars, 7)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (await _context.ShortenedUrls.AnyAsync(u => u.Code == code));

            return code;
        }
        private static bool IsValidHttpUrl(string url)
        {
            try
            {
                Uri uri;                
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                {
                    return false;
                }
                return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            }
            catch (UriFormatException)
            {
                return false;
            }
        }
        private string FormatLocation(LocationInfo? location)
        {
            if (location == null) return "Unknown";

            var parts = new List<string>();

            if (!string.IsNullOrEmpty(location.City))
                parts.Add(location.City);
            if (!string.IsNullOrEmpty(location.State))
                parts.Add(location.State);
            if (!string.IsNullOrEmpty(location.Country))
                parts.Add(location.Country);

            return parts.Any() ? string.Join(", ", parts) : "Unknown";
        }
        #endregion methods
    }
}
