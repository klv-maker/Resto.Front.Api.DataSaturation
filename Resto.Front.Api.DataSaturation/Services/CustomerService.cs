using Resto.Front.Api.Data.Brd;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.Data.Security;
using Resto.Front.Api.DataSaturation.Domain.Helpers;
using Resto.Front.Api.DataSaturation.Domain.Models;
using Resto.Front.Api.DataSaturation.Interfaces.Services;
using Resto.Front.Api.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.Services
{
    public static class CustomerService
    {
        public static void AddCustomerToOrder(IIikoCardService iikoCardService, IOrder order, IOperationService os, CustomerInfo customerData, CancellationTokenSource cancellationTokenSource)
        {
            CustomerAddData customerDataNew = null;
            Task.Run(async () =>
            {
                customerDataNew = await iikoCardService.AddCustomerToOrder(customerData.userData.phone, order.Id, cancellationTokenSource.Token);
            }, cancellationTokenSource.Token).GetAwaiter().GetResult();

            var editSession = os.CreateEditSession();
            var guest = order.Guests.FirstOrDefault();
            if (guest != null)
                editSession.RenameOrderGuest(guest.Id, customerData.userData.name, order);
            editSession.AddOrderExternalData(Constants.ExternalDataKeyCustomerNumber, customerData.userData.phone, true, order);
            editSession.AddOrderExternalData(Constants.ExternalDataKeyCustomerBalance, customerData.userWallets.FirstOrDefault().balance.ToString("F2"), true, order);
            if (customerDataNew is null)
                PluginContext.Log.Error($"[{nameof(BarcodeScannerService)}|{nameof(AddCustomerToOrder)}] Something wrong");

            os.SubmitChanges(editSession, os.GetDefaultCredentials());
            var credits = os.GetDefaultCredentials();
            var client = os.TryGetClientByPhone(customerData.userData.phone, credits);
            if (client is null)
            {
                client = CreateIikoClient(os, credits, customerData);
                if (client is null)
                    PluginContext.Log.Error($"[{nameof(CustomerService)}|{nameof(AddCustomerToOrder)}] Something wrong you need try manual");
            }
            if (client != null)
            {
                if (!os.AddClientToOrder(order, client, credits))
                    PluginContext.Log.Error($"[{nameof(CustomerService)}|{nameof(AddCustomerToOrder)}] IIKO client not added {customerData.userData.lastName} {customerData.userData.name}");
            }
            PluginContext.Log.Info($"[{nameof(BarcodeScannerService)}|{nameof(AddCustomerToOrder)}] Add client {customerData.userData.lastName} {customerData.userData.name} {customerData.userData.phone} to order {order.Id} {order.Number}");
        }

        private static IClient CreateIikoClient(IOperationService os, ICredentials credentials, CustomerInfo customerData)
        {
            try
            {
                PluginContext.Log.Info($"[{nameof(CustomerService)}|{nameof(CreateIikoClient)}] Trying to create new customer in iiko front {customerData.userData.lastName} {customerData.userData.name} {customerData.userData.phone}");
                return os.CreateClient(Guid.NewGuid(), customerData.userData.name, new List<PhoneDto> { new PhoneDto { IsMain = true, PhoneValue = customerData.userData.phone } }, null, DateTime.Now, credentials);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(CustomerService)}|{nameof(CreateIikoClient)}] Get error create customer in iiko front {customerData.userData.lastName} {customerData.userData.name} {customerData.userData.phone}. Error {ex}");
                return null;
            }
        }
    }
}
