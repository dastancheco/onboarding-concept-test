using Onboarding.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Onboarding.Core.Interfaces
{
    public interface IValidationPipeline
    {
        List<ValidationError> ValidateField(
            string fieldKey,
            string? value,
            string dataType,
            Dictionary<string, object> config);
    }
}
