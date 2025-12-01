using Onboarding.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Onboarding.Core.Services
{
    public class RuleEngine : IRuleEngine
    {
        private readonly Dictionary<string, IOperatorStrategy> _strategies;

        // INYECCIÓN DE DEPENDENCIAS: Recibimos todas las estrategias registradas
        public RuleEngine(IEnumerable<IOperatorStrategy> strategies)
        {
            // Creamos un diccionario para búsqueda rápida por símbolo
            _strategies = strategies.ToDictionary(s => s.Symbol);
        }

        public bool Evaluate(string expression, string jsonData)
        {
            if (string.IsNullOrWhiteSpace(expression) || expression.Trim().ToLower() == "true") return true;

            try
            {
                var jsonNode = JsonNode.Parse(jsonData);
                if (jsonNode == null) return false;

                // 1. Lógica OR (||) recursiva
                var orParts = expression.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in orParts)
                {
                    if (EvaluateAndBlock(part.Trim(), jsonNode)) return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RULE ENGINE ERROR] {ex.Message}");
                return false;
            }
        }

        private bool EvaluateAndBlock(string expression, JsonNode jsonNode)
        {
            // 2. Lógica AND (&&) recursiva
            var andParts = expression.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in andParts)
            {
                if (!EvaluateAtomicCondition(part.Trim(), jsonNode)) return false;
            }
            return true;
        }

        private bool EvaluateAtomicCondition(string condition, JsonNode jsonNode)
        {
            // 3. BÚSQUEDA DE ESTRATEGIA (OCP en acción)
            // Buscamos qué operador está presente en la cadena
            var strategy = _strategies.Values
                .OrderByDescending(s => s.Symbol.Length)
                .FirstOrDefault(s => condition.Contains(s.Symbol));

            if (strategy == null)
            {
                Console.WriteLine($"[WARNING] Operador no soportado en: {condition}");
                return false;
            }

            // Separamos la condición usando el símbolo de la estrategia encontrada
            var parts = condition.Split(new[] { strategy.Symbol }, StringSplitOptions.None);
            if (parts.Length < 2) return false;

            var leftRaw = parts[0].Trim();
            var rightRaw = parts[1].Trim();

            // A. Resolver valor del JSON (Izquierda) - Soportar navegación anidada
            var leftValue = ResolveJsonPath(jsonNode, leftRaw);

            // B. Resolver valor constante (Derecha)
            var rightValue = rightRaw.Replace("'", "").Replace("\"", "");

            // C. DELEGAR EVALUACIÓN
            return strategy.Evaluate(leftValue, rightValue);
        }

        /// <summary>
        /// Resuelve un path JSON que puede estar anidado (ej: "input.app_id", "data.country")
        /// </summary>
        private string? ResolveJsonPath(JsonNode jsonNode, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            // Dividir el path por puntos para navegar la estructura
            var pathParts = path.Split('.');
            JsonNode? currentNode = jsonNode;

            foreach (var part in pathParts)
            {
                if (currentNode == null)
                    return null;

                // Intentar acceder al nodo hijo
                currentNode = currentNode[part];
            }

            return currentNode?.ToString();
        }
    }
}
