using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.Domain.Models
{
    public class TokenExpiredException : Exception
    {
        public string code { get; set; }
        public string errorCode { get; set; }
        public string message { get; set; }
        public string description { get; set; }
        public int httpStatusCode { get; set; }
        public object uiMessage { get; set; }
        public bool isIntegrationError { get; set; }
    }
}
