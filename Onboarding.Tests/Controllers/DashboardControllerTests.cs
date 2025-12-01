using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Onboarding.Api.Controllers;
using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Entities.App;
using OvexDataModelingTest.Entities.Config;
using System.Text.Json;

namespace Onboarding.Tests.Controllers
{
    /// <summary>
    /// Tests unitarios para el Dashboard Controller
    /// Valida endpoints de monitoreo, estadísticas y búsqueda
    /// </summary>
    public class DashboardControllerTests
    {
        private readonly DashboardController _sut;
        private readonly Mock<IRepository<Prospect>> _prospectRepoMock;
        private readonly Mock<IRepository<User>> _userRepoMock;
        private readonly Mock<IRepository<Workflow>> _workflowRepoMock;
        private readonly Mock<IRepository<Phase>> _phaseRepoMock;
        private readonly Mock<IRepository<Step>> _stepRepoMock;
        private readonly Mock<IProspectDataService> _prospectDataServiceMock;
        private readonly Mock<IProspectStatusService> _prospectStatusServiceMock;
        private readonly Mock<ILogger<DashboardController>> _loggerMock;

        public DashboardControllerTests()
        {
            _prospectRepoMock = new Mock<IRepository<Prospect>>();
            _userRepoMock = new Mock<IRepository<User>>();
            _workflowRepoMock = new Mock<IRepository<Workflow>>();
            _phaseRepoMock = new Mock<IRepository<Phase>>();
            _stepRepoMock = new Mock<IRepository<Step>>();
            _prospectDataServiceMock = new Mock<IProspectDataService>();
            _prospectStatusServiceMock = new Mock<IProspectStatusService>();
            _loggerMock = new Mock<ILogger<DashboardController>>();

            _sut = new DashboardController(
                _prospectRepoMock.Object,
                _userRepoMock.Object,
                _workflowRepoMock.Object,
                _phaseRepoMock.Object,
                _stepRepoMock.Object,
                _prospectDataServiceMock.Object,
                _prospectStatusServiceMock.Object,
                _loggerMock.Object
            );
        }

        #region Helper Methods

        private static JsonElement GetJsonElement(object value)
        {
            var json = JsonSerializer.Serialize(value);
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        private static T GetProperty<T>(JsonElement element, string propertyName)
        {
            var property = element.GetProperty(propertyName);
            return JsonSerializer.Deserialize<T>(property.GetRawText())!;
        }

        #endregion

        #region GetProspects Tests

        [Fact]
        public async Task GetProspects_WithoutFilters_ReturnsAllProspects()
        {
            // Arrange
            var prospects = new List<Prospect>
            {
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 1, Status = "IN_PROGRESS", CurrentStepId = 1, CreatedAt = DateTime.UtcNow },
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 1, Status = "COMPLETED", CurrentStepId = null, CreatedAt = DateTime.UtcNow.AddDays(-1) }
            };

            var users = new List<User>
            {
                new User { UserId = prospects[0].UserId, Email = "user1@test.com", CreatedAt = DateTime.UtcNow },
                new User { UserId = prospects[1].UserId, Email = "user2@test.com", CreatedAt = DateTime.UtcNow }
            };

            var workflows = new List<Workflow>
            {
                new Workflow { WorkflowId = 1, Name = "Test Workflow", WorkflowType = "PROSPECT", SubTypeKey = "TEST" }
            };

            var steps = new List<Step>
            {
                new Step { StepId = 1, Name = "Step 1", PhaseId = 1, Order = 1 }
            };

            _prospectRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(prospects);
            _userRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(users);
            _workflowRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(workflows);
            _stepRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(steps);

            // Act
            var result = await _sut.GetProspects();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            var jsonElement = GetJsonElement(okResult.Value!);
            GetProperty<int>(jsonElement, "totalCount").Should().Be(2);
            GetProperty<int>(jsonElement, "page").Should().Be(1);
            GetProperty<int>(jsonElement, "pageSize").Should().Be(20);
        }

