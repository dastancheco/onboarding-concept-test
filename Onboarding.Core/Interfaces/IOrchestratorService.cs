using System;
using System.Collections.Generic;
using System.Text;

namespace Onboarding.Core.Interfaces
{
    public interface IOrchestratorService
    {
        Task ProcessEventAsync(string eventType, string payloadJson);
    }
}
