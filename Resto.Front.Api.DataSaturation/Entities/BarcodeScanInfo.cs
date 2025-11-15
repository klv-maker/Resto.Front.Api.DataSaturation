namespace Resto.Front.Api.DataSaturation.Entities
{
    public class BarcodeScanInfo
    {
        public string iikoCustomerId { get; set; }
        public string totp { get; set; }
        public long timestamp { get; set; }
    }
}
