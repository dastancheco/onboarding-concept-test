using Onboarding.Core.Domain;

namespace Onboarding.Tests.Domain
{
    /// <summary>
    /// Pruebas unitarias para la máquina de estados de prospectos.
    /// 
    /// Cobertura de pruebas:
    /// - Transiciones válidas entre estados
    /// - Transiciones inválidas bloqueadas
    /// - Identificación de estados terminales
    /// - Validación de estados permitidos
    /// - Todos los estados del sistema real
    /// </summary>
    public class ProspectStateMachineTests
    {
        #region Valid Transitions Tests

        /// <summary>
        /// Verifica que se permiten transiciones válidas desde STARTED.
        /// </summary>
        [Theory]
        [InlineData(ProspectStateMachine.States.Started, ProspectStateMachine.States.InProgress)]
        [InlineData(ProspectStateMachine.States.Started, ProspectStateMachine.States.Cancelled)]
        public void CanTransition_FromStarted_AllowsValidTransitions(string from, string to)
        {
            // Act
            var result = ProspectStateMachine.CanTransition(from, to);

            // Assert
            result.Should().BeTrue($"la transición de {from} a {to} es válida");
        }

        /// <summary>
        /// Verifica que se permiten transiciones válidas desde IN_PROGRESS.
        /// </summary>
        [Theory]
        [InlineData(ProspectStateMachine.States.InProgress, ProspectStateMachine.States.PendingReview)]
        [InlineData(ProspectStateMachine.States.InProgress, ProspectStateMachine.States.Cancelled)]
        [InlineData(ProspectStateMachine.States.InProgress, ProspectStateMachine.States.Failed)]
        [InlineData(ProspectStateMachine.States.InProgress, ProspectStateMachine.States.MoreInfoRequired)]
        public void CanTransition_FromInProgress_AllowsValidTransitions(string from, string to)
        {
            // Act
            var result = ProspectStateMachine.CanTransition(from, to);

            // Assert
            result.Should().BeTrue($"la transición de {from} a {to} es válida");
        }

        /// <summary>
        /// Verifica que se permiten transiciones válidas desde PENDING_REVIEW.
        /// </summary>
        [Theory]
        [InlineData(ProspectStateMachine.States.PendingReview, ProspectStateMachine.States.InReview)]
        [InlineData(ProspectStateMachine.States.PendingReview, ProspectStateMachine.States.Cancelled)]
        [InlineData(ProspectStateMachine.States.PendingReview, ProspectStateMachine.States.MoreInfoRequired)]
        public void CanTransition_FromPendingReview_AllowsValidTransitions(string from, string to)
        {
            // Act
            var result = ProspectStateMachine.CanTransition(from, to);

            // Assert
            result.Should().BeTrue($"la transición de {from} a {to} es válida");
        }

        /// <summary>
        /// Verifica que se permiten transiciones válidas desde IN_REVIEW.
        /// </summary>
        [Theory]
        [InlineData(ProspectStateMachine.States.InReview, ProspectStateMachine.States.Approved)]
        [InlineData(ProspectStateMachine.States.InReview, ProspectStateMachine.States.Rejected)]
        [InlineData(ProspectStateMachine.States.InReview, ProspectStateMachine.States.MoreInfoRequired)]
        public void CanTransition_FromInReview_AllowsValidTransitions(string from, string to)
        {
            // Act
            var result = ProspectStateMachine.CanTransition(from, to);

            // Assert
            result.Should().BeTrue($"la transición de {from} a {to} es válida");
        }

        /// <summary>
        /// Verifica que MORE_INFO_REQUIRED puede volver a IN_PROGRESS o cancelarse.
        /// </summary>
        [Theory]
        [InlineData(ProspectStateMachine.States.MoreInfoRequired, ProspectStateMachine.States.InProgress)]
        [InlineData(ProspectStateMachine.States.MoreInfoRequired, ProspectStateMachine.States.Cancelled)]
        public void CanTransition_FromMoreInfoRequired_AllowsValidTransitions(string from, string to)
        {
            // Act
            var result = ProspectStateMachine.CanTransition(from, to);

            // Assert
            result.Should().BeTrue($"la transición de {from} a {to} es válida");
        }

