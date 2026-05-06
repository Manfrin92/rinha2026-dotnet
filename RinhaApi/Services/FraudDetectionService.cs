using RinhaApi.Controllers.Dtos;

namespace RinhaApi.Services;

public class FraudDetectionService(int legitCount,
    int fraudCount,
    byte[] labels,
    byte[] vectors,
    int vectorSize,
    int count) : IFraudDetectionService
{
    private int _legitCount = legitCount;

    private int _fraudCount = fraudCount;

    // All references stored in one single array
    private byte[] _labels = labels; // 0 = legit, 1 = fraud

    // Super simplified version of the references
    private byte[] _vectors = vectors;

    private int _count = count;

    private int _vectorSize = vectorSize;

    public FraudScoreResponse IsFraudulent(FraudScoreRequest request)
    {
        FraudScoreResponse response = new(Approved: true, Fraud_score: 0);

        return response;
    }

    public bool IsReady()
    {
        Console.WriteLine($"FraudDetectionService - Legit count: {_legitCount}, Fraud count: {_fraudCount}");
        Console.WriteLine($"FraudDetectionService - Labels length: {_labels?.Length}, Vectors length: {_vectors?.Length}");
        Console.WriteLine($"FraudDetectionService - Vector size: {_vectorSize}, Count: {_count}");

        return _legitCount + _fraudCount > 0 && _labels != null && _labels.Length > 0 && _vectors != null && _vectors.Length > 0;
    }
}