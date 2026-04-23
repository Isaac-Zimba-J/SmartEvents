using SmartEvents.API.Application.Interfaces;

namespace SmartEvents.API.Infrastructure.Notifications;

/// <summary>
/// SMS service stub using Africa's Talking API.
/// Configure AfricasTalking:ApiKey and AfricasTalking:Username in appsettings.
/// Replace HttpClient calls with the official AT SDK when going to production.
/// </summary>
public class SmsService(IConfiguration configuration, ILogger<SmsService> logger) : ISmsService
{
    public async Task SendAsync(string phoneNumber, string message)
    {
        var at = configuration.GetSection("AfricasTalking");
        var apiKey = at["ApiKey"];
        var username = at["Username"];
        var senderId = at["SenderId"] ?? "SmartEvents";

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(username))
        {
            logger.LogWarning("AfricasTalking not configured. SMS to {Phone}: {Message}", phoneNumber, message);
            return;
        }

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("apiKey", apiKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var payload = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = username,
                ["to"] = phoneNumber,
                ["message"] = message,
                ["from"] = senderId
            });

            var response = await client.PostAsync(
                "https://api.africastalking.com/version1/messaging", payload);

            if (response.IsSuccessStatusCode)
                logger.LogInformation("SMS sent to {Phone}", phoneNumber);
            else
                logger.LogWarning("SMS failed to {Phone}: {Status}", phoneNumber, response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMS exception for {Phone}", phoneNumber);
        }
    }
}
