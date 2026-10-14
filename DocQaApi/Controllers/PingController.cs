using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DocQaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PingController:ControllerBase
    {
        [HttpGet]
        public ActionResult Get()
        {
            return Ok(new { message = "Hello from the .Net API" });
        }
    }
}
