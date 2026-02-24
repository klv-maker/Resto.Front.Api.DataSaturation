using Resto.Front.Api.Data.Brd;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.DataSaturation.Domain.Entities;
using Resto.Front.Api.DataSaturation.Domain.Helpers;
using Resto.Front.Api.DataSaturation.Domain.Models;
using Resto.Front.Api.DataSaturation.Domain.Views;
using Resto.Front.Api.DataSaturation.Interfaces.Services;
using Resto.Front.Api.DataSaturation.Interfaces.ViewModels;
using Resto.Front.Api.DataSaturation.ViewModels;
using Resto.Front.Api.DataSaturation.Views;
using Resto.Front.Api.UI;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.Services
{
    public class BarcodeScannerService : IBarcodeScannerService
    {
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();
        private bool isDisposed = false;
        private readonly IIikoCardService iikoCardService;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private const string masterSecret = "sk_KmDZI7u2GwaftSOfM0zR";
        private readonly BarcodeConfig config;
        private WindowOwner windowOwner;
        private ICustomerViewModel customerViewModel;

        public BarcodeScannerService(IIikoCardService iikoCardService) 
        {
            this.iikoCardService = iikoCardService;
            config = new BarcodeConfig { digits = 6, period = 30, window = 1 };
            subscriptions.Add(PluginContext.Notifications.OrderEditBarcodeScanned.Subscribe(BarcodeScanned));
        }

        private bool BarcodeScanned((string barcode, IOrder order, IOperationService os, IViewManager vm) obj)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(obj.barcode))
                    return false;
                var barcode = obj.barcode;
                if (barcode.FirstOrDefault() == '"')
                    barcode = barcode.Remove(0, 1);

                if (obj.barcode.LastOrDefault() == '"')
                    barcode = barcode.Remove(barcode.Length - 1, 1);

                var payload = barcode.DeserializeFromJson<BarcodeScanInfo>();

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
                            client = await iikoCardService.GetIikoCardCustomerAsync(payload.i, cancellationTokenSource.Token);
                        }, cancellationTokenSource.Token).GetAwaiter().GetResult();

                        if (client == null)
                        {
                            obj.vm.ShowErrorPopup($"Не найден покупатель по ID {payload.i}");
                            return false; 
                        }
                            

                        //TODO: знаю, что какая-то шляпа, но у нас пока нет метода для получения нормального покупателя
                        CustomerInfo customerInfo = null;
                        Task.Run(async () =>
                        {
                            customerInfo = await iikoCardService.GetCustomerAsync(client.phone, cancellationTokenSource.Token);
                        }, cancellationTokenSource.Token).GetAwaiter().GetResult();
                        if (customerInfo is null)
                        {
                            obj.vm.ShowErrorPopup($"Не найден покупатель по номеру телефона {client.phone}");
                            return false;
                        }

                        ShowCustomer(customerInfo);
                        AddCustomerToOrder(obj.order, obj.os, customerInfo.userData, client.id);
                    }
                    catch (Exception ex)
                    {
                        PluginContext.Log.Info($"[{nameof(BarcodeScannerService)}|{nameof(BarcodeScanned)}] Get error in GetClientById on id: {payload.i}. {ex}");
                    }
                }
                else 
                    obj.vm.ShowErrorPopup("QR-код устарел");
                return matches;
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(BarcodeScannerService)}|{nameof(BarcodeScanned)}] Get error barcode {ex}");
                return false;
            }
        }

        private void ShowCustomer(CustomerInfo customerInfo)
        {
            if (isDisposed)
                return;

            //явно вызываем очистку
            if (windowOwner != null)
            {
                windowOwner.Dispose();
                windowOwner = null;
            }
            windowOwner = new WindowOwner();
            customerViewModel = new CustomerViewModel(customerInfo);
            windowOwner.ShowDialog<CustomerWindow>(customerViewModel);
        }

        private void AddCustomerToOrder(IOrder order, IOperationService os, CustomerData customerData, Guid customerId)
        {
            var editSession = os.CreateEditSession();
            var guest = order.Guests.FirstOrDefault();
            if (guest != null)
                editSession.RenameOrderGuest(guest.Id, customerData.name, order);
            editSession.AddOrderExternalData(Constants.ExternalDataKeyCustomerNumber, customerData.phone, true, order);
            os.SubmitChanges(editSession, os.GetDefaultCredentials());
            PluginContext.Log.Info($"[{nameof(BarcodeScannerService)}|{nameof(AddCustomerToOrder)}] Add client {customerId} {customerData.lastName} {customerData.name} to order {order.Id} {order.Number}");

            CustomerAddData customerDataNew = null;
            Task.Run(async () => 
            {
                customerDataNew = await iikoCardService.AddCustomerToOrder(customerData.phone, order.Id, cancellationTokenSource.Token); 
            }, cancellationTokenSource.Token).GetAwaiter().GetResult();
            if (customerDataNew is null)
                PluginContext.Log.Error($"[{nameof(BarcodeScannerService)}|{nameof(AddCustomerToOrder)}] Something wrong");
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
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            subscriptions?.Dispose();

            if (customerViewModel?.CloseAction != null)
                customerViewModel.CloseAction();
            windowOwner?.Dispose();
        }
    }
}
