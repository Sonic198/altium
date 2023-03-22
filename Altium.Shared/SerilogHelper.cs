using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Formatting.Compact;
using System.Threading;

namespace Altium.Shared;

public static class SerilogHelper
{
    public static Logger CreateLogger(IConfiguration config)
    {
        return new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
                .WithDefaultDestructurers())
            .WriteTo.Console(new CompactJsonFormatter())
            .CreateLogger();
    }
}
