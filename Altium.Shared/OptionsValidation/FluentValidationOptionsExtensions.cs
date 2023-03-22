using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Altium.Shared.OptionsValidation;

public static class FluentValidationOptionsExtensions
{
    public static OptionsBuilder<TOptions> ConfigureWithValidation<TOptions, TValidator>(
        this IServiceCollection services,
        string configurationSection)
    where TOptions : class
    where TValidator : class, IValidator<TOptions>
    {
        // Add the validator
        services.TryAddSingleton<IValidator<TOptions>, TValidator>();

        return services.AddOptions<TOptions>()
            .BindConfiguration(configurationSection)
            .ValidateFluentValidation()
            .ValidateOnStart();
    }
}