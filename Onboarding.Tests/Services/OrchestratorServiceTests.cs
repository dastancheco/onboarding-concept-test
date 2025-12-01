using Onboarding.Core.Interfaces;
using Onboarding.Core.Models;
using Onboarding.Core.Services;
using Onboarding.Core.Events;
using OvexDataModelingTest.Entities.App;
using OvexDataModelingTest.Entities.Config;
using System.Text.Json;

namespace Onboarding.Tests.Services
{
    /// <summary>
    /// Pruebas unitarias para el servicio OrchestratorService (REFACTORIZADO con Strategy Pattern).
    /// 
    /// Cobertura de pruebas:
    /// - Procesamiento de eventos con handlers específicos
    /// - Evaluación de reglas de negocio
    /// - Manejo de errores y casos excepcionales
    /// - Integración con Event Handlers y Factory
    /// </summary>
    public class OrchestratorServiceTests
    {
        private readonly Mock<IEventHandlerFactory> _handlerFactoryMock;
        private readonly Mock<IRepository<Rule>> _rulesRepoMock;
        private readonly Mock<IRepository<Prospect>> _prospectRepoMock;
        private readonly Mock<IActionExecutor> _actionExecutorMock;
        private readonly Mock<IRuleEngine> _ruleEngineMock;
        private readonly Mock<ILogger<OrchestratorService>> _loggerMock;
        private readonly OrchestratorService _sut; // System Under Test

        public OrchestratorServiceTests()
        {
            // Arrange: Configurar todos los mocks necesarios
            _handlerFactoryMock = new Mock<IEventHandlerFactory>();
            _rulesRepoMock = new Mock<IRepository<Rule>>();
            _prospectRepoMock = new Mock<IRepository<Prospect>>();
            _actionExecutorMock = new Mock<IActionExecutor>();
            _ruleEngineMock = new Mock<IRuleEngine>();
            _loggerMock = new Mock<ILogger<OrchestratorService>>();

            // Crear instancia del servicio con las nuevas dependencias
            _sut = new OrchestratorService(
                _handlerFactoryMock.Object,
                _rulesRepoMock.Object,
                _prospectRepoMock.Object,
                _actionExecutorMock.Object,
                _ruleEngineMock.Object,
                _loggerMock.Object);
        }

        #region Event Processing Tests

        /// <summary>
        /// Verifica que un evento con handler específico se procesa correctamente.
        /// </summary>
        [Fact]
        public async Task ProcessEventAsync_WithRegisteredHandler_ExecutesHandlerAndRules()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var workflowId = 1;
            var payloadJson = JsonSerializer.Serialize(new
            {
                prospect_id = prospectId.ToString(),
                data = "test"
            });

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = workflowId,
                Status = "IN_PROGRESS",
                CurrentStepId = 101
            };

            var mockHandler = new Mock<IEventHandler>();
            mockHandler.Setup(h => h.EventType).Returns("TestEvent");
            mockHandler.Setup(h => h.CanHandle("TestEvent")).Returns(true);
            mockHandler.Setup(h => h.HandleAsync(It.IsAny<EventContext>()))
                .Callback<EventContext>(ctx =>
                {
                    ctx.ProspectId = prospectId;
                    ctx.WorkflowId = workflowId;
                })
                .Returns(Task.CompletedTask);

