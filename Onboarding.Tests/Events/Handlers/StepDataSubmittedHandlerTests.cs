using Onboarding.Core.Events;
using Onboarding.Core.Events.Handlers;
using Onboarding.Core.Interfaces;
using Onboarding.Core.Models;
using OvexDataModelingTest.Entities.App;
using Xunit;
using Moq;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Onboarding.Tests.Events.Handlers
{
    /// <summary>
    /// Pruebas unitarias para StepDataSubmittedHandler.
    /// 
    /// Cobertura de pruebas:
    /// - Validación y persistencia de datos de step
    /// - Manejo de errores de validación
    /// - Validación de prospecto y step activo
    /// - Actualización del contexto
    /// </summary>
    public class StepDataSubmittedHandlerTests
    {
        private readonly Mock<IRepository<Prospect>> _prospectRepoMock;
        private readonly Mock<IStepValidationService> _validatorMock;
        private readonly Mock<IProspectDataService> _prospectDataServiceMock;
        private readonly Mock<ILogger<StepDataSubmittedHandler>> _loggerMock;
        private readonly StepDataSubmittedHandler _sut;

        public StepDataSubmittedHandlerTests()
        {
            _prospectRepoMock = new Mock<IRepository<Prospect>>();
            _validatorMock = new Mock<IStepValidationService>();
            _prospectDataServiceMock = new Mock<IProspectDataService>();
            _loggerMock = new Mock<ILogger<StepDataSubmittedHandler>>();

            _sut = new StepDataSubmittedHandler(
                _prospectRepoMock.Object,
                _validatorMock.Object,
                _prospectDataServiceMock.Object,
                _loggerMock.Object);
        }

        #region EventType Tests

        [Fact]
        public void EventType_ShouldBeStepDataSubmitted()
        {
            // Assert
            _sut.EventType.Should().Be("StepDataSubmitted");
        }

        #endregion

        #region Successful Submission Tests

        /// <summary>
        /// Verifica el flujo completo de sumisión exitosa de datos de step.
        /// </summary>
        [Fact]
        public async Task HandleAsync_ValidData_ProcessesSuccessfully()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var stepId = 101;
            var workflowId = 20;

            var payloadJson = JsonSerializer.Serialize(new
            {
                company_rfc = "FLOT101010ABC",
                clave_ciec = "TestCIEC123"
            });

            var context = new EventContext
            {
                EventType = "StepDataSubmitted",
                ProspectId = prospectId,
                PayloadJson = payloadJson
            };

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = workflowId,
                CurrentStepId = stepId,
                Status = "IN_PROGRESS"
            };

            var validationResult = new ValidationResult
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

            // Act
            await _sut.HandleAsync(context);

            // Assert
            _prospectRepoMock.Verify(
                x => x.GetByIdAsync(prospectId),
                Times.Once,
                "Debe buscar el prospecto");

            _validatorMock.Verify(
                x => x.ValidateStepAsync(stepId, payloadJson),
                Times.Once,
                "Debe validar los datos del step");

            _prospectDataServiceMock.Verify(
                x => x.UpdateProspectDataAsync(prospectId, payloadJson),
                Times.Once,
                "Debe actualizar los datos del prospecto");

            context.WorkflowId.Should().Be(workflowId, "Debe actualizar el contexto con WorkflowId");
        }

        [Fact]
        public async Task HandleAsync_WithComplexData_ValidatesAndPersists()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var stepId = 102;

            var complexPayload = new
            {
                full_name = "Juan Pérez González",
                email = "juan.perez@example.com",
                phone = "+52 55 1234 5678",
                curp = "PEGJ850101HDFRNN09",
                birth_date = "1985-01-01",
                gender = "M",
                address = new
                {
                    street = "Av. Principal 123",
                    city = "CDMX",
                    postal_code = "01234"
                }
            };

            var payloadJson = JsonSerializer.Serialize(complexPayload);

            var context = new EventContext
            {
                EventType = "StepDataSubmitted",
                ProspectId = prospectId,
                PayloadJson = payloadJson
            };

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = 20,
                CurrentStepId = stepId,
                Status = "IN_PROGRESS"
            };

            var validationResult = new ValidationResult { IsValid = true };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _validatorMock
                .Setup(x => x.ValidateStepAsync(stepId, payloadJson))
                .ReturnsAsync(validationResult);

            _prospectDataServiceMock
                .Setup(x => x.UpdateProspectDataAsync(prospectId, payloadJson))
                .ReturnsAsync(payloadJson);

            // Act
            await _sut.HandleAsync(context);

            // Assert
            _prospectDataServiceMock.Verify(
                x => x.UpdateProspectDataAsync(
                    prospectId,
                    It.Is<string>(json => json == payloadJson)),
                Times.Once,
                "Debe preservar la estructura compleja del payload");
        }

        #endregion

        #region Validation Error Tests

        [Fact]
        public async Task HandleAsync_InvalidData_ThrowsException()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var stepId = 101;

            var payloadJson = JsonSerializer.Serialize(new
            {
                company_rfc = "INVALID",
                clave_ciec = "short"
            });

            var context = new EventContext
            {
                EventType = "StepDataSubmitted",
                ProspectId = prospectId,
                PayloadJson = payloadJson
            };

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = 20,
                CurrentStepId = stepId,
                Status = "IN_PROGRESS"
            };

            var validationResult = new ValidationResult
            {
                IsValid = false,
                Message = "Validación fallida",
                Errors = new List<ValidationError>
                {
                    new ValidationError
                    {
                        FieldKey = "company_rfc",
                        ErrorCode = "INVALID_FORMAT",
                        Message = "RFC inválido"
                    },
                    new ValidationError
                    {
                        FieldKey = "clave_ciec",
                        ErrorCode = "MIN_LENGTH",
                        Message = "Longitud mínima no cumplida"
                    }
                }
            };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _validatorMock
                .Setup(x => x.ValidateStepAsync(stepId, payloadJson))
                .ReturnsAsync(validationResult);

            // Act
            Func<Task> act = async () => await _sut.HandleAsync(context);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Validación fallida*");

            _prospectDataServiceMock.Verify(
                x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), It.IsAny<string>()),
                Times.Never,
                "No debe actualizar datos si la validación falla");
        }

        [Fact]
        public async Task HandleAsync_ValidationFails_LogsErrors()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var stepId = 101;

            var context = new EventContext
            {
                EventType = "StepDataSubmitted",
                ProspectId = prospectId,
                PayloadJson = "{}"
            };

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = 20,
                CurrentStepId = stepId,
                Status = "IN_PROGRESS"
            };

            var validationResult = new ValidationResult
            {
                IsValid = false,
                Message = "Campos requeridos faltantes",
                Errors = new List<ValidationError>
                {
                    new ValidationError
                    {
                        FieldKey = "company_rfc",
                        ErrorCode = "REQUIRED",
                        Message = "Campo requerido"
                    }
                }
            };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _validatorMock
                .Setup(x => x.ValidateStepAsync(stepId, It.IsAny<string>()))
                .ReturnsAsync(validationResult);

            // Act
            Func<Task> act = async () => await _sut.HandleAsync(context);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();

            // Verificar que se loggeó la advertencia de validación
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validation failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce,
                "Debe loggear cuando la validación falla");
        }

        #endregion

        #region ProspectId Validation Tests

        [Fact]
        public async Task HandleAsync_WithoutProspectId_ThrowsException()
        {
            // Arrange
            var context = new EventContext
            {
                EventType = "StepDataSubmitted",
                ProspectId = null, // Sin ProspectId
                PayloadJson = "{}"
            };

            // Act
            Func<Task> act = async () => await _sut.HandleAsync(context);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*ProspectId es requerido*");

            _prospectRepoMock.Verify(
                x => x.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never,
                "No debe buscar prospecto si no hay ProspectId");
        }

        [Fact]
        public async Task HandleAsync_ProspectNotFound_ThrowsException()
        {
            // Arrange
            var prospectId = Guid.NewGuid();

            var context = new EventContext
            {
                EventType = "StepDataSubmitted",
                ProspectId = prospectId,
                PayloadJson = "{}"
            };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync((Prospect?)null);

            // Act
            Func<Task> act = async () => await _sut.HandleAsync(context);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"*Prospecto no encontrado: {prospectId}*");

            _validatorMock.Verify(
                x => x.ValidateStepAsync(It.IsAny<int>(), It.IsAny<string>()),
                Times.Never,
                "No debe validar si el prospecto no existe");
        }

        #endregion

        #region Active Step Validation Tests

        [Fact]
        public async Task HandleAsync_NoActiveStep_ThrowsException()
        {
            // Arrange
            var prospectId = Guid.NewGuid();

            var context = new EventContext
            {
                EventType = "StepDataSubmitted",
                ProspectId = prospectId,
                PayloadJson = "{}"
            };

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = 20,
                CurrentStepId = null, // Sin step activo
                Status = "COMPLETED"
            };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            // Act
            Func<Task> act = async () => await _sut.HandleAsync(context);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*no tiene un paso activo asignado*");

            _validatorMock.Verify(
                x => x.ValidateStepAsync(It.IsAny<int>(), It.IsAny<string>()),
                Times.Never,
                "No debe validar si no hay step activo");
        }

        #endregion

        #region Context Update Tests

        [Fact]
        public async Task HandleAsync_UpdatesContextWithWorkflowId()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var workflowId = 20;

            var context = new EventContext
            {
                EventType = "StepDataSubmitted",
                ProspectId = prospectId,
                PayloadJson = "{}",
                WorkflowId = null
            };

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = workflowId,
                CurrentStepId = 101,
                Status = "IN_PROGRESS"
            };

            var validationResult = new ValidationResult { IsValid = true };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _validatorMock
                .Setup(x => x.ValidateStepAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(validationResult);

            _prospectDataServiceMock
                .Setup(x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync("{}");

            // Act
            await _sut.HandleAsync(context);

            // Assert
            context.WorkflowId.Should().Be(workflowId, "Debe actualizar el contexto con el WorkflowId del prospecto");
        }

        #endregion

        #region Different Step Tests

        [Theory]
        [InlineData(101)]
        [InlineData(102)]
        [InlineData(201)]
        public async Task HandleAsync_ValidatesCorrectStep(int stepId)
        {
            // Arrange
            var prospectId = Guid.NewGuid();

            var context = new EventContext
            {
                EventType = "StepDataSubmitted",
                ProspectId = prospectId,
                PayloadJson = "{}"
            };

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = 20,
                CurrentStepId = stepId,
                Status = "IN_PROGRESS"
            };

            var validationResult = new ValidationResult { IsValid = true };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _validatorMock
                .Setup(x => x.ValidateStepAsync(stepId, It.IsAny<string>()))
                .ReturnsAsync(validationResult);

            _prospectDataServiceMock
                .Setup(x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync("{}");

            // Act
            await _sut.HandleAsync(context);

            // Assert
            _validatorMock.Verify(
                x => x.ValidateStepAsync(stepId, context.PayloadJson),
                Times.Once,
                $"Debe validar el step {stepId}");
        }

        #endregion

        #region Data Persistence Tests

        [Fact]
        public async Task HandleAsync_PersistsValidatedData()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var stepId = 101;

            var payloadJson = JsonSerializer.Serialize(new
            {
                field1 = "value1",
                field2 = "value2"
            });

            var context = new EventContext
            {
                EventType = "StepDataSubmitted",
                ProspectId = prospectId,
                PayloadJson = payloadJson
            };

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = 20,
                CurrentStepId = stepId,
                Status = "IN_PROGRESS"
            };

            var validationResult = new ValidationResult { IsValid = true };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _validatorMock
                .Setup(x => x.ValidateStepAsync(stepId, payloadJson))
                .ReturnsAsync(validationResult);

            _prospectDataServiceMock
                .Setup(x => x.UpdateProspectDataAsync(prospectId, payloadJson))
                .ReturnsAsync(payloadJson);

            // Act
            await _sut.HandleAsync(context);

            // Assert
            _prospectDataServiceMock.Verify(
                x => x.UpdateProspectDataAsync(
                    prospectId,
                    It.Is<string>(json => json == payloadJson)),
                Times.Once,
                "Debe persistir exactamente los datos validados");
        }

        [Fact]
        public async Task HandleAsync_OnlyPersistsAfterSuccessfulValidation()
        {
            // Arrange
            var prospectId = Guid.NewGuid();

            var context = new EventContext
            {
                EventType = "StepDataSubmitted",
                ProspectId = prospectId,
                PayloadJson = "{}"
            };

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = 20,
                CurrentStepId = 101,
                Status = "IN_PROGRESS"
            };

            // Primera llamada: validación falla
            var validationResult = new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError> { new ValidationError { FieldKey = "test" } }
            };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _validatorMock
                .Setup(x => x.ValidateStepAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(validationResult);

            // Act
            Func<Task> act = async () => await _sut.HandleAsync(context);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();

            _prospectDataServiceMock.Verify(
                x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), It.IsAny<string>()),
                Times.Never,
                "No debe persistir datos si la validación falla");
        }

        #endregion
    }
}
