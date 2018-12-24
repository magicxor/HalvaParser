using HalvaParser.Models.Application;
using HalvaParser.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;

namespace HalvaParser.DependencyInjection
{
    public static class ContainerConfiguration
    {
        private static IServiceCollection ConfigureServices(this IServiceCollection serviceCollection, CommandLineOptions commandLineOptions)
        {
            serviceCollection
                .AddSingleton(commandLineOptions)
                .AddSingleton<ILoggerFactory>(provider => new LoggerFactory().AddNLog(new NLogProviderOptions
                {
                    CaptureMessageTemplates = true,
                    CaptureMessageProperties = true
                }))
                .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
                .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace))
                .AddScoped<PlainTextMarkupFormatter>()
                .AddScoped<Exporter>()
                .AddScoped<Scraper>()
                .AddScoped<CoreService>();
            return serviceCollection;
        }

        public static IServiceProvider CreateServiceProvider(CommandLineOptions commandLineOptions)
        {
            return new ServiceCollection().ConfigureServices(commandLineOptions).BuildServiceProvider();
        }
    }
}