            _handlerFactoryMock
                .Setup(x => x.GetHandler("TestEvent"))
                .Returns(mockHandler.Object);

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _rulesRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rule, bool>>>()))
                .ReturnsAsync(new List<Rule>());

            // Act
            await _sut.ProcessEventAsync("TestEvent", payloadJson);

            // Assert
            mockHandler.Verify(
                h => h.HandleAsync(It.Is<EventContext>(ctx =>
                    ctx.EventType == "TestEvent" &&
                    ctx.PayloadJson == payloadJson)),
                Times.Once,
                "Debe ejecutar el handler específico");

            _rulesRepoMock.Verify(
                x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rule, bool>>>()),
                Times.Once,
                "Debe evaluar reglas después del handler");
        }

        /// <summary>
        /// Verifica que un evento sin handler registrado continúa con la evaluación de reglas.
        /// </summary>
        [Fact]
        public async Task ProcessEventAsync_WithoutRegisteredHandler_SkipsHandlerExecution()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var payloadJson = JsonSerializer.Serialize(new
            {
                prospect_id = prospectId.ToString()
            });

            var mockProspect = new Prospect
            {
                ProspectId = prospectId,
                UserId = Guid.NewGuid(),
                WorkflowId = 1,
                Status = "IN_PROGRESS"
            };

            _handlerFactoryMock
                .Setup(x => x.GetHandler("UnknownEvent"))
                .Returns((IEventHandler?)null);

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _rulesRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rule, bool>>>()))
                .ReturnsAsync(new List<Rule>());

            // Act
            await _sut.ProcessEventAsync("UnknownEvent", payloadJson);

            // Assert
            _rulesRepoMock.Verify(
                x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rule, bool>>>()),
                Times.Once,
                "Debe evaluar reglas incluso sin handler específico");
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
                TriggerEvent = "TestEvent",
                ConditionExpression = "data.status == 'complete'",
                ActionKeyOnTrue = "test_action"
            };

            var mockHandler = new Mock<IEventHandler>();
            mockHandler.Setup(h => h.EventType).Returns("TestEvent");
            mockHandler.Setup(h => h.HandleAsync(It.IsAny<EventContext>()))
                .Callback<EventContext>(ctx =>
                {
                    ctx.ProspectId = prospectId;
                    ctx.WorkflowId = workflowId;
                })
                .Returns(Task.CompletedTask);

            _handlerFactoryMock
                .Setup(x => x.GetHandler("TestEvent"))
                .Returns(mockHandler.Object);

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _rulesRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rule, bool>>>()))
                .ReturnsAsync(new List<Rule> { mockRule });

            _ruleEngineMock
                .Setup(x => x.Evaluate(mockRule.ConditionExpression, payloadJson))
                .Returns(true);

            _actionExecutorMock
                .Setup(x => x.ExecuteActionAsync(mockRule.ActionKeyOnTrue, prospectId, payloadJson))
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ProcessEventAsync("TestEvent", payloadJson);

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
        /// Verifica que se evalúan múltiples reglas cuando existen varias configuradas.
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
                Status = "IN_PROGRESS"
            };

            var mockRules = new List<Rule>
            {
                new Rule
                {
                    RuleId = 1,
                    WorkflowId = workflowId,
                    TriggerEvent = "TestEvent",
                    ConditionExpression = "data.status == 'complete'",
                    ActionKeyOnTrue = "action1"
                },
                new Rule
                {
                    RuleId = 2,
                    WorkflowId = workflowId,
                    TriggerEvent = "TestEvent",
                    ConditionExpression = "data.score > 80",
                    ActionKeyOnTrue = "action2"
                }
            };

            var mockHandler = new Mock<IEventHandler>();
            mockHandler.Setup(h => h.HandleAsync(It.IsAny<EventContext>()))
                .Callback<EventContext>(ctx =>
                {
                    ctx.ProspectId = prospectId;
                    ctx.WorkflowId = workflowId;
                })
                .Returns(Task.CompletedTask);

            _handlerFactoryMock
                .Setup(x => x.GetHandler("TestEvent"))
                .Returns(mockHandler.Object);

            _prospectRepoMock
                .Setup(x => x.GetByIdAsync(prospectId))
                .ReturnsAsync(mockProspect);

            _rulesRepoMock
                .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rule, bool>>>()))
                .ReturnsAsync(mockRules);

            _ruleEngineMock
                .Setup(x => x.Evaluate(It.IsAny<string>(), payloadJson))
                .Returns(true);

            _actionExecutorMock
                .Setup(x => x.ExecuteActionAsync(It.IsAny<string>(), prospectId, payloadJson))
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ProcessEventAsync("TestEvent", payloadJson);

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
