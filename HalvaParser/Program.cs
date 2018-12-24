using CommandLine;
using HalvaParser.DependencyInjection;
using HalvaParser.Models.Application;
using HalvaParser.Services;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HalvaParser
{
    internal class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // see: https://docs.microsoft.com/en-us/windows/desktop/Debug/system-error-codes
        private const int ErrorSuccess = 0;
        private const int ErrorBadArguments = 160;

        private static async Task<int> RunApplicationAsync(CommandLineOptions commandLineOptions)
        {
            var serviceProvider = ContainerConfiguration.CreateServiceProvider(commandLineOptions);
            var applicationCore = serviceProvider.GetService<CoreService>();

            var cancelTokenSource = new CancellationTokenSource();
            var cancellationToken = cancelTokenSource.Token;
            Console.CancelKeyPress += (s, a) =>
            {
                cancelTokenSource.Cancel();
                a.Cancel = true;
            };
            await applicationCore.RunAsync(cancellationToken);

            return ErrorSuccess;
        }

        private static async Task<int> Main(string[] commandLineArguments)
        {
            try
            {
                var exitCode = await Parser.Default.ParseArguments<CommandLineOptions>(commandLineArguments)
                    .MapResult(RunApplicationAsync, errors => Task.FromResult(ErrorBadArguments));
                return exitCode;
            }
            catch (Exception e)
            {
                Logger.Fatal(e);
                throw;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }
    }
}
