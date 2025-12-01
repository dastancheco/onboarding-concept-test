using Onboarding.Core.Interfaces;
using Onboarding.Core.Services;
using System.Text.Json;

namespace Onboarding.Tests.Services
{
    /// <summary>
    /// Pruebas unitarias para el motor de reglas (RuleEngine).
    /// 
    /// Cobertura de pruebas:
    /// - Evaluación de operadores básicos (==, !=, >, >=, CONTAINS)
    /// - Lógica booleana (AND, OR)
    /// - Condiciones compuestas
    /// - Manejo de valores nulos y casos edge
    /// - Evaluación de expresiones inválidas
    /// 
    /// NOTA: Solo se prueban los operadores implementados en el sistema:
    /// - EqualsStrategy (==)
    /// - NotEqualsStrategy (!=)
    /// - GreaterThanStrategy (>
    /// - GreaterOrEqualStrategy (>=)
    /// - ContainsStrategy (CONTAINS)
    /// </summary>
    public class RuleEngineTests
    {
        private readonly RuleEngine _sut;
        private readonly List<IOperatorStrategy> _strategies;

        public RuleEngineTests()
        {
            // Arrange: Configurar estrategias de operadores reales
            _strategies = new List<IOperatorStrategy>
            {
                new Infrastructure.Strategies.Rules.EqualsStrategy(),
                new Infrastructure.Strategies.Rules.NotEqualsStrategy(),
                new Infrastructure.Strategies.Rules.GreaterThanStrategy(),
                new Infrastructure.Strategies.Rules.GreaterOrEqualStrategy(),
                new Infrastructure.Strategies.Rules.ContainsStrategy()
            };

            _sut = new RuleEngine(_strategies);
        }

        #region Equals Operator Tests

        /// <summary>
        /// Verifica que el operador de igualdad funciona correctamente con strings.
        /// </summary>
        [Fact]
        public void Evaluate_EqualsOperator_WithMatchingStrings_ReturnsTrue()
        {
            // Arrange
            var expression = "status == 'active'";  // Sin el prefijo "data."
            var jsonData = JsonSerializer.Serialize(new { status = "active" });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeTrue("el valor coincide con la condición");
        }

        /// <summary>
        /// Verifica que el operador de igualdad devuelve false cuando los valores no coinciden.
        /// </summary>
        [Fact]
        public void Evaluate_EqualsOperator_WithNonMatchingStrings_ReturnsFalse()
        {
            // Arrange
            var expression = "status == 'active'";
            var jsonData = JsonSerializer.Serialize(new { status = "inactive" });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeFalse("el valor no coincide con la condición");
        }

        /// <summary>
        /// Verifica que el operador de igualdad funciona correctamente con números.
        /// </summary>
        [Fact]
        public void Evaluate_EqualsOperator_WithMatchingNumbers_ReturnsTrue()
        {
            // Arrange
            var expression = "age == '25'";
            var jsonData = JsonSerializer.Serialize(new { age = 25 });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeTrue("el valor numérico coincide");
        }

        #endregion

        #region Not Equals Operator Tests

        /// <summary>
        /// Verifica que el operador de desigualdad funciona correctamente.
        /// </summary>
        [Fact]
        public void Evaluate_NotEqualsOperator_WithDifferentValues_ReturnsTrue()
        {
            // Arrange
            var expression = "status != 'pending'";
            var jsonData = JsonSerializer.Serialize(new { status = "active" });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeTrue("los valores son diferentes");
        }

        /// <summary>
        /// Verifica que el operador de desigualdad devuelve false cuando los valores son iguales.
        /// </summary>
        [Fact]
        public void Evaluate_NotEqualsOperator_WithSameValues_ReturnsFalse()
        {
            // Arrange
            var expression = "status != 'active'";
            var jsonData = JsonSerializer.Serialize(new { status = "active" });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeFalse("los valores son iguales");
        }

        #endregion

        #region Comparison Operator Tests

        /// <summary>
        /// Verifica que el operador mayor que funciona correctamente.
        /// </summary>
        [Fact]
        public void Evaluate_GreaterThanOperator_ReturnsCorrectResult()
        {
            // Arrange
            var expression = "score > '80'";
            var jsonData = JsonSerializer.Serialize(new { score = 85 });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeTrue("85 es mayor que 80");
        }

