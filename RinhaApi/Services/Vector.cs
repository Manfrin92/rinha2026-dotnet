using RinhaApi.Controllers.Dtos;

namespace RinhaApi.Services;

public class Vector : IVector
{
    private const float MAX_AMOUNT = 10000;

    private const int MAX_INSTALLMENTS = 12;

    private const int AMOUNT_VS_AVG_RATIO = 10;

    private const int MAX_MINUTES = 1440;

    private const int MAX_KM = 1000;

    private const int MAX_TX_COUNT_24H = 20;

    private const float MAX_MERCHANT_AVG_AMOUNT = 10000;

    private const int VECTOR_TRUNCATE_SIZE = 5;

    public List<float> GetVectorByRequest(FraudScoreRequest request)
    {
        float amount = float.Round(Math.Clamp(request.Transaction.Amount / MAX_AMOUNT, 0, 1), 4);

        float installments = float.Round(Math.Clamp((float)request.Transaction.Installments / MAX_INSTALLMENTS, 0, 1), 4);
        
        float amountVsAvgRatio = float.Round(Math.Clamp(request.Transaction.Amount / request.Customer.Avg_amount / AMOUNT_VS_AVG_RATIO, 0, 1), 4);

        float hourOfDay = float.Round((float)request.Transaction.Requested_at.Hour / 23, 4);
        
        // dotnet consider Monday = 0
        int adjustedDay = ((int)request.Transaction.Requested_at.DayOfWeek + 6) % 7;
        float dayOfWeek = float.Round((float)adjustedDay / 6, 4);
        
        float minutesSinceLastTx = request.Last_transaction?.Timestamp != null 
            ? float.Round(Math.Clamp((float)(DateTime.UtcNow - request.Last_transaction.Timestamp).TotalMinutes / MAX_MINUTES, 0, 1), 4) 
            : -1;

        float kmFromLastTx = request.Last_transaction != null 
            ? float.Round(Math.Clamp(request.Last_transaction.Km_from_current / MAX_KM, 0, 1), 4) 
            : -1;
        
        float kmFromHome = float.Round(Math.Clamp(request.Terminal.Km_from_home / MAX_KM, 0, 1), 4);

        float txCount24h = float.Round(Math.Clamp((float)request.Customer.Tx_count_24h / MAX_TX_COUNT_24H, 0, 1), 4);
        
        float isOnline = request.Terminal.Is_online ? 1 : 0;

        float cardPresent = request.Terminal.Card_present ? 1 : 0;

        float unknownMerchant = request.Customer.Known_merchants.Contains(request.Merchant.Id) ? 0 : 1;

        float mccRisk = GetMccRisk(request.Merchant.Mcc);

        float merchantAvgAmount = float.Round(Math.Clamp(request.Merchant.Avg_amount / MAX_MERCHANT_AVG_AMOUNT, 0, 1), 4);
        
        return
        [
            amount,
            installments,
            amountVsAvgRatio,
            hourOfDay,
            dayOfWeek,
            minutesSinceLastTx,
            kmFromLastTx,
            kmFromHome,
            txCount24h,
            isOnline,
            cardPresent,
            unknownMerchant,
            mccRisk,
            merchantAvgAmount
        ];
    }

    private static float GetMccRisk(string mcc) => mcc switch
    {
        "5411" => 0.15f,
        "5812" => 0.30f,
        "5912" => 0.20f,
        "5944" => 0.45f,
        "7801" => 0.80f,
        "7802" => 0.75f,
        "7995" => 0.85f,
        "4511" => 0.35f,
        "5311" => 0.25f,
        "5999" => 0.50f,
        _ => 0.5f // Risco médio para outros MCCs
    };

    /// <summary>
    /// Reduces the dimensionality of the input vector by truncating it.
    /// </summary>
    /// <param name="inputVector">The input vector to truncate.</param>
    /// <returns>The truncated vector.</returns>
    public byte[] GetTruncatedVectorByRequest(List<float> inputVector)
    {
        var queryVector = new byte[VECTOR_TRUNCATE_SIZE];

        for (int i = 0; i < VECTOR_TRUNCATE_SIZE; i++)
        {
            float value = inputVector[i];

            // clamp
            if (value < 0) value = 0;
            if (value > 1) value = 1;

            queryVector[i] = (byte)(value * 255 + 0.5f);
        }

        return queryVector;
    }
}