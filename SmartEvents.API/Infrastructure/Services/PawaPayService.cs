using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartEvents.API.Application.DTOs;
using SmartEvents.API.Application.Interfaces;

namespace SmartEvents.API.Infrastructure.Services;

public class PawaPayService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IPawaPayService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("PawaPay");
        var token = configuration["PawaPay:ApiToken"]
            ?? throw new InvalidOperationException("PawaPay:ApiToken not configured.");
        var baseUrl = configuration["PawaPay:BaseUrl"]
            ?? "https://api.sandbox.pawapay.cloud";
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<PawaPayInitiateResponse> InitiateDepositAsync(
        string depositId,
        decimal amount,
        string currency,
        string correspondent,
        string phoneNumber)
    {
        var body = new PawaPayDepositRequest(
            DepositId: depositId,
            Amount: amount,
            Currency: currency,
            Correspondent: correspondent,
            Payer: new PawaPayPayer("MSISDN", new PawaPayAddress(phoneNumber)),
            CustomerTimestamp: DateTime.UtcNow.ToString("o"),
            StatementDescription: "SmartEvents ticket purchase"
        );

        var json = JsonSerializer.Serialize(body, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = CreateClient();
        var response = await client.PostAsync("/deposits", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"PawaPay deposit initiation failed: {responseBody}");

        return JsonSerializer.Deserialize<PawaPayInitiateResponse>(responseBody, JsonOpts)
            ?? throw new InvalidOperationException("Invalid PawaPay initiation response.");
    }

    public async Task<PawaPayDepositStatusResponse> GetDepositStatusAsync(string depositId)
    {
        var client = CreateClient();
        var response = await client.GetAsync($"/deposits/{depositId}");
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"PawaPay status check failed: {responseBody}");

        // PawaPay returns an array for GET /deposits/{id}
        var results = JsonSerializer.Deserialize<PawaPayDepositStatusResponse[]>(responseBody, JsonOpts);
        return results?.FirstOrDefault()
            ?? throw new InvalidOperationException("Empty PawaPay status response.");
    }
}