        /// <summary>
        /// Verifica que el operador mayor que devuelve false cuando no se cumple.
        /// </summary>
        [Fact]
        public void Evaluate_GreaterThanOperator_WithLowerValue_ReturnsFalse()
        {
            // Arrange
            var expression = "score > '90'";
            var jsonData = JsonSerializer.Serialize(new { score = 85 });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeFalse("85 no es mayor que 90");
        }

        /// <summary>
        /// Verifica que el operador mayor o igual funciona correctamente.
        /// </summary>
        [Theory]
        [InlineData(85, true)]  // Mayor
        [InlineData(80, true)]  // Igual
        [InlineData(75, false)] // Menor
        public void Evaluate_GreaterOrEqualOperator_ReturnsCorrectResult(int score, bool expected)
        {
            // Arrange
            var expression = "score >= '80'";
            var jsonData = JsonSerializer.Serialize(new { score });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().Be(expected, $"score {score} debería resultar en {expected}");
        }

        #endregion

        #region Contains Operator Tests

        /// <summary>
        /// Verifica que el operador CONTAINS funciona correctamente.
        /// </summary>
        [Fact]
        public void Evaluate_ContainsOperator_WithMatchingSubstring_ReturnsTrue()
        {
            // Arrange
            var expression = "email CONTAINS '@example.com'";
            var jsonData = JsonSerializer.Serialize(new { email = "user@example.com" });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeTrue("el email contiene '@example.com'");
        }

        /// <summary>
        /// Verifica que el operador CONTAINS devuelve false cuando no hay coincidencia.
        /// </summary>
        [Fact]
        public void Evaluate_ContainsOperator_WithNonMatchingSubstring_ReturnsFalse()
        {
            // Arrange
            var expression = "email CONTAINS '@gmail.com'";
            var jsonData = JsonSerializer.Serialize(new { email = "user@example.com" });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeFalse("el email no contiene '@gmail.com'");
        }

        /// <summary>
        /// Verifica que CONTAINS funciona con subcadenas parciales.
        /// </summary>
        [Fact]
        public void Evaluate_ContainsOperator_WithPartialMatch_ReturnsTrue()
        {
            // Arrange
            var expression = "name CONTAINS 'John'";
            var jsonData = JsonSerializer.Serialize(new { name = "John Doe" });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeTrue("el nombre contiene 'John'");
        }

        #endregion

        #region Boolean Logic Tests

        /// <summary>
        /// Verifica que la lógica AND funciona correctamente cuando todas las condiciones son verdaderas.
        /// </summary>
        [Fact]
        public void Evaluate_AndOperator_AllTrue_ReturnsTrue()
        {
            // Arrange
            var expression = "status == 'active' && score > '70'";
            var jsonData = JsonSerializer.Serialize(new { status = "active", score = 85 });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeTrue("todas las condiciones son verdaderas");
        }

        /// <summary>
        /// Verifica que la lógica AND devuelve false cuando al menos una condición es falsa.
        /// </summary>
        [Fact]
        public void Evaluate_AndOperator_OneFalse_ReturnsFalse()
        {
            // Arrange
            var expression = "status == 'active' && score > '90'";
            var jsonData = JsonSerializer.Serialize(new { status = "active", score = 85 });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeFalse("la segunda condición es falsa");
        }

        /// <summary>
        /// Verifica que la lógica OR funciona correctamente cuando al menos una condición es verdadera.
        /// </summary>
        [Fact]
        public void Evaluate_OrOperator_OneTrue_ReturnsTrue()
        {
            // Arrange
            var expression = "status == 'inactive' || score > '70'";
            var jsonData = JsonSerializer.Serialize(new { status = "active", score = 85 });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeTrue("la segunda condición es verdadera");
        }

        /// <summary>
        /// Verifica que la lógica OR devuelve false cuando todas las condiciones son falsas.
        /// </summary>
        [Fact]
        public void Evaluate_OrOperator_AllFalse_ReturnsFalse()
        {
            // Arrange
            var expression = "status == 'inactive' || score > '100'";
            var jsonData = JsonSerializer.Serialize(new { status = "active", score = 85 });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeFalse("todas las condiciones son falsas");
        }

