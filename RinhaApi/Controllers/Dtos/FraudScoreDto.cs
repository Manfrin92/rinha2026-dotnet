namespace RinhaApi.Controllers.Dtos;

public record FraudScoreRequest(string Id, Transaction Transaction, Customer Customer, Merchant Merchant, Terminal Terminal, LastTransaction? Last_transaction);

public record Transaction(float Amount, int Installments, DateTime Requested_at);

public record Customer(float Avg_amount, int Tx_count_24h, List<string> Known_merchants);

public record Merchant(string Id, string Mcc, float Avg_amount);

public record Terminal(bool Is_online, bool Card_present, float Km_from_home);

public record LastTransaction(DateTime Timestamp, float Km_from_current);

public record FraudScoreResponse(bool Approved, float Fraud_score);
