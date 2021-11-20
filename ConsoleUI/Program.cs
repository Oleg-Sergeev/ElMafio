using ConsoleUI;
using Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services;



using var host = Application.CreateHostBuilder(args)
                    .Build();

host.Services.GetRequiredService<LoggingService>();

using (var scope = host.Services.CreateScope())
    await scope.ServiceProvider.DatabaseMigrateAsync();

await host.RunAsync();
