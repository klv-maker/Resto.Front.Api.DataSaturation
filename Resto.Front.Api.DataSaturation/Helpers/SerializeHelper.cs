using Newtonsoft.Json;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Resto.Front.Api.DataSaturation.Helpers
{
    public class SerializeHelper
    {
        public static string SerializeToXml<T>(T data) where T : class
        {
            using (var sw = new StringWriter())
            using (var writer = XmlWriter.Create(sw))
            {
                new XmlSerializer(typeof(T)).Serialize(writer, data);
                return sw.ToString();
            }
        }

        public static T DeserializeFromXml<T>(string data) where T : class
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            using (TextReader reader = new StringReader(data))
            {
                return (T)ser.Deserialize(reader);
            }
        }

        public static string SerializeToJson<T>(T data) where T : class 
        {
            return JsonConvert.SerializeObject(data);
        }
    }
}
