using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace KicksMvc.Services.Payments;

// Local development tool only. Never serves an HTTP endpoint or accepts credentials as arguments.
public static class StripeTestListener
{
    public static async Task<int> RunAsync(IConfiguration configuration,IHostEnvironment environment,bool isolatedOnly=true)
    {
        if(!environment.IsDevelopment()){Console.WriteLine("Listener is Development-only.");return 2;}
        try
        {
            var target=(isolatedOnly?Environment.GetEnvironmentVariable("KICKS_TEST_CONNECTION"):configuration.GetConnectionString("DefaultConnection"))??throw new InvalidOperationException();
            var cs=new SqlConnectionStringBuilder(target);
            var isolated=Regex.IsMatch(cs.InitialCatalog,@"^Kicks_BookingTest_\d{8}_[a-f0-9]{8}$");
            if(!cs.IntegratedSecurity||!string.Equals(cs.DataSource,"localhost",StringComparison.OrdinalIgnoreCase)||
                (isolatedOnly?!isolated:cs.InitialCatalog!="Kicks"))throw new InvalidOperationException();
            using(var connection=new SqlConnection(target))
            {
                await connection.OpenAsync();using var command=connection.CreateCommand();
                command.CommandText=isolatedOnly?"SELECT CASE WHEN DB_NAME()=@name AND DB_NAME()<>N'Kicks' AND NOT EXISTS(SELECT 1 FROM sys.master_files t JOIN sys.master_files o ON o.physical_name=t.physical_name AND o.database_id=DB_ID(N'Kicks') WHERE t.database_id=DB_ID()) THEN 1 ELSE 0 END":"SELECT CASE WHEN DB_NAME()=@name AND OBJECT_ID(N'dbo.gateway_payments') IS NOT NULL THEN 1 ELSE 0 END";
                command.Parameters.AddWithValue("@name",cs.InitialCatalog);if(Convert.ToInt32(await command.ExecuteScalarAsync())!=1)throw new InvalidOperationException();
            }
            var settings=new SandboxSettings(configuration);_=settings.Rate();
            var forward=isolatedOnly?new Uri("http://127.0.0.1:5621"):new Uri(settings.BaseUrl());
            if(!forward.IsLoopback||forward.Port<1024)throw new InvalidOperationException();
            var path=Path.GetFullPath(".local/stripe-phase/tools/stripe.exe");
            if(!File.Exists(path))path="stripe"; // Official CLI may also be installed on PATH.
            var start=new ProcessStartInfo(path){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
            foreach(var arg in new[]{"listen","--forward-to",forward.GetLeftPart(UriPartial.Authority)+"/payments/stripe/webhook","--events","checkout.session.completed,checkout.session.async_payment_succeeded,checkout.session.expired,payment_intent.payment_failed","--skip-update"})start.ArgumentList.Add(arg);
            start.Environment["STRIPE_API_KEY"]=configuration["Stripe:SecretKey"];
            start.Environment["STRIPE_CLI_TELEMETRY_OPTOUT"]="true";
            using var process=Process.Start(start)??throw new InvalidOperationException();
            using var stop=new CancellationTokenSource();Console.CancelKeyPress+=(sender,e)=>{e.Cancel=true;stop.Cancel();};
            var setup=new SemaphoreSlim(1,1);var ready=false;
            async Task Consume(StreamReader reader)
            {
                while(await reader.ReadLineAsync() is string line)
                {
                    var match=Regex.Match(line,@"whsec_[A-Za-z0-9_]{16,512}");if(!match.Success)continue;
                    await setup.WaitAsync();try
                    {
                        if(ready)continue;
                        var secretStart=new ProcessStartInfo("dotnet"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
                        foreach(var arg in new[]{"user-secrets","set","--project",Path.GetFullPath("KicksMvc.csproj"),"--configuration","Release"})secretStart.ArgumentList.Add(arg);
                        using var secretProcess=Process.Start(secretStart)??throw new InvalidOperationException();
                        var stdout=secretProcess.StandardOutput.ReadToEndAsync();var stderr=secretProcess.StandardError.ReadToEndAsync();
                        await secretProcess.StandardInput.WriteAsync(JsonSerializer.Serialize(new Dictionary<string,string>{{"Stripe:WebhookSecret",match.Value}}));secretProcess.StandardInput.Close();
                        await secretProcess.WaitForExitAsync();await stdout;await stderr;if(secretProcess.ExitCode!=0)throw new InvalidOperationException();
                        ready=true;
                        Directory.CreateDirectory(".local/stripe-phase");
                        await File.WriteAllTextAsync(".local/stripe-phase/listener-status.json",JsonSerializer.Serialize(new{Ready=true,SigningSecretStored=true,ChildProcessId=process.Id,ForwardPort=forward.Port,Database=cs.InitialCatalog,IsolatedOnly=isolatedOnly}));
                        Console.WriteLine($"READY: Stripe TEST listener for {cs.InitialCatalog}, local port {forward.Port}; signing secret stored securely. No credential output. Start/restart the matching MVC app after READY.");
                    }finally{setup.Release();}
                }
            }
            var output=Task.WhenAll(Consume(process.StandardOutput),Consume(process.StandardError));
            try
            {
                var ended=process.WaitForExitAsync(stop.Token);
                await Task.WhenAny(ended,output,Task.Delay(TimeSpan.FromSeconds(45),stop.Token));
                if(output.IsFaulted||!ready)throw new InvalidOperationException();
                await ended;await output;return 0;
            }
            finally{if(!process.HasExited)process.Kill(true);}
        }
        catch(OperationCanceledException){Console.WriteLine("Sandbox listener stopped.");return 0;}
        catch{Console.WriteLine("Sandbox listener could not start safely. Check Development mode, target database/migration and official Stripe CLI installation. No credentials or CLI output were disclosed.");return 2;}
    }
}