        /// <summary>
        /// Verifica que APPROVED puede transicionar a FINALIZED.
        /// </summary>
        [Fact]
        public void CanTransition_FromApproved_CanMoveToFinalized()
        {
            // Act
            var result = ProspectStateMachine.CanTransition(
                ProspectStateMachine.States.Approved,
                ProspectStateMachine.States.Finalized);

            // Assert
            result.Should().BeTrue("APPROVED puede finalizar");
        }

        /// <summary>
        /// Verifica que FAILED puede reintentar volviendo a IN_PROGRESS.
        /// </summary>
        [Fact]
        public void CanTransition_FromFailed_CanRetryToInProgress()
        {
            // Act
            var result = ProspectStateMachine.CanTransition(
                ProspectStateMachine.States.Failed,
                ProspectStateMachine.States.InProgress);

            // Assert
            result.Should().BeTrue("FAILED puede reintentar");
        }

        #endregion

        #region Invalid Transitions Tests

        /// <summary>
        /// Verifica que se bloquean las transiciones inválidas desde STARTED.
        /// </summary>
        [Theory]
        [InlineData(ProspectStateMachine.States.Started, ProspectStateMachine.States.Approved)]
        [InlineData(ProspectStateMachine.States.Started, ProspectStateMachine.States.Rejected)]
        [InlineData(ProspectStateMachine.States.Started, ProspectStateMachine.States.InReview)]
        public void CanTransition_FromStarted_BlocksInvalidTransitions(string from, string to)
        {
            // Act
            var result = ProspectStateMachine.CanTransition(from, to);

            // Assert
            result.Should().BeFalse($"la transición de {from} a {to} no es válida");
        }

        /// <summary>
        /// Verifica que no se permite transicionar al mismo estado.
        /// </summary>
        [Theory]
        [InlineData(ProspectStateMachine.States.Started)]
        [InlineData(ProspectStateMachine.States.InProgress)]
        [InlineData(ProspectStateMachine.States.PendingReview)]
        public void CanTransition_ToSameState_ReturnsFalse(string status)
        {
            // Act
            var result = ProspectStateMachine.CanTransition(status, status);

            // Assert
            result.Should().BeFalse("no se permite permanecer en el mismo estado");
        }

        /// <summary>
        /// Verifica que los estados terminales no permiten transiciones.
        /// </summary>
        [Theory]
        [InlineData(ProspectStateMachine.States.Rejected, ProspectStateMachine.States.InProgress)]
        [InlineData(ProspectStateMachine.States.Cancelled, ProspectStateMachine.States.InProgress)]
        [InlineData(ProspectStateMachine.States.Finalized, ProspectStateMachine.States.Approved)]
        public void CanTransition_FromTerminalStates_BlocksAllTransitions(string from, string to)
        {
            // Act
            var result = ProspectStateMachine.CanTransition(from, to);

            // Assert
            result.Should().BeFalse($"{from} es terminal y no permite transiciones");
        }

        #endregion

        #region Terminal States Tests

        /// <summary>
        /// Verifica que se identifican correctamente los estados terminales.
        /// </summary>
        [Theory]
        [InlineData(ProspectStateMachine.States.Rejected, true)]
        [InlineData(ProspectStateMachine.States.Cancelled, true)]
        [InlineData(ProspectStateMachine.States.Finalized, true)]
        public void IsTerminalState_WithTerminalStates_ReturnsTrue(string status, bool expected)
        {
            // Act
            var result = ProspectStateMachine.IsTerminalState(status);

            // Assert
            result.Should().Be(expected, $"{status} debería ser terminal: {expected}");
        }

        /// <summary>
        /// Verifica que los estados no terminales se identifican correctamente.
        /// </summary>
        [Theory]
        [InlineData(ProspectStateMachine.States.Started)]
        [InlineData(ProspectStateMachine.States.InProgress)]
        [InlineData(ProspectStateMachine.States.PendingReview)]
        [InlineData(ProspectStateMachine.States.InReview)]
        [InlineData(ProspectStateMachine.States.MoreInfoRequired)]
        [InlineData(ProspectStateMachine.States.Approved)]
        [InlineData(ProspectStateMachine.States.Failed)]
        public void IsTerminalState_WithNonTerminalStates_ReturnsFalse(string status)
        {
            // Act
            var result = ProspectStateMachine.IsTerminalState(status);

            // Assert
            result.Should().BeFalse($"{status} no debería ser terminal");
        }

