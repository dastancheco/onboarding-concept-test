using Onboarding.Core.Interfaces;
using Onboarding.Core.Models;
using Onboarding.Core.Services;
using OvexDataModelingTest.Entities.App;
using OvexDataModelingTest.Entities.Config;
using System.Text.Json;

namespace Onboarding.Tests.Services
{
    /// <summary>
    /// Pruebas unitarias para el servicio OrchestratorService.
    /// 
    /// Cobertura de pruebas:
    /// - Procesamiento de eventos de registro de usuario
    /// - Procesamiento de eventos de submisión de datos
    /// - Evaluación de reglas de negocio
    /// - Manejo de errores y casos excepcionales
    /// - Integración con servicios dependientes
    /// </summary>
    public class OrchestratorServiceTests
    {
        private readonly Mock<IRepository<Rule>> _rulesRepoMock;
        private readonly Mock<IRepository<Prospect>> _prospectRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IActionExecutor> _actionExecutorMock;
        private readonly Mock<IRuleEngine> _ruleEngineMock;
        private readonly Mock<IStepValidationService> _validatorMock;
        private readonly Mock<IProspectDataService> _prospectDataServiceMock;
        private readonly Mock<ICustomerDataService> _customerDataServiceMock;
        private readonly Mock<IUserManagementService> _userManagementServiceMock;
        private readonly Mock<IWorkflowRoutingService> _workflowRoutingServiceMock;
        private readonly Mock<ILogger<OrchestratorService>> _loggerMock;
        private readonly OrchestratorService _sut; // System Under Test

        public OrchestratorServiceTests()
        {
            // Arrange: Configurar todos los mocks necesarios
            _rulesRepoMock = new Mock<IRepository<Rule>>();
            _prospectRepoMock = new Mock<IRepository<Prospect>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _actionExecutorMock = new Mock<IActionExecutor>();
            _ruleEngineMock = new Mock<IRuleEngine>();
            _validatorMock = new Mock<IStepValidationService>();
            _prospectDataServiceMock = new Mock<IProspectDataService>();
            _customerDataServiceMock = new Mock<ICustomerDataService>();
            _userManagementServiceMock = new Mock<IUserManagementService>();
            _workflowRoutingServiceMock = new Mock<IWorkflowRoutingService>();
            _loggerMock = new Mock<ILogger<OrchestratorService>>();

            // Crear instancia del servicio con todas las dependencias
            _sut = new OrchestratorService(
                _rulesRepoMock.Object,
                _prospectRepoMock.Object,
                _unitOfWorkMock.Object,
                _actionExecutorMock.Object,
                _ruleEngineMock.Object,
                _validatorMock.Object,
                _prospectDataServiceMock.Object,
                _customerDataServiceMock.Object,
                _userManagementServiceMock.Object,
                _workflowRoutingServiceMock.Object,
                _loggerMock.Object);
        }

        #region UserRegistered Event Tests

        /// <summary>
        /// Verifica que el registro de un nuevo usuario crea correctamente un prospecto
        /// y ejecuta las reglas de inicio del workflow.
        /// </summary>
        [Fact]
        public async Task ProcessEventAsync_UserRegistered_CreatesProspectAndEvaluatesRules()
        {
            // Arrange: Preparar datos de prueba
            var payloadJson = JsonSerializer.Serialize(new
            {
                email = "test@example.com",
                name = "Test User",
                country = "MX"
            });

            var mockUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com"
            };

            var workflowId = 1;

            // Configurar comportamiento de los mocks
            _userManagementServiceMock
                .Setup(x => x.ExtractEmailFromPayload(It.IsAny<string>()))
                .Returns("test@example.com");

            _userManagementServiceMock
                .Setup(x => x.GetOrCreateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockUser);

            _workflowRoutingServiceMock
                .Setup(x => x.DetermineWorkflowAsync(It.IsAny<string>()))
                .ReturnsAsync(workflowId);

            _prospectRepoMock
                .Setup(x => x.AddAsync(It.IsAny<Prospect>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            _prospectDataServiceMock
                .Setup(x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(payloadJson);

            _rulesRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rule, bool>>>()))
                .ReturnsAsync(new List<Rule>());

            // Act: Ejecutar el método bajo prueba
            await _sut.ProcessEventAsync("UserRegistered", payloadJson);

            // Assert: Verificar que se ejecutaron las operaciones esperadas
            _userManagementServiceMock.Verify(
                x => x.GetOrCreateUserAsync("test@example.com", payloadJson),
                Times.Once,
                "Debe crear o recuperar el usuario");

            _workflowRoutingServiceMock.Verify(
                x => x.DetermineWorkflowAsync(payloadJson),
                Times.Once,
                "Debe determinar el workflow apropiado");

            _prospectRepoMock.Verify(
                x => x.AddAsync(It.Is<Prospect>(p =>
                    p.UserId == mockUser.UserId &&
                    p.WorkflowId == workflowId &&
                    p.Status == "STARTED")),
                Times.Once,
                "Debe crear un nuevo prospecto con los datos correctos");

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once,
                "Debe guardar los cambios en la base de datos");

