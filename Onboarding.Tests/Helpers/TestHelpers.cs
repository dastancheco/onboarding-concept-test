using System.Text.Json;

namespace Onboarding.Tests.Helpers
{
    /// <summary>
    /// Clase de utilidades para ayudar en las pruebas.
    /// Proporciona métodos helper comunes para crear datos de prueba.
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Crea un JSON de prueba con los campos especificados.
        /// </summary>
        public static string CreateTestJson(params (string key, object value)[] fields)
        {
            var dictionary = new Dictionary<string, object>();
            foreach (var (key, value) in fields)
            {
                dictionary[key] = value;
            }
            return JsonSerializer.Serialize(dictionary);
        }

        /// <summary>
        /// Crea un payload de registro de usuario de prueba.
        /// </summary>
        public static string CreateUserRegistrationPayload(
            string email = "test@example.com",
            string name = "Test User",
            string country = "MX")
        {
            return JsonSerializer.Serialize(new
            {
                email,
                name,
                country,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Crea un payload de submisión de datos de paso de prueba.
        /// </summary>
        public static string CreateStepDataPayload(
            Guid prospectId,
            Dictionary<string, object>? additionalData = null)
        {
            var data = new Dictionary<string, object>
            {
                ["prospect_id"] = prospectId.ToString()
            };

            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    data[kvp.Key] = kvp.Value;
                }
            }

            return JsonSerializer.Serialize(data);
        }

        /// <summary>
        /// Genera un email aleatorio para pruebas.
        /// </summary>
        public static string GenerateRandomEmail()
        {
            return $"test_{Guid.NewGuid():N}@example.com";
        }

        /// <summary>
        /// Genera múltiples prospectos de prueba.
        /// </summary>
        public static List<OvexDataModelingTest.Entities.App.Prospect> GenerateTestProspects(
            int count,
            Guid userId,
            int workflowId = 1)
        {
            var prospects = new List<OvexDataModelingTest.Entities.App.Prospect>();

            for (int i = 0; i < count; i++)
            {
                prospects.Add(new OvexDataModelingTest.Entities.App.Prospect
                {
                    ProspectId = Guid.NewGuid(),
                    UserId = userId,
                    WorkflowId = workflowId,
                    Status = "IN_PROGRESS",
                    CreatedAt = DateTime.UtcNow.AddDays(-i),
                    UpdatedAt = DateTime.UtcNow
                });
            }

            return prospects;
        }

        /// <summary>
        /// Genera una configuración de campo de prueba.
        /// </summary>
        public static string CreateFieldConfig(
            bool required = true,
            int? minLength = null,
            int? maxLength = null,
            string? pattern = null)
        {
            var config = new Dictionary<string, object>
            {
                ["required"] = required
            };

            if (minLength.HasValue)
                config["min_length"] = minLength.Value;

            if (maxLength.HasValue)
                config["max_length"] = maxLength.Value;

            if (!string.IsNullOrEmpty(pattern))
                config["pattern"] = pattern;

            return JsonSerializer.Serialize(config);
        }

        /// <summary>
        /// Valida que un JSON contiene todos los campos especificados.
        /// </summary>
        public static bool JsonContainsFields(string json, params string[] fieldNames)
        {
            try
            {
                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                foreach (var fieldName in fieldNames)
                {
                    if (!root.TryGetProperty(fieldName, out _))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Extrae un valor de un JSON por su clave.
        /// </summary>
        public static T? GetJsonValue<T>(string json, string key)
        {
            try
            {
                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.TryGetProperty(key, out var element))
                {
                    return JsonSerializer.Deserialize<T>(element.GetRawText());
                }

                return default;
            }
            catch
            {
                return default;
            }
        }
    }

    /// <summary>
    /// Pruebas unitarias para los helpers de pruebas.
    /// Sí, ¡probamos nuestros helpers de pruebas! ??
    /// </summary>
    public class TestHelpersTests
    {
        #region CreateTestJson Tests

        [Fact]
        public void CreateTestJson_WithSingleField_CreatesValidJson()
        {
            // Act
            var json = TestHelpers.CreateTestJson(("name", "John"));

            // Assert
            json.Should().NotBeNullOrEmpty();
            json.Should().Contain("name");
            json.Should().Contain("John");

            // Verificar que es JSON válido
            var parsed = JsonDocument.Parse(json);
            parsed.RootElement.GetProperty("name").GetString().Should().Be("John");
        }

        [Fact]
        public void CreateTestJson_WithMultipleFields_CreatesValidJson()
        {
            // Act
            var json = TestHelpers.CreateTestJson(
                ("name", "John"),
                ("age", 30),
                ("active", true));

            // Assert
            json.Should().NotBeNullOrEmpty();
            TestHelpers.JsonContainsFields(json, "name", "age", "active").Should().BeTrue();
        }

        #endregion

        #region CreateUserRegistrationPayload Tests

        [Fact]
        public void CreateUserRegistrationPayload_WithDefaults_CreatesValidPayload()
        {
            // Act
            var payload = TestHelpers.CreateUserRegistrationPayload();

            // Assert
            payload.Should().NotBeNullOrEmpty();
            TestHelpers.JsonContainsFields(payload, "email", "name", "country").Should().BeTrue();

            var email = TestHelpers.GetJsonValue<string>(payload, "email");
            email.Should().Be("test@example.com");
        }

        [Fact]
        public void CreateUserRegistrationPayload_WithCustomValues_UsesProvidedValues()
        {
            // Act
            var payload = TestHelpers.CreateUserRegistrationPayload(
                email: "custom@example.com",
                name: "Custom User",
                country: "US");

            // Assert
            var email = TestHelpers.GetJsonValue<string>(payload, "email");
            var name = TestHelpers.GetJsonValue<string>(payload, "name");
            var country = TestHelpers.GetJsonValue<string>(payload, "country");

            email.Should().Be("custom@example.com");
            name.Should().Be("Custom User");
            country.Should().Be("US");
        }

        #endregion

        #region CreateStepDataPayload Tests

        [Fact]
        public void CreateStepDataPayload_WithProspectIdOnly_CreatesMinimalPayload()
        {
            // Arrange
            var prospectId = Guid.NewGuid();

            // Act
            var payload = TestHelpers.CreateStepDataPayload(prospectId);

            // Assert
            payload.Should().NotBeNullOrEmpty();
            TestHelpers.JsonContainsFields(payload, "prospect_id").Should().BeTrue();

            var extractedId = TestHelpers.GetJsonValue<string>(payload, "prospect_id");
            extractedId.Should().Be(prospectId.ToString());
        }

        [Fact]
        public void CreateStepDataPayload_WithAdditionalData_IncludesAllFields()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var additionalData = new Dictionary<string, object>
            {
                ["first_name"] = "John",
                ["last_name"] = "Doe",
                ["age"] = 30
            };

            // Act
            var payload = TestHelpers.CreateStepDataPayload(prospectId, additionalData);

            // Assert
            TestHelpers.JsonContainsFields(payload, 
                "prospect_id", "first_name", "last_name", "age").Should().BeTrue();

            var firstName = TestHelpers.GetJsonValue<string>(payload, "first_name");
            firstName.Should().Be("John");
        }

        #endregion

        #region GenerateRandomEmail Tests

        [Fact]
        public void GenerateRandomEmail_CreatesValidEmail()
        {
            // Act
            var email = TestHelpers.GenerateRandomEmail();

            // Assert
            email.Should().NotBeNullOrEmpty();
            email.Should().Contain("@example.com");
            email.Should().StartWith("test_");
        }

        [Fact]
        public void GenerateRandomEmail_CreatesDifferentEmails()
        {
            // Act
            var email1 = TestHelpers.GenerateRandomEmail();
            var email2 = TestHelpers.GenerateRandomEmail();

            // Assert
            email1.Should().NotBe(email2, "cada llamada debe generar un email único");
        }

        #endregion

        #region GenerateTestProspects Tests

        [Fact]
        public void GenerateTestProspects_WithCount_CreatesCorrectNumber()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var count = 5;

            // Act
            var prospects = TestHelpers.GenerateTestProspects(count, userId);

            // Assert
            prospects.Should().HaveCount(count);
            prospects.Should().OnlyContain(p => p.UserId == userId);
        }

        [Fact]
        public void GenerateTestProspects_CreatesProspectsWithDifferentDates()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var prospects = TestHelpers.GenerateTestProspects(3, userId);

            // Assert
            prospects.Should().HaveCount(3);
            
            // Verificar que las fechas son diferentes
            var dates = prospects.Select(p => p.CreatedAt).ToList();
            dates.Distinct().Should().HaveCount(3, "cada prospecto debe tener una fecha diferente");
        }

