using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces;
using System.IO;
using System.Reflection;

namespace Resto.Front.Api.DataSaturation.Settings
{
    public partial class Settings
    {
        private static ISettings instance;
        private Settings() { }
        /// <summary>
        /// instance for work with settings
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static ISettings Instance()
        {
            if (instance == null)
            {
                var settingsFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(typeof(Settings)).Location), "Settings", "Settings.xml");
                if (File.Exists(settingsFilePath))
                {
                    var settingsXml = File.ReadAllText(settingsFilePath);
                    PluginContext.Log.Info(settingsXml);
                    instance = SerializeHelper.DeserializeFromXml<Settings>(settingsXml);
                }
                else
                {
                    PluginContext.Log.Error($"File settings not found in path {settingsFilePath}");
                    throw new FileNotFoundException(settingsFilePath);
                }
            }
            return instance;
        }
    }

    // Примечание. Для запуска созданного кода может потребоваться NET Framework версии 4.5 или более поздней версии и .NET Core или Standard версии 2.0 или более поздней.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class Settings : ISettings
    {

        private SettingsListener listenerField;

        /// <remarks/>
        public SettingsListener Listener
        {
            get
            {
                return this.listenerField;
            }
            set
            {
                this.listenerField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SettingsListener
    {

        private string addressApiField;

        private string portField;

        /// <remarks/>
        public string AddressApi
        {
            get
            {
                return this.addressApiField;
            }
            set
            {
                this.addressApiField = value;
            }
        }

        /// <remarks/>
        public string Port
        {
            get
            {
                return this.portField;
            }
            set
            {
                this.portField = value;
            }
        }
    }


}
