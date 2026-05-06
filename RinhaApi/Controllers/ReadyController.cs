using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RinhaApi.Services;

namespace RinhaApi.Controllers;

[ApiController]
[Route("ready")]
public class ReadyController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetReady([FromServices] IFraudDetectionService fraudDetectionService)
    {
        var process = Process.GetCurrentProcess();
        Console.WriteLine($"Managed memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
        Console.WriteLine($"Working set: {process.WorkingSet64 / (1024 * 1024)} MB");
        if (fraudDetectionService.IsReady())
        {
            return Ok();
        }
        return StatusCode(503);
    }
}
