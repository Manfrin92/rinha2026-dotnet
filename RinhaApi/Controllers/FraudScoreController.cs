using Microsoft.AspNetCore.Mvc;
using RinhaApi.Controllers.Dtos;
using RinhaApi.Services;

namespace RinhaApi.Controllers;

[ApiController]
[Route("fraud-score")]
public class FraudScoreController : ControllerBase
{
    [HttpPost]
    public FraudScoreResponse IsFraudulent(
        [FromBody] FraudScoreRequest request,
        [FromServices] IFraudDetectionService fraudDetectionService)
    {
        return fraudDetectionService.IsFraudulent(request);
    }
}
