using Microsoft.EntityFrameworkCore;
using OvexDataModelingTest.Data;
using OvexDataModelingTest.Entities.App;
using OvexDataModelingTest.Entities.Config;
using Onboarding.Infrastructure.Data.Repositories;

namespace Onboarding.Tests.Integration
{
    /// <summary>
    /// Pruebas de integración para el repositorio genérico con Entity Framework.
    /// 
    /// Estas pruebas verifican la interacción real con una base de datos en memoria,
    /// asegurando que las operaciones CRUD funcionan correctamente.
    /// </summary>
    public class EfRepositoryIntegrationTests : IDisposable
    {
        private readonly OnboardingDbContext _context;
        private readonly EfRepository<Prospect> _prospectRepository;
        private readonly EfRepository<User> _userRepository;

        public EfRepositoryIntegrationTests()
        {
            // Arrange: Configurar base de datos en memoria
            var options = new DbContextOptionsBuilder<OnboardingDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new OnboardingDbContext(options);
            _prospectRepository = new EfRepository<Prospect>(_context);
            _userRepository = new EfRepository<User>(_context);

            // Sembrar datos iniciales
            SeedData();
        }

        private void SeedData()
        {
            var user1 = new User
            {
                UserId = Guid.NewGuid(),
                Email = "user1@example.com",
                CreatedAt = DateTime.UtcNow
            };

            var user2 = new User
            {
                UserId = Guid.NewGuid(),
                Email = "user2@example.com",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.AddRange(user1, user2);
            _context.SaveChanges();
        }

        #region GetByIdAsync Tests

        /// <summary>
        /// Verifica que se puede recuperar una entidad por su ID.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithExistingId_ReturnsEntity()
        {
            // Arrange
            var user = _context.Users.First();

            // Act
            var result = await _userRepository.GetByIdAsync(user.UserId);

            // Assert
            result.Should().NotBeNull();
            result!.UserId.Should().Be(user.UserId);
            result.Email.Should().Be(user.Email);
        }

        /// <summary>
        /// Verifica que GetByIdAsync devuelve null para un ID inexistente.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _userRepository.GetByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetAllAsync Tests

        /// <summary>
        /// Verifica que GetAllAsync devuelve todas las entidades.
        /// </summary>
        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Act
            var result = await _userRepository.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2, "se sembraron 2 usuarios");
        }

        #endregion

        #region FindAsync Tests

        /// <summary>
        /// Verifica que FindAsync devuelve las entidades que coinciden con el predicado.
        /// </summary>
        [Fact]
        public async Task FindAsync_WithMatchingPredicate_ReturnsMatchingEntities()
        {
            // Arrange
            var targetEmail = "user1@example.com";

            // Act
            var result = await _userRepository.FindAsync(u => u.Email == targetEmail);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Email.Should().Be(targetEmail);
        }

        /// <summary>
        /// Verifica que FindAsync devuelve lista vacía cuando no hay coincidencias.
        /// </summary>
        [Fact]
        public async Task FindAsync_WithNoMatches_ReturnsEmptyList()
        {
            // Act
            var result = await _userRepository.FindAsync(u => u.Email == "nonexistent@example.com");

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region AddAsync Tests

        /// <summary>
        /// Verifica que se puede agregar una nueva entidad.
        /// </summary>
        [Fact]
        public async Task AddAsync_AddsNewEntity()
        {
            // Arrange
            var newProspect = new Prospect
            {
                ProspectId = Guid.NewGuid(),
                UserId = _context.Users.First().UserId,
                WorkflowId = 1,
                Status = "STARTED",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            await _prospectRepository.AddAsync(newProspect);
            await _context.SaveChangesAsync();

            // Assert
            var savedProspect = await _prospectRepository.GetByIdAsync(newProspect.ProspectId);
            savedProspect.Should().NotBeNull();
            savedProspect!.Status.Should().Be("STARTED");
        }

        #endregion

        #region UpdateAsync Tests

        /// <summary>
        /// Verifica que se puede actualizar una entidad existente.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_UpdatesExistingEntity()
        {
            // Arrange
            var user = _context.Users.First();
            var originalEmail = user.Email;
            user.Email = "updated@example.com";

            // Act
            await _userRepository.UpdateAsync(user);
            await _context.SaveChangesAsync();

            // Assert
            var updatedUser = await _userRepository.GetByIdAsync(user.UserId);
            updatedUser.Should().NotBeNull();
            updatedUser!.Email.Should().Be("updated@example.com");
            updatedUser.Email.Should().NotBe(originalEmail);
        }

        #endregion

        #region DeleteAsync Tests

        

        #endregion

        #region Complex Query Tests

        /// <summary>
        /// Verifica que se pueden realizar consultas complejas con múltiples condiciones.
        /// </summary>
        [Fact]
        public async Task FindAsync_WithComplexPredicate_ReturnsCorrectResults()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var user1 = _context.Users.First();

            var prospect1 = new Prospect
            {
                ProspectId = Guid.NewGuid(),
                UserId = user1.UserId,
                WorkflowId = 1,
                Status = "IN_PROGRESS",
                CreatedAt = now.AddDays(-5)
            };

            var prospect2 = new Prospect
            {
                ProspectId = Guid.NewGuid(),
                UserId = user1.UserId,
                WorkflowId = 1,
                Status = "APPROVED",
                CreatedAt = now.AddDays(-2)
            };

            await _prospectRepository.AddAsync(prospect1);
            await _prospectRepository.AddAsync(prospect2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _prospectRepository.FindAsync(p =>
                p.UserId == user1.UserId &&
                p.WorkflowId == 1 &&
                p.Status == "IN_PROGRESS");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().ProspectId.Should().Be(prospect1.ProspectId);
        }

        /// <summary>
        /// Verifica que se pueden realizar consultas con ordenamiento.
        /// </summary>
        [Fact]
        public async Task FindAsync_WithOrdering_ReturnsOrderedResults()
        {
            // Arrange
            var user1 = _context.Users.First();

            var prospect1 = new Prospect
            {
                ProspectId = Guid.NewGuid(),
                UserId = user1.UserId,
                WorkflowId = 1,
                Status = "STARTED",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };

            var prospect2 = new Prospect
            {
                ProspectId = Guid.NewGuid(),
                UserId = user1.UserId,
                WorkflowId = 1,
                Status = "IN_PROGRESS",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };

            await _prospectRepository.AddAsync(prospect1);
            await _prospectRepository.AddAsync(prospect2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _prospectRepository.FindAsync(p => p.UserId == user1.UserId);
            var orderedResults = result.OrderByDescending(p => p.CreatedAt).ToList();

            // Assert
            orderedResults.Should().HaveCount(2);
            orderedResults.First().CreatedAt.Should().BeAfter(orderedResults.Last().CreatedAt);
        }

        #endregion

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
