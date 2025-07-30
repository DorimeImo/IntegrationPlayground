using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Salesforce.InternalAccountChanged.DTOs
{
    public class BillingAddressDto
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string InvoiceEmail { get; set; } = string.Empty;
    }
}
