using Contracts.Salesforce.InternalAccountChanged;
using Contracts.Salesforce.InternalAccountChanged.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Observability.Shared.Contracts;
using Observability.Shared.Helpers;
using SalesforceAccountChangedPublisher.Infrastructure.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceAccountChangedPublisher.Host
{
    public class SalesforceCdcListener 
    {
    }
       
}
