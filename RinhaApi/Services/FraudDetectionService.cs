using RinhaApi.Controllers.Dtos;

namespace RinhaApi.Services;

public class FraudDetectionService : IFraudDetectionService
{
    public FraudScoreResponse IsFraudulent(FraudScoreRequest request)
    {
        FraudScoreResponse response = new(Approved: true, Fraud_score: 0);

        return response;
    }

    public bool IsReady()
    {
        return true;
    }
}