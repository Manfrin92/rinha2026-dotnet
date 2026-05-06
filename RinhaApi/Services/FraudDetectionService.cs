using RinhaApi.Controllers.Dtos;

namespace RinhaApi.Services;

public class FraudDetectionService(int legitCount,
    int fraudCount,
    byte[] labels,
    byte[] vectors,
    int vectorSize,
    int count,
    IVector vectorService) : IFraudDetectionService
{
    private int _legitCount = legitCount;

    private int _fraudCount = fraudCount;

    // All references stored in one single array
    private byte[] _labels = labels; // 0 = legit, 1 = fraud

    // Super simplified version of the references
    private byte[] _vectors = vectors;

    private int _count = count;

    private int _vectorSize = vectorSize;

    private IVector _vectorService = vectorService;

    public FraudScoreResponse IsFraudulent(FraudScoreRequest request)
    {
        var truncatedVector = _vectorService.GetTruncatedVectorByRequest(_vectorService.GetVectorByRequest(request));

        var (approved, fraudScore) = Evaluate(truncatedVector);

        FraudScoreResponse response = new(Approved: approved, Fraud_score: fraudScore);

        return response;
    }

    public (bool approved, float fraudScore) Evaluate(byte[] query)
    {
        int k = 5;

        int[] bestIndices = new int[k];
        int[] bestDistances = new int[k];

        for (int i = 0; i < k; i++)
        {
            bestDistances[i] = int.MaxValue;
            bestIndices[i] = -1;
        }

        // --- find top 5 ---
        for (int i = 0; i < _count; i++)
        {
            int baseOffset = i * _vectorSize;

            int dist = 0;

            for (int d = 0; d < _vectorSize; d++)
            {
                int diff = _vectors[baseOffset + d] - query[d];
                dist += diff * diff;
            }

            for (int j = 0; j < k; j++)
            {
                if (dist < bestDistances[j])
                {
                    for (int s = k - 1; s > j; s--)
                    {
                        bestDistances[s] = bestDistances[s - 1];
                        bestIndices[s] = bestIndices[s - 1];
                    }

                    bestDistances[j] = dist;
                    bestIndices[j] = i;
                    break;
                }
            }
        }

        // --- compute fraud score ---
        int fraudCount = 0;

        for (int i = 0; i < k; i++)
        {
            if (_labels[bestIndices[i]] == 1)
            {
                fraudCount++;                
            }
        }

        float fraudScore = fraudCount / 5f;
        bool approved = fraudScore < 0.6f;

        return (approved, fraudScore);
    }

    public bool IsReady()
    {
        Console.WriteLine($"FraudDetectionService - Legit count: {_legitCount}, Fraud count: {_fraudCount}");
        Console.WriteLine($"FraudDetectionService - Labels length: {_labels?.Length}, Vectors length: {_vectors?.Length}");
        Console.WriteLine($"FraudDetectionService - Vector size: {_vectorSize}, Count: {_count}");

        return _legitCount + _fraudCount > 0 && _labels != null && _labels.Length > 0 && _vectors != null && _vectors.Length > 0;
    }
}