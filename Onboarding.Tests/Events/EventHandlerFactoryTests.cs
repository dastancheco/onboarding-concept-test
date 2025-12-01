using Onboarding.Core.Events;
using Xunit;
using Moq;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Onboarding.Tests.Events
{
    /// <summary>
    /// Pruebas unitarias para EventHandlerFactory.
    /// 
    /// Cobertura de pruebas:
    /// - Resolución correcta de handlers por tipo
    /// - Manejo de eventos sin handler
    /// - Registro de múltiples handlers
    /// - Verificación de handlers registrados
    /// </summary>
    public class EventHandlerFactoryTests
    {
        private readonly Mock<ILogger<EventHandlerFactory>> _loggerMock;

        public EventHandlerFactoryTests()
        {
            _loggerMock = new Mock<ILogger<EventHandlerFactory>>();
        }

        #region GetHandler Tests

        [Fact]
        public void GetHandler_WithRegisteredHandler_ReturnsCorrectHandler()
        {
            // Arrange
            var mockHandler = CreateMockHandler("TestEvent");
            var handlers = new List<IEventHandler> { mockHandler.Object };

            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act
            var result = factory.GetHandler("TestEvent");

            // Assert
            result.Should().NotBeNull("Debe encontrar un handler registrado");
            result!.EventType.Should().Be("TestEvent");
        }

        [Fact]
        public void GetHandler_WithUnregisteredEvent_ReturnsNull()
        {
            // Arrange
            var mockHandler = CreateMockHandler("TestEvent");
            var handlers = new List<IEventHandler> { mockHandler.Object };

            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act
            var result = factory.GetHandler("UnknownEvent");

            // Assert
            result.Should().BeNull("No debe encontrar handler para evento no registrado");
        }

        [Fact]
        public void GetHandler_WithMultipleHandlers_ReturnsCorrectOne()
        {
            // Arrange
            var handler1 = CreateMockHandler("Event1");
            var handler2 = CreateMockHandler("Event2");
            var handler3 = CreateMockHandler("Event3");

            var handlers = new List<IEventHandler>
            {
                handler1.Object,
                handler2.Object,
                handler3.Object
            };

            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act
            var result = factory.GetHandler("Event2");

            // Assert
            result.Should().NotBeNull();
            result!.EventType.Should().Be("Event2");
            result.Should().BeSameAs(handler2.Object, "Debe retornar el handler correcto");
        }

        [Fact]
        public void GetHandler_CaseInsensitive_FindsHandler()
        {
            // Arrange
            var mockHandler = CreateMockHandler("TestEvent");
            var handlers = new List<IEventHandler> { mockHandler.Object };

            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act
            var result1 = factory.GetHandler("TestEvent");
            var result2 = factory.GetHandler("testevent");
            var result3 = factory.GetHandler("TESTEVENT");

            // Assert
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result3.Should().NotBeNull();
            result1.Should().BeSameAs(result2);
            result2.Should().BeSameAs(result3);
        }

        #endregion

        #region HasHandler Tests

        [Fact]
        public void HasHandler_WithRegisteredHandler_ReturnsTrue()
        {
            // Arrange
            var mockHandler = CreateMockHandler("TestEvent");
            var handlers = new List<IEventHandler> { mockHandler.Object };

            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act
            var result = factory.HasHandler("TestEvent");

            // Assert
            result.Should().BeTrue("Debe indicar que el handler existe");
        }

        [Fact]
        public void HasHandler_WithUnregisteredEvent_ReturnsFalse()
        {
            // Arrange
            var mockHandler = CreateMockHandler("TestEvent");
            var handlers = new List<IEventHandler> { mockHandler.Object };

            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act
            var result = factory.HasHandler("UnknownEvent");

            // Assert
            result.Should().BeFalse("Debe indicar que el handler no existe");
        }

        [Fact]
        public void HasHandler_CaseInsensitive_ReturnsTrue()
        {
            // Arrange
            var mockHandler = CreateMockHandler("TestEvent");
            var handlers = new List<IEventHandler> { mockHandler.Object };

            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act
            var result1 = factory.HasHandler("TestEvent");
            var result2 = factory.HasHandler("testevent");
            var result3 = factory.HasHandler("TESTEVENT");

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeTrue();
            result3.Should().BeTrue();
        }

        #endregion

        #region Multiple Handlers Tests

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void Constructor_WithVariableHandlerCount_WorksCorrectly(int handlerCount)
        {
            // Arrange
            var handlers = new List<IEventHandler>();
            for (int i = 0; i < handlerCount; i++)
            {
                handlers.Add(CreateMockHandler($"Event{i}").Object);
            }

            // Act
            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Assert
            for (int i = 0; i < handlerCount; i++)
            {
                factory.HasHandler($"Event{i}").Should().BeTrue($"Debe tener handler para Event{i}");
            }
        }

        [Fact]
        public void Constructor_WithEmptyHandlers_DoesNotThrow()
        {
            // Arrange
            var handlers = new List<IEventHandler>();

            // Act
            Action act = () => new EventHandlerFactory(handlers, _loggerMock.Object);

            // Assert
            act.Should().NotThrow("Debe manejar lista vacía de handlers");
        }

        [Fact]
        public void GetHandler_WithNoHandlers_ReturnsNull()
        {
            // Arrange
            var handlers = new List<IEventHandler>();
            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act
            var result = factory.GetHandler("AnyEvent");

            // Assert
            result.Should().BeNull("No debe encontrar handler cuando no hay ninguno registrado");
        }

        #endregion

        #region Logging Tests

        [Fact]
        public void Constructor_LogsRegisteredHandlers()
        {
            // Arrange
            var handler1 = CreateMockHandler("Event1");
            var handler2 = CreateMockHandler("Event2");
            var handlers = new List<IEventHandler> { handler1.Object, handler2.Object };

            // Act
            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("initialized with 2 handlers")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "Debe loggear el número de handlers registrados");
        }

        [Fact]
        public void GetHandler_WhenNotFound_LogsWarning()
        {
            // Arrange
            var mockHandler = CreateMockHandler("TestEvent");
            var handlers = new List<IEventHandler> { mockHandler.Object };
            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act
            var result = factory.GetHandler("UnknownEvent");

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No handler found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "Debe loggear warning cuando no encuentra handler");
        }

        [Fact]
        public void GetHandler_WhenFound_LogsDebug()
        {
            // Arrange
            var mockHandler = CreateMockHandler("TestEvent");
            var handlers = new List<IEventHandler> { mockHandler.Object };
            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act
            var result = factory.GetHandler("TestEvent");

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Resolved handler")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "Debe loggear debug cuando encuentra handler");
        }

        #endregion

        #region Handler Registration Tests

        [Fact]
        public void GetHandler_WithDuplicateEventTypes_ReturnsFirstRegistered()
        {
            // Arrange
            var handler1 = CreateMockHandler("DuplicateEvent");
            var handler2 = CreateMockHandler("DuplicateEvent");

            var handlers = new List<IEventHandler>
            {
                handler1.Object,
                handler2.Object
            };

            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act
            var result = factory.GetHandler("DuplicateEvent");

            // Assert
            result.Should().BeSameAs(handler1.Object, "Debe retornar el primer handler registrado");
        }

        [Fact]
        public void GetHandler_WithCommonEventTypes_ReturnsCorrectHandler()
        {
            // Arrange
            var userRegisteredHandler = CreateMockHandler("UserRegistered");
            var stepSubmittedHandler = CreateMockHandler("StepDataSubmitted");
            var creditCheckHandler = CreateMockHandler("CreditCheckComplete");

            var handlers = new List<IEventHandler>
            {
                userRegisteredHandler.Object,
                stepSubmittedHandler.Object,
                creditCheckHandler.Object
            };

            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act & Assert
            factory.GetHandler("UserRegistered").Should().BeSameAs(userRegisteredHandler.Object);
            factory.GetHandler("StepDataSubmitted").Should().BeSameAs(stepSubmittedHandler.Object);
            factory.GetHandler("CreditCheckComplete").Should().BeSameAs(creditCheckHandler.Object);
        }

        #endregion

        #region Edge Cases Tests

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void GetHandler_WithInvalidEventType_ReturnsNull(string eventType)
        {
            // Arrange
            var mockHandler = CreateMockHandler("TestEvent");
            var handlers = new List<IEventHandler> { mockHandler.Object };
            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act
            var result = factory.GetHandler(eventType);

            // Assert
            result.Should().BeNull("Debe retornar null para tipos de evento inválidos");
        }

        [Fact]
        public void GetHandler_WithNullEventType_ReturnsNull()
        {
            // Arrange
            var mockHandler = CreateMockHandler("TestEvent");
            var handlers = new List<IEventHandler> { mockHandler.Object };
            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act
            var result = factory.GetHandler(null!);

            // Assert
            result.Should().BeNull("Debe retornar null para eventType null");
        }

        [Fact]
        public void HasHandler_AfterGetHandler_ProducesSameResult()
        {
            // Arrange
            var mockHandler = CreateMockHandler("TestEvent");
            var handlers = new List<IEventHandler> { mockHandler.Object };
            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act
            var hasResult = factory.HasHandler("TestEvent");
            var getResult = factory.GetHandler("TestEvent");

            // Assert
            hasResult.Should().BeTrue();
            getResult.Should().NotBeNull();
        }

        [Fact]
        public void GetHandler_CalledMultipleTimes_ReturnsSameInstance()
        {
            // Arrange
            var mockHandler = CreateMockHandler("TestEvent");
            var handlers = new List<IEventHandler> { mockHandler.Object };
            var factory = new EventHandlerFactory(handlers, _loggerMock.Object);

            // Act
            var result1 = factory.GetHandler("TestEvent");
            var result2 = factory.GetHandler("TestEvent");
            var result3 = factory.GetHandler("TestEvent");

            // Assert
            result1.Should().BeSameAs(result2);
            result2.Should().BeSameAs(result3);
        }

        #endregion

        #region Helper Methods

        private Mock<IEventHandler> CreateMockHandler(string eventType)
        {
            var mock = new Mock<IEventHandler>();
            mock.Setup(h => h.EventType).Returns(eventType);
            mock.Setup(h => h.CanHandle(It.IsAny<string>()))
                .Returns<string?>(type => 
                    type != null && type.Equals(eventType, System.StringComparison.OrdinalIgnoreCase));
            return mock;
        }

        #endregion
    }
}
