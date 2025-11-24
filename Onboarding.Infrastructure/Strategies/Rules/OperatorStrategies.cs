using Onboarding.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Onboarding.Infrastructure.Strategies.Rules
{
    // Estrategia para IGUALDAD (==)
    public class EqualsStrategy : IOperatorStrategy
    {
        public string Symbol => "==";
        public bool Evaluate(string left, string right) => left?.Trim() == right?.Trim();
    }

    // Estrategia para DIFERENCIA (!=)
    public class NotEqualsStrategy : IOperatorStrategy
    {
        public string Symbol => "!=";
        public bool Evaluate(string left, string right) => left?.Trim() != right?.Trim();
    }

    // Estrategia para MAYOR QUE (>) - Maneja números
    public class GreaterThanStrategy : IOperatorStrategy
    {
        public string Symbol => ">";
        public bool Evaluate(string left, string right)
        {
            if (double.TryParse(left, out double l) && double.TryParse(right, out double r))
                return l > r;
            return false; 
        }
    }

    // Estrategia para MAYOR O IGUAL (>=)
    public class GreaterOrEqualStrategy : IOperatorStrategy
    {
        public string Symbol => ">=";
        public bool Evaluate(string left, string right)
        {
            if (double.TryParse(left, out double l) && double.TryParse(right, out double r))
                return l >= r;
            return false;
        }
    }

    public class ContainsStrategy : IOperatorStrategy
    {
        public string Symbol => "CONTAINS";
        public bool Evaluate(string left, string right)
            => left != null && right != null && left.Contains(right);
    }
}