        /// <summary>
        /// Verifica que estados inválidos o nulos retornan false.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("INVALID_STATUS")]
        [InlineData(null)]
        public void IsTerminalState_WithInvalidStatus_ReturnsFalse(string? status)
        {
            // Act
            var result = ProspectStateMachine.IsTerminalState(status ?? "");

            // Assert
            result.Should().BeFalse("estados inválidos no son terminales");
        }

        #endregion

        #region Get Available Transitions Tests

        /// <summary>
        /// Verifica que se devuelven las transiciones disponibles correctas desde STARTED.
        /// </summary>
        [Fact]
        public void GetAllowedTransitions_FromStarted_ReturnsCorrectTransitions()
        {
            // Act
            var transitions = ProspectStateMachine.GetAllowedTransitions(ProspectStateMachine.States.Started);

            // Assert
            transitions.Should().NotBeNull();
            transitions.Should().Contain(new[] { 
                ProspectStateMachine.States.InProgress, 
                ProspectStateMachine.States.Cancelled 
            });
            transitions.Should().HaveCount(2);
        }

        /// <summary>
        /// Verifica las transiciones disponibles desde IN_PROGRESS.
        /// </summary>
        [Fact]
        public void GetAllowedTransitions_FromInProgress_ReturnsCorrectTransitions()
        {
            // Act
            var transitions = ProspectStateMachine.GetAllowedTransitions(ProspectStateMachine.States.InProgress);

            // Assert
            transitions.Should().NotBeNull();
            transitions.Should().Contain(new[] { 
                ProspectStateMachine.States.PendingReview, 
                ProspectStateMachine.States.Cancelled,
                ProspectStateMachine.States.Failed,
                ProspectStateMachine.States.MoreInfoRequired
            });
            transitions.Should().HaveCount(4);
        }

        /// <summary>
        /// Verifica que los estados terminales no tienen transiciones disponibles.
        /// </summary>
        [Theory]
        [InlineData(ProspectStateMachine.States.Rejected)]
        [InlineData(ProspectStateMachine.States.Cancelled)]
        [InlineData(ProspectStateMachine.States.Finalized)]
        public void GetAllowedTransitions_FromTerminalState_ReturnsEmpty(string status)
        {
            // Act
            var transitions = ProspectStateMachine.GetAllowedTransitions(status);

            // Assert
            transitions.Should().BeEmpty($"{status} es un estado terminal sin transiciones");
        }

        /// <summary>
        /// Verifica que un estado inválido no tiene transiciones disponibles.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("INVALID_STATUS")]
        [InlineData(null)]
        public void GetAllowedTransitions_WithInvalidStatus_ReturnsEmpty(string? status)
        {
            // Act
            var transitions = ProspectStateMachine.GetAllowedTransitions(status ?? "");

            // Assert
            transitions.Should().BeEmpty("estados inválidos no tienen transiciones");
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Verifica que CanTransition maneja valores nulos correctamente.
        /// </summary>
        [Theory]
        [InlineData(null, ProspectStateMachine.States.InProgress)]
        [InlineData(ProspectStateMachine.States.Started, null)]
        [InlineData(null, null)]
        public void CanTransition_WithNullValues_ReturnsFalse(string? from, string? to)
        {
            // Act
            var result = ProspectStateMachine.CanTransition(from ?? "", to ?? "");

            // Assert
            result.Should().BeFalse("valores nulos no son válidos");
        }

        /// <summary>
        /// Verifica que CanTransition maneja espacios en blanco correctamente.
        /// </summary>
        [Theory]
        [InlineData("  ", ProspectStateMachine.States.InProgress)]
        [InlineData(ProspectStateMachine.States.Started, "  ")]
        public void CanTransition_WithWhitespace_ReturnsFalse(string from, string to)
        {
            // Act
            var result = ProspectStateMachine.CanTransition(from, to);

            // Assert
            result.Should().BeFalse("espacios en blanco no son válidos");
        }

        #endregion

        #region ValidateTransition Tests

        /// <summary>
        /// Verifica que ValidateTransition lanza excepción para transiciones inválidas.
        /// </summary>
        [Fact]
        public void ValidateTransition_WithInvalidTransition_ThrowsException()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var currentStatus = ProspectStateMachine.States.Approved;
            var newStatus = ProspectStateMachine.States.InProgress;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => ProspectStateMachine.ValidateTransition(prospectId, currentStatus, newStatus));

            exception.Message.Should().Contain("Transición de estado inválida");
            exception.Message.Should().Contain(prospectId.ToString());
            exception.Message.Should().Contain(currentStatus);
            exception.Message.Should().Contain(newStatus);
        }

