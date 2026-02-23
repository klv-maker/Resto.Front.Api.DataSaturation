using System;
using System.Collections.Generic;

namespace Resto.Front.Api.DataSaturation.Domain.Models
{
    public class CustomerInfo
    {
        public CustomerData userData { get; set; }
        public List<CustomerWallet> userWallets { get; set; } = new List<CustomerWallet>();
    }

    public class CustomerWallet
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public bool isInteger { get; set; }
        public decimal balance { get; set; }
    }

    public class CustomerData
    {
        public string name { get; set; }
        public string lastName { get; set; }
        public string phone { get; set; }
        public DateTime? birthday { get; set; } = null;
        public Sex sex { get; set; }
    }


    public class CustomerAddData
    {
        public object cardNumber { get; set; }
        public object cardTrack { get; set; }
        public CustomerAddDataInfo userData { get; set; }
    }

    public class CustomerAddDataInfo
    {
        public object birthday { get; set; }
        public string fullName { get; set; }
        public string lastName { get; set; }
        public string name { get; set; }
        public string phone { get; set; }
        public Guid? referrer { get; set; }
        public Sex sex { get; set; }
    }

}
