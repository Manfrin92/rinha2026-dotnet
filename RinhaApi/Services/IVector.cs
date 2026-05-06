using RinhaApi.Controllers.Dtos;

namespace RinhaApi.Services;

public interface IVector
{
    List<float> GetVectorByRequest(FraudScoreRequest request);

    byte[] GetTruncatedVectorByRequest(List<float> vector);
}