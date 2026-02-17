using Resto.Front.Api.DataSaturation.Domain.Entities;
using Resto.Front.Api.DataSaturation.Domain.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces.Services;
using Resto.Front.Api.DataSaturation.Settings;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.Services
{
    public class IikoCardService : IIikoCardService
    {
        private HttpClient client;
        private bool isDisposed;
        private string user;
        private string password;
        private string address;
        private Guid organization;
        private string token;
        public IikoCardService(IikoCard iikoCard) 
        {
            user = iikoCard.User;
            password = iikoCard.Secret;
            organization = iikoCard.Organization;
            address = iikoCard.Address;
            CreateHttpClient();
        }

        private void CreateHttpClient()
        {
            if (string.IsNullOrWhiteSpace(address))
                return;

            client = new HttpClient();
            Uri baseUri = new Uri(address);
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.ConnectionClose = true;
            client.Timeout = TimeSpan.FromSeconds(30);
        }

        public void UpdateSettings(IikoCard iikoCard)
        {
            user = iikoCard.User;
            password = iikoCard.Secret;
            organization = iikoCard.Organization;
            address = iikoCard.Address;
            CreateHttpClient();
        }

        private async void Auth(CancellationToken cancellationToken)
        {
            if (client is null)
                return;

            var result = await client.ExecuteGetRequestAsync($"/api/0/auth/access_token?user_id={user}&user_secret={password}", cancellationToken);
            if (string.IsNullOrWhiteSpace(result))
            {
                token = null;
                PluginContext.Log.Error($"[{nameof(IikoCardService)}|{nameof(Auth)}] Can't get auth result");
            }
            else
                token = result;
        }

        private async void ChangeTokenIfNeed(CancellationToken cancellationToken)
        {
            if (client is null)
                return;

            var result = await client.ExecuteGetRequestAsync($"/api/0/auth/echo?msg=check&access_token={token}", cancellationToken);
            if (string.IsNullOrWhiteSpace(result))
            {
                token = null;
                Auth(cancellationToken);
                return;
            }
            if (!string.Equals(result, Constants.WrongToken, StringComparison.OrdinalIgnoreCase))
                return;

            token = null;
            Auth(cancellationToken);
        }

        public async Task<OrganizationGuestInfo> GetCustomerAsync(string customerId, CancellationToken cancellationToken)
        {
            if (client is null)
                return null;

            ChangeTokenIfNeed(cancellationToken);
            var result = await client.ExecuteGetRequestAsync($"/api/0/customers/get_customer_by_id?access_token={token}&organization={organization}&id={customerId}", cancellationToken);
            if (string.IsNullOrWhiteSpace(result))
            {
                PluginContext.Log.Error($"[{nameof(IikoCardService)}|{nameof(GetCustomerAsync)}] Something wrong");
                return null;
            }
            return SerializeHelper.DeserializeFromJson<OrganizationGuestInfo>(result);
        }

        public void Dispose()
        {
            if (isDisposed) 
                return;

            client?.Dispose();
            isDisposed = true;
        }
    }
}