            _prospectDataServiceMock.Verify(
                x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), payloadJson),
                Times.Once,
                "Debe inicializar los datos del prospecto");
        }

        /// <summary>
        /// Verifica que el sistema maneja correctamente el caso donde no se puede
        /// extraer el email del payload.
        /// </summary>
        [Fact]
        public async Task ProcessEventAsync_UserRegistered_WithoutEmail_DoesNotCreateProspect()
        {
            // Arrange
            var payloadJson = JsonSerializer.Serialize(new { name = "Test User" });

            _userManagementServiceMock
                .Setup(x => x.ExtractEmailFromPayload(It.IsAny<string>()))
                .Returns((string?)null);

            // Act
            await _sut.ProcessEventAsync("UserRegistered", payloadJson);

            // Assert
            _prospectRepoMock.Verify(
                x => x.AddAsync(It.IsAny<Prospect>()),
                Times.Never,
                "No debe crear prospecto si no hay email");

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never,
                "No debe guardar cambios si no se creó prospecto");
        }

        /// <summary>
        /// Verifica que el sistema maneja correctamente el caso donde no se encuentra
        /// una regla de ruteo para el usuario.
        /// </summary>
        [Fact]
        public async Task ProcessEventAsync_UserRegistered_NoRoutingRuleMatch_DoesNotCreateProspect()
        {
            // Arrange
            var payloadJson = JsonSerializer.Serialize(new
            {
                email = "test@example.com",
                country = "UNKNOWN"
            });

            var mockUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com"
            };

            _userManagementServiceMock
                .Setup(x => x.ExtractEmailFromPayload(It.IsAny<string>()))
                .Returns("test@example.com");

            _userManagementServiceMock
                .Setup(x => x.GetOrCreateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockUser);

            _workflowRoutingServiceMock
                .Setup(x => x.DetermineWorkflowAsync(It.IsAny<string>()))
                .ReturnsAsync(0); // No workflow encontrado

            // Act
            await _sut.ProcessEventAsync("UserRegistered", payloadJson);

            // Assert
            _prospectRepoMock.Verify(
                x => x.AddAsync(It.IsAny<Prospect>()),
                Times.Never,
                "No debe crear prospecto si no hay workflow asignado");

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never,
                "No debe guardar cambios si no se creó prospecto");
        }

        #endregion

        #region StepDataSubmitted Event Tests

        /// <summary>
        /// Verifica que la submisión de datos válidos actualiza correctamente
        /// el prospecto y evalúa las reglas subsecuentes.
        /// </summary>
        [Fact]
        public async Task ProcessEventAsync_StepDataSubmitted_ValidData_UpdatesProspectData()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var stepId = 1;
            var workflowId = 1;

            var payloadJson = JsonSerializer.Serialize(new
            {
                prospect_id = prospectId.ToString(),
                first_name = "John",
                last_name = "Doe",
                phone = "+525512345678"
            });

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = workflowId,
                CurrentStepId = stepId,
                Status = "IN_PROGRESS"
            };

            var validationResult = new Onboarding.Core.Models.ValidationResult
            {
                IsValid = true,
                Message = "Validación exitosa"
            };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _validatorMock
                .Setup(x => x.ValidateStepAsync(stepId, payloadJson))
                .ReturnsAsync(validationResult);

            _prospectDataServiceMock
                .Setup(x => x.UpdateProspectDataAsync(prospectId, payloadJson))
                .ReturnsAsync(payloadJson);

            _rulesRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rule, bool>>>()))
                .ReturnsAsync(new List<Rule>());

            // Act
            await _sut.ProcessEventAsync("StepDataSubmitted", payloadJson);

            // Assert
            _validatorMock.Verify(
                x => x.ValidateStepAsync(stepId, payloadJson),
                Times.Once,
                "Debe validar los datos del paso");

            _prospectDataServiceMock.Verify(
                x => x.UpdateProspectDataAsync(prospectId, payloadJson),
                Times.Once,
                "Debe actualizar los datos del prospecto");

            _rulesRepoMock.Verify(
                x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rule, bool>>>()),
                Times.Once,
                "Debe buscar y evaluar reglas del workflow");
        }

        /// <summary>
        /// Verifica que la submisión de datos inválidos no actualiza el prospecto
        /// y lanza una excepción apropiada.
        /// </summary>
        [Fact]
        public async Task ProcessEventAsync_StepDataSubmitted_InvalidData_ThrowsException()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var stepId = 1;
            var workflowId = 1;

            var payloadJson = JsonSerializer.Serialize(new
            {
                prospect_id = prospectId.ToString(),
                phone = "invalid-phone" // Dato inválido
            });

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = workflowId,
                CurrentStepId = stepId,
                Status = "IN_PROGRESS"
            };

            var validationResult = new Onboarding.Core.Models.ValidationResult
            {
                IsValid = false,
                Message = "Validación fallida",
                Errors = new List<Onboarding.Core.Models.ValidationError>
                {
                    new Onboarding.Core.Models.ValidationError
                    {
                        FieldKey = "phone",
                        ErrorCode = "INVALID_FORMAT",
                        Message = "El teléfono debe tener formato válido"
                    }
                }
            };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _validatorMock
                .Setup(x => x.ValidateStepAsync(stepId, payloadJson))
                .ReturnsAsync(validationResult);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.ProcessEventAsync("StepDataSubmitted", payloadJson));

            exception.Message.Should().Contain("Validación fallida");

            _prospectDataServiceMock.Verify(
                x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), It.IsAny<string>()),
                Times.Never,
                "No debe actualizar datos si la validación falla");
        }

        /// <summary>
        /// Verifica que el sistema maneja correctamente el caso donde el prospecto
        /// no tiene un paso activo asignado.
        /// </summary>
        [Fact]
        public async Task ProcessEventAsync_StepDataSubmitted_NoActiveStep_ThrowsException()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var payloadJson = JsonSerializer.Serialize(new
            {
                prospect_id = prospectId.ToString(),
                data = "test"
            });

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = 1,
                CurrentStepId = null, // Sin paso activo
                Status = "IN_PROGRESS"
            };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.ProcessEventAsync("StepDataSubmitted", payloadJson));

            exception.Message.Should().Contain("no tiene un paso activo asignado");
        }

        /// <summary>
        /// Verifica que el sistema maneja correctamente el caso donde no se encuentra
        /// el prospecto especificado.
        /// </summary>
        [Fact]
        public async Task ProcessEventAsync_StepDataSubmitted_ProspectNotFound_DoesNothing()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var payloadJson = JsonSerializer.Serialize(new
            {
                prospect_id = prospectId.ToString(),
                data = "test"
            });

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync((Prospect?)null);

            // Act
            await _sut.ProcessEventAsync("StepDataSubmitted", payloadJson);

            // Assert
            _validatorMock.Verify(
                x => x.ValidateStepAsync(It.IsAny<int>(), It.IsAny<string>()),
                Times.Never,
                "No debe validar si el prospecto no existe");

            _prospectDataServiceMock.Verify(
                x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), It.IsAny<string>()),
                Times.Never,
                "No debe actualizar datos si el prospecto no existe");
        }

        #endregion

        #region Rule Evaluation Tests

        /// <summary>
        /// Verifica que las reglas se evalúan correctamente y se ejecutan las acciones
        /// cuando las condiciones se cumplen.
        /// </summary>
        [Fact]
        public async Task ProcessEventAsync_WithMatchingRule_ExecutesAction()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var workflowId = 1;
            var payloadJson = JsonSerializer.Serialize(new
            {
                prospect_id = prospectId.ToString(),
                status = "complete"
            });

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = workflowId,
                Status = "IN_PROGRESS",
                CurrentStepId = 101
            };

            var mockRule = new Rule
            {
                RuleId = 1,
                WorkflowId = workflowId,
                TriggerEvent = "StepDataSubmitted",
                ConditionExpression = "data.status == 'complete'",
                ActionKeyOnTrue = "promote_to_golden_record"
            };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _rulesRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rule, bool>>>()))
                .ReturnsAsync(new List<Rule> { mockRule });

            _validatorMock
                .Setup(x => x.ValidateStepAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new ValidationResult { IsValid = true, Message = "Validación exitosa" });

            _prospectDataServiceMock
                .Setup(x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(payloadJson);

            _ruleEngineMock
                .Setup(x => x.Evaluate(mockRule.ConditionExpression, payloadJson))
                .Returns(true);

            _actionExecutorMock
                .Setup(x => x.ExecuteActionAsync(mockRule.ActionKeyOnTrue, prospectId, payloadJson))
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ProcessEventAsync("StepDataSubmitted", payloadJson);

            // Assert
            _ruleEngineMock.Verify(
                x => x.Evaluate(mockRule.ConditionExpression, payloadJson),
                Times.Once,
                "Debe evaluar la condición de la regla");

            _actionExecutorMock.Verify(
                x => x.ExecuteActionAsync(mockRule.ActionKeyOnTrue, prospectId, payloadJson),
                Times.Once,
                "Debe ejecutar la acción cuando la regla coincide");
        }

        /// <summary>
        /// Verifica que las acciones no se ejecutan cuando las condiciones de las reglas
        /// no se cumplen.
        /// </summary>
        [Fact]
        public async Task ProcessEventAsync_WithNonMatchingRule_DoesNotExecuteAction()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var workflowId = 1;
            var payloadJson = JsonSerializer.Serialize(new
            {
                prospect_id = prospectId.ToString(),
                status = "pending"
            });

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = workflowId,
                Status = "IN_PROGRESS",
                CurrentStepId = 101
            };

            var mockRule = new Rule
            {
                RuleId = 1,
                WorkflowId = workflowId,
                TriggerEvent = "StepDataSubmitted",
                ConditionExpression = "data.status == 'complete'",
                ActionKeyOnTrue = "promote_to_golden_record"
            };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _rulesRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rule, bool>>>()))
                .ReturnsAsync(new List<Rule> { mockRule });

            _validatorMock
                .Setup(x => x.ValidateStepAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new ValidationResult { IsValid = true, Message = "Validación exitosa" });

            _prospectDataServiceMock
                .Setup(x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(payloadJson);

            _ruleEngineMock
                .Setup(x => x.Evaluate(mockRule.ConditionExpression, payloadJson))
                .Returns(false); // Condición no se cumple

            // Act
            await _sut.ProcessEventAsync("StepDataSubmitted", payloadJson);

            // Assert
            _actionExecutorMock.Verify(
                x => x.ExecuteActionAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>()),
                Times.Never,
                "No debe ejecutar acción si la regla no coincide");
        }

        /// <summary>
        /// Verifica que se evalúan múltiples reglas cuando existen varias configuradas
        /// para el mismo evento.
        /// </summary>
        [Fact]
        public async Task ProcessEventAsync_WithMultipleRules_EvaluatesAll()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var workflowId = 1;
            var payloadJson = JsonSerializer.Serialize(new
            {
                prospect_id = prospectId.ToString(),
                status = "complete",
                score = 85
            });

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = workflowId,
                Status = "IN_PROGRESS",
                CurrentStepId = 101
            };

            var mockRules = new List<Rule>
            {
                new Rule
                {
                    RuleId = 1,
                    WorkflowId = workflowId,
                    TriggerEvent = "StepDataSubmitted",
                    ConditionExpression = "data.status == 'complete'",
                    ActionKeyOnTrue = "action1"
                },
                new Rule
                {
                    RuleId = 2,
                    WorkflowId = workflowId,
                    TriggerEvent = "StepDataSubmitted",
                    ConditionExpression = "data.score > 80",
                    ActionKeyOnTrue = "action2"
                }
            };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _rulesRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rule, bool>>>()))
                .ReturnsAsync(mockRules);

            _validatorMock
                .Setup(x => x.ValidateStepAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new ValidationResult { IsValid = true, Message = "Validación exitosa" });

            _prospectDataServiceMock
                .Setup(x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(payloadJson);

            _ruleEngineMock
                .Setup(x => x.Evaluate(It.IsAny<string>(), payloadJson))
                .Returns(true);

            _actionExecutorMock
                .Setup(x => x.ExecuteActionAsync(It.IsAny<string>(), prospectId, payloadJson))
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ProcessEventAsync("StepDataSubmitted", payloadJson);

            // Assert
            _ruleEngineMock.Verify(
                x => x.Evaluate(It.IsAny<string>(), payloadJson),
                Times.Exactly(2),
                "Debe evaluar todas las reglas configuradas");

            _actionExecutorMock.Verify(
                x => x.ExecuteActionAsync(It.IsAny<string>(), prospectId, payloadJson),
                Times.Exactly(2),
                "Debe ejecutar todas las acciones de reglas que coinciden");
        }

        #endregion
    }
}
