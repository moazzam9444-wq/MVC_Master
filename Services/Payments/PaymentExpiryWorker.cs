namespace KicksMvc.Services.Payments;

public sealed class PaymentExpiryWorker(IServiceScopeFactory scopes,ILogger<PaymentExpiryWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer=new PeriodicTimer(TimeSpan.FromSeconds(15));
        while(!stoppingToken.IsCancellationRequested)
        {
            try{using var scope=scopes.CreateScope();await scope.ServiceProvider.GetRequiredService<StripePaymentService>().Sweep();}
            catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
            catch{logger.LogWarning("Sandbox hold maintenance will retry. No payment success was inferred.");}
            try{if(!await timer.WaitForNextTickAsync(stoppingToken))break;}catch(OperationCanceledException){break;}
        }
    }
}
