using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Resto.Front.Api.DataSaturation.Settings
{
    public partial class Settings
    {
        private static ISettings instance;
        private Settings() { }
        private static string ConfigFileName = "Settings.xml";
        private const string baseServerUrl = "http://192.168.0.227:8080/json.rpc";
        private static string FilePath
        {
            get { return Path.GetFullPath(Path.Combine(PluginContext.Integration.GetConfigsDirectoryPath(), ConfigFileName)); }
        }
        /// <summary>
        /// instance for work with settings
        /// </summary>
        public static ISettings Instance()
        {
            if (instance == null)
            {
                var settingsFilePath = FilePath;
                if (File.Exists(settingsFilePath))
                {
                    var settingsXml = File.ReadAllText(settingsFilePath);
                    PluginContext.Log.Info(settingsXml);
                    instance = SerializeHelper.DeserializeFromXml<Settings>(settingsXml);
                }
                else
                    CreateSettingsIfNotExists();
            }
            return instance;
        }

        private static void CreateSettingsIfNotExists()
        {
            var settings = new Settings();
            settings.AdressesApi = new List<string>() { baseServerUrl };
            settings.Save();
            instance = settings;
        }

        private void Save()
        {
            try
            {
                PluginContext.Log.InfoFormat("Saving config to {0}", FilePath);
                this.SerializeToFileXml(FilePath);
                instance = this;
            }
            catch (Exception e)
            {
                PluginContext.Log.Error("Failed to save config.", e);
            }
        }

        public void Update(List<string> addresses, int switchMediaTime)
        {
            PluginContext.Log.Info($"Start update settings with values {string.Join(",", addresses)}");
            this.AdressesApi = addresses;
            this.SwitchMediaTime = switchMediaTime;
            Save();
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

        private List<string> adressesApiField;

        private int switchMediaTimeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Address", IsNullable = false)]
        public List<string> AdressesApi
        {
            get
            {
                return this.adressesApiField;
            }
            set
            {
                this.adressesApiField = value;
            }
        }

        /// <remarks/>
        public int SwitchMediaTime
        {
            get
            {
                return this.switchMediaTimeField;
            }
            set
            {
                this.switchMediaTimeField = value;
            }
        }
    }


}
