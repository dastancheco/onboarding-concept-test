using Onboarding.Core.Events;
using Onboarding.Core.Events.Handlers;
using Onboarding.Core.Interfaces;
using Onboarding.Core.Validation; // NUEVO
using OvexDataModelingTest.Entities.App;
using Xunit;
using Moq;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Onboarding.Tests.Events.Handlers
{
    /// <summary>
    /// Pruebas unitarias para UserRegisteredHandler.
    /// 
    /// Cobertura de pruebas:
    /// - Creación exitosa de usuario y prospecto
    /// - Manejo de errores al extraer email
    /// - Manejo de errores cuando no hay workflow
    /// - Validación de datos del payload
    /// - Actualización correcta del contexto
    /// </summary>
    public class UserRegisteredHandlerTests
    {
        private readonly Mock<IUserManagementService> _userManagementServiceMock;
        private readonly Mock<IWorkflowRoutingService> _workflowRoutingServiceMock;
        private readonly Mock<IRepository<Prospect>> _prospectRepoMock;
        private readonly Mock<IProspectDataService> _prospectDataServiceMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IValidationOrchestrator> _validationOrchestratorMock; // NUEVO
        private readonly Mock<ILogger<UserRegisteredHandler>> _loggerMock;
        private readonly UserRegisteredHandler _sut;

        public UserRegisteredHandlerTests()
        {
            _userManagementServiceMock = new Mock<IUserManagementService>();
            _workflowRoutingServiceMock = new Mock<IWorkflowRoutingService>();
            _prospectRepoMock = new Mock<IRepository<Prospect>>();
            _prospectDataServiceMock = new Mock<IProspectDataService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _validationOrchestratorMock = new Mock<IValidationOrchestrator>(); // NUEVO
            _loggerMock = new Mock<ILogger<UserRegisteredHandler>>();

            _sut = new UserRegisteredHandler(
                _userManagementServiceMock.Object,
                _workflowRoutingServiceMock.Object,
                _prospectRepoMock.Object,
                _prospectDataServiceMock.Object,
                _unitOfWorkMock.Object,
                _validationOrchestratorMock.Object, // NUEVO
                _loggerMock.Object);
        }

        #region EventType Tests

        [Fact]
        public void EventType_ShouldBeUserRegistered()
        {
            // Assert
            _sut.EventType.Should().Be("UserRegistered");
        }

        #endregion

        #region Successful Registration Tests

        /// <summary>
        /// Verifica el flujo completo de registro exitoso:
        /// 1. Extraer email
        /// 2. Crear/recuperar usuario
        /// 3. Determinar workflow
        /// 4. Crear prospecto
        /// 5. Inicializar datos
        /// 6. Actualizar contexto
        /// </summary>
        [Fact]
        public async Task HandleAsync_ValidUser_CreatesProspectSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workflowId = 20;
            var email = "test@example.com";

            var context = new EventContext
            {
                EventType = "UserRegistered",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    email = email,
                    name = "Test User",
                    country = "MX"
                })
            };

            var mockUser = new User
            {
                UserId = userId,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            _userManagementServiceMock
                .Setup(x => x.ExtractEmailFromPayload(It.IsAny<string>()))
                .Returns(email);

            _userManagementServiceMock
                .Setup(x => x.GetOrCreateUserAsync(email, It.IsAny<string>()))
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
                .ReturnsAsync(context.PayloadJson);

            // Act
            await _sut.HandleAsync(context);

            // Assert
            _userManagementServiceMock.Verify(
                x => x.ExtractEmailFromPayload(context.PayloadJson),
                Times.Once,
                "Debe extraer el email del payload");

            _userManagementServiceMock.Verify(
                x => x.GetOrCreateUserAsync(email, context.PayloadJson),
                Times.Once,
                "Debe crear o recuperar el usuario");

            _workflowRoutingServiceMock.Verify(
                x => x.DetermineWorkflowAsync(context.PayloadJson),
                Times.Once,
                "Debe determinar el workflow apropiado");

            _prospectRepoMock.Verify(
                x => x.AddAsync(It.Is<Prospect>(p =>
                    p.UserId == userId &&
                    p.WorkflowId == workflowId &&
                    p.Status == "STARTED")),
                Times.Once,
                "Debe crear un nuevo prospecto con datos correctos");

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once,
                "Debe guardar los cambios en la base de datos");

            _prospectDataServiceMock.Verify(
                x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), context.PayloadJson),
                Times.Once,
                "Debe inicializar los datos del prospecto");

            // Verificar que el contexto se actualizó
            context.ProspectId.Should().NotBeNull("El contexto debe contener el ProspectId");
            context.WorkflowId.Should().Be(workflowId, "El contexto debe contener el WorkflowId");
        }

        [Fact]
        public async Task HandleAsync_WithComplexPayload_PreservesAllData()
        {
            // Arrange
            var complexPayload = new
            {
                email = "complex@example.com",
                name = "Complex User",
                country = "MX",
                phone = "+52 55 1234 5678",
                metadata = new
                {
                    source = "mobile_app",
                    version = "2.0"
                }
            };

            var context = new EventContext
            {
                EventType = "UserRegistered",
                PayloadJson = JsonSerializer.Serialize(complexPayload)
            };

            var mockUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = "complex@example.com"
            };

            _userManagementServiceMock
                .Setup(x => x.ExtractEmailFromPayload(It.IsAny<string>()))
                .Returns("complex@example.com");

            _userManagementServiceMock
                .Setup(x => x.GetOrCreateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockUser);

            _workflowRoutingServiceMock
                .Setup(x => x.DetermineWorkflowAsync(It.IsAny<string>()))
                .ReturnsAsync(10);

            _prospectRepoMock
                .Setup(x => x.AddAsync(It.IsAny<Prospect>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            _prospectDataServiceMock
                .Setup(x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(context.PayloadJson);

            // Act
            await _sut.HandleAsync(context);

            // Assert
            _prospectDataServiceMock.Verify(
                x => x.UpdateProspectDataAsync(
                    It.IsAny<Guid>(),
                    It.Is<string>(json => json == context.PayloadJson)),
                Times.Once,
                "Debe preservar todo el payload original");
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task HandleAsync_WithoutEmail_ThrowsException()
        {
            // Arrange
            var context = new EventContext
            {
                EventType = "UserRegistered",
                PayloadJson = JsonSerializer.Serialize(new { name = "User Without Email" })
            };

            _userManagementServiceMock
                .Setup(x => x.ExtractEmailFromPayload(It.IsAny<string>()))
                .Returns((string?)null);

            // Act
            Func<Task> act = async () => await _sut.HandleAsync(context);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Email no encontrado*");

            _prospectRepoMock.Verify(
                x => x.AddAsync(It.IsAny<Prospect>()),
                Times.Never,
                "No debe crear prospecto si no hay email");

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never,
                "No debe guardar cambios si falta email");
        }

        [Fact]
        public async Task HandleAsync_WithEmptyEmail_ThrowsException()
        {
            // Arrange
            var context = new EventContext
            {
                EventType = "UserRegistered",
                PayloadJson = JsonSerializer.Serialize(new { email = "" })
            };

            _userManagementServiceMock
                .Setup(x => x.ExtractEmailFromPayload(It.IsAny<string>()))
                .Returns(string.Empty);

            // Act
            Func<Task> act = async () => await _sut.HandleAsync(context);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Email no encontrado*");
        }

        [Fact]
        public async Task HandleAsync_NoWorkflowMatch_ThrowsException()
        {
            // Arrange
            var context = new EventContext
            {
                EventType = "UserRegistered",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    email = "test@example.com",
                    country = "UNKNOWN"
                })
            };

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
            Func<Task> act = async () => await _sut.HandleAsync(context);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*No se encontró workflow*");

            _prospectRepoMock.Verify(
                x => x.AddAsync(It.IsAny<Prospect>()),
                Times.Never,
                "No debe crear prospecto si no hay workflow");

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never,
                "No debe guardar cambios si no hay workflow");
        }

        [Fact]
        public async Task HandleAsync_UserServiceThrowsException_PropagatesException()
        {
            // Arrange
            var context = new EventContext
            {
                EventType = "UserRegistered",
                PayloadJson = JsonSerializer.Serialize(new { email = "test@example.com" })
            };

            _userManagementServiceMock
                .Setup(x => x.ExtractEmailFromPayload(It.IsAny<string>()))
                .Returns("test@example.com");

            _userManagementServiceMock
                .Setup(x => x.GetOrCreateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            Func<Task> act = async () => await _sut.HandleAsync(context);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Database connection error");

            _prospectRepoMock.Verify(
                x => x.AddAsync(It.IsAny<Prospect>()),
                Times.Never,
                "No debe intentar crear prospecto si falla la creación del usuario");
        }

        #endregion

        #region Context Update Tests

        [Fact]
        public async Task HandleAsync_UpdatesContextWithProspectAndWorkflow()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workflowId = 10;

            var context = new EventContext
            {
                EventType = "UserRegistered",
                PayloadJson = JsonSerializer.Serialize(new { email = "test@example.com" }),
                ProspectId = null,
                WorkflowId = null
            };

            var mockUser = new User { UserId = userId, Email = "test@example.com" };

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
                .ReturnsAsync(context.PayloadJson);

            // Act
            await _sut.HandleAsync(context);

            // Assert
            context.ProspectId.Should().NotBeNull("Debe establecer ProspectId en el contexto");
            context.WorkflowId.Should().Be(workflowId, "Debe establecer WorkflowId en el contexto");
        }

        #endregion

        #region Workflow Assignment Tests

        [Theory]
        [InlineData(10)]
        [InlineData(20)]
        [InlineData(30)]
        public async Task HandleAsync_AssignsCorrectWorkflow(int expectedWorkflowId)
        {
            // Arrange
            var context = new EventContext
            {
                EventType = "UserRegistered",
                PayloadJson = JsonSerializer.Serialize(new { email = "test@example.com" })
            };

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
                .ReturnsAsync(expectedWorkflowId);

            _prospectRepoMock
                .Setup(x => x.AddAsync(It.IsAny<Prospect>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            _prospectDataServiceMock
                .Setup(x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(context.PayloadJson);

            // Act
            await _sut.HandleAsync(context);

            // Assert
            _prospectRepoMock.Verify(
                x => x.AddAsync(It.Is<Prospect>(p => p.WorkflowId == expectedWorkflowId)),
                Times.Once,
                $"Debe crear prospecto con WorkflowId {expectedWorkflowId}");
        }

        #endregion

        #region Prospect Data Tests

        [Fact]
        public async Task HandleAsync_CreatesProspectWithCorrectStatus()
        {
            // Arrange
            var context = new EventContext
            {
                EventType = "UserRegistered",
                PayloadJson = JsonSerializer.Serialize(new { email = "test@example.com" })
            };

            var mockUser = new User { UserId = Guid.NewGuid(), Email = "test@example.com" };

            _userManagementServiceMock
                .Setup(x => x.ExtractEmailFromPayload(It.IsAny<string>()))
                .Returns("test@example.com");

            _userManagementServiceMock
                .Setup(x => x.GetOrCreateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockUser);

            _workflowRoutingServiceMock
                .Setup(x => x.DetermineWorkflowAsync(It.IsAny<string>()))
                .ReturnsAsync(10);

            _prospectRepoMock
                .Setup(x => x.AddAsync(It.IsAny<Prospect>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            _prospectDataServiceMock
                .Setup(x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(context.PayloadJson);

            // Act
            await _sut.HandleAsync(context);

            // Assert
            _prospectRepoMock.Verify(
                x => x.AddAsync(It.Is<Prospect>(p =>
                    p.Status == "STARTED" &&
                    p.CreatedAt != default)),
                Times.Once,
                "Debe crear prospecto con status STARTED y fecha de creación");
        }

        [Fact]
        public async Task HandleAsync_GeneratesNewProspectId()
        {
            // Arrange
            var context = new EventContext
            {
                EventType = "UserRegistered",
                PayloadJson = JsonSerializer.Serialize(new { email = "test@example.com" })
            };

            var mockUser = new User { UserId = Guid.NewGuid(), Email = "test@example.com" };

            _userManagementServiceMock
                .Setup(x => x.ExtractEmailFromPayload(It.IsAny<string>()))
                .Returns("test@example.com");

            _userManagementServiceMock
                .Setup(x => x.GetOrCreateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockUser);

            _workflowRoutingServiceMock
                .Setup(x => x.DetermineWorkflowAsync(It.IsAny<string>()))
                .ReturnsAsync(10);

            _prospectRepoMock
                .Setup(x => x.AddAsync(It.IsAny<Prospect>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            _prospectDataServiceMock
                .Setup(x => x.UpdateProspectDataAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(context.PayloadJson);

            // Act
            await _sut.HandleAsync(context);

            // Assert
            _prospectRepoMock.Verify(
                x => x.AddAsync(It.Is<Prospect>(p => p.ProspectId != Guid.Empty)),
                Times.Once,
                "Debe generar un ProspectId único");
        }

        #endregion
    }
}