        /// <summary>
        /// Verifica que ValidateTransition no lanza excepción para transiciones válidas.
        /// </summary>
        [Fact]
        public void ValidateTransition_WithValidTransition_DoesNotThrow()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var currentStatus = ProspectStateMachine.States.Started;
            var newStatus = ProspectStateMachine.States.InProgress;

            // Act & Assert
            var act = () => ProspectStateMachine.ValidateTransition(prospectId, currentStatus, newStatus);
            
            act.Should().NotThrow("la transición es válida");
        }

        #endregion

        #region Get All States Tests

        /// <summary>
        /// Verifica que GetAllStates devuelve todos los estados del sistema.
        /// </summary>
        [Fact]
        public void GetAllStates_ReturnsAllDefinedStates()
        {
            // Act
            var allStates = ProspectStateMachine.GetAllStates();

            // Assert
            allStates.Should().NotBeNull();
            allStates.Should().HaveCount(10, "hay 10 estados definidos en el sistema");
            allStates.Should().Contain(new[]
            {
                ProspectStateMachine.States.Started,
                ProspectStateMachine.States.InProgress,
                ProspectStateMachine.States.PendingReview,
                ProspectStateMachine.States.InReview,
                ProspectStateMachine.States.MoreInfoRequired,
                ProspectStateMachine.States.Approved,
                ProspectStateMachine.States.Rejected,
                ProspectStateMachine.States.Cancelled,
                ProspectStateMachine.States.Failed,
                ProspectStateMachine.States.Finalized
            });
        }

        #endregion

        #region Business Flow Tests

        /// <summary>
        /// Verifica el flujo completo exitoso del sistema.
        /// </summary>
        [Fact]
        public void ValidateCompleteSuccessfulFlow()
        {
            // Arrange: STARTED ? IN_PROGRESS ? PENDING_REVIEW ? IN_REVIEW ? APPROVED ? FINALIZED
            var flow = new[]
            {
                (ProspectStateMachine.States.Started, ProspectStateMachine.States.InProgress),
                (ProspectStateMachine.States.InProgress, ProspectStateMachine.States.PendingReview),
                (ProspectStateMachine.States.PendingReview, ProspectStateMachine.States.InReview),
                (ProspectStateMachine.States.InReview, ProspectStateMachine.States.Approved),
                (ProspectStateMachine.States.Approved, ProspectStateMachine.States.Finalized)
            };

            // Act & Assert
            foreach (var (from, to) in flow)
            {
                var isValid = ProspectStateMachine.CanTransition(from, to);
                isValid.Should().BeTrue($"el flujo exitoso debe permitir {from} ? {to}");
            }
        }

        /// <summary>
        /// Verifica el flujo cuando se requiere más información.
        /// </summary>
        [Fact]
        public void ValidateMoreInfoRequiredFlow()
        {
            // Arrange: IN_REVIEW ? MORE_INFO_REQUIRED ? IN_PROGRESS ? PENDING_REVIEW
            var flow = new[]
            {
                (ProspectStateMachine.States.InReview, ProspectStateMachine.States.MoreInfoRequired),
                (ProspectStateMachine.States.MoreInfoRequired, ProspectStateMachine.States.InProgress),
                (ProspectStateMachine.States.InProgress, ProspectStateMachine.States.PendingReview)
            };

            // Act & Assert
            foreach (var (from, to) in flow)
            {
                var isValid = ProspectStateMachine.CanTransition(from, to);
                isValid.Should().BeTrue($"el flujo de más información debe permitir {from} ? {to}");
            }
        }

        /// <summary>
        /// Verifica el flujo de reintento después de falla.
        /// </summary>
        [Fact]
        public void ValidateFailureRetryFlow()
        {
            // Arrange: IN_PROGRESS ? FAILED ? IN_PROGRESS
            var flow = new[]
            {
                (ProspectStateMachine.States.InProgress, ProspectStateMachine.States.Failed),
                (ProspectStateMachine.States.Failed, ProspectStateMachine.States.InProgress)
            };

            // Act & Assert
            foreach (var (from, to) in flow)
            {
                var isValid = ProspectStateMachine.CanTransition(from, to);
                isValid.Should().BeTrue($"el flujo de reintento debe permitir {from} ? {to}");
            }
        }

        #endregion
    }
}
