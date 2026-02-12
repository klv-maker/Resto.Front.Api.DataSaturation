using Newtonsoft.Json;
using Resto.Front.Api.DataSaturation.MindBox.Entities;
using Resto.Front.Api.DataSaturation.MindBox.Interfaces;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.MindBox.Services
{
    public class MindBoxService : IMindBoxService
    {
        private readonly HttpClient client;
        public MindBoxService(IMindBoxSettings mindBoxSettings) 
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.s.mindbox.ru/v3/operations/sync?endpointId=coffee-like.Offline&operation=Offline.CheckCustomer");
            client = new HttpClient();
            Uri baseUri = new Uri(mindBoxSettings.AddressApi);
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.ConnectionClose = true;
            client.DefaultRequestHeaders.Add("Authorization", $"SecretKey {mindBoxSettings.Key}");
            client.Timeout = TimeSpan.FromSeconds(30);
        }

        private async Task<T> ExecutePostRequestAsync<T>(string endpoint, object requestData, CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    PluginContext.Log.Info($"[{nameof(MindBoxService)}|{nameof(ExecutePostRequestAsync)}] start get data {endpoint} for {requestData}");
                    // Проверяем отмену в начале каждой попытки
                    cancellationToken.ThrowIfCancellationRequested();

                    var jsonContent = CreateJsonContent(requestData);
                    var response = await client.PostAsync(endpoint, jsonContent, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    PluginContext.Log.Info($"Response from {endpoint} attempt {attempt + 1}: {jsonResponse}");

                    return JsonConvert.DeserializeObject<T>(jsonResponse);
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    // Это не запрошенная нами отмена, вероятно таймаут HttpClient, поэтому логируем и повторяем
                    PluginContext.Log.Error($"Timeout in {endpoint} attempt {attempt + 1}: {ex}");
                    if (attempt < 4)
                        await Task.Delay(1000 * (attempt + 1), cancellationToken);
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    PluginContext.Log.Error($"HTTP error in {endpoint} attempt {attempt + 1}: {ex}");
                    if (attempt < 4)
                        await Task.Delay(1000 * (attempt + 1), cancellationToken);
                    continue;
                }
                catch (Exception ex)
                {
                    PluginContext.Log.Error($"Error in {endpoint} attempt {attempt + 1}: {ex}");
                    if (attempt < 4)
                        await Task.Delay(1000 * (attempt + 1), cancellationToken);
                    continue;
                }
            }
            return default;
        }

        private async Task<string> ExecutePostRequestAsync(string endpoint, object requestData, CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    PluginContext.Log.Info($"[{nameof(MindBoxService)}|{nameof(ExecutePostRequestAsync)}] start get data {endpoint} for {requestData}");
                    // Проверяем отмену в начале каждой попытки
                    cancellationToken.ThrowIfCancellationRequested();

                    var jsonContent = CreateJsonContent(requestData);
                    var response = await client.PostAsync(endpoint, jsonContent, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var result = await response.Content.ReadAsStringAsync();
                    PluginContext.Log.Info($"Response from {endpoint} attempt {attempt + 1}: {result}");

                    return result;
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    // Это не запрошенная нами отмена, вероятно таймаут HttpClient, поэтому логируем и повторяем
                    PluginContext.Log.Error($"Timeout in {endpoint} attempt {attempt + 1}: {ex}");
                    if (attempt < 4)
                        await Task.Delay(1000 * (attempt + 1), cancellationToken);
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    PluginContext.Log.Error($"HTTP error in {endpoint} attempt {attempt + 1}: {ex}");
                    if (attempt < 4)
                        await Task.Delay(1000 * (attempt + 1), cancellationToken);
                    continue;
                }
                catch (Exception ex)
                {
                    PluginContext.Log.Error($"Error in {endpoint} attempt {attempt + 1}: {ex}");
                    if (attempt < 4)
                        await Task.Delay(1000 * (attempt + 1), cancellationToken);
                    continue;
                }
            }
            return null;
        }

        private StringContent CreateJsonContent(object data)
        {
            var json = JsonConvert.SerializeObject(data);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        public async Task<CustomerInfo> CheckCustomer(string pointOfContact, string mobilePhone, CancellationToken cancellationToken)
        {
            var request = new
            {
                pointOfContact,
                customer = new
                {
                    mobilePhone
                }
            };
            return await ExecutePostRequestAsync<CustomerInfo>("operation=Offline.CheckCustomer", request, cancellationToken);
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
