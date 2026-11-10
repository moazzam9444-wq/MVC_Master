using KicksMvc.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Identity;
using KicksMvc.Models.Entities;
using KicksMvc.Services.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;
using KicksMvc.Services.Fields;
using KicksMvc.Services.Bookings;
var builder = WebApplication.CreateBuilder(args);
if(args.Contains("--stripe-test-listener")||args.Contains("--stripe-local-listener"))
{
    Environment.ExitCode=await KicksMvc.Services.Payments.StripeTestListener.RunAsync(builder.Configuration,builder.Environment,args.Contains("--stripe-test-listener"));
    return;
}
// Read-only sandbox preflight. Exits before database registration or web hosting.
if (args.Contains("--verify-stripe-sandbox"))
{
    Environment.ExitCode = await KicksMvc.Services.Payments.StripeSandboxPreflight.RunAsync(
        builder.Configuration, builder.Environment);
    return;
}
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add(new ResponseCacheAttribute { NoStore = true, Location = ResponseCacheLocation.None });
});
if (args.Contains("--verify-model") || args.Contains("--verify-model-details"))
{
    using var db = new KicksDbContext(new DbContextOptionsBuilder<KicksDbContext>().UseSqlServer("Server=unused;Database=Kicks;Integrated Security=True").Options);
    if (args.Contains("--verify-model-details"))
    {
        Console.WriteLine(KicksMvc.Data.Diagnostics.KicksModelInspection.Describe(db, args.Contains("--verify-model-behavior")));
        return;
    }
    var entities = db.Model.GetEntityTypes().ToList();
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { Tables = entities.Count, Columns = entities.Sum(e => e.GetProperties().Count()), Relationships = entities.Sum(e => e.GetForeignKeys().Count()) }));
    return;
}
var connection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection before starting KICKS.");
builder.Services.AddDbContext<KicksDbContext>(options => options.UseSqlServer(connection));
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version2;
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
}).AddEntityFrameworkStores<KicksDbContext>().AddDefaultTokenProviders()
    .AddSignInManager<KicksSignInManager>().AddPasswordValidator<LetterPasswordValidator>()
    .AddClaimsPrincipalFactory<KicksClaimsPrincipalFactory>();
builder.Services.AddScoped<OwnershipDocumentStore>();
builder.Services.AddScoped<FieldService>();
builder.Services.AddScoped<KicksMvc.Services.Teams.TeamService>();
builder.Services.AddScoped<KicksMvc.Services.Teams.TeamLogoStore>();
builder.Services.AddScoped<KicksMvc.Services.Activities.ActivityRules>();
builder.Services.AddScoped<KicksMvc.Services.Activities.OpenGameService>();
builder.Services.AddScoped<KicksMvc.Services.Activities.ChallengeService>();
builder.Services.AddScoped<KicksMvc.Services.Tournaments.TournamentService>();
builder.Services.AddScoped<FieldImageStore>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<KicksMvc.Services.Workspace.WorkspaceService>();
builder.Services.AddScoped<KicksMvc.Services.Payments.SandboxSettings>();
builder.Services.AddScoped<KicksMvc.Services.Payments.PaymentReservations>();
builder.Services.AddScoped<KicksMvc.Services.Payments.StripePaymentService>();
builder.Services.AddHostedService<KicksMvc.Services.Payments.PaymentExpiryWorker>();
// Explicit reference-data initialization only; no account or schema changes.
if(args.Contains("--initialize-field-amenities"))
{
    using var host=builder.Build();
    if(!host.Environment.IsDevelopment())throw new InvalidOperationException("Reference initialization is Development-only.");
    using var scope=host.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<KicksDbContext>();
    foreach(var name in new[]{"Floodlights","Parking","Changing rooms","Showers","Seating","Water"})
        if(!db.Amenities.Any(a=>a.Name==name))db.Amenities.Add(new Amenity{Name=name});
    await db.SaveChangesAsync();Console.WriteLine("Field amenity reference data initialized.");return;
}
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Pages/Auth/login.html";
    options.AccessDeniedPath = "/Pages/Auth/access-denied.html";
    options.ReturnUrlParameter = "returnTo";
    options.Cookie.Name = "Kicks.Identity";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
    options.Events.OnValidatePrincipal = AccountAccess.ValidateCookieAsync;
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var feedbackRequest=context.HttpContext.Request.Path=="/feedback";
        context.HttpContext.Response.Headers.RetryAfter = feedbackRequest?"300":"60";
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(feedbackRequest?"Too many feedback submissions. Wait a few minutes and try again.":"Too many requests. Wait one minute and try again.", cancellationToken);
    };
    options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("feedback", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 5, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));
    options.AddPolicy("messages", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
// Explicit CLI only. This branch is never invoked by ordinary application startup.
if (args.Contains("--provision-admin"))
{
    using var host = builder.Build();
    Environment.ExitCode = await DevelopmentAdminProvisioning.RunAsync(host.Services, host.Environment);
    return;
}
var app = builder.Build();
app.Use(async(context,next)=>
{
    context.Response.Headers.XContentTypeOptions="nosniff";
    context.Response.Headers.XFrameOptions="DENY";
    context.Response.Headers["Referrer-Policy"]="strict-origin-when-cross-origin";
    await next();
});
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
var types = new FileExtensionContentTypeProvider();
app.UseWhen(context=>HttpMethods.IsGet(context.Request.Method) &&
    context.Request.Headers.Accept.ToString().Contains("text/html",StringComparison.OrdinalIgnoreCase) &&
    !context.Request.Path.StartsWithSegments("/api"),
    branch=>branch.UseStatusCodePagesWithReExecute("/status/{0}"));
types.Mappings[".sql"] = "text/plain; charset=utf-8";
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = types });
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
