using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.DataSaturation.Domain.Entities;
using Resto.Front.Api.DataSaturation.Domain.Helpers;
using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces.Services;
using Resto.Front.Api.DataSaturation.MindBox.Entities;
using Resto.Front.Api.Extensions;
using Resto.Front.Api.UI;
using System;
using System.Reactive.Disposables;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Resto.Front.Api.DataSaturation.Services
{
    public class BarcodeScannerService : IBarcodeScannerService
    {
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();
        private bool isDisposed = false;
        private readonly IIikoCardService iikoCardService;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public BarcodeScannerService(IIikoCardService iikoCardService) 
        {
            this.iikoCardService = iikoCardService;
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
                try
                {
                    OrganizationGuestInfo client = null;
                    Task.Run(async () =>
                    {
                        client = await iikoCardService.GetCustomerAsync(payload.i, cancellationTokenSource.Token);
                    }, cancellationTokenSource.Token).GetAwaiter().GetResult(); ;
                        if (client == null)
                            return !matches;

                        var editSession = obj.os.CreateEditSession();
                        editSession.RenameOrderGuest(obj.order.Guests[0].Id, client.name, obj.order);
                        //editSession.AddOrderGuest(client.id, client.name, obj.order);
                        //editSession.DeleteOrderGuest(obj.order, obj.order.Guests[0]);
                        obj.os.SubmitChanges(editSession, obj.os.GetDefaultCredentials());
                        PluginContext.Log.Info($"[{nameof(BarcodeScannerService)}|{nameof(BarcodeScanned)}] Add client {client.id} {client.surname} {client.name} to order {obj.order.Id} {obj.order.Number}");
                    }
                catch (Exception ex)
                {
                    PluginContext.Log.Info($"[{nameof(BarcodeScannerService)}|{nameof(BarcodeScanned)}] Get error in GetClientById on id: {payload.i}. {ex}");
                }
            }
            return matches;
        }
/*
        private async Task AddCustomerToOrder(IOrder order, IOperationService os, string customerId)
        {
            try
            {
                var client = await iikoCardService.GetCustomerAsync(customerId, cancellationTokenSource.Token); 
                if (client == null) 
                    return;

                var editSession = PluginContext.Operations.CreateEditSession();
                editSession.RenameOrderGuest(client.id, client.name, order);
                PluginContext.Operations.SubmitChanges(editSession, PluginContext.Operations.GetDefaultCredentials());
                PluginContext.Log.Info($"[{nameof(BarcodeScannerService)}|{nameof(BarcodeScanned)}] Add client {client.id} {client.surname} {client.name} to order {order.Id} {order.Number}");
            }
            catch (Exception ex)
            {
                PluginContext.Log.Info($"[{nameof(BarcodeScannerService)}|{nameof(BarcodeScanned)}] Get error in GetClientById on id: {customerId}. {ex}");
            }
        }
*/
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
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            subscriptions?.Dispose();
        }
    }
}
