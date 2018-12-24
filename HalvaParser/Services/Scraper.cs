using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Extensions;
using AngleSharp.Network;
using HalvaParser.Models;
using HalvaParser.Models.Application;
using HalvaParser.Models.Domain;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Async;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HttpMethod = AngleSharp.Network.HttpMethod;

namespace HalvaParser.Services
{
    public class Scraper
    {
        private readonly ILogger<Scraper> _logger;
        private readonly CommandLineOptions _commandLineOptions;
        private readonly PlainTextMarkupFormatter _plainTextMarkupFormatter;

        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:64.0) Gecko/20100101 Firefox/64.0";

        public Scraper(ILogger<Scraper> logger, CommandLineOptions commandLineOptions, PlainTextMarkupFormatter plainTextMarkupFormatter)
        {
            _logger = logger;
            _commandLineOptions = commandLineOptions;
            _plainTextMarkupFormatter = plainTextMarkupFormatter;
        }

        private IBrowsingContext CreateBrowsingContext()
        {
            var browsingContextConfig = Configuration.Default.WithDefaultLoader().WithCss().WithLocaleBasedEncoding().WithCookies();
            var browsingContext = BrowsingContext.New(browsingContextConfig);
            return browsingContext;
        }

        private string GetHref(IElement link)
        {
            var href = link.Attributes.GetNamedItem("href").Value;
            return href;
        }

        private List<string> GetLinks(IDocument pageContent)
        {
            var linkElements = pageContent.QuerySelectorAll("a");
            var links = linkElements.Select(GetHref).ToList();
            return links;
        }

        public async Task<List<string>> GetPagePartnerUriListAsync(int pageNumber, IBrowsingContext browsingContext, CancellationToken cancellationToken)
        {
            var pageUri = HttpRequests.BuildUri(Defaults.HalvacardPagerUri, new Dictionary<string, string>()
            {
                { "page", pageNumber.ToString() },
                { "category", "all-shops" },
            });
            var documentRequest = new DocumentRequest(Url.Convert(pageUri));
            documentRequest.Headers.TryAdd("User-Agent", UserAgent);
            var pageContent = await browsingContext.OpenAsync(documentRequest, cancellationToken);
            var pageText = pageContent.ToHtml(_plainTextMarkupFormatter);
            var notFound = pageContent.StatusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(pageText) || pageText.Contains("ничего не найдено");
            var pageLinks = notFound ? new List<string>() : GetLinks(pageContent);
            _logger.LogInformation($"{pageLinks.Count} links found on {pageUri}, status code: {pageContent.StatusCode}");

            return pageLinks;
        }

        public async Task<List<string>> GetPartnerUriListAsync(IBrowsingContext browsingContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("obtaining partner URIs started");
            var allUriList = new List<string>();
            List<string> pageUriList;

            var i = 0;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                pageUriList = await GetPagePartnerUriListAsync(i, browsingContext, cancellationToken);
                allUriList.AddRange(pageUriList);
                i++;
            } while (pageUriList.Count > 0);

