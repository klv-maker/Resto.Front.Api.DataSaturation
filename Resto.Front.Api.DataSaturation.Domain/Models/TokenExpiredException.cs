using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.Domain.Models
{
    public class TokenExpiredException : Exception
    {
        public string exception { get; set; }
        public string message { get; set; }
    }
}
