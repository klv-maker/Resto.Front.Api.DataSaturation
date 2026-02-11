using Resto.Front.Api.DataSaturation.MindBox.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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
            request.Headers.Add("Authorization", "SecretKey F0gMqZHHaIUPbFgisi2yqcWayMDSO5sa");
            client = new HttpClient();
            //client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.ConnectionClose = true;
            //client.DefaultRequestHeaders.Add("X-Auth-User", _login);
            //client.DefaultRequestHeaders.Add("X-Auth-Pass", _password);
            client.Timeout = TimeSpan.FromSeconds(30);
        }


        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
