using System.Numerics;
using System.Runtime.CompilerServices;
using RinhaApi.Controllers.Dtos;

namespace RinhaApi.Services;

public class FraudDetectionService(
    byte[] labels,
    byte[] vectors,
    int vectorSize,
    int bitsPerDim,
    IVector vectorService,
    Dictionary<long, List<int>> grid) : IFraudDetectionService
{
    private const int K = 5;

    public FraudScoreResponse IsFraudulent(FraudScoreRequest request)
    {
        var truncatedVector = vectorService.GetTruncatedVectorByRequest(vectorService.GetVectorByRequest(request));
        var (approved, fraudScore) = Evaluate(truncatedVector);
        return new FraudScoreResponse(Approved: approved, Fraud_score: fraudScore);
    }

    public (bool approved, float fraudScore) Evaluate(byte[] query)
    {
        long key = GetGridKey(query);

        // Try exact cell first, then expand to neighbors if not enough points
        var candidates = GetCandidates(key, query);

        return ScoreFromCandidates(candidates, query);
    }

    private List<int> GetCandidates(long key, byte[] query)
    {
        // Exact cell has enough points — fast path
        if (grid.TryGetValue(key, out var exactBucket) && exactBucket.Count >= K * 4)
            return exactBucket;

        // Expand to neighbor cells by flipping each dim's bin by ±1
        var merged = new HashSet<int>(exactBucket ?? []);

        for (int dim = 0; dim < vectorSize; dim++)
        {
            foreach (int delta in new[] { -1, 1 })
            {
                long neighborKey = ShiftDim(key, dim, delta);
                if (grid.TryGetValue(neighborKey, out var neighborBucket))
                    foreach (var idx in neighborBucket)
                        merged.Add(idx);
            }

            if (merged.Count >= K * 4) break; // enough candidates
        }

        // Last resort: full scan (should rarely happen)
        if (merged.Count < K)
            return null!;

        return merged.ToList();
    }

    private (bool approved, float fraudScore) ScoreFromCandidates(List<int>? candidates, byte[] query)
    {
        Span<int> bestDistances = stackalloc int[K];
        Span<int> bestIndices   = stackalloc int[K];
        bestDistances.Fill(int.MaxValue);
        bestIndices.Fill(-1);

        ReadOnlySpan<byte> querySpan   = query;
        ReadOnlySpan<byte> vectorsSpan = vectors;

        // Full scan fallback if grid gave nothing
        int searchCount = candidates?.Count ?? (vectors.Length / vectorSize);

        for (int ci = 0; ci < searchCount; ci++)
        {
            int i = candidates != null ? candidates[ci] : ci;
            var candidate = vectorsSpan.Slice(i * vectorSize, vectorSize);

            if (!ComputeDistEarlyExit(candidate, querySpan, bestDistances[K - 1], out int dist))
                continue;

            if (dist >= bestDistances[K - 1]) continue;

            for (int j = 0; j < K; j++)
            {
                if (dist < bestDistances[j])
                {
                    for (int s = K - 1; s > j; s--)
                    {
                        bestDistances[s] = bestDistances[s - 1];
                        bestIndices[s]   = bestIndices[s - 1];
                    }
                    bestDistances[j] = dist;
                    bestIndices[j]   = i;
                    break;
                }
            }
        }

        int fraudVotes = 0;
        ReadOnlySpan<byte> labelsSpan = labels;
        for (int i = 0; i < K; i++)
            if (bestIndices[i] >= 0 && labelsSpan[bestIndices[i]] == 1)
                fraudVotes++;

        float fraudScore = fraudVotes / (float)K;
        return (fraudScore < 0.6f, fraudScore);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ComputeDistEarlyExit(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> query, int worstBest, out int dist)
    {
        dist = 0;
        for (int d = 0; d < query.Length; d++)
        {
            int diff = candidate[d] - query[d];
            dist += diff * diff;
            if (dist >= worstBest) return false; // prune early
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GetGridKey(byte[] v)
    {
        long key = 0;
        for (int i = 0; i < vectorSize; i++)
            key |= (long)(v[i] >> (8 - bitsPerDim)) << (i * bitsPerDim);
        return key;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long ShiftDim(long key, int dim, int delta)
    {
        int shift = dim * bitsPerDim;
        int mask  = (1 << bitsPerDim) - 1;
        int bin   = (int)((key >> shift) & mask) + delta;
        if (bin < 0 || bin >= (1 << bitsPerDim)) return -1; // out of bounds
        return (key & ~((long)mask << shift)) | ((long)bin << shift);
    }

    public bool IsReady()
    {
        Console.WriteLine($"FraudDetectionService - Grid cells: {grid.Count}");
        Console.WriteLine($"FraudDetectionService - Vector size: {vectorSize}, Count: {vectors.Length / vectorSize}");
        return labels?.Length > 0 && vectors?.Length > 0;
    }
}