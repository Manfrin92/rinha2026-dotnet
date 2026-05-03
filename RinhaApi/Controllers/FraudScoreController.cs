using Microsoft.AspNetCore.Mvc;
using RinhaApi.Controllers.Dtos;

namespace RinhaApi.Controllers;

[ApiController]
[Route("fraud-score")]
public class FraudScoreController : ControllerBase
{
    [HttpPost]
    public IActionResult GetReady([FromBody] FraudScoreRequest request, [FromServices] Services.IFraudDetectionService fraudDetectionService)
    {
        FraudScoreResponse response = fraudDetectionService.IsFraudulent(request);

        return Ok(response);
    }
}
