using RinhaApi.Controllers.Dtos;

namespace RinhaApi.Services;

public interface IFraudDetectionService
{
    bool IsReady();
    
    FraudScoreResponse IsFraudulent(FraudScoreRequest request);
}