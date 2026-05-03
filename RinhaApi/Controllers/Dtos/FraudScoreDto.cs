namespace RinhaApi.Controllers.Dtos;

public record FraudScoreRequest(string Id, Transaction Transaction, Customer Customer, Merchant Merchant, Terminal Terminal, LastTransaction? Last_transaction);

public record Transaction(decimal Amount, int Installments, DateTime Requested_at);

public record Customer(decimal Avg_amount, int Tx_count_24h, List<string> Known_merchants);

public record Merchant(string Id, string Mcc, decimal Avg_amount);

public record Terminal(bool Is_online, bool Card_present, decimal Km_from_home);

public record LastTransaction(DateTime Timestamp, decimal Km_from_current);

public record FraudScoreResponse(bool Approved, decimal Fraud_score);
