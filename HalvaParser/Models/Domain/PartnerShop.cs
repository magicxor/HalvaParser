using Newtonsoft.Json;
using System.Collections.Generic;

namespace HalvaParser.Models.Domain
{
    public class PartnerShop
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("phones")]
        public List<string> Phones { get; set; }

        [JsonProperty("kladrId")]
        public string KladrId { get; set; }

        [JsonProperty("shopId")]
        public string ShopId { get; set; }

        [JsonProperty("iconUrl")]
        public string IconUrl { get; set; }

        [JsonProperty("installmentPeriod")]
        public string InstallmentPeriod { get; set; }

        [JsonProperty("site")]
        public string Site { get; set; }

        [JsonProperty("point")]
        public List<decimal> Point { get; set; }

        [JsonProperty("partnerName")]
        public string PartnerName { get; set; }

        [JsonProperty("installmentTerms")]
        public List<object> InstallmentTerms { get; set; }

        [JsonProperty("siteTitle")]
        public string SiteTitle { get; set; }

        [JsonProperty("distance")]
        public decimal Distance { get; set; }
    }
}
