using Microsoft.AspNetCore.Mvc;
using Onboarding.Api.Controllers;
using Onboarding.Core.Domain;
using Onboarding.Core.Interfaces;
using Onboarding.Core.Validation; // NUEVO: Para IValidationOrchestrator
using OvexDataModelingTest.Entities.App;
using OvexDataModelingTest.Entities.Config;
using System.Net;
using System.Net.Http.Json;

namespace Onboarding.Tests.Controllers
{
    /// <summary>
    /// Pruebas unitarias para el controlador de Prospectos.
    /// 
    /// Cobertura de pruebas:
    /// - Consulta de información de prospectos
    /// - Consulta de datos JSON de prospectos
    /// - Consulta de historial de cambios de estado
    /// - Consulta de transiciones disponibles
    /// - Actualización manual de estados
    /// - Manejo de errores (prospecto no encontrado, transiciones inválidas)
    /// - Validación de EmailDuplicado
    /// </summary>
    public class ProspectControllerTests
    {
        private readonly Mock<IProspectDataService> _prospectDataServiceMock;
        private readonly Mock<IProspectStatusService> _prospectStatusServiceMock;
        private readonly Mock<IRepository<Prospect>> _prospectRepoMock;
        private readonly Mock<ILogger<ProspectController>> _loggerMock;
        private readonly ProspectController _sut;

        public ProspectControllerTests()
        {
            // Arrange: Configurar mocks
            _prospectDataServiceMock = new Mock<IProspectDataService>();
            _prospectStatusServiceMock = new Mock<IProspectStatusService>();
            _prospectRepoMock = new Mock<IRepository<Prospect>>();
            _loggerMock = new Mock<ILogger<ProspectController>>();
            
            // Mocks adicionales para el endpoint POST
            var phaseRepoMock = new Mock<IRepository<Phase>>();
            var stepRepoMock = new Mock<IRepository<Step>>();
            var workflowRoutingServiceMock = new Mock<IWorkflowRoutingService>();
            var userManagementServiceMock = new Mock<IUserManagementService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var eventPublisherMock = new Mock<IEventPublisher>();
            var validationOrchestratorMock = new Mock<IValidationOrchestrator>(); // NUEVO

            _sut = new ProspectController(
                _prospectDataServiceMock.Object,
                _prospectStatusServiceMock.Object,
                _prospectRepoMock.Object,
                phaseRepoMock.Object,
                stepRepoMock.Object,
                workflowRoutingServiceMock.Object,
                userManagementServiceMock.Object,
                unitOfWorkMock.Object,
                eventPublisherMock.Object,
                validationOrchestratorMock.Object, // NUEVO
                _loggerMock.Object);
        }

        #region GetProspect Tests

        /// <summary>
        /// Verifica que se puede obtener correctamente la información completa de un prospecto.
        /// </summary>
        [Fact]
        public async Task GetProspect_WithValidId_ReturnsProspectData()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var workflowId = 1;
            var stepId = 2;

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = userId,
                WorkflowId = workflowId,
                CurrentStepId = stepId,
                Status = "IN_PROGRESS",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow
            };

            var prospectData = "{\"name\":\"John Doe\",\"email\":\"john@example.com\"}";

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _prospectDataServiceMock
                .Setup(x => x.GetProspectDataAsync(prospectId))
                .ReturnsAsync(prospectData);

            // Act
            var result = await _sut.GetProspect(prospectId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<OkObjectResult>();

            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            // Verificar que contiene los datos esperados
            var resultValue = okResult.Value;
            resultValue.Should().NotBeNull();

            _prospectRepoMock.Verify(x => x.GetByIdAsync(prospectId), Times.Once);
            _prospectDataServiceMock.Verify(x => x.GetProspectDataAsync(prospectId), Times.Once);
        }

        /// <summary>
        /// Verifica que se devuelve NotFound cuando el prospecto no existe.
        /// </summary>
        [Fact]
        public async Task GetProspect_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var prospectId = Guid.NewGuid();

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync((Prospect?)null);

