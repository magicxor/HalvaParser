using CsvHelper;
using CsvHelper.Configuration;
using HalvaParser.Models.Application;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;

namespace HalvaParser.Services
{
    public class Exporter
    {
        private readonly ILogger<Exporter> _logger;
        private readonly CommandLineOptions _commandLineOptions;

        public Exporter(ILogger<Exporter> logger, CommandLineOptions commandLineOptions)
        {
            _logger = logger;
            _commandLineOptions = commandLineOptions;
        }

        public void ExportToCsv<T>(IList<T> items)
        {
            if (items.Count == 0)
            {
                _logger.LogInformation($"Export has been cancelled: items.Count == 0");
            }
            else
            {
                _logger.LogInformation($"Exporting {items.Count} items to {_commandLineOptions.Output}...");
                var stream = new FileStream(_commandLineOptions.Output, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: false);
                var writer = new StreamWriter(stream) { AutoFlush = true };
                var csv = new CsvWriter(writer, new Configuration { Delimiter = _commandLineOptions.Delimiter });
                csv.WriteRecords(items);
                _logger.LogInformation($"Export has been successfully finished");
            }
        }
    }
}
