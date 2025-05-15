
namespace eRaptors.Services
{
    public interface IGeoLocationService
    {
        Task<LocationInfo?> GetLocationFromIpAsync(string ipAddress);
    }

    public class IpApiGeoLocationService : IGeoLocationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IpApiGeoLocationService> _logger;
        private readonly IMemoryCache _cache;
        private const string BaseUrl = "http://ip-api.com/json/";

        public IpApiGeoLocationService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            ILogger<IpApiGeoLocationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<LocationInfo?> GetLocationFromIpAsync(string ipAddress)
        {
            try
            {
                // Check for localhost or invalid IPs
                if (string.IsNullOrEmpty(ipAddress) || ipAddress == "127.0.0.1" || ipAddress == "::1")
                {
                    return new LocationInfo { City = "localhost" };
                }

                // Try to get from cache first
                var cacheKey = $"geo_{ipAddress}";
                if (_cache.TryGetValue<LocationInfo>(cacheKey, out var cachedLocation))
                {
                    return cachedLocation;
                }

                using var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{BaseUrl}{ipAddress}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadFromJsonAsync<IpApiResponse>();
                if (content == null || content.Status != "success")
                {
                    return null;
                }

                var locationInfo = new LocationInfo
                {
                    City = content.City,
                    State = content.RegionName,
                    Country = content.Country,
                    CountryCode = content.CountryCode,
                    Latitude = content.Lat,
                    Longitude = content.Lon,
                    TimeZone = content.Timezone
                };

                // Cache the result for 24 hours
                _cache.Set(cacheKey, locationInfo, TimeSpan.FromHours(24));

                return locationInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location for IP: {IP}", ipAddress);
                return null;
            }
        }
    }

    public class LocationInfo
    {
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? CountryCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? TimeZone { get; set; }
    }

    public class IpApiResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Timezone { get; set; } = string.Empty;
    }
}
