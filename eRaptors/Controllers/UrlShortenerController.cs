
namespace eRaptors.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UrlShortenerController : BaseApiController
    {
        private readonly IUrlShortenerService _urlShortenerService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UrlShortenerController(IUrlShortenerService urlShortenerService, IHttpContextAccessor httpContextAccessor)
        {
            _urlShortenerService = urlShortenerService;
            _httpContextAccessor = httpContextAccessor;
        }

        #region generate shorturl
        [HttpPost("shorten")]
        [ProducesResponseType(typeof(ApiResponse<ShortenedUrl>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ShortenedUrl>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ShortenedUrl>> ShortenUrl([FromBody] ShortenUrlRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.LongUrl))
                {
                    return Error<ShortenedUrl>("Please enter the URL");
                }
                var result = await _urlShortenerService.ShortenUrlAsync(request.LongUrl);
                return Success<ShortenedUrl>(result, "URL shortened successfully");
            }
            catch (ArgumentException ex)
            {
                return Error<ShortenedUrl>(ex.Message);
            }
        }
        #endregion generate shorturl

        #region redirect handler
        [HttpGet("{code}")]
        [ProducesResponseType(typeof(ApiResponse<ShortenedUrl>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ShortenedUrl>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RedirectToUrl(string code)
        {
            var shortenedUrl = await _urlShortenerService.GetByCodeAsync(code);

            if (shortenedUrl == null)
                return Error<ShortenedUrl>("Not Found");

            // Get client IP
            await _urlShortenerService.TrackVisitAsync(shortenedUrl, Utility.GetVisitorIPAddress(_httpContextAccessor, true));

            return Redirect(shortenedUrl.LongUrl);
        }
        #endregion redirect handler

        #region get history of code - short url
        [HttpGet("history/{code}")]
        [ProducesResponseType(typeof(ApiResponse<ShortenedUrl>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ShortenedUrl>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<ShortenedUrl>>> GetUrlHistory(string code)
        {
            try
            {
                if(string.IsNullOrEmpty(code))
                    return Error<ShortenedUrl>("Please enter the unique code");

                var history = await _urlShortenerService.GetUrlHistoryAsync(code);
                if (history == null)
                {
                    return Error<ShortenedUrl>($"No URL found with code: {code}", statusCode: 404);
                }

                return Success(history, "URL history retrieved successfully");
            }
            catch (Exception)
            {
                return Error<ShortenedUrl>("Failed to retrieve URL history");
            }
        }
        #endregion get history of code - short url

        #region get IP & Address
        [HttpGet("GetVisitorDetails")]
        [ProducesResponseType(typeof(ApiResponse<VisitorAddress>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<VisitorAddress>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<VisitorAddress>>> GetVisitorDetails()
        {
            try
            {
                var ipAddress = Utility.GetVisitorIPAddress(_httpContextAccessor, true);
                var visitorAddress = await _urlShortenerService.GetVisitorDetails(ipAddress);
                if (visitorAddress == null) 
                {
                    return Error<VisitorAddress>($"No Address found with associated Ip", statusCode: 404);
                }
                return Success<VisitorAddress>(visitorAddress, "Address found with associated Ip");
            }
            catch (Exception)
            {
                return Error<VisitorAddress>("Failed to retrieve visitor details");
            }
        }
        #endregion get IP & Address

        #region QR Coder
        [HttpPost("GenerateQRCode")]
        public IActionResult GenerateQRCode(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return BadRequest(new { success = false, error = "URL is required" });
            }
            try
            {
                using (var qrGenerator = new QRCodeGenerator())
                {
                    var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q); // Q = 25% error correction
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeImage = qrCode.GetGraphic(20); // Size of 20 pixels per module
                        string base64String = Convert.ToBase64String(qrCodeImage);

                        return Ok(new
                        {
                            success = true,
                            qrCode = base64String,
                            message = "QR Code generated successfully"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = $"Failed to generate QR code: {ex.Message}"
                });
            }
        }
        #endregion QR Coder
    }

    public class ShortenUrlRequest
    {             
        public string LongUrl { get; set; } = string.Empty;
    }
}
