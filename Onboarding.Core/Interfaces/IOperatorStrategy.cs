using System;
using System.Collections.Generic;
using System.Text;

namespace Onboarding.Core.Interfaces
{
    public interface IOperatorStrategy
    {
        // El símbolo que identifica la operación (Ej. "==", ">", "CONTAINS")
        string Symbol { get; }

        // La lógica de evaluación
        bool Evaluate(string leftValue, string rightValue);
    }
}
