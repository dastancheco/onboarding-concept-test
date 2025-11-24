using System;
using System.Collections.Generic;
using System.Text;

namespace Onboarding.Core.Interfaces
{
    public interface IActionExecutor
    {
        Task ExecuteActionAsync(string actionKey, Guid prospectId, string payloadJson);
    }
}