        [Fact]
        public async Task GetProspects_WithWorkflowFilter_ReturnsFilteredProspects()
        {
            // Arrange
            var prospects = new List<Prospect>
            {
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 1, Status = "IN_PROGRESS", CreatedAt = DateTime.UtcNow },
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 2, Status = "IN_PROGRESS", CreatedAt = DateTime.UtcNow }
            };

            var users = new List<User>
            {
                new User { UserId = prospects[0].UserId, Email = "user1@test.com", CreatedAt = DateTime.UtcNow },
                new User { UserId = prospects[1].UserId, Email = "user2@test.com", CreatedAt = DateTime.UtcNow }
            };

            var workflows = new List<Workflow>
            {
                new Workflow { WorkflowId = 1, Name = "Workflow 1", WorkflowType = "PROSPECT", SubTypeKey = "W1" },
                new Workflow { WorkflowId = 2, Name = "Workflow 2", WorkflowType = "PROSPECT", SubTypeKey = "W2" }
            };

            _prospectRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(prospects);
            _userRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(users);
            _workflowRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(workflows);
            _stepRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Step>());

            // Act
            var result = await _sut.GetProspects(workflowId: 1);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var jsonElement = GetJsonElement(okResult.Value!);
            GetProperty<int>(jsonElement, "totalCount").Should().Be(1);
        }

        [Fact]
        public async Task GetProspects_WithStatusFilter_ReturnsFilteredProspects()
        {
            // Arrange
            var prospects = new List<Prospect>
            {
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 1, Status = "IN_PROGRESS", CreatedAt = DateTime.UtcNow },
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 1, Status = "COMPLETED", CreatedAt = DateTime.UtcNow }
            };

            var users = new List<User>
            {
                new User { UserId = prospects[0].UserId, Email = "user1@test.com", CreatedAt = DateTime.UtcNow },
                new User { UserId = prospects[1].UserId, Email = "user2@test.com", CreatedAt = DateTime.UtcNow }
            };

            var workflows = new List<Workflow>
            {
                new Workflow { WorkflowId = 1, Name = "Test Workflow", WorkflowType = "PROSPECT", SubTypeKey = "TEST" }
            };

            _prospectRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(prospects);
            _userRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(users);
            _workflowRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(workflows);
            _stepRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Step>());

            // Act
            var result = await _sut.GetProspects(status: "IN_PROGRESS");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var jsonElement = GetJsonElement(okResult.Value!);
            GetProperty<int>(jsonElement, "totalCount").Should().Be(1);
        }

        [Fact]
        public async Task GetProspects_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var prospects = Enumerable.Range(1, 50)
                .Select(i => new Prospect
                {
                    ProspectId = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    WorkflowId = 1,
                    Status = "IN_PROGRESS",
                    CreatedAt = DateTime.UtcNow.AddDays(-i)
                })
                .ToList();

            var users = prospects.Select(p => new User { UserId = p.UserId, Email = $"user{p.ProspectId}@test.com", CreatedAt = DateTime.UtcNow }).ToList();
            var workflows = new List<Workflow> { new Workflow { WorkflowId = 1, Name = "Test Workflow", WorkflowType = "PROSPECT", SubTypeKey = "TEST" } };

            _prospectRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(prospects);
            _userRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(users);
            _workflowRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(workflows);
            _stepRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Step>());

            // Act
            var result = await _sut.GetProspects(page: 2, pageSize: 10);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var jsonElement = GetJsonElement(okResult.Value!);
            GetProperty<int>(jsonElement, "page").Should().Be(2);
            GetProperty<int>(jsonElement, "pageSize").Should().Be(10);
            GetProperty<int>(jsonElement, "totalCount").Should().Be(50);
            GetProperty<int>(jsonElement, "totalPages").Should().Be(5);
        }

        #endregion

        #region GetProspectDetail Tests

        [Fact]
        public async Task GetProspectDetail_WithValidId_ReturnsProspectDetails()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var prospect = new Prospect
            {
                ProspectId = prospectId,
                UserId = userId,
                WorkflowId = 1,
                Status = "IN_PROGRESS",
                CurrentStepId = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var user = new User { UserId = userId, Email = "test@test.com", CreatedAt = DateTime.UtcNow };
            var workflow = new Workflow { WorkflowId = 1, Name = "Test Workflow", WorkflowType = "PROSPECT", SubTypeKey = "TEST" };
            var prospectData = JsonSerializer.Serialize(new { name = "Test", email = "test@test.com" });
            var statusHistory = new List<ProspectStatusHistory>();

            var phases = new List<Phase>
            {
                new Phase { PhaseId = 1, Name = "Phase 1", WorkflowId = 1, Order = 1 }
            };

            var steps = new List<Step>
            {
                new Step { StepId = 1, Name = "Step 1", PhaseId = 1, Order = 1 },
                new Step { StepId = 2, Name = "Step 2", PhaseId = 1, Order = 2 }
            };

            _prospectRepoMock.Setup(x => x.GetByIdAsync(prospectId)).ReturnsAsync(prospect);
            _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
            _workflowRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(workflow);
            _prospectDataServiceMock.Setup(x => x.GetProspectDataAsync(prospectId)).ReturnsAsync(prospectData);
            _prospectStatusServiceMock.Setup(x => x.GetStatusHistoryAsync(prospectId)).ReturnsAsync(statusHistory);
            _phaseRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(phases);
            _stepRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(steps);

            // Act
            var result = await _sut.GetProspectDetail(prospectId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            var jsonElement = GetJsonElement(okResult.Value!);
            GetProperty<Guid>(jsonElement, "prospectId").Should().Be(prospectId);
            GetProperty<string>(jsonElement, "status").Should().Be("IN_PROGRESS");
        }

        [Fact]
        public async Task GetProspectDetail_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            _prospectRepoMock.Setup(x => x.GetByIdAsync(prospectId)).ReturnsAsync((Prospect?)null);

            // Act
            var result = await _sut.GetProspectDetail(prospectId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetProspectDetail_CalculatesProgressCorrectly()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var prospect = new Prospect
            {
                ProspectId = prospectId,
                UserId = userId,
                WorkflowId = 1,
                Status = "IN_PROGRESS",
                CurrentStepId = 2,
                CreatedAt = DateTime.UtcNow
            };

            var phases = new List<Phase>
            {
                new Phase { PhaseId = 1, Name = "Phase 1", WorkflowId = 1, Order = 1 }
            };

            var steps = new List<Step>
            {
                new Step { StepId = 1, Name = "Step 1", PhaseId = 1, Order = 1 },
                new Step { StepId = 2, Name = "Step 2", PhaseId = 1, Order = 2 },
                new Step { StepId = 3, Name = "Step 3", PhaseId = 1, Order = 3 }
            };

            _prospectRepoMock.Setup(x => x.GetByIdAsync(prospectId)).ReturnsAsync(prospect);
            _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(new User { UserId = userId, Email = "test@test.com", CreatedAt = DateTime.UtcNow });
            _workflowRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Workflow { WorkflowId = 1, Name = "Test", WorkflowType = "PROSPECT", SubTypeKey = "TEST" });
            _prospectDataServiceMock.Setup(x => x.GetProspectDataAsync(prospectId)).ReturnsAsync("{}");
            _prospectStatusServiceMock.Setup(x => x.GetStatusHistoryAsync(prospectId)).ReturnsAsync(new List<ProspectStatusHistory>());
            _phaseRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(phases);
            _stepRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(steps);

            // Act
            var result = await _sut.GetProspectDetail(prospectId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var jsonElement = GetJsonElement(okResult.Value!);
            var progress = jsonElement.GetProperty("progress");
            
            GetProperty<int>(progress, "totalSteps").Should().Be(3);
            GetProperty<int>(progress, "completedSteps").Should().Be(1);
            GetProperty<int>(progress, "progressPercentage").Should().Be(33);
        }

        #endregion

        #region GetStatistics Tests

        [Fact]
        public async Task GetStatistics_ReturnsCorrectSummary()
        {
            // Arrange
            var prospects = new List<Prospect>
            {
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 1, Status = "IN_PROGRESS", CurrentStepId = 1, CreatedAt = DateTime.UtcNow },
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 1, Status = "COMPLETED", CurrentStepId = null, CreatedAt = DateTime.UtcNow.AddDays(-2), UpdatedAt = DateTime.UtcNow.AddDays(-1) },
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 2, Status = "REJECTED", CurrentStepId = null, CreatedAt = DateTime.UtcNow.AddDays(-5) }
            };

            var workflows = new List<Workflow>
            {
                new Workflow { WorkflowId = 1, Name = "Workflow 1", WorkflowType = "PROSPECT", SubTypeKey = "W1" },
                new Workflow { WorkflowId = 2, Name = "Workflow 2", WorkflowType = "PROSPECT", SubTypeKey = "W2" }
            };

            _prospectRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(prospects);
            _workflowRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(workflows);

            // Act
            var result = await _sut.GetStatistics();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var jsonElement = GetJsonElement(okResult.Value!);
            var summary = jsonElement.GetProperty("summary");
            
            GetProperty<int>(summary, "totalProspects").Should().Be(3);
            GetProperty<int>(summary, "activeProspects").Should().Be(1);
            GetProperty<int>(summary, "completedProspects").Should().Be(2);
            GetProperty<int>(summary, "rejectedProspects").Should().Be(1);
        }

        [Fact]
        public async Task GetStatistics_CalculatesAverageCompletionDays()
        {
            // Arrange
            var prospects = new List<Prospect>
            {
                new Prospect 
                { 
                    ProspectId = Guid.NewGuid(), 
                    UserId = Guid.NewGuid(), 
                    WorkflowId = 1, 
                    Status = "COMPLETED", 
                    CurrentStepId = null, 
                    CreatedAt = DateTime.UtcNow.AddDays(-3), 
                    UpdatedAt = DateTime.UtcNow
                },
                new Prospect 
                { 
                    ProspectId = Guid.NewGuid(), 
                    UserId = Guid.NewGuid(), 
                    WorkflowId = 1, 
                    Status = "COMPLETED", 
                    CurrentStepId = null, 
                    CreatedAt = DateTime.UtcNow.AddDays(-1), 
                    UpdatedAt = DateTime.UtcNow
                }
            };

            var workflows = new List<Workflow>
            {
                new Workflow { WorkflowId = 1, Name = "Workflow 1", WorkflowType = "PROSPECT", SubTypeKey = "W1" }
            };

            _prospectRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(prospects);
            _workflowRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(workflows);

            // Act
            var result = await _sut.GetStatistics();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var jsonElement = GetJsonElement(okResult.Value!);
            var summary = jsonElement.GetProperty("summary");
            
            GetProperty<double>(summary, "averageCompletionDays").Should().Be(2.0);
        }

        [Fact]
        public async Task GetStatistics_GroupsByWorkflow()
        {
            // Arrange
            var prospects = new List<Prospect>
            {
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 1, Status = "IN_PROGRESS", CurrentStepId = 1, CreatedAt = DateTime.UtcNow },
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 1, Status = "COMPLETED", CurrentStepId = null, CreatedAt = DateTime.UtcNow },
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 2, Status = "IN_PROGRESS", CurrentStepId = 1, CreatedAt = DateTime.UtcNow }
            };

            var workflows = new List<Workflow>
            {
                new Workflow { WorkflowId = 1, Name = "Workflow 1", WorkflowType = "PROSPECT", SubTypeKey = "W1" },
                new Workflow { WorkflowId = 2, Name = "Workflow 2", WorkflowType = "PROSPECT", SubTypeKey = "W2" }
            };

            _prospectRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(prospects);
            _workflowRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(workflows);

            // Act
            var result = await _sut.GetStatistics();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var jsonElement = GetJsonElement(okResult.Value!);
            var byWorkflow = jsonElement.GetProperty("byWorkflow");
            
            byWorkflow.GetArrayLength().Should().Be(2);
        }

        #endregion

        #region GetTimeline Tests

        [Fact]
        public async Task GetTimeline_ReturnsRecentActivity()
        {
            // Arrange
            var prospects = new List<Prospect>
            {
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 1, Status = "IN_PROGRESS", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 1, Status = "COMPLETED", CreatedAt = DateTime.UtcNow.AddDays(-2), UpdatedAt = DateTime.UtcNow.AddDays(-1) }
            };

            var users = prospects.Select(p => new User { UserId = p.UserId, Email = $"user{p.ProspectId}@test.com", CreatedAt = DateTime.UtcNow }).ToList();
            var workflows = new List<Workflow> { new Workflow { WorkflowId = 1, Name = "Test Workflow", WorkflowType = "PROSPECT", SubTypeKey = "TEST" } };

            _prospectRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(prospects);
            _userRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(users);
            _workflowRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(workflows);

            // Act
            var result = await _sut.GetTimeline(days: 7, limit: 50);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var jsonElement = GetJsonElement(okResult.Value!);
            
            GetProperty<int>(jsonElement, "days").Should().Be(7);
            GetProperty<int>(jsonElement, "limit").Should().Be(50);
            GetProperty<int>(jsonElement, "count").Should().Be(2);
        }

        [Fact]
        public async Task GetTimeline_FiltersOldProspects()
        {
            // Arrange
            var prospects = new List<Prospect>
            {
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 1, Status = "IN_PROGRESS", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Prospect { ProspectId = Guid.NewGuid(), UserId = Guid.NewGuid(), WorkflowId = 1, Status = "COMPLETED", CreatedAt = DateTime.UtcNow.AddDays(-10), UpdatedAt = DateTime.UtcNow.AddDays(-10) }
            };

            var users = prospects.Select(p => new User { UserId = p.UserId, Email = $"user{p.ProspectId}@test.com", CreatedAt = DateTime.UtcNow }).ToList();
            var workflows = new List<Workflow> { new Workflow { WorkflowId = 1, Name = "Test Workflow", WorkflowType = "PROSPECT", SubTypeKey = "TEST" } };

            _prospectRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(prospects);
            _userRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(users);
            _workflowRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(workflows);

            // Act
            var result = await _sut.GetTimeline(days: 7, limit: 50);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var jsonElement = GetJsonElement(okResult.Value!);
            
            GetProperty<int>(jsonElement, "count").Should().Be(1);
        }

        #endregion

        #region SearchProspects Tests

        [Fact]
        public async Task SearchProspects_WithoutQuery_ReturnsBadRequest()
        {
            // Act
            var result = await _sut.SearchProspects("");

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)result;
            badRequestResult.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task SearchProspects_WithValidGuid_ReturnsProspectById()
        {
            // Arrange
            var prospectId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var prospect = new Prospect { ProspectId = prospectId, UserId = userId, WorkflowId = 1, Status = "IN_PROGRESS", CreatedAt = DateTime.UtcNow };
            var user = new User { UserId = userId, Email = "test@test.com", CreatedAt = DateTime.UtcNow };
            var workflow = new Workflow { WorkflowId = 1, Name = "Test Workflow", WorkflowType = "PROSPECT", SubTypeKey = "TEST" };

            _prospectRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Prospect> { prospect });
            _userRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User> { user });
            _workflowRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Workflow> { workflow });

            // Act
            var result = await _sut.SearchProspects(prospectId.ToString());

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var jsonElement = GetJsonElement(okResult.Value!);
            
            GetProperty<int>(jsonElement, "count").Should().Be(1);
        }

        [Fact]
        public async Task SearchProspects_WithEmail_ReturnsMatchingProspects()
        {
            // Arrange
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();
            var prospects = new List<Prospect>
            {
                new Prospect { ProspectId = Guid.NewGuid(), UserId = userId1, WorkflowId = 1, Status = "IN_PROGRESS", CreatedAt = DateTime.UtcNow },
                new Prospect { ProspectId = Guid.NewGuid(), UserId = userId2, WorkflowId = 1, Status = "IN_PROGRESS", CreatedAt = DateTime.UtcNow }
            };

            var users = new List<User>
            {
                new User { UserId = userId1, Email = "john@example.com", CreatedAt = DateTime.UtcNow },
                new User { UserId = userId2, Email = "jane@example.com", CreatedAt = DateTime.UtcNow }
            };

            var workflows = new List<Workflow> { new Workflow { WorkflowId = 1, Name = "Test Workflow", WorkflowType = "PROSPECT", SubTypeKey = "TEST" } };

            _prospectRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(prospects);
            _userRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(users);
            _workflowRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(workflows);

            // Act
            var result = await _sut.SearchProspects("john");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var jsonElement = GetJsonElement(okResult.Value!);
            
            GetProperty<int>(jsonElement, "count").Should().Be(1);
        }

        [Fact]
        public async Task SearchProspects_LimitsResults()
        {
            // Arrange
            var prospects = Enumerable.Range(1, 30)
                .Select(i =>
                {
                    var userId = Guid.NewGuid();
                    return new Prospect
                    {
                        ProspectId = Guid.NewGuid(),
                        UserId = userId,
                        WorkflowId = 1,
                        Status = "IN_PROGRESS",
                        CreatedAt = DateTime.UtcNow
                    };
                })
                .ToList();

            var users = prospects.Select(p => new User { UserId = p.UserId, Email = $"test{p.ProspectId}@example.com", CreatedAt = DateTime.UtcNow }).ToList();
            var workflows = new List<Workflow> { new Workflow { WorkflowId = 1, Name = "Test Workflow", WorkflowType = "PROSPECT", SubTypeKey = "TEST" } };

            _prospectRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(prospects);
            _userRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(users);
            _workflowRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(workflows);

            // Act
            var result = await _sut.SearchProspects("test");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var jsonElement = GetJsonElement(okResult.Value!);
            var results = jsonElement.GetProperty("results");
            
            results.GetArrayLength().Should().BeLessOrEqualTo(20);
        }

        #endregion
    }
}
