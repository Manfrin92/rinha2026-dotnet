using System.Runtime.CompilerServices;
using RinhaApi.Controllers.Dtos;

namespace RinhaApi.Services;

public class Vector : IVector
{
    private const float MAX_AMOUNT               = 10000;
    private const int   MAX_INSTALLMENTS         = 12;
    private const int   AMOUNT_VS_AVG_RATIO      = 10;
    private const int   MAX_MINUTES              = 1440;
    private const int   MAX_KM                   = 1000;
    private const int   MAX_TX_COUNT_24H         = 20;
    private const float MAX_MERCHANT_AVG_AMOUNT  = 10000;

    public const int VECTOR_TRUNCATE_SIZE = 16; // padded to 16 for SIMD alignment

    private const float INV_MAX_AMOUNT               = 1f / MAX_AMOUNT;
    private const float INV_MAX_INSTALLMENTS         = 1f / MAX_INSTALLMENTS;
    private const float INV_AMOUNT_VS_AVG_RATIO      = 1f / AMOUNT_VS_AVG_RATIO;
    private const float INV_MAX_MINUTES              = 1f / MAX_MINUTES;
    private const float INV_MAX_KM                   = 1f / MAX_KM;
    private const float INV_MAX_TX_COUNT_24H         = 1f / MAX_TX_COUNT_24H;
    private const float INV_MAX_MERCHANT_AVG_AMOUNT  = 1f / MAX_MERCHANT_AVG_AMOUNT;
    private const float INV_23                       = 1f / 23f;
    private const float INV_6                        = 1f / 6f;

    public float[] GetVectorByRequest(FraudScoreRequest request)
    {
        var tx       = request.Transaction;
        var customer = request.Customer;
        var terminal = request.Terminal;
        var merchant = request.Merchant;
        var lastTx   = request.Last_transaction;

        float amount           = Clamp01(tx.Amount * INV_MAX_AMOUNT);
        float installments     = Clamp01((float)tx.Installments * INV_MAX_INSTALLMENTS);
        float amountVsAvgRatio = Clamp01(tx.Amount / customer.Avg_amount * INV_AMOUNT_VS_AVG_RATIO);
        float hourOfDay        = tx.Requested_at.Hour * INV_23;
        float dayOfWeek        = (int)(tx.Requested_at.DayOfWeek + 6) % 7 * INV_6;

        float minutesSinceLastTx = lastTx?.Timestamp != null
            ? Clamp01((float)(CachedClock.UtcNow - lastTx.Timestamp).TotalMinutes * INV_MAX_MINUTES)
            : -1f;

        float kmFromLastTx = lastTx != null
            ? Clamp01(lastTx.Km_from_current * INV_MAX_KM)
            : -1f;

        float kmFromHome      = Clamp01(terminal.Km_from_home * INV_MAX_KM);
        float txCount24h      = Clamp01((float)customer.Tx_count_24h * INV_MAX_TX_COUNT_24H);
        float isOnline        = terminal.Is_online    ? 1f : 0f;
        float cardPresent     = terminal.Card_present ? 1f : 0f;
        float unknownMerchant = customer.Known_merchants.Contains(merchant.Id) ? 0f : 1f;
        float mccRisk         = GetMccRisk(merchant.Mcc);
        float merchantAvgAmt  = Clamp01(merchant.Avg_amount * INV_MAX_MERCHANT_AVG_AMOUNT);

        return
        [
            amount, installments, amountVsAvgRatio, hourOfDay, dayOfWeek,
            minutesSinceLastTx, kmFromLastTx, kmFromHome, txCount24h,
            isOnline, cardPresent, unknownMerchant, mccRisk, merchantAvgAmt,
            0f, 0f // padding to 16 for SIMD alignment
        ];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

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
        _      => 0.5f
    };

    public byte[] GetTruncatedVectorByRequest(float[] inputVector)
    {
        var queryVector = new byte[VECTOR_TRUNCATE_SIZE];
        for (int i = 0; i < VECTOR_TRUNCATE_SIZE; i++)
        {
            float value = inputVector[i] < 0 ? 0 : inputVector[i] > 1 ? 1 : inputVector[i];
            queryVector[i] = (byte)(value * 255 + 0.5f);
        }
        return queryVector;
    }
}