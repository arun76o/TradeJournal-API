using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TradeJournal.Api.Models;

namespace TradeJournal.Api.Services;

public class BrevoEmailService : IBrevoEmailService
{
    private readonly HttpClient _httpClient;
    private readonly BrevoOptions _options;
    private readonly ILogger<BrevoEmailService> _logger;

    public BrevoEmailService(
        HttpClient httpClient,
        IOptions<BrevoOptions> options,
        ILogger<BrevoEmailService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(string toEmail, string otp)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Brevo API key is not configured.");

        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentException("Recipient email cannot be empty.");

        var payload = new
        {
            sender = new
            {
                name = _options.SenderName,
                email = _options.SenderEmail
            },
            to = new[]
    {
        new { email = toEmail }
    },
            templateId = _options.TemplateId,
            @params = new Dictionary<string, string>
            {
                ["OTP"] = otp
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = JsonContent.Create(payload, options: new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        request.Headers.Add("api-key", _options.ApiKey);

        try
        {
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Brevo API returned {StatusCode}: {Body}", response.StatusCode, body);
                throw new InvalidOperationException("Failed to send verification email.");
            }

            _logger.LogInformation("OTP email sent successfully to {Email}", toEmail);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error sending OTP email to {Email}", toEmail);
            throw new InvalidOperationException("Failed to send verification email due to a network error.");
        }
    }
}