        /// <summary>
        /// Verifica que se pueden combinar múltiples operadores lógicos.
        /// </summary>
        [Fact]
        public void Evaluate_ComplexExpression_EvaluatesCorrectly()
        {
            // Arrange
            var expression = "status == 'active' && score > '70' || country == 'MX'";
            var jsonData = JsonSerializer.Serialize(new
            {
                status = "inactive",
                score = 50,
                country = "MX"
            });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeTrue("la condición del país es verdadera (OR tiene precedencia)");
        }

        #endregion

        #region Edge Cases Tests

        /// <summary>
        /// Verifica que una expresión vacía o "true" siempre devuelve true.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("true")]
        [InlineData("TRUE")]
        [InlineData("  true  ")]
        public void Evaluate_EmptyOrTrueExpression_ReturnsTrue(string expression)
        {
            // Arrange
            var jsonData = JsonSerializer.Serialize(new { test = "value" });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeTrue("las expresiones vacías o 'true' siempre son verdaderas");
        }

        /// <summary>
        /// Verifica que se maneja correctamente un campo que no existe en el JSON.
        /// </summary>
        [Fact]
        public void Evaluate_WithNonExistentField_ReturnsFalse()
        {
            // Arrange
            var expression = "nonexistent == 'value'";
            var jsonData = JsonSerializer.Serialize(new { other = "value" });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeFalse("el campo no existe en el JSON");
        }

        /// <summary>
        /// Verifica que se maneja correctamente un JSON inválido.
        /// </summary>
        [Fact]
        public void Evaluate_WithInvalidJson_ReturnsFalse()
        {
            // Arrange
            var expression = "status == 'active'";
            var invalidJson = "{ invalid json }";

            // Act
            var result = _sut.Evaluate(expression, invalidJson);

            // Assert
            result.Should().BeFalse("el JSON es inválido");
        }

        /// <summary>
        /// Verifica que se maneja correctamente un operador no soportado.
        /// </summary>
        [Fact]
        public void Evaluate_WithUnsupportedOperator_ReturnsFalse()
        {
            // Arrange
            var expression = "status =~ 'active'"; // Operador regex no soportado
            var jsonData = JsonSerializer.Serialize(new { status = "active" });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeFalse("el operador no está soportado");
        }

        /// <summary>
        /// Verifica que se manejan correctamente los valores nulos.
        /// </summary>
        [Fact]
        public void Evaluate_WithNullValue_HandlesGracefully()
        {
            // Arrange
            var expression = "optional == 'value'";
            var jsonData = JsonSerializer.Serialize(new { optional = (string?)null });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeFalse("el valor es nulo");
        }

        #endregion

        #region Data Type Tests

        /// <summary>
        /// Verifica comparaciones numéricas con el operador mayor que.
        /// </summary>
        [Theory]
        [InlineData(90, true)]
        [InlineData(85, true)]
        [InlineData(80, false)]
        [InlineData(75, false)]
        public void Evaluate_GreaterThan_WithNumericValues_WorksCorrectly(int value, bool expected)
        {
            // Arrange
            var expression = "value > '80'";
            var jsonData = JsonSerializer.Serialize(new { value });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().Be(expected, $"valor {value} > 80 debería ser {expected}");
        }

        /// <summary>
        /// Verifica que las comparaciones de strings funcionan correctamente.
        /// </summary>
        [Fact]
        public void Evaluate_StringComparison_WorksCorrectly()
        {
            // Arrange
            var expression = "country == 'MX'";
            var jsonData = JsonSerializer.Serialize(new { country = "MX" });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeTrue("la comparación de strings funciona");
        }

        #endregion

        #region Whitespace Handling Tests

        /// <summary>
        /// Verifica que los espacios en blanco se manejan correctamente en las comparaciones.
        /// </summary>
        [Fact]
        public void Evaluate_WithWhitespaceInValues_TrimsAndCompares()
        {
            // Arrange
            var expression = "status == 'active'";
            var jsonData = JsonSerializer.Serialize(new { status = " active " });

            // Act
            var result = _sut.Evaluate(expression, jsonData);

            // Assert
            result.Should().BeTrue("los valores se comparan después de trimear espacios");
        }

        #endregion
    }
}
