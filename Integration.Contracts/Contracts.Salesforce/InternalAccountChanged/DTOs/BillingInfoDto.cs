using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Salesforce.InternalAccountChanged.DTOs
{
    public class BillingInfoDto
    {
        public string VatId { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentTerms { get; set; } = string.Empty;
    }
}
