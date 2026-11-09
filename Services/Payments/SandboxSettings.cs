using System.Globalization;
using System.Text.RegularExpressions;
using KicksMvc.Services.Activities;
using Stripe;

namespace KicksMvc.Services.Payments;

public sealed class SandboxSettings(IConfiguration configuration)
{
    public decimal Rate()
    {
        // No default, live mode, adaptive pricing or implicit currency conversion.
        if (configuration["Stripe:SandboxOnly"] != "True" && configuration["Stripe:SandboxOnly"] != "true" ||
            !ValidKey(configuration["Stripe:SecretKey"],"sk") || !ValidKey(configuration["Stripe:PublishableKey"],"pk") ||
            !decimal.TryParse(configuration["Stripe:UsdPerJod"], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var rate) ||
            rate <= 0 || rate > 999999 || decimal.Round(rate,6)!=rate)
            throw new ActivityException("Stripe sandbox is not configured safely. Payments are unavailable.",503);
        return rate;
    }
    public (decimal Rate,long Cents) Convert(decimal jod)
    {
        var rate=Rate();
        if(jod<=0 || jod>9999999999.99m || decimal.Round(jod,2)!=jod)throw new ActivityException("Invalid payable amount.",400);
        var cents=decimal.Round(jod*rate*100,0,MidpointRounding.AwayFromZero);
        if(cents<50 || cents>99999999)throw new ActivityException("The converted sandbox amount must be between 0.50 and 999,999.99 USD.",400);
        return(rate,checked((long)cents));
    }
    public StripeClient Client() { Rate(); return new StripeClient(configuration["Stripe:SecretKey"]); }
    public string SigningSecret()
    {
        Rate();var value=configuration["Stripe:WebhookSecret"];
        if(value is null || !Regex.IsMatch(value,"^whsec_[A-Za-z0-9_]{16,512}$"))throw new ActivityException("Stripe sandbox webhook signing is not configured.",503);
        return value;
    }
    public string BaseUrl()
    {
        if(!Uri.TryCreate(configuration["Stripe:PublicBaseUrl"],UriKind.Absolute,out var uri) ||
           (!uri.IsLoopback&&uri.Scheme!="https") || uri.Scheme is not ("http" or "https") ||
           uri.UserInfo.Length>0 || uri.Query.Length>0 || uri.Fragment.Length>0 || uri.AbsolutePath!="/")
            throw new ActivityException("Configure a trusted sandbox return URL.",503);
        return uri.GetLeftPart(UriPartial.Authority);
    }
    static bool ValidKey(string? value,string prefix)=>value is not null&&Regex.IsMatch(value,"^"+prefix+"_test_[A-Za-z0-9_]{16,512}$");
}
