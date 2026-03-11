using Resto.Front.Api.DataSaturation.Domain.Helpers;
using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

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
            var settings = new Settings()
            {
                AdressesApi = new List<string>() { baseServerUrl },
                SwitchMediaTime = 60,
                EnableOrdersService = false,
                DataQR = "",
                IikoCard = new IikoCard()
            };
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

        public void Update(List<string> addresses, int switchMediaTime, bool enableOrdersService, string dataQR, IikoCard iikoCard)
        {
            PluginContext.Log.Info($"Start update settings with values {string.Join(",", addresses)}");
            this.AdressesApi = addresses;
            this.SwitchMediaTime = switchMediaTime;
            this.EnableOrdersService = enableOrdersService;
            this.DataQR = dataQR == null ? "" : dataQR;
            this.IikoCard = iikoCard;
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

        private bool enableOrdersServiceField;
        private string dataQRField;
        private IikoCard iikoCardField = new IikoCard();

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

        public bool EnableOrdersService
        {
            get
            {
                return this.enableOrdersServiceField;
            }
            set
            {
                this.enableOrdersServiceField = value;
            }
        }

        public string DataQR
        {
            get
            {
                return this.dataQRField;
            }
            set
            {
                this.dataQRField = value;
            }
        }

        public IikoCard IikoCard
        {
            get
            {
                return iikoCardField;
            }
            set
            {
                iikoCardField = value;
            }
        }
    }

    // Примечание. Для запуска созданного кода может потребоваться NET Framework версии 4.5 или более поздней версии и .NET Core или Standard версии 2.0 или более поздней.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class IikoCard
    {

        private string addressField;

        private string userField;

        private string secretField;

        private Guid organizationField;

        private string userLocalField;

        private string userEncoded;

        private string secretLocalField;

        private string secretEncoded;

        /// <remarks/>
        public string Address
        {
            get
            {
                return this.addressField;
            }
            set
            {
                this.addressField = value;
            }
        }

        /// <remarks/>
        public string User
        {
            get
            {
                return this.userField;
            }
            set
            {
                this.userField = value;
            }
        }

        /// <remarks/>
        public string Secret
        {
            get
            {
                return this.secretField;
            }
            set
            {
                this.secretField = value;
            }
        }

        public Guid Organization
        {
            get
            {
                return this.organizationField;
            }
            set
            {
                this.organizationField = value;
            }
        }

        public string UserLocal
        {
            get
            {
                return this.userLocalField;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(userEncoded))
                    userEncoded = string.Empty;

                this.userLocalField = value;
            }
        }

        [XmlIgnore]
        public string UserEncoded
        {
            get
            {
                if (string.IsNullOrWhiteSpace(userEncoded))
                {
                    userEncoded = EncodeHelper.DecodeAndDecrypt(this.userLocalField);
                }
                return userEncoded;
            }
            set
            {
                UserLocal = EncodeHelper.EncryptAndEncode(value);
            }
        }

        public string SecretLocal
        {
            get
            {
                return this.secretLocalField;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(secretEncoded))
                    secretEncoded = string.Empty;

                this.secretLocalField = value;
            }
        }

        [XmlIgnore]
        public string SecretEncoded
        {
            get
            {
                if (string.IsNullOrWhiteSpace(secretEncoded))
                {
                    secretEncoded = EncodeHelper.DecodeAndDecrypt(this.secretLocalField);
                }
                return secretEncoded;
            }
            set
            {
                SecretLocal = EncodeHelper.EncryptAndEncode(value);
            }
        }
    }
}
