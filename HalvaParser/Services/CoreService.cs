using HalvaParser.Models.Domain;
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
                var dtoList = items
                    .SelectMany(item => item.Value)
                    .Select(item => new PartnerShopDto()
                    {
                        Name = item.Name,
                        City = item.City,
                        Address = item.Address,
                        Phone1 = item.Phones.ElementAtOrDefault(0),
                        Phone2 = item.Phones.ElementAtOrDefault(1),
                        KladrId = item.KladrId,
                        ShopId = item.ShopId,
                        IconUrl = item.IconUrl,
                        InstallmentPeriod = item.InstallmentPeriod,
                        Site = item.Site,
                        Latitude = item.Point.ElementAtOrDefault(0),
                        Longitude = item.Point.ElementAtOrDefault(1),
                        PartnerName = item.PartnerName,
                        SiteTitle = item.SiteTitle,
                        Distance = item.Distance,
                    }).ToList();
                _exporter.ExportToCsv(dtoList);
            }
            catch (OperationCanceledException e)
            {
                _logger.LogError(e, "Operation canceled");
            }
        }
    }
}
