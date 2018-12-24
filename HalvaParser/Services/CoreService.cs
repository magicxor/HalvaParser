using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HalvaParser.Services
{
    public class CoreService
    {
        private readonly ILogger<CoreService> _logger;
        private readonly Scraper _scraper;
        private readonly Exporter _exporter;

        public CoreService(ILogger<CoreService> logger, Scraper scraper, Exporter exporter)
        {
            _logger = logger;
            _scraper = scraper;
            _exporter = exporter;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                var items = await _scraper.ScrapeAsync(cancellationToken);
                var values = items.SelectMany(item => item.Value).ToList();
                _exporter.ExportToCsv(values);
            }
            catch (OperationCanceledException e)
            {
                _logger.LogError(e, "Operation canceled");
            }
        }
    }
}