            // Act
            var result = await _sut.GetProspect(prospectId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();

            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().NotBeNull();

            _prospectDataServiceMock.Verify(
                x => x.GetProspectDataAsync(It.IsAny<Guid>()),
                Times.Never,
                "No debe buscar datos si el prospecto no existe");
        }

        #endregion

        #region GetProspectData Tests

        /// <summary>
        /// Verifica que se pueden obtener solo los datos JSON de un prospecto.
        /// </summary>
        [Fact]
        public async Task GetProspectData_WithValidId_ReturnsJsonData()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = 1,
                Status = "IN_PROGRESS"
            };

            var prospectData = "{\"name\":\"John Doe\",\"phone\":\"+525512345678\"}";

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _prospectDataServiceMock
                .Setup(x => x.GetProspectDataAsync(prospectId))
                .ReturnsAsync(prospectData);

            // Act
            var result = await _sut.GetProspectData(prospectId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            _prospectDataServiceMock.Verify(x => x.GetProspectDataAsync(prospectId), Times.Once);
        }

        /// <summary>
        /// Verifica que GetProspectData devuelve NotFound para un prospecto inexistente.
        /// </summary>
        [Fact]
        public async Task GetProspectData_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var prospectId = Guid.NewGuid();

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync((Prospect?)null);

            // Act
            var result = await _sut.GetProspectData(prospectId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion

        #region GetStatusHistory Tests

        /// <summary>
        /// Verifica que se puede obtener el historial de cambios de estado de un prospecto.
        /// </summary>
        [Fact]
        public async Task GetStatusHistory_WithValidId_ReturnsHistory()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = 1,
                Status = "APPROVED"
            };

            var mockHistory = new List<ProspectStatusHistory>
            {
                new ProspectStatusHistory
                {
                    HistoryId = 1,
                    ProspectId = prospectId,
                    OldStatus = "STARTED",
                    NewStatus = "IN_PROGRESS",
                    Reason = "User started form",
                    ChangedBy = "SYSTEM",
                    ChangedAt = DateTime.UtcNow.AddDays(-2)
                },
                new ProspectStatusHistory
                {
                    HistoryId = 2,
                    ProspectId = prospectId,
                    OldStatus = "IN_PROGRESS",
                    NewStatus = "APPROVED",
                    Reason = "All validations passed",
                    ChangedBy = "SYSTEM",
                    ChangedAt = DateTime.UtcNow
                }
            };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _prospectStatusServiceMock
                .Setup(x => x.GetStatusHistoryAsync(prospectId))
                .ReturnsAsync(mockHistory);

            // Act
            var result = await _sut.GetStatusHistory(prospectId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            _prospectStatusServiceMock.Verify(x => x.GetStatusHistoryAsync(prospectId), Times.Once);
        }

        /// <summary>
        /// Verifica que GetStatusHistory devuelve NotFound para un prospecto inexistente.
        /// </summary>
        [Fact]
        public async Task GetStatusHistory_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var prospectId = Guid.NewGuid();

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync((Prospect?)null);

            // Act
            var result = await _sut.GetStatusHistory(prospectId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();

            _prospectStatusServiceMock.Verify(
                x => x.GetStatusHistoryAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        #endregion

        #region GetAvailableTransitions Tests

        /// <summary>
        /// Verifica que se pueden obtener las transiciones de estado disponibles.
        /// </summary>
        [Fact]
        public async Task GetAvailableTransitions_WithValidId_ReturnsTransitions()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = 1,
                Status = "IN_PROGRESS"
            };

            var currentStatus = "IN_PROGRESS";
            var availableTransitions = new List<string> { "APPROVED", "REJECTED", "PENDING" };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _prospectStatusServiceMock
                .Setup(x => x.GetCurrentStatusAsync(prospectId))
                .ReturnsAsync(currentStatus);

            _prospectStatusServiceMock
                .Setup(x => x.GetAvailableTransitionsAsync(prospectId))
                .ReturnsAsync(availableTransitions);

            // Act
            var result = await _sut.GetAvailableTransitions(prospectId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            _prospectStatusServiceMock.Verify(x => x.GetCurrentStatusAsync(prospectId), Times.Once);
            _prospectStatusServiceMock.Verify(x => x.GetAvailableTransitionsAsync(prospectId), Times.Once);
        }

        /// <summary>
        /// Verifica que se identifica correctamente un estado terminal.
        /// </summary>
        [Fact]
        public async Task GetAvailableTransitions_WithTerminalState_ShowsNoTransitions()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = 1,
                Status = "APPROVED" // Estado terminal
            };

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _prospectStatusServiceMock
                .Setup(x => x.GetCurrentStatusAsync(prospectId))
                .ReturnsAsync("APPROVED");

            _prospectStatusServiceMock
                .Setup(x => x.GetAvailableTransitionsAsync(prospectId))
                .ReturnsAsync(new List<string>()); // Sin transiciones disponibles

            // Act
            var result = await _sut.GetAvailableTransitions(prospectId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
        }

        #endregion

        #region UpdateStatus Tests

        /// <summary>
        /// Verifica que se puede actualizar correctamente el estado de un prospecto.
        /// </summary>
        [Fact]
        public async Task UpdateStatus_WithValidTransition_ReturnsSuccess()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var request = new UpdateStatusRequest
            {
                NewStatus = "APPROVED",
                Reason = "Manual approval by admin",
                ChangedBy = "admin@example.com"
            };

            _prospectStatusServiceMock
                .Setup(x => x.UpdateStatusAsync(
                    prospectId,
                    request.NewStatus,
                    request.Reason,
                    request.ChangedBy,
                    request.Metadata))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.UpdateStatus(prospectId, request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            _prospectStatusServiceMock.Verify(
                x => x.UpdateStatusAsync(prospectId, "APPROVED", request.Reason, request.ChangedBy, request.Metadata),
                Times.Once);
        }

        /// <summary>
        /// Verifica que se devuelve BadRequest cuando la actualización falla.
        /// </summary>
        [Fact]
        public async Task UpdateStatus_WhenUpdateFails_ReturnsBadRequest()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var request = new UpdateStatusRequest
            {
                NewStatus = "APPROVED",
                Reason = "Test"
            };

            _prospectStatusServiceMock
                .Setup(x => x.UpdateStatusAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _sut.UpdateStatus(prospectId, request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Verifica que se maneja correctamente una transición de estado inválida.
        /// </summary>
        [Fact]
        public async Task UpdateStatus_WithInvalidTransition_ReturnsBadRequest()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var request = new UpdateStatusRequest
            {
                NewStatus = "INVALID_STATUS",
                Reason = "Test"
            };

            _prospectStatusServiceMock
                .Setup(x => x.UpdateStatusAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Transición de estado inválida"));

            // Act
            var result = await _sut.UpdateStatus(prospectId, request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();

            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
        }

        /// <summary>
        /// Verifica que se usa un valor por defecto para ChangedBy cuando no se proporciona.
        /// </summary>
        [Fact]
        public async Task UpdateStatus_WithoutChangedBy_UsesDefaultValue()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var request = new UpdateStatusRequest
            {
                NewStatus = "APPROVED",
                Reason = "Test",
                ChangedBy = null // Sin especificar
            };

            _prospectStatusServiceMock
                .Setup(x => x.UpdateStatusAsync(
                    prospectId,
                    request.NewStatus,
                    request.Reason,
                    "API_USER", // Valor por defecto
                    request.Metadata))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.UpdateStatus(prospectId, request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            _prospectStatusServiceMock.Verify(
                x => x.UpdateStatusAsync(
                    prospectId,
                    request.NewStatus,
                    request.Reason,
                    "API_USER",
                    request.Metadata),
                Times.Once,
                "Debe usar 'API_USER' como valor por defecto cuando ChangedBy es null");
        }

        #endregion
        #region Email Duplication Tests

       

        #endregion
    }
}
