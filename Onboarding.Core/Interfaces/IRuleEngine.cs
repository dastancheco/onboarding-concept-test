namespace Onboarding.Core.Interfaces
{
    /// <summary>
    /// Interfaz para el motor de evaluación de reglas de negocio.
    /// Aplica DIP: Dependency Inversion Principle - Los consumidores dependen de la abstracción.
    /// Aplica OCP: Open/Closed Principle - Extensible sin modificar código existente.
    /// </summary>
    public interface IRuleEngine
    {
        /// <summary>
        /// Evalúa una expresión de regla contra datos JSON.
        /// Soporta operadores lógicos: AND (&&), OR (||)
        /// Soporta operadores de comparación: ==, !=, >, <, >=, <=, CONTAINS
        /// </summary>
        /// <param name="expression">Expresión de la regla (ej: "data.status == 'complete' && data.score > 80")</param>
        /// <param name="jsonData">Datos JSON contra los cuales evaluar</param>
        /// <returns>True si la expresión se cumple, False en caso contrario</returns>
        bool Evaluate(string expression, string jsonData);
    }
}
