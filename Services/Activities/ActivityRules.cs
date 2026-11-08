using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KicksMvc.ViewModels;
using Microsoft.AspNetCore.DataProtection;
namespace KicksMvc.Services.Activities;
public sealed class ActivityException(string message,int status=409):Exception(message){public int Status {get;}=status;}
public sealed class ActivityRules(IDataProtectionProvider protection,TimeProvider clock)
{
    readonly IDataProtector versions=protection.CreateProtector("Kicks.ActivityVersion.v1");
    public DateTimeOffset Now=>clock.GetUtcNow();
    public DateTimeOffset Start(ActivitySlotInput input)
    {
        if(input.Date is null||input.Hour is <13 or >22||input.DurationMinutes is not(60 or 120)||input.Hour+input.DurationMinutes/60>23)throw new ActivityException("Choose a full-hour 60/120-minute session within 13:00–23:00 Amman.",400);
        var start=new DateTimeOffset(input.Date.Value.ToDateTime(new TimeOnly(input.Hour,0)),TimeSpan.FromHours(3));if(start<=Now)throw new ActivityException("Choose a future Amman date and time.",400);return start;
    }
    public bool Valid(DateTimeOffset start,int duration)=>start.Offset==TimeSpan.FromHours(3)&&start.TimeOfDay.Ticks%TimeSpan.TicksPerHour==0&&duration is 60 or 120&&start.Hour>=13&&start.Hour+duration/60<=23;
    public string Version(object value)=>versions.Protect(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)))));
    public void Check(string value,string expected){try{if(versions.Unprotect(value)!=versions.Unprotect(expected))throw new ActivityException("This record changed. Refresh before trying again.");}catch(Exception e)when(e is CryptographicException or FormatException or ArgumentException){throw new ActivityException("This record changed. Refresh before trying again.");}}
    public static decimal Price(decimal hourly,int duration){var hours=duration/60m;if(duration is not(60 or 120)||hourly<0||hourly>9999999999.99m/hours)throw new ActivityException("The reservation price is outside the supported range.",400);return hourly*hours;}
}
