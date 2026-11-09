using System.Text.Json;
using System.Text.RegularExpressions;
using Stripe;

namespace KicksMvc.Services.Payments;

/// <summary>Explicit, read-only development diagnostic. Never serializes SDK objects or exceptions.</summary>
public static class StripeSandboxPreflight
{
    public static async Task<int> RunAsync(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment()) return Report("DevelopmentOnly", false, null);
        var secret = configuration["Stripe:SecretKey"];
        var publishable = configuration["Stripe:PublishableKey"];
        if (!IsTestKey(secret, "sk") || !IsTestKey(publishable, "pk"))
            return Report("TestCredentialsRequired", false, null);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var client = new StripeClient(secret);
            var balance = await client.V1.Balance.GetAsync(cancellationToken: timeout.Token);
            if (balance.Livemode) return Report("TestModeRequired", false, null);
            // GET /v1/account retrieves only the account associated with the configured key.
            var account = await client.RequestAsync<Account>(HttpMethod.Get, "/v1/account",
                null, null, timeout.Token);
            if (string.IsNullOrWhiteSpace(account.Country))
                return Report("AccountCountryUnavailable", true, null);
            var spec = await client.V1.CountrySpecs.GetAsync(account.Country, cancellationToken: timeout.Token);
            var supportsJod = spec.SupportedPaymentCurrencies?.Contains("jod", StringComparer.OrdinalIgnoreCase) == true;
            var supportsUsd = spec.SupportedPaymentCurrencies?.Contains("usd", StringComparer.OrdinalIgnoreCase) == true;
            _ = new SandboxSettings(configuration).Rate();
            return Report(supportsUsd ? "ReadyUsdSandbox" : "UsdUnsupported", true, supportsJod);
        }
        catch
        {
            // Stripe errors can contain request/account data: deliberately do not log them.
            return Report("ReadOnlyStripeCheckFailed", false, null);
        }
    }

    private static bool IsTestKey(string? value, string prefix) => value is not null &&
        Regex.IsMatch(value, "^" + prefix + "_test_[A-Za-z0-9_]{16,512}$", RegexOptions.CultureInvariant);

    private static int Report(string status, bool testModeVerified, bool? jodSupported)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { Status = status, TestModeVerified = testModeVerified,
            JodSupported = jodSupported, ReadOnly = true, DatabaseAccessed = false }));
        return status == "ReadyUsdSandbox" ? 0 : 2;
    }
}
