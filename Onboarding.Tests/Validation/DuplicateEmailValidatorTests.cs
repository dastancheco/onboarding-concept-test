using Onboarding.Core.Interfaces;
using Onboarding.Core.Validation;
using Onboarding.Infrastructure.Validation.Providers;
using OvexDataModelingTest.Entities.App;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Onboarding.Tests.Validation
{
    public class DuplicateEmailValidatorTests
    {
        [Fact]
        public async Task ValidateAsync_ShouldPass_WhenEmailIsUnique()
        {
            // Arrange
            var userRepoMock = new Mock<IRepository<User>>();
            var prospectRepoMock = new Mock<IRepository<Prospect>>();
            var loggerMock = new Mock<ILogger<DuplicateEmailValidator>>();

            userRepoMock.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(new List<User>()); // No users in the system

            var validator = new DuplicateEmailValidator(userRepoMock.Object, prospectRepoMock.Object, loggerMock.Object);
            validator.Configure(JsonSerializer.Serialize(new { CheckScope = "ALL_WORKFLOWS" }));

            var context = new ValidationContext { Email = "unique@example.com", WorkflowId = 1 };

            // Act
            var result = await validator.ValidateAsync(context);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal("Email is available", result.Message);
        }

        [Fact]
        public async Task ValidateAsync_ShouldFail_WhenEmailIsDuplicateInUsers()
        {
            // Arrange
            var userRepoMock = new Mock<IRepository<User>>();
            var prospectRepoMock = new Mock<IRepository<Prospect>>();
            var loggerMock = new Mock<ILogger<DuplicateEmailValidator>>();
            userRepoMock.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(new List<User>
                {
                    new User { Email = "repeated@example.com" }
                });

            var validator = new DuplicateEmailValidator(userRepoMock.Object, prospectRepoMock.Object, loggerMock.Object);
            validator.Configure(JsonSerializer.Serialize(new { CheckScope = "ALL_WORKFLOWS" }));

            var context = new ValidationContext { Email = "repeated@example.com", WorkflowId = 1 };

            // Act
            var result = await validator.ValidateAsync(context);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Email is already taken", result.Message);
        }


    }
}
