using Altium.FileSorter.Options;
using Altium.Shared;
using Altium.Shared.OptionsValidation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var host = Host.CreateDefaultBuilder()
    .ConfigureHostConfiguration(builder =>
    {
        builder.BuildConfig();
    })
    .ConfigureServices((context, services) =>
    {
        Log.Logger = SerilogHelper.CreateLogger(context.Configuration);
        //configuration
        services.ConfigureWithValidation<ExternalSortOptions, ExternalSortOptionsValidator>(ExternalSortOptions.Section);
        //servcies
        services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton, includeInternalTypes: true);
        services.AddSingleton<IExternalSortService, ExternalSortService>();
    })
    .UseSerilog()
    .Build();

try
{
    host.Start();

    var cancelationToken = CancellationTokenHelper.CancelOnKeyPress();

    var externalSort = host.Services.GetRequiredService<IExternalSortService>();
    await externalSort.SortAsync(cancelationToken);
}
catch (Exception e)
{
    Log.Error(e, e.Message);
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

Console.ReadLine();
