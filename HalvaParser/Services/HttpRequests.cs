using System;
using System.Collections.Generic;

namespace HalvaParser.Services
{
    public static class HttpRequests
    {
        private static string BuildQuery(Dictionary<string, string> parameters)
        {
            var parametersList = new List<string>();
            foreach (var item in parameters)
            {
                parametersList.Add(item.Key + "=" + Uri.EscapeDataString(string.IsNullOrEmpty(item.Value) ? "" : item.Value));
            }
            var query = string.Join("&", parametersList);
            return query;
        }

        public static Uri BuildUri(string scheme, string hostName, int port, string path, Dictionary<string, string> parameters)
        {
            var query = BuildQuery(parameters);
            var uriBuilder = new UriBuilder(scheme, hostName, port)
            {
                Path = path,
                Query = query
            };
            return uriBuilder.Uri;
        }

        public static Uri BuildUri(string schemeAndHost, string path, Dictionary<string, string> parameters)
        {
            var query = BuildQuery(parameters);
            var uri = schemeAndHost + path + "?" + query;
            var uriBuilder = new UriBuilder(uri);
            return uriBuilder.Uri;
        }

        public static Uri BuildUri(string uri, Dictionary<string, string> parameters)
        {
            var query = BuildQuery(parameters);
            uri = uri + "?" + query;
            var uriBuilder = new UriBuilder(uri);
            return uriBuilder.Uri;
        }
    }
}
