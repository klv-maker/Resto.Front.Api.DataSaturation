using Resto.Front.Api.DataSaturation.Domain.Helpers;
using Resto.Front.Api.DataSaturation.MindBox.Interfaces;
using System;
using System.IO;

namespace Resto.Front.Api.DataSaturation.Settings
{
    public partial class MindBoxSettings
    {
        private static IMindBoxSettings instance;
        private static string ConfigFileName = "MindBoxSettings.xml";
        private static string FilePath
        {
            get { return Path.GetFullPath(Path.Combine(PluginContext.Integration.GetConfigsDirectoryPath(), ConfigFileName)); }
        }

        private MindBoxSettings() { }
        /// <summary>
        /// instance for work with settings
        /// </summary>
        public static IMindBoxSettings Instance()
        {
            if (instance == null)
            {
                var settingsFilePath = FilePath;
                if (File.Exists(settingsFilePath))
                {
                    var settingsXml = File.ReadAllText(settingsFilePath);
                    instance = SerializeHelper.DeserializeFromXml<MindBoxSettings>(settingsXml);
                }
                else
                    CreateSettingsIfNotExists();
            }
            return instance;
        }

        private static void CreateSettingsIfNotExists()
        {
            var settings = new MindBoxSettings()
            {
                AddressApi = string.Empty,
                Key = string.Empty
            };
            settings.Save();
            instance = settings;
        }

        private void Save()
        {
            try
            {
                PluginContext.Log.InfoFormat("Saving mind box config to {0}", FilePath);
                this.SerializeToFileXml(FilePath);
                instance = this;
            }
            catch (Exception e)
            {
                PluginContext.Log.Error("Failed to save mind box config.", e);
            }
        }

        public void Update(string address, string key)
        {
            this.AddressApi = address;
            this.Key = key;
            Save();
        }
    }

    // Примечание. Для запуска созданного кода может потребоваться NET Framework версии 4.5 или более поздней версии и .NET Core или Standard версии 2.0 или более поздней.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class MindBoxSettings : IMindBoxSettings
    {
        private string addressApiField;

        private string keyField;

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
        public string Key
        {
            get
            {
                return this.keyField;
            }
            set
            {
                this.keyField = value;
            }
        }
    }
}
