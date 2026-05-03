using Microsoft.AspNetCore.Mvc;
using RinhaApi.Controllers.Dtos;

namespace RinhaApi.Controllers;

[ApiController]
[Route("fraud-score")]
public class FraudScoreController : ControllerBase
{
    [HttpPost]
    public IActionResult GetReady([FromBody] FraudScoreRequest request)
    {
        FraudScoreResponse response = new(Approved: true, Fraud_score: 0.0m);

        return Ok(response);
    }
}
