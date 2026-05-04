using RinhaApi.Controllers.Dtos;

namespace RinhaApi.Services;

public class Vector : IVector
{
    private const decimal MAX_AMOUNT = 10000;

    private const int MAX_INSTALLMENTS = 12;

    private const int AMOUNT_VS_AVG_RATIO = 10;

    private const int MAX_MINUTES = 1440;

    private const int MAX_KM = 1000;

    private const int MAX_TX_COUNT_24H = 20;

    private const decimal MAX_MERCHANT_AVG_AMOUNT = 10000;

    public List<decimal> GetVectorByRequest(FraudScoreRequest request)
    {
        decimal amount = decimal.Round(Math.Clamp(request.Transaction.Amount / MAX_AMOUNT, 0, 1), 4);

        decimal installments = decimal.Round(Math.Clamp((decimal)request.Transaction.Installments / MAX_INSTALLMENTS, 0, 1), 4);
        
        decimal amountVsAvgRatio = decimal.Round(Math.Clamp(request.Transaction.Amount / request.Customer.Avg_amount / AMOUNT_VS_AVG_RATIO, 0, 1), 4);

        decimal hourOfDay = decimal.Round((decimal)request.Transaction.Requested_at.Hour / 23, 4);
        
        // dotnet consider Monday = 0
        int adjustedDay = ((int)request.Transaction.Requested_at.DayOfWeek + 6) % 7;
        decimal dayOfWeek = decimal.Round((decimal)adjustedDay / 6, 4);
        
        decimal minutesSinceLastTx = request.Last_transaction?.Timestamp != null 
            ? decimal.Round(Math.Clamp((decimal)(DateTime.UtcNow - request.Last_transaction.Timestamp).TotalMinutes / MAX_MINUTES, 0, 1), 4) 
            : -1;

        decimal kmFromLastTx = request.Last_transaction != null 
            ? decimal.Round(Math.Clamp(request.Last_transaction.Km_from_current / MAX_KM, 0, 1), 4) 
            : -1;
        
        decimal kmFromHome = decimal.Round(Math.Clamp(request.Terminal.Km_from_home / MAX_KM, 0, 1), 4);

        decimal txCount24h = decimal.Round(Math.Clamp((decimal)request.Customer.Tx_count_24h / MAX_TX_COUNT_24H, 0, 1), 4);
        
        decimal isOnline = request.Terminal.Is_online ? 1 : 0;

        decimal cardPresent = request.Terminal.Card_present ? 1 : 0;

        decimal unknownMerchant = request.Customer.Known_merchants.Contains(request.Merchant.Id) ? 0 : 1;

        decimal mccRisk = GetMccRisk(request.Merchant.Mcc);

        decimal merchantAvgAmount = decimal.Round(Math.Clamp(request.Merchant.Avg_amount / MAX_MERCHANT_AVG_AMOUNT, 0, 1), 4);
        
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

    private decimal GetMccRisk(string mcc)
    {
        return mcc switch
        {
            "5411" => 0.15m,
            "5812" => 0.30m,
            "5912" => 0.20m,
            "5944" => 0.45m,
            "7801" => 0.80m,
            "7802" => 0.75m,
            "7995" => 0.85m,
            "4511" => 0.35m,
            "5311" => 0.25m,
            "5999" => 0.50m,
            _ => 0.5m // Risco médio para outros MCCs
        };
    }
}