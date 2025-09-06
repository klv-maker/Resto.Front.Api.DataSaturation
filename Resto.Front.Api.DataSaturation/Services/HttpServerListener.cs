using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces;
using Resto.Front.Api.DataSaturation.Settings;
using System;
using System.Net;
using System.Text;

namespace Resto.Front.Api.DataSaturation.Services
{
    /// <summary>
    /// класс для доступа к плагину из вне по адресу в веб
    /// </summary>
    public class HttpServerListener : IHttpServerListener
    {
        private readonly IProductsService productService;
        private readonly HttpListener httpListener;
        public HttpServerListener(SettingsListener settingsListener, IProductsService productsService) 
        {
            this.productService = productsService;
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"{settingsListener.AddressApi}:{settingsListener.Port}/");
            httpListener.Start();
            httpListener.BeginGetContext(ProcessRequest, httpListener);
        }

        private void ProcessRequest(IAsyncResult ar)
        {
            //если остановили получение для формирования ответа, надо будет перезапустить
            bool isStoppedListeninig = false;
            try
            {
                HttpListener listener = (HttpListener)ar.AsyncState;
                HttpListenerContext context = listener.EndGetContext(ar);
                isStoppedListeninig = true;
                PluginContext.Log.Info($"[{nameof(HttpServerListener)}|{nameof(ProcessRequest)}] Request path = {context.Request.Url.AbsolutePath}");
                var stringarr = context.Request.Url.AbsolutePath.Split('/');
                PluginContext.Log.Info($"[{nameof(HttpServerListener)}|{nameof(ProcessRequest)}] Path length = {stringarr.Length}");
                if (!context.Request.HttpMethod.Equals("get", StringComparison.CurrentCultureIgnoreCase) || stringarr.Length > 2)
                {
                    PluginContext.Log.Error(@"Invalid request");
                    SendResponse(context, @"Invalid request parameter", HttpStatusCode.BadRequest);
                    return;
                }
                switch (stringarr[1])
                {
                    case Constants.ProductRequest:
                        SendResponse(context, productService.GetProductsChangedInJson(), HttpStatusCode.OK);
                        break;
                }
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(HttpServerListener)}|{nameof(ProcessRequest)}] Get error while process request {ex}");
            }
            finally
            {
                if (isStoppedListeninig)
                    httpListener.BeginGetContext(ProcessRequest, httpListener);
            }
        }
        private void SendResponse(HttpListenerContext context, string msg, HttpStatusCode httpStatusCode)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(msg);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = @"application/json";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
            context.Response.StatusCode = (int)httpStatusCode;
            context.Response.Close();
        }

        public void Dispose()
        {
            httpListener?.Close();
        }
    }
}
