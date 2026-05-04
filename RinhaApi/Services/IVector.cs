using RinhaApi.Controllers.Dtos;

namespace RinhaApi.Services;

public interface IVector
{
    List<decimal> GetVectorByRequest(FraudScoreRequest request);
}