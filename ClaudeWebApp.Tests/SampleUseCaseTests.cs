using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using ClaudeWebApp.Data;
using ClaudeWebApp.UseCases;
using ClaudeWebApp.Models;

namespace ClaudeWebApp.Tests;

[TestFixture]
public class SampleUseCaseTests
{
    private ApplicationDbContext _context;
    private ISampleUseCase _sampleUseCase;

    [SetUp]
    public void Setup()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var baseConnectionString = Environment.GetEnvironmentVariable("TEST_MYSQL_CONNECTION")
            ?? "Server=localhost;User=root;Password=root;";
        var connectionString = $"{baseConnectionString}Database={dbName};";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _sampleUseCase = new SampleUseCase(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task GetAllSamplesAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _sampleUseCase.GetAllSamplesAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task CreateSampleAsync_ValidDto_ReturnsCreatedSample()
    {
        // Arrange
        var sampleDto = new SampleDto
        {
            Name = "Test Sample",
            Description = "Test Description"
        };

        // Act
        var result = await _sampleUseCase.CreateSampleAsync(sampleDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.GreaterThan(0));
            Assert.That(result.Name, Is.EqualTo("Test Sample"));
            Assert.That(result.Description, Is.EqualTo("Test Description"));
            Assert.That(result.CreatedAt, Is.Not.EqualTo(default(DateTime)));
        });
    }

    [Test]
    public async Task GetSampleByIdAsync_ExistingId_ReturnsSample()
    {
        // Arrange
        var sampleDto = new SampleDto
        {
            Name = "Test Sample",
            Description = "Test Description"
        };
        var createdSample = await _sampleUseCase.CreateSampleAsync(sampleDto);

        // Act
        var result = await _sampleUseCase.GetSampleByIdAsync(createdSample.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(createdSample.Id));
            Assert.That(result.Name, Is.EqualTo(createdSample.Name));
            Assert.That(result.Description, Is.EqualTo(createdSample.Description));
        });
    }

    [Test]
    public async Task GetSampleByIdAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _sampleUseCase.GetSampleByIdAsync(999);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateSampleAsync_ExistingId_ReturnsTrue()
    {
        // Arrange
        var sampleDto = new SampleDto
        {
            Name = "Original Name",
            Description = "Original Description"
        };
        var createdSample = await _sampleUseCase.CreateSampleAsync(sampleDto);

        var updateDto = new SampleDto
        {
            Name = "Updated Name",
            Description = "Updated Description"
        };

        // Act
        var result = await _sampleUseCase.UpdateSampleAsync(createdSample.Id, updateDto);

        // Assert
        Assert.That(result, Is.True);

        // Verify the update
        var updatedSample = await _sampleUseCase.GetSampleByIdAsync(createdSample.Id);
        Assert.That(updatedSample, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(updatedSample.Name, Is.EqualTo("Updated Name"));
            Assert.That(updatedSample.Description, Is.EqualTo("Updated Description"));
            Assert.That(updatedSample.CreatedAt, Is.EqualTo(createdSample.CreatedAt));
        });
    }

    [Test]
    public async Task UpdateSampleAsync_NonExistingId_ReturnsFalse()
    {
        // Arrange
        var updateDto = new SampleDto
        {
            Name = "Updated Name",
            Description = "Updated Description"
        };

        // Act
        var result = await _sampleUseCase.UpdateSampleAsync(999, updateDto);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteSampleAsync_ExistingId_ReturnsTrue()
    {
        // Arrange
        var sampleDto = new SampleDto
        {
            Name = "Test Sample",
            Description = "Test Description"
        };
        var createdSample = await _sampleUseCase.CreateSampleAsync(sampleDto);

        // Act
        var result = await _sampleUseCase.DeleteSampleAsync(createdSample.Id);

        // Assert
        Assert.That(result, Is.True);

        // Verify deletion
        var deletedSample = await _sampleUseCase.GetSampleByIdAsync(createdSample.Id);
        Assert.That(deletedSample, Is.Null);
    }

    [Test]
    public async Task DeleteSampleAsync_NonExistingId_ReturnsFalse()
    {
        // Act
        var result = await _sampleUseCase.DeleteSampleAsync(999);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetAllSamplesAsync_MultipleSamples_ReturnsAllSamples()
    {
        // Arrange
        var sample1 = new SampleDto { Name = "Sample 1", Description = "Desc 1" };
        var sample2 = new SampleDto { Name = "Sample 2", Description = "Desc 2" };

        await _sampleUseCase.CreateSampleAsync(sample1);
        await _sampleUseCase.CreateSampleAsync(sample2);

        // Act
        var result = (await _sampleUseCase.GetAllSamplesAsync()).ToList();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result, Has.Some.Matches<Sample>(s => s.Name == "Sample 1"));
        Assert.That(result, Has.Some.Matches<Sample>(s => s.Name == "Sample 2"));
    }
}
