using eRaptors.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eRaptors.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CacheController : BaseApiController
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<CacheController> _logger;

        public CacheController(
            ICacheService cacheService,
            ILogger<CacheController> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        [HttpPost("clear")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
        public ActionResult<ApiResponse<bool>> ClearCache()
        {
            try
            {
                _cacheService.ClearAll();
                return Success(true, "Cache cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                return Error<bool>("Failed to clear cache", statusCode: 500);
            }
        }

        [HttpGet("keys")]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
        public ActionResult<ApiResponse<List<string>>> GetCacheKeys()
        {
            try
            {
                var keys = _cacheService.GetAllKeys().ToList();
                return Success(keys, $"Found {keys.Count} cached items");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cache keys");
                return Error<List<string>>("Failed to retrieve cache keys", statusCode: 500);
            }
        }

        [HttpDelete("keys/{key}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public ActionResult<ApiResponse<bool>> RemoveCacheKey(string key)
        {
            try
            {
                _cacheService.Remove(key);
                return Success(true, $"Cache key '{key}' removed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache key: {Key}", key);
                return Error<bool>($"Failed to remove cache key: {key}", statusCode: 500);
            }
        }
    }
}
