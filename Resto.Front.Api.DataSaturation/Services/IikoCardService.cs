using Resto.Front.Api.DataSaturation.Domain.Entities;
using Resto.Front.Api.DataSaturation.Domain.Helpers;
using Resto.Front.Api.DataSaturation.Domain.Models;
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
        private HttpClient localClient;
        private HttpClient iikoCardClient;
        private bool isDisposed;
        private string user;
        private string password;
        private string userLocal;
        private string passwordLocal;
        private string address;
        private Guid organization;
        private string token;
        private string tokenLocal;
        public IikoCardService(IikoCard iikoCard) 
        {
            user = iikoCard.User;
            password = iikoCard.Secret;
            organization = iikoCard.Organization;
            address = iikoCard.Address;
            userLocal = iikoCard.UserEncoded;
            passwordLocal = iikoCard.SecretEncoded;
            CreateLocalHttpClient();
            CreateIikoCardHttpClient();
        }

        private void CreateLocalHttpClient()
        {
            localClient = new HttpClient();
            Uri baseUri = new Uri("http://localhost:7001");
            localClient.BaseAddress = baseUri;
            localClient.DefaultRequestHeaders.Clear();
            localClient.DefaultRequestHeaders.ConnectionClose = true;
            localClient.Timeout = TimeSpan.FromSeconds(30);
        }

        private void CreateIikoCardHttpClient()
        {
            if (string.IsNullOrWhiteSpace(address))
                return;

            iikoCardClient = new HttpClient();
            Uri baseUri = new Uri(address);
            iikoCardClient.BaseAddress = baseUri;
            iikoCardClient.DefaultRequestHeaders.Clear();
            iikoCardClient.DefaultRequestHeaders.ConnectionClose = true;
            iikoCardClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public void UpdateSettings(IikoCard iikoCard)
        {
            user = iikoCard.User;
            password = iikoCard.Secret;
            organization = iikoCard.Organization;
            address = iikoCard.Address;
            userLocal = iikoCard.UserEncoded;
            passwordLocal = iikoCard.SecretEncoded;
            localClient?.CancelPendingRequests();
            localClient?.Dispose();
            localClient = null;
            CreateLocalHttpClient();
            CreateIikoCardHttpClient();
        }

        private async Task AuthIikoCardApi(CancellationToken cancellationToken)
        {
            if (iikoCardClient is null)
                return;

            token = null;
            var result = await iikoCardClient.ExecuteGetRequestAsync($"/api/0/auth/access_token?user_id={user}&user_secret={password}", cancellationToken);
            if (string.IsNullOrWhiteSpace(result))
            {
                token = null;
                PluginContext.Log.Error($"[{nameof(IikoCardService)}|{nameof(AuthLocalClient)}] Can't get auth result");
            }
            else
                token = result.Trim('\"');
        }

        private async Task ChangeTokenIfNeed(CancellationToken cancellationToken)
        {
            if (iikoCardClient is null)
                return;

            if (string.IsNullOrWhiteSpace(token))
            {
                await AuthIikoCardApi(cancellationToken);
                return;
            }

            var result = await iikoCardClient.ExecuteGetRequestAsync($"/api/0/auth/echo?msg=check&access_token={token}", cancellationToken);
            if (string.IsNullOrWhiteSpace(result))
            {
                await AuthIikoCardApi(cancellationToken);
                return;
            }

            if (!string.Equals(result, Constants.WrongToken, StringComparison.OrdinalIgnoreCase))
                return;

            await AuthIikoCardApi(cancellationToken);
        }

        public async Task<OrganizationGuestInfo> GetIikoCardCustomerAsync(string customerId, CancellationToken cancellationToken)
        {
            if (iikoCardClient is null)
                return null;

            await ChangeTokenIfNeed(cancellationToken);
            var result = await iikoCardClient.ExecuteGetRequestAsync($"/api/0/customers/get_customer_by_id?access_token={token}&organization={organization}&id={customerId}", cancellationToken);
            if (string.IsNullOrWhiteSpace(result))
            {
                PluginContext.Log.Error($"[{nameof(IikoCardService)}|{nameof(GetCustomerAsync)}] Something wrong");
                return null;
            }
            return SerializeHelper.DeserializeFromJson<OrganizationGuestInfo>(result);
        }

        private async Task AuthLocalClient(CancellationToken cancellationToken)
        {
            if (localClient is null)
                return;

            localClient.DefaultRequestHeaders.Clear();
            tokenLocal = null;
            var result = await localClient.ExecuteGetRequestAsync($"/api/v2/getAccessToken?userName={userLocal}&password={passwordLocal}", cancellationToken);
            if (string.IsNullOrWhiteSpace(result))
            {
                tokenLocal = null;
                PluginContext.Log.Error($"[{nameof(IikoCardService)}|{nameof(AuthLocalClient)}] Can't get auth result");
                return;
            }

            tokenLocal = result.Trim('\"');
            localClient.DefaultRequestHeaders.Add("AccessToken", tokenLocal);
        }

        public async Task<CustomerInfo> GetCustomerAsync(string customerNumber, CancellationToken cancellationToken)
        {
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    if (localClient is null)
                        return null;
                    if (string.IsNullOrWhiteSpace(tokenLocal))
                        await AuthLocalClient(cancellationToken);

                    if (string.IsNullOrWhiteSpace(tokenLocal))
                        return null;

                    var request = new 
                    {
                        request = new
                        {
                            authData = new 
                            {
                                credential = customerNumber, 
                                searchScope = (int)SearchScopeIikoCard.Phone
                            }
                        }
                    };
                    return await localClient.ExecutePostRequestAsync<CustomerInfo>($"/api/v2/guests/userWallets", request, cancellationToken);
                }
                catch (TokenExpiredException ex)
                {
                    PluginContext.Log.Error($"[{nameof(IikoCardService)}|{nameof(GetCustomerAsync)}] Get token expired ex {ex.message}. Get new token");
                    await AuthLocalClient(cancellationToken);
                }
            }
            return null;
        }

        public async Task<CustomerAddData> AddCustomerToOrder(string customerNumber, Guid orderId, CancellationToken cancellationToken)
        {
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    if (localClient is null)
                        return null;
                    if (string.IsNullOrWhiteSpace(tokenLocal))
                        await AuthLocalClient(cancellationToken);

                    if (string.IsNullOrWhiteSpace(tokenLocal))
                        return null;

                    var request = new
                    {
                        request = new
                        {
                            authData = new
                            {
                                credential = customerNumber,
                                searchScope = (int)SearchScopeIikoCard.Phone
                            },
                            orderId = orderId
                        }
                    };
                    return await localClient.ExecutePostRequestAsync<CustomerAddData>($"/api/v2/authorize", request, cancellationToken);
                }
                catch (TokenExpiredException ex)
                {
                    PluginContext.Log.Error($"[{nameof(IikoCardService)}|{nameof(GetCustomerAsync)}] Get token expired ex {ex.message}. Get new token");
                    await AuthLocalClient(cancellationToken);
                }
            }
            return null;
        }

        public void Dispose()
        {
            if (isDisposed) 
                return;

            localClient?.Dispose();
            isDisposed = true;
        }
    }
}
