namespace SmartEvents.API.Application.DTOs;

// Sent to POST /deposits
public record PawaPayDepositRequest(
    string DepositId,
    string Amount,
    string Currency,
    string Correspondent,
    PawaPayPayer Payer,
    string CustomerTimestamp,
    string StatementDescription
);

public record PawaPayPayer(string Type, PawaPayAddress Address);

public record PawaPayAddress(string Value);

// Received from POST /deposits (initiation response)
public record PawaPayInitiateResponse(
    string DepositId,
    string Status,
    string? Created
);

// Received from GET /deposits/{id} (status poll — PawaPay returns an array)
public record PawaPayDepositStatusResponse(
    string DepositId,
    string Status,           // ACCEPTED | COMPLETED | FAILED | DUPLICATE_IGNORED
    decimal? Amount,
    string? Currency,
    string? Correspondent,
    PawaPayPayer? Payer,
    string? Created,
    string? RespondedByPayer
);

// Webhook callback body from PawaPay
public record PawaPayWebhookPayload(
    string DepositId,
    string Status,
    decimal? Amount,
    string? Currency
);
