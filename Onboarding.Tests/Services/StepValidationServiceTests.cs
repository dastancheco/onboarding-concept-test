using Onboarding.Core.Interfaces;
using Onboarding.Core.Models;
using Onboarding.Core.Services;
using OvexDataModelingTest.Entities.Config;
using System.Text.Json;

namespace Onboarding.Tests.Services
{
    /// <summary>
    /// Pruebas unitarias para el servicio de validación de pasos (StepValidationService).
    /// 
    /// Cobertura de pruebas:
    /// - Validación de campos requeridos
    /// - Validación de tipos de datos
    /// - Validación de formatos (email, teléfono, etc.)
    /// - Validación de longitudes mínimas y máximas
    /// - Validación de rangos numéricos
    /// - Manejo de configuraciones override
    /// - Manejo de errores y casos edge
    /// </summary>
    public class StepValidationServiceTests
    {
        private readonly Mock<IRepository<Step_Field>> _stepFieldsRepoMock;
        private readonly Mock<IRepository<FieldDefinition>> _fieldsRepoMock;
        private readonly Mock<IValidationPipeline> _validationPipelineMock;
        private readonly Mock<ILogger<StepValidationService>> _loggerMock;
        private readonly StepValidationService _sut;

        public StepValidationServiceTests()
        {
            // Arrange: Configurar mocks
            _stepFieldsRepoMock = new Mock<IRepository<Step_Field>>();
            _fieldsRepoMock = new Mock<IRepository<FieldDefinition>>();
            _validationPipelineMock = new Mock<IValidationPipeline>();
            _loggerMock = new Mock<ILogger<StepValidationService>>();

            _sut = new StepValidationService(
                _stepFieldsRepoMock.Object,
                _fieldsRepoMock.Object,
                _validationPipelineMock.Object,
                _loggerMock.Object);
        }

        #region Basic Validation Tests

        /// <summary>
        /// Verifica que la validación es exitosa cuando todos los campos requeridos
        /// están presentes y son válidos.
        /// </summary>
        [Fact]
        public async Task ValidateStepAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var stepId = 1;
            var payloadJson = JsonSerializer.Serialize(new
            {
                first_name = "John",
                last_name = "Doe",
                email = "john.doe@example.com"
            });

            var stepFields = new List<Step_Field>
            {
                new Step_Field { StepId = stepId, FieldId = 1 },
                new Step_Field { StepId = stepId, FieldId = 2 },
                new Step_Field { StepId = stepId, FieldId = 3 }
            };

            var fieldDefinitions = new List<FieldDefinition>
            {
                new FieldDefinition
                {
                    FieldId = 1,
                    FieldKey = "first_name",
                    DataType = "string",
                    Config = JsonSerializer.Serialize(new { required = true })
                },
                new FieldDefinition
                {
                    FieldId = 2,
                    FieldKey = "last_name",
                    DataType = "string",
                    Config = JsonSerializer.Serialize(new { required = true })
                },
                new FieldDefinition
                {
                    FieldId = 3,
                    FieldKey = "email",
                    DataType = "email",
                    Config = JsonSerializer.Serialize(new { required = true })
                }
            };

