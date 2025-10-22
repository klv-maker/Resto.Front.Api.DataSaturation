using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.Helpers
{
    public class JsonRPC
    {
        public class JsonRpcRequest
        {
            public string jsonrpc { get; set; } = "2.0";
            public string method { get; set; }
            public object[] @params { get; set; }
            public Guid id { get; set; }
        }

        public class JsonRpcClient
        {
            private readonly HttpClient _httpClient;
            private readonly string _rpcUrl;

            public JsonRpcClient(string rpcUrl)
            {
                _httpClient = new HttpClient();
                _rpcUrl = rpcUrl;
            }


            public async Task<string> SendRequestAsync(string method, CancellationToken cancellationToken, object[] parametrs)
            {
                var request = new JsonRpcRequest
                {
                    method = method,
                    @params = parametrs,
                    id = Guid.NewGuid() 
                };

                string jsonRequest = request.SerializeToJson();

                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_rpcUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode(); 

                return await response.Content.ReadAsStringAsync();
            }

            public async Task<string> SendRequestAsync(string method, CancellationToken cancellationToken)
            {
                return await SendRequestAsync(method, cancellationToken, new object[0]);
            }

            public async Task<string> SendRequestAsync<T>(string method, CancellationToken cancellationToken, T parameters)
            {
                return await SendRequestAsync(method, cancellationToken, new object[] { parameters });
            }
        }
    }
}
