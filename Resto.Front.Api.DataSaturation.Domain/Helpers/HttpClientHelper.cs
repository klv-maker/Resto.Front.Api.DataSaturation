using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.Domain.Helpers
{
    public static class HttpClientHelper
    {
        private const int MaxRetryAttempts = 5;

        public static async Task<string> ExecuteGetRequestAsync(this HttpClient client, string endpoint, CancellationToken cancellationToken)
        {
            return await ExecuteWithRetryAsync(
                endpoint,
                async () =>
                {
                    var response = await client.GetAsync(endpoint, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                },
                cancellationToken);
        }

        public static async Task<T> ExecutePostRequestAsync<T>(this HttpClient client, string endpoint, object requestData, CancellationToken cancellationToken) where T : class
        {
            var result = await ExecuteWithRetryAsync(
                endpoint,
                async () =>
                {
                    var jsonContent = CreateJsonContent(requestData);
                    var response = await client.PostAsync(endpoint, jsonContent, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return SerializeHelper.DeserializeFromJson<T>(jsonResponse);
                },
                cancellationToken);

            return result;
        }

        public static async Task<string> ExecutePostRequestAsync(this HttpClient client, string endpoint, object requestData, CancellationToken cancellationToken)
        {
            return await ExecuteWithRetryAsync(
                endpoint,
                async () =>
                {
                    var jsonContent = CreateJsonContent(requestData);
                    var response = await client.PostAsync(endpoint, jsonContent, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    return await response.Content.ReadAsStringAsync();
                },
                cancellationToken);
        }

        private static async Task<T> ExecuteWithRetryAsync<T>(string endpoint, Func<Task<T>> action, CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < MaxRetryAttempts; attempt++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    PluginContext.Log.Info($"[{nameof(HttpClientHelper)}] Starting request to {endpoint}, attempt {attempt + 1}");

                    var result = await action();

                    PluginContext.Log.Info($"Successfully completed request to {endpoint}, attempt {attempt + 1}");
                    return result;
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    PluginContext.Log.Error($"Timeout in {endpoint} attempt {attempt + 1}: {ex}");
                    if (attempt < MaxRetryAttempts - 1)
                        await Task.Delay(GetDelayForAttempt(attempt), cancellationToken);
                }
                catch (HttpRequestException ex)
                {
                    PluginContext.Log.Error($"HTTP error in {endpoint} attempt {attempt + 1}: {ex}");
                    if (attempt < MaxRetryAttempts - 1)
                        await Task.Delay(GetDelayForAttempt(attempt), cancellationToken);
                }
                catch (Exception ex)
                {
                    PluginContext.Log.Error($"Error in {endpoint} attempt {attempt + 1}: {ex}");
                    if (attempt < MaxRetryAttempts - 1)
                        await Task.Delay(GetDelayForAttempt(attempt), cancellationToken);
                }
            }

            PluginContext.Log.Error($"All {MaxRetryAttempts} attempts failed for {endpoint}");
            return default;
        }

        private static TimeSpan GetDelayForAttempt(int attempt)
        {
            return TimeSpan.FromSeconds(attempt + 1);
        }

        private static StringContent CreateJsonContent(object data)
        {
            var json = JsonConvert.SerializeObject(data);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }
}