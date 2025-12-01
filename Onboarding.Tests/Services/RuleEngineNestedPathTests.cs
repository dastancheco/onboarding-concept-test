using Onboarding.Core.Interfaces;
using Onboarding.Core.Services;
using System.Collections.Generic;
using Xunit;

namespace Onboarding.Tests.Services
{
    /// <summary>
    /// Tests específicos para verificar el soporte de paths JSON anidados en múltiples niveles
    /// </summary>
    public class RuleEngineNestedPathTests
    {
        private readonly IRuleEngine _ruleEngine;

        public RuleEngineNestedPathTests()
        {
            // Configurar estrategias de operadores usando las implementaciones reales
            var strategies = new List<IOperatorStrategy>
            {
                new Infrastructure.Strategies.Rules.EqualsStrategy(),
                new Infrastructure.Strategies.Rules.NotEqualsStrategy(),
                new Infrastructure.Strategies.Rules.GreaterThanStrategy(),
                new Infrastructure.Strategies.Rules.GreaterOrEqualStrategy()
            };

            _ruleEngine = new RuleEngine(strategies);
        }

        [Fact]
        public void Evaluate_SingleLevelPath_ReturnsTrue()
        {
            // Arrange
            var expression = "country == \"MX\"";
            var jsonData = "{\"country\":\"MX\"}";

            // Act
            var result = _ruleEngine.Evaluate(expression, jsonData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_TwoLevelPath_ReturnsTrue()
        {
            // Arrange
            var expression = "input.app_id == \"OVEX\"";
            var jsonData = "{\"input\":{\"app_id\":\"OVEX\"}}";

            // Act
            var result = _ruleEngine.Evaluate(expression, jsonData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_ThreeLevelPath_ReturnsTrue()
        {
            // Arrange
            var expression = "input.data.app_id == \"OVEX\"";
            var jsonData = "{\"input\":{\"data\":{\"app_id\":\"OVEX\"}}}";

            // Act
            var result = _ruleEngine.Evaluate(expression, jsonData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_FourLevelPath_ReturnsTrue()
        {
            // Arrange
            var expression = "prospect.financial.details.income == \"50000\"";
            var jsonData = "{\"prospect\":{\"financial\":{\"details\":{\"income\":\"50000\"}}}}";

            // Act
            var result = _ruleEngine.Evaluate(expression, jsonData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_FiveLevelPath_ReturnsTrue()
        {
            // Arrange
            var expression = "data.user.profile.contact.email == \"test@example.com\"";
            var jsonData = "{\"data\":{\"user\":{\"profile\":{\"contact\":{\"email\":\"test@example.com\"}}}}}";

            // Act
            var result = _ruleEngine.Evaluate(expression, jsonData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_MultipleNestedPaths_ReturnsTrue()
        {
            // Arrange
            var expression = "input.data.app_id == \"OVEX\" && input.data.client_type == \"CLIENT\"";
            var jsonData = "{\"input\":{\"data\":{\"app_id\":\"OVEX\",\"client_type\":\"CLIENT\"}}}";

            // Act
            var result = _ruleEngine.Evaluate(expression, jsonData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_MixedDepthPaths_ReturnsTrue()
        {
            // Arrange - Un path de 2 niveles y otro de 3
            var expression = "input.app_id == \"OVEX\" && data.user.country == \"MX\"";
            var jsonData = "{\"input\":{\"app_id\":\"OVEX\"},\"data\":{\"user\":{\"country\":\"MX\"}}}";

            // Act
            var result = _ruleEngine.Evaluate(expression, jsonData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_NonexistentIntermediateLevel_ReturnsFalse()
        {
            // Arrange - El path intermedio "data" no existe
            var expression = "input.data.app_id == \"OVEX\"";
            var jsonData = "{\"input\":{\"app_id\":\"OVEX\"}}";

            // Act
            var result = _ruleEngine.Evaluate(expression, jsonData);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Evaluate_NonexistentDeepLevel_ReturnsFalse()
        {
            // Arrange - Solo existen 2 niveles, pero se busca en el 3ro
            var expression = "input.data.details.app_id == \"OVEX\"";
            var jsonData = "{\"input\":{\"data\":{\"app_id\":\"OVEX\"}}}";

            // Act
            var result = _ruleEngine.Evaluate(expression, jsonData);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Evaluate_DeepNestedWithNumericComparison_ReturnsTrue()
        {
            // Arrange
            var expression = "prospect.financial.credit.score > \"600\"";
            var jsonData = "{\"prospect\":{\"financial\":{\"credit\":{\"score\":\"750\"}}}}";

            // Act
            var result = _ruleEngine.Evaluate(expression, jsonData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_ComplexNestedWithOr_ReturnsTrue()
        {
            // Arrange
            var expression = "input.data.app_id == \"OVEX\" || input.data.app_id == \"SANTANDER\"";
            var jsonData = "{\"input\":{\"data\":{\"app_id\":\"SANTANDER\"}}}";

            // Act
            var result = _ruleEngine.Evaluate(expression, jsonData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_VeryDeepNesting_ReturnsTrue()
        {
            // Arrange - 7 niveles de anidación
            var expression = "level1.level2.level3.level4.level5.level6.level7 == \"deep\"";
            var jsonData = "{\"level1\":{\"level2\":{\"level3\":{\"level4\":{\"level5\":{\"level6\":{\"level7\":\"deep\"}}}}}}}";

            // Act
            var result = _ruleEngine.Evaluate(expression, jsonData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_ArrayIndexPath_WorksWithDotNotation()
        {
            // Arrange - Acceso a elementos de array usando índice numérico
            var expression = "data.items.0.name == \"Item1\"";
            var jsonData = "{\"data\":{\"items\":[{\"name\":\"Item1\"},{\"name\":\"Item2\"}]}}";

            // Act
            var result = _ruleEngine.Evaluate(expression, jsonData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_RealWorldScenario_InputPath_ReturnsTrue()
        {
            // Arrange - Escenario real del sistema de ruteo
            var expression = "input.app_id == \"OVEX\" && input.client_type == \"CLIENT\"";
            var jsonData = "{\"input\":{\"app_id\":\"OVEX\",\"client_type\":\"CLIENT\"}}";

            // Act
            var result = _ruleEngine.Evaluate(expression, jsonData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_RealWorldScenario_FlotillaPath_ReturnsTrue()
        {
            // Arrange - Escenario real del sistema de ruteo para flotilla
            var expression = "input.app_id == \"OVEX\" && input.client_type == \"FLOTILLA_EMP\"";
            var jsonData = "{\"input\":{\"app_id\":\"OVEX\",\"client_type\":\"FLOTILLA_EMP\"}}";

            // Act
            var result = _ruleEngine.Evaluate(expression, jsonData);

            // Assert
            Assert.True(result);
        }
    }
}