        #endregion

        #region CreateFieldConfig Tests

        [Fact]
        public void CreateFieldConfig_WithDefaults_CreatesBasicConfig()
        {
            // Act
            var config = TestHelpers.CreateFieldConfig();

            // Assert
            config.Should().NotBeNullOrEmpty();
            TestHelpers.JsonContainsFields(config, "required").Should().BeTrue();

            var required = TestHelpers.GetJsonValue<bool>(config, "required");
            required.Should().BeTrue();
        }

        [Fact]
        public void CreateFieldConfig_WithAllParameters_IncludesAllFields()
        {
            // Act
            var config = TestHelpers.CreateFieldConfig(
                required: true,
                minLength: 3,
                maxLength: 50,
                pattern: "^[A-Z]");

            // Assert
            TestHelpers.JsonContainsFields(config, 
                "required", "min_length", "max_length", "pattern").Should().BeTrue();

            var minLength = TestHelpers.GetJsonValue<int>(config, "min_length");
            minLength.Should().Be(3);
        }

        #endregion

        #region JsonContainsFields Tests

        [Fact]
        public void JsonContainsFields_WithExistingFields_ReturnsTrue()
        {
            // Arrange
            var json = "{\"name\":\"John\",\"age\":30}";

            // Act
            var result = TestHelpers.JsonContainsFields(json, "name", "age");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void JsonContainsFields_WithMissingField_ReturnsFalse()
        {
            // Arrange
            var json = "{\"name\":\"John\"}";

            // Act
            var result = TestHelpers.JsonContainsFields(json, "name", "age");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void JsonContainsFields_WithInvalidJson_ReturnsFalse()
        {
            // Arrange
            var invalidJson = "{ invalid }";

            // Act
            var result = TestHelpers.JsonContainsFields(invalidJson, "name");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetJsonValue Tests

        [Fact]
        public void GetJsonValue_WithExistingKey_ReturnsValue()
        {
            // Arrange
            var json = "{\"name\":\"John\",\"age\":30}";

            // Act
            var name = TestHelpers.GetJsonValue<string>(json, "name");
            var age = TestHelpers.GetJsonValue<int>(json, "age");

            // Assert
            name.Should().Be("John");
            age.Should().Be(30);
        }

        [Fact]
        public void GetJsonValue_WithNonExistentKey_ReturnsDefault()
        {
            // Arrange
            var json = "{\"name\":\"John\"}";

            // Act
            var age = TestHelpers.GetJsonValue<int>(json, "age");

            // Assert
            age.Should().Be(0, "debe retornar el valor por defecto del tipo");
        }

        [Fact]
        public void GetJsonValue_WithInvalidJson_ReturnsDefault()
        {
            // Arrange
            var invalidJson = "{ invalid }";

            // Act
            var value = TestHelpers.GetJsonValue<string>(invalidJson, "name");

            // Assert
            value.Should().BeNull();
        }

        #endregion
    }
}