            _stepFieldsRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Step_Field, bool>>>()))
                .ReturnsAsync(stepFields);

            _fieldsRepoMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(fieldDefinitions);

            _validationPipelineMock
                .Setup(x => x.ValidateField(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .Returns(new List<ValidationError>()); // Sin errores

            // Act
            var result = await _sut.ValidateStepAsync(stepId, payloadJson);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue("todos los campos son válidos");
            result.Errors.Should().BeEmpty();
            result.Message.Should().Contain("exitosa");
        }

        /// <summary>
        /// Verifica que la validación falla cuando hay errores de validación en los campos.
        /// </summary>
        [Fact]
        public async Task ValidateStepAsync_WithInvalidData_ReturnsFailure()
        {
            // Arrange
            var stepId = 1;
            var payloadJson = JsonSerializer.Serialize(new
            {
                email = "invalid-email" // Email inválido
            });

            var stepFields = new List<Step_Field>
            {
                new Step_Field { StepId = stepId, FieldId = 1 }
            };

            var fieldDefinitions = new List<FieldDefinition>
            {
                new FieldDefinition
                {
                    FieldId = 1,
                    FieldKey = "email",
                    DataType = "email",
                    Config = JsonSerializer.Serialize(new { required = true })
                }
            };

            var validationErrors = new List<ValidationError>
            {
                new ValidationError
                {
                    FieldKey = "email",
                    ErrorCode = "INVALID_FORMAT",
                    Message = "El email no tiene un formato válido"
                }
            };

            _stepFieldsRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Step_Field, bool>>>()))
                .ReturnsAsync(stepFields);

            _fieldsRepoMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(fieldDefinitions);

            _validationPipelineMock
                .Setup(x => x.ValidateField("email", "invalid-email", "email", It.IsAny<Dictionary<string, object>>()))
                .Returns(validationErrors);

            // Act
            var result = await _sut.ValidateStepAsync(stepId, payloadJson);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse("el email es inválido");
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldKey.Should().Be("email");
            result.Errors[0].ErrorCode.Should().Be("INVALID_FORMAT");
            result.Message.Should().Contain("error");
        }

        /// <summary>
        /// Verifica que la validación falla cuando el payload no es un JSON válido.
        /// </summary>
        [Fact]
        public async Task ValidateStepAsync_WithInvalidJson_ReturnsFailure()
        {
            // Arrange
            var stepId = 1;
            var invalidJson = "{ invalid json }";

            // Act
            var result = await _sut.ValidateStepAsync(stepId, invalidJson);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse("el JSON es inválido");
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldKey.Should().Be("_payload");
            result.Errors[0].ErrorCode.Should().Be("INVALID_JSON");
            result.Message.Should().Contain("inválido");
        }

        /// <summary>
        /// Verifica que la validación maneja correctamente un paso sin campos configurados.
        /// </summary>
        [Fact]
        public async Task ValidateStepAsync_WithNoConfiguredFields_ReturnsSuccess()
        {
            // Arrange
            var stepId = 1;
            var payloadJson = JsonSerializer.Serialize(new { data = "test" });

            _stepFieldsRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Step_Field, bool>>>()))
                .ReturnsAsync(new List<Step_Field>()); // Sin campos configurados

            // Act
            var result = await _sut.ValidateStepAsync(stepId, payloadJson);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue("no hay campos para validar");
            result.Message.Should().Contain("No hay campos configurados");
        }

        #endregion

        #region Config Override Tests

        /// <summary>
        /// Verifica que la configuración override tiene prioridad sobre la configuración base.
        /// </summary>
        [Fact]
        public async Task ValidateStepAsync_WithConfigOverride_UsesOverriddenConfig()
        {
            // Arrange
            var stepId = 1;
            var payloadJson = JsonSerializer.Serialize(new { username = "ab" }); // Solo 2 caracteres

            var stepFields = new List<Step_Field>
            {
                new Step_Field
                {
                    StepId = stepId,
                    FieldId = 1,
                    ConfigOverride = JsonSerializer.Serialize(new { min_length = 5 }) // Override: min 5 caracteres
                }
            };

            var fieldDefinitions = new List<FieldDefinition>
            {
                new FieldDefinition
                {
                    FieldId = 1,
                    FieldKey = "username",
                    DataType = "string",
                    Config = JsonSerializer.Serialize(new { min_length = 3 }) // Base: min 3 caracteres
                }
            };

            var validationErrors = new List<ValidationError>
            {
                new ValidationError
                {
                    FieldKey = "username",
                    ErrorCode = "MIN_LENGTH",
                    Message = "El username debe tener al menos 5 caracteres"
                }
            };

            _stepFieldsRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Step_Field, bool>>>()))
                .ReturnsAsync(stepFields);

            _fieldsRepoMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(fieldDefinitions);

            // Verificar que se llama con la configuración combinada (override prevalece)
            _validationPipelineMock
                .Setup(x => x.ValidateField(
                    "username",
                    "ab",
                    "string",
                    It.Is<Dictionary<string, object>>(d => VerifyMinLength(d, 5))))
                .Returns(validationErrors);

            // Act
            var result = await _sut.ValidateStepAsync(stepId, payloadJson);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);

            _validationPipelineMock.Verify(
                x => x.ValidateField(
                    "username",
                    "ab",
                    "string",
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once,
                "Debe validar con la configuración override");
        }

        // Helper para verificar el valor de min_length en el diccionario
        private bool VerifyMinLength(Dictionary<string, object> config, int expectedMinLength)
        {
            if (!config.ContainsKey("min_length")) return false;

            var value = config["min_length"];
            if (value is JsonElement element)
            {
                return element.GetInt32() == expectedMinLength;
            }

            return false;
        }

        #endregion

        #region Multiple Fields Validation Tests

        /// <summary>
        /// Verifica que se validan múltiples campos correctamente y se acumulan todos los errores.
        /// </summary>
        [Fact]
        public async Task ValidateStepAsync_WithMultipleInvalidFields_ReturnsAllErrors()
        {
            // Arrange
            var stepId = 1;
            var payloadJson = JsonSerializer.Serialize(new
            {
                email = "invalid",
                phone = "12345", // Formato inválido
                age = -5 // Valor inválido
            });

            var stepFields = new List<Step_Field>
            {
                new Step_Field { StepId = stepId, FieldId = 1 },
                new Step_Field { StepId = stepId, FieldId = 2 },
                new Step_Field { StepId = stepId, FieldId = 3 }
            };

            var fieldDefinitions = new List<FieldDefinition>
            {
                new FieldDefinition { FieldId = 1, FieldKey = "email", DataType = "email" },
                new FieldDefinition { FieldId = 2, FieldKey = "phone", DataType = "phone" },
                new FieldDefinition { FieldId = 3, FieldKey = "age", DataType = "integer" }
            };

            _stepFieldsRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Step_Field, bool>>>()))
                .ReturnsAsync(stepFields);

            _fieldsRepoMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(fieldDefinitions);

            // Configurar errores para cada campo
            _validationPipelineMock
                .Setup(x => x.ValidateField("email", "invalid", "email", It.IsAny<Dictionary<string, object>>()))
                .Returns(new List<ValidationError>
                {
                    new ValidationError { FieldKey = "email", ErrorCode = "INVALID_FORMAT", Message = "Email inválido" }
                });

            _validationPipelineMock
                .Setup(x => x.ValidateField("phone", "12345", "phone", It.IsAny<Dictionary<string, object>>()))
                .Returns(new List<ValidationError>
                {
                    new ValidationError { FieldKey = "phone", ErrorCode = "INVALID_FORMAT", Message = "Teléfono inválido" }
                });

            _validationPipelineMock
                .Setup(x => x.ValidateField("age", "-5", "integer", It.IsAny<Dictionary<string, object>>()))
                .Returns(new List<ValidationError>
                {
                    new ValidationError { FieldKey = "age", ErrorCode = "INVALID_RANGE", Message = "Edad debe ser positiva" }
                });

            // Act
            var result = await _sut.ValidateStepAsync(stepId, payloadJson);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(3, "hay 3 campos inválidos");
            result.Errors.Should().Contain(e => e.FieldKey == "email");
            result.Errors.Should().Contain(e => e.FieldKey == "phone");
            result.Errors.Should().Contain(e => e.FieldKey == "age");
        }

        #endregion

        #region Missing Field Tests

        /// <summary>
        /// Verifica que se maneja correctamente un campo requerido que falta en el payload.
        /// </summary>
        [Fact]
        public async Task ValidateStepAsync_WithMissingRequiredField_ReturnsError()
        {
            // Arrange
            var stepId = 1;
            var payloadJson = JsonSerializer.Serialize(new { other_field = "value" }); // Falta 'email'

            var stepFields = new List<Step_Field>
            {
                new Step_Field { StepId = stepId, FieldId = 1 }
            };

            var fieldDefinitions = new List<FieldDefinition>
            {
                new FieldDefinition
                {
                    FieldId = 1,
                    FieldKey = "email",
                    DataType = "email",
                    Config = JsonSerializer.Serialize(new { required = true })
                }
            };

            var validationErrors = new List<ValidationError>
            {
                new ValidationError
                {
                    FieldKey = "email",
                    ErrorCode = "REQUIRED",
                    Message = "El campo email es requerido"
                }
            };

            _stepFieldsRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Step_Field, bool>>>()))
                .ReturnsAsync(stepFields);

            _fieldsRepoMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(fieldDefinitions);

            _validationPipelineMock
                .Setup(x => x.ValidateField("email", null, "email", It.IsAny<Dictionary<string, object>>()))
                .Returns(validationErrors);

            // Act
            var result = await _sut.ValidateStepAsync(stepId, payloadJson);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].ErrorCode.Should().Be("REQUIRED");
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Verifica que se maneja correctamente un campo opcional que no está presente.
        /// </summary>
        [Fact]
        public async Task ValidateStepAsync_WithMissingOptionalField_ReturnsSuccess()
        {
            // Arrange
            var stepId = 1;
            var payloadJson = JsonSerializer.Serialize(new { name = "John" }); // Falta 'middle_name' (opcional)

            var stepFields = new List<Step_Field>
            {
                new Step_Field { StepId = stepId, FieldId = 1 },
                new Step_Field { StepId = stepId, FieldId = 2 }
            };

            var fieldDefinitions = new List<FieldDefinition>
            {
                new FieldDefinition
                {
                    FieldId = 1,
                    FieldKey = "name",
                    DataType = "string",
                    Config = JsonSerializer.Serialize(new { required = true })
                },
                new FieldDefinition
                {
                    FieldId = 2,
                    FieldKey = "middle_name",
                    DataType = "string",
                    Config = JsonSerializer.Serialize(new { required = false })
                }
            };

            _stepFieldsRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Step_Field, bool>>>()))
                .ReturnsAsync(stepFields);

            _fieldsRepoMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(fieldDefinitions);

            _validationPipelineMock
                .Setup(x => x.ValidateField(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .Returns(new List<ValidationError>()); // Sin errores

            // Act
            var result = await _sut.ValidateStepAsync(stepId, payloadJson);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue("el campo opcional puede estar ausente");
        }

        /// <summary>
        /// Verifica que se maneja correctamente un error en la configuración base.
        /// </summary>
        [Fact]
        public async Task ValidateStepAsync_WithInvalidBaseConfig_ContinuesValidation()
        {
            // Arrange
            var stepId = 1;
            var payloadJson = JsonSerializer.Serialize(new { name = "John" });

            var stepFields = new List<Step_Field>
            {
                new Step_Field { StepId = stepId, FieldId = 1 }
            };

            var fieldDefinitions = new List<FieldDefinition>
            {
                new FieldDefinition
                {
                    FieldId = 1,
                    FieldKey = "name",
                    DataType = "string",
                    Config = "{ invalid json }" // Config inválido
                }
            };

            _stepFieldsRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Step_Field, bool>>>()))
                .ReturnsAsync(stepFields);

            _fieldsRepoMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(fieldDefinitions);

            _validationPipelineMock
                .Setup(x => x.ValidateField(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .Returns(new List<ValidationError>());

            // Act
            var result = await _sut.ValidateStepAsync(stepId, payloadJson);

            // Assert
            result.Should().NotBeNull();
            // La validación debe continuar aunque el config sea inválido
            _validationPipelineMock.Verify(
                x => x.ValidateField("name", "John", "string", It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }

        #endregion
    }
}
