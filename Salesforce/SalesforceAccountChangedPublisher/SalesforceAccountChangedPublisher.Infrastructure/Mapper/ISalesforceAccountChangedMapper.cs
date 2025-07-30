using Contracts.Salesforce.InternalAccountChanged;
using SalesforceAccountChangedPublisher.Infrastructure.Mapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceAccountChangedPublisher.Infrastructure.Salesforce
{
    public interface ISalesforceAccountChangedMapper
    {
        InternalAccountChangedMessage? Map(string json);
    }
}
