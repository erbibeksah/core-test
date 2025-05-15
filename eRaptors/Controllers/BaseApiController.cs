
namespace eRaptors.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        protected ActionResult Success<T>(T data, string message = "")
        {
            return Ok(ApiResponse<T>.CreateSuccess(data, message));
        }

        protected ActionResult Error<T>(string message, int statusCode = 400)
        {
            var response = ApiResponse<T>.CreateError(message);
            return StatusCode(statusCode, response);
        }
    }
}
