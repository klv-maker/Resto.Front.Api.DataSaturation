using Newtonsoft.Json;

namespace Resto.Front.Api.DataSaturation.Domain.Entities
{
    public class BarcodeScanInfo
    {
        [JsonProperty("p")]
        public string PhoneNumber { get; set; }
        [JsonProperty("o")]
        public string Totp { get; set; }
        [JsonProperty("t")]
        public long Timestamp { get; set; }
    }
}
