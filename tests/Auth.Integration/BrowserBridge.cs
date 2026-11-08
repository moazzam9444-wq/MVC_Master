using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using KicksMvc.Data;

// Loopback transport for browser tests against the exact MVC TestServer application.
// Authentication override and mutation guard exist only in this excluded test assembly.
static class BrowserBridge
{
    public static async Task RunAsync()
    {
        var marker = Environment.GetEnvironmentVariable("KICKS_BROWSER_MARKER") ?? "";
        var token = Environment.GetEnvironmentVariable("KICKS_REVIEW_TOKEN") ?? "";
        if (!System.Text.RegularExpressions.Regex.IsMatch(marker,"^auth-browser-[a-f0-9]{32}$") || token.Length < 48)
            throw new InvalidOperationException("Browser verification requires a unique marker and private test token.");
        using var factory = new AuthFactory { AllowedPrefix = marker, ReviewerToken = token };
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KicksDbContext>();
        factory.ReviewerId = await db.UserRoles.Where(ur => db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "admin"))
            .Select(ur => ur.UserId).SingleAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect=false, HandleCookies=false });
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:5613");
        builder.Logging.ClearProviders();
        await using var app = builder.Build();
        app.Run(async context =>
        {
            var destination = new Uri("http://127.0.0.1:5613" + context.Request.Path + context.Request.QueryString);
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method),destination);
            if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
                request.Content = new StreamContent(context.Request.Body);
            foreach (var header in context.Request.Headers)
                if (!request.Headers.TryAddWithoutValidation(header.Key,header.Value.ToArray()) && request.Content is not null)
                    request.Content.Headers.TryAddWithoutValidation(header.Key,header.Value.ToArray());
            using var response = await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,context.RequestAborted);
            context.Response.StatusCode=(int)response.StatusCode;
            foreach (var header in response.Headers) context.Response.Headers[header.Key]=header.Value.ToArray();
            foreach (var header in response.Content.Headers) context.Response.Headers[header.Key]=header.Value.ToArray();
            context.Response.Headers.Remove("transfer-encoding");
            await response.Content.CopyToAsync(context.Response.Body,context.RequestAborted);
        });
        Console.WriteLine("Browser verification bridge ready on loopback port 5613. Test token is not logged.");
        await app.RunAsync();
    }
}