            _logger.LogInformation($"obtaining partner URIs finished ({allUriList.Count} total)");
            return allUriList;
        }

        public async Task<List<PartnerShop>> GetPartnerShopsPageAsync(string partnerUri, string csrf, int offset, int limit, IBrowsingContext browsingContext, CancellationToken cancellationToken)
        {
            var uri = Defaults.HalvacardUri + partnerUri + "/nearest-shop";
            var formData = new Dictionary<string, string>()
            {
                { "offset", offset.ToString() },
                { "limit", limit.ToString() },
                { "lat", _commandLineOptions.Latitude },
                { "lng", _commandLineOptions.Longitude },
                { "_csrf", csrf },
            };
            var formDataEncoded = new FormUrlEncodedContent(formData);
            var formDataStream = await formDataEncoded.ReadAsStreamAsync();
            var documentRequest = new DocumentRequest(new Url(uri))
            {
                Method = HttpMethod.Post,
                Referer = Defaults.HalvacardUri + partnerUri,
                Body = formDataStream,
            };
            documentRequest.Headers.TryAdd("Accept", "*/*");
            documentRequest.Headers.TryAdd("User-Agent", UserAgent);
            documentRequest.Headers.TryAdd("Content-Type", "application/x-www-form-urlencoded");
            documentRequest.Headers.TryAdd("X-Requested-With", "XMLHttpRequest");
            var pageContent = await browsingContext.OpenAsync(documentRequest, cancellationToken);
            var pageContentString = pageContent.ToHtml(_plainTextMarkupFormatter);
            var partnerShops =
                pageContent.StatusCode == HttpStatusCode.OK
                && !string.IsNullOrWhiteSpace(pageContentString)
                    ? JsonConvert.DeserializeObject<List<PartnerShop>>(pageContentString)
                    : new List<PartnerShop>();
            _logger.LogInformation($"{partnerShops.Count} shops found on {uri} ({nameof(offset)}={offset}, {nameof(limit)}={limit}), status code: {pageContent.StatusCode}");
            return partnerShops;
        }

        public async Task<string> GetCsrfAsync(string partnerUri, IBrowsingContext browsingContext, CancellationToken cancellationToken)
        {
            var uri = Defaults.HalvacardUri + partnerUri;
            var documentRequest = new DocumentRequest(new Url(uri));
            var pageContent = await browsingContext.OpenAsync(documentRequest, cancellationToken);
            var errorMessage = $"csrf not found on {uri}; status code: {pageContent.StatusCode}";
            var csrfElement = pageContent.Head.QuerySelector("meta[name=csrf-token]");
            if (csrfElement != null)
            {
                if (csrfElement.HasAttribute("content"))
                {
                    var csrf = csrfElement.GetAttribute("content");
                    _logger.LogDebug($"got csrf token from {uri}: {csrf}; status code: {pageContent.StatusCode}");
                    return csrf;
                }
                else
                {
                    throw new Exception(errorMessage);
                }
            }
            else
            {
                throw new Exception(errorMessage);
            }
        }

        public async Task<List<PartnerShop>> GetPartnerShopsAsync(string partnerUri, IBrowsingContext browsingContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"obtaining partner shops started ({partnerUri})");
            var allPartnerShops = new List<PartnerShop>();
            List<PartnerShop> pagePartnerShops;

            const int limit = 10000;
            var offset = 0;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                var csrf = await GetCsrfAsync(partnerUri, browsingContext, cancellationToken);
                pagePartnerShops = await GetPartnerShopsPageAsync(partnerUri, csrf, offset, limit, browsingContext, cancellationToken);
                allPartnerShops.AddRange(pagePartnerShops);
                offset += limit;
            } while (pagePartnerShops.Count > 0);

            _logger.LogInformation($"obtaining partner shops finished ({allPartnerShops.Count} total)");
            return allPartnerShops;
        }

        public async Task<Dictionary<string, List<PartnerShop>>> GetAllPartnerShopsAsync(List<string> partnerUriList, CancellationToken cancellationToken)
        {
            _logger.LogInformation("obtaining partners shops started");
            var allPartnerShops = new ConcurrentDictionary<string, List<PartnerShop>>();

            await partnerUriList.ParallelForEachAsync(async partnerUri =>
                {
                    var browsingContext = CreateBrowsingContext();
                    var partnerShops = await GetPartnerShopsAsync(partnerUri, browsingContext, cancellationToken);
                    allPartnerShops.TryAdd(partnerUri, partnerShops);
                }, 10, cancellationToken);
            
            _logger.LogInformation($"obtaining partners shops finished ({allPartnerShops.Count} total)");
            return allPartnerShops.ToDictionary(x => x.Key, x => x.Value);
        }

        public async Task<Dictionary<string, List<PartnerShop>>> ScrapeAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("scraping started");
            var browsingContext = CreateBrowsingContext();
            var partnerUriList = await GetPartnerUriListAsync(browsingContext, cancellationToken);
            var partnerShops = await GetAllPartnerShopsAsync(partnerUriList, cancellationToken);
            _logger.LogInformation($"scraping finished ({partnerShops.Count} partners total)");
            return partnerShops;
        }
    }
}
