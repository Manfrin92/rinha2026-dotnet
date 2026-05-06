using RinhaApi.Controllers.Dtos;

namespace RinhaApi.Services;

public interface IVector
{
    float[] GetVectorByRequest(FraudScoreRequest request);

    byte[] GetTruncatedVectorByRequest(float[]vector);
}