using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPIs.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    //[ApiVersionNeutral]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public class ApiControllerBase : ControllerBase
    {

    }
}
