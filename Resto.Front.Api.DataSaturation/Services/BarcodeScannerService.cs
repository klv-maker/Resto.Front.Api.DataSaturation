using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.DataSaturation.Entities;
using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces.Services;
using Resto.Front.Api.UI;
using System;
using System.Reactive.Disposables;
using System.Security.Cryptography;
using System.Text;

namespace Resto.Front.Api.DataSaturation.Services
{
    public class BarcodeScannerService : IBarcodeScannerService
    {
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();
        private bool isDisposed = false;
        public BarcodeScannerService() 
        {
            subscriptions.Add(PluginContext.Notifications.OrderEditBarcodeScanned.Subscribe(BarcodeScanned));
        }

        private bool BarcodeScanned((string barcode, IOrder order, IOperationService os, IViewManager vm) obj)
        {
            var payload = obj.barcode.DeserializeFromJson<BarcodeScanInfo>();
            string masterSecret = "sk_KmDZI7u2GwaftSOfM0zR";
            var config = new { digits = 6, period = 30, window = 1 };

            string derivedSecretKey = DeriveSecret(masterSecret, payload.i);
            string computedTotp = GenerateTotp(derivedSecretKey, payload.t * 1000, config.digits, config.period);
            bool matches = computedTotp == payload.o;

            PluginContext.Log.Info("derivedSecretKey: " + derivedSecretKey);
            PluginContext.Log.Info("TOTP(из расчёта): " + computedTotp);
            PluginContext.Log.Info("Совпадает с payload.totp: " + matches);
            if (matches)
            {
                var client = obj.os.GetClientById(Guid.Parse(payload.i));
                obj.os.AddClientToOrder(obj.order, client, obj.os.GetDefaultCredentials());
                PluginContext.Log.Info($"[{nameof(BarcodeScannerService)}|{nameof(BarcodeScanned)}] Add client {client.Id} {client.Surname} {client.Name} to order {obj.order.Id} {obj.order.Number}");
            }
            return matches;
        }

        static string DeriveSecret(string master, string customerId)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(master);
            byte[] messageBytes = Encoding.UTF8.GetBytes(customerId);

            using (var hmac = new HMACSHA1(keyBytes))
            {
                byte[] hash = hmac.ComputeHash(messageBytes);
                return Convert.ToBase64String(hash);
            }
        }

        static byte[] NumberToBigEndianBytes(long num)
        {
            byte[] bytes = new byte[8];
            for (int i = 7; i >= 0; i--)
            {
                bytes[i] = (byte)(num & 0xFF);
                num >>= 8;
            }
            return bytes;
        }

        static string TruncateToCode(byte[] hmac)
        {
            int offset = hmac[hmac.Length - 1] & 0x0F;
            int binCode =
                ((hmac[offset] & 0x7F) << 24) |
                ((hmac[offset + 1] & 0xFF) << 16) |
                ((hmac[offset + 2] & 0xFF) << 8) |
                (hmac[offset + 3] & 0xFF);

            int modulus = (int)Math.Pow(10, 6); // config.digits = 6
            return (binCode % modulus).ToString().PadLeft(6, '0');
        }

        static string GenerateTotp(string secretBase64, long timestampMs, int digits, int period)
        {
            long counter = timestampMs / 1000 / period;
            byte[] counterBytes = NumberToBigEndianBytes(counter);
            byte[] keyBytes = Convert.FromBase64String(secretBase64);

            using (var hmac = new HMACSHA1(keyBytes))
            {
                byte[] hash = hmac.ComputeHash(counterBytes);
                return TruncateToCode(hash);
            }
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            subscriptions?.Dispose();
        }
    }
}
