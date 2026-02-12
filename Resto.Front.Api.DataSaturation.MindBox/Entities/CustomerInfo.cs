namespace Resto.Front.Api.DataSaturation.MindBox.Entities
{
    public class CustomerInfo
    {
        public string status { get; set; }
        public Customer customer { get; set; }
    }

    public class Customer
    {
        public string firstName { get; set; }
        public string middleName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public bool isEmailInvalid { get; set; }
        public long mobilePhone { get; set; }
        public bool isMobilePhoneInvalid { get; set; }
        public bool isMobilePhoneConfirmed { get; set; }
        public long pendingMobilePhone { get; set; }
        public string birthDate { get; set; }
        public string sex { get; set; }
        public string changeDateTimeUtc { get; set; }
        public string ianaTimeZone { get; set; }
        public string timeZoneSource { get; set; }
        public string processingStatus { get; set; }
        public Ids ids { get; set; }
        public Area area { get; set; }
        public Subscription[] subscriptions { get; set; }
    }

    public class Ids
    {
        public int mindboxId { get; set; }
    }

    public class Area
    {
        public string name { get; set; }
        public IdsExternal ids { get; set; }
    }

    public class IdsExternal
    {
        public string externalId { get; set; }
    }

    public class Subscription
    {
        public string pointOfContact { get; set; }
        public string topic { get; set; }
        public bool isSubscribed { get; set; }
    }

}
