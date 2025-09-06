using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.Helpers
{
    public static class Extenstions
    {
        public static string SerializeToJson<T>(this T data) where T : class
        {
            return SerializeHelper.SerializeToJson(data);
        }
    }
}
