using Newtonsoft.Json;
using Resto.Front.Api.DataSaturation.Settings;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Resto.Front.Api.DataSaturation.Helpers
{
    public static class SerializeHelper
    {
        public static string SerializeToXml<T>(this T data) where T : class
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            using (var sw = new StringWriter())
            using (var writer = XmlWriter.Create(sw))
            {
                ser.Serialize(writer, data);
                return sw.ToString();
            }
        }

        public static void SerializeToFileXml<T>(this T data, string path) where T : class
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            using (Stream stream = new FileStream(path, FileMode.Create))
            {
                ser.Serialize(stream, data);
            }
        }

        public static T DeserializeFromXml<T>(this string data) where T : class
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            using (TextReader reader = new StringReader(data))
            {
                return (T)ser.Deserialize(reader);
            }
        }

        public static T DeserializeFromXmlFileStream<T>(string path) where T : class
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            using (var stream = new FileStream(path, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                return (T)ser.Deserialize(reader);
            }
        }

        public static string SerializeToJson<T>(this T data) where T : class 
        {
            return JsonConvert.SerializeObject(data);
        }

        public static T DeserializeFromJson<T>(this string data) where T : class
        {
            return JsonConvert.DeserializeObject<T>(data);
        }
    }
}
