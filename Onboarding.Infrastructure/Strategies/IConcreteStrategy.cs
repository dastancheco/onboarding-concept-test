using System;
using System.Collections.Generic;
using System.Text;

namespace Onboarding.Infrastructure.Strategies
{
    public interface IConcreteStrategy
    {
        Task ExecuteAsync(Guid prospectId, string configJson, string payloadJson);
    }
}
