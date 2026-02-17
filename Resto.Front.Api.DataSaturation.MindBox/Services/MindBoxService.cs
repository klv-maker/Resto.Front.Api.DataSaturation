using Resto.Front.Api.DataSaturation.Domain.Helpers;
using Resto.Front.Api.DataSaturation.MindBox.Entities;
using Resto.Front.Api.DataSaturation.MindBox.Interfaces;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.MindBox.Services
{
    public class MindBoxService : IMindBoxService
    {
        private readonly HttpClient client;
        public MindBoxService(IMindBoxSettings mindBoxSettings) 
        {
            client = new HttpClient();
            Uri baseUri = new Uri(mindBoxSettings.AddressApi);
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.ConnectionClose = true;
            client.DefaultRequestHeaders.Add("Authorization", $"SecretKey {mindBoxSettings.Key}");
            client.Timeout = TimeSpan.FromSeconds(30);
        }        

        public async Task<CustomerInfo> CheckCustomer(string mobilePhone, CancellationToken cancellationToken)
        {
            var request = new
            {
                customer = new
                {
                    mobilePhone
                }
            };
            return await client.ExecutePostRequestAsync<CustomerInfo>("operation=Offline.CheckCustomer", request, cancellationToken);
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
