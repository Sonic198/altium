using Altium.FileGenerator.Options;
using Altium.FileGenerator.Services;
using Altium.FileGenerator.Services.Interface;
using Altium.Shared;
using Altium.Shared.OptionsValidation;
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
        services.ConfigureWithValidation<FileGenerationOptions, FileGenerationOptionsValidator>(FileGenerationOptions.Section);
        //services
        services.AddSingleton<IFileGeneratorService, FileGeneratorService>();
    })
    .UseSerilog()
    .Build();

try
{ 
    host.Start();

    var cancelationToken = CancellationTokenHelper.CancelOnKeyPress();

    var fileGenerator = host.Services.GetRequiredService<IFileGeneratorService>();
    await fileGenerator.GenerateFile(cancelationToken);
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



