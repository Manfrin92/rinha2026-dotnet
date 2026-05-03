using Microsoft.AspNetCore.Mvc;

namespace RinhaApi.Controllers;

[ApiController]
[Route("ready")]
public class ReadyController : ControllerBase
{
    [HttpGet]
    public IActionResult GetReady()
    {
        return Ok();
    }
}
