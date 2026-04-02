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
        var result = await _sampleUseCase.GetAllSamplesAsync();

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Is.Empty);
            Assert.That(result.TotalCount, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task CreateSampleAsync_ValidRequest_ReturnsCreatedSample()
    {
        var request = new SampleRequest { Name = "Test Sample", Description = "Test Description" };

        var result = await _sampleUseCase.CreateSampleAsync(request);

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
        var request = new SampleRequest { Name = "Test Sample", Description = "Test Description" };
        var created = await _sampleUseCase.CreateSampleAsync(request);

        var result = await _sampleUseCase.GetSampleByIdAsync(created.Id);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(created.Id));
            Assert.That(result.Name, Is.EqualTo(created.Name));
            Assert.That(result.Description, Is.EqualTo(created.Description));
        });
    }

    [Test]
    public async Task GetSampleByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _sampleUseCase.GetSampleByIdAsync(999);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateSampleAsync_ExistingId_ReturnsTrue()
    {
        var created = await _sampleUseCase.CreateSampleAsync(
            new SampleRequest { Name = "Original", Description = "Original Desc" });

        var success = await _sampleUseCase.UpdateSampleAsync(created.Id,
            new SampleRequest { Name = "Updated", Description = "Updated Desc" });

        Assert.That(success, Is.True);

        var updated = await _sampleUseCase.GetSampleByIdAsync(created.Id);
        Assert.That(updated, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(updated.Name, Is.EqualTo("Updated"));
            Assert.That(updated.Description, Is.EqualTo("Updated Desc"));
            Assert.That(updated.CreatedAt, Is.EqualTo(created.CreatedAt).Within(TimeSpan.FromSeconds(1)));
        });
    }

    [Test]
    public async Task UpdateSampleAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _sampleUseCase.UpdateSampleAsync(999,
            new SampleRequest { Name = "Updated", Description = "Updated Desc" });

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteSampleAsync_ExistingId_ReturnsTrue()
    {
        var created = await _sampleUseCase.CreateSampleAsync(
            new SampleRequest { Name = "Test Sample", Description = "Test Description" });

        var success = await _sampleUseCase.DeleteSampleAsync(created.Id);

        Assert.That(success, Is.True);
        Assert.That(await _sampleUseCase.GetSampleByIdAsync(created.Id), Is.Null);
    }

    [Test]
    public async Task DeleteSampleAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _sampleUseCase.DeleteSampleAsync(999);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetAllSamplesAsync_MultipleSamples_ReturnsPaginatedResult()
    {
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Sample 1", Description = "Desc 1" });
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Sample 2", Description = "Desc 2" });

        var result = await _sampleUseCase.GetAllSamplesAsync(page: 1, pageSize: 20);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.TotalCount, Is.EqualTo(2));
            Assert.That(result.Items, Has.Some.Matches<SampleResponse>(s => s.Name == "Sample 1"));
            Assert.That(result.Items, Has.Some.Matches<SampleResponse>(s => s.Name == "Sample 2"));
        });
    }

    [Test]
    public async Task GetAllSamplesAsync_WithNameFilter_ReturnsFilteredResult()
    {
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Apple", Description = "" });
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Banana", Description = "" });

        var result = await _sampleUseCase.GetAllSamplesAsync(nameFilter: "App");

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalCount, Is.EqualTo(1));
            Assert.That(result.Items.First().Name, Is.EqualTo("Apple"));
        });
    }

    [Test]
    public async Task GetAllSamplesAsync_WithPagination_ReturnsCorrectPage()
    {
        for (int i = 1; i <= 5; i++)
            await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = $"Sample {i}", Description = "" });

        var result = await _sampleUseCase.GetAllSamplesAsync(page: 2, pageSize: 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalCount, Is.EqualTo(5));
            Assert.That(result.Items.Count(), Is.EqualTo(2));
            Assert.That(result.Page, Is.EqualTo(2));
            Assert.That(result.TotalPages, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task GetAllSamplesAsync_WithSortByName_ReturnsSortedResult()
    {
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Zebra", Description = "" });
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Alpha", Description = "" });

        var result = await _sampleUseCase.GetAllSamplesAsync(sortBy: "name");
        var names = result.Items.Select(s => s.Name).ToList();

        Assert.That(names, Is.EqualTo(new[] { "Alpha", "Zebra" }));
    }

    [Test]
    public async Task GetAllSamplesAsync_WithSortByNameDescending_ReturnsSortedResult()
    {
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Alpha", Description = "" });
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Zebra", Description = "" });

        var result = await _sampleUseCase.GetAllSamplesAsync(sortBy: "name", descending: true);
        var names = result.Items.Select(s => s.Name).ToList();

        Assert.That(names, Is.EqualTo(new[] { "Zebra", "Alpha" }));
    }

    [Test]
    public async Task GetAllSamplesAsync_WithSortByCreatedAt_ReturnsSortedResult()
    {
        var first = await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "First", Description = "" });
        await Task.Delay(1100); // MySQL DATETIME 秒精度の差をつける
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Second", Description = "" });

        var result = await _sampleUseCase.GetAllSamplesAsync(sortBy: "createdAt");
        var names = result.Items.Select(s => s.Name).ToList();

        Assert.That(names, Is.EqualTo(new[] { "First", "Second" }));
    }

    [Test]
    public async Task GetAllSamplesAsync_LastPage_ReturnsRemainingItems()
    {
        for (int i = 1; i <= 5; i++)
            await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = $"Sample {i}", Description = "" });

        var result = await _sampleUseCase.GetAllSamplesAsync(page: 3, pageSize: 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalCount, Is.EqualTo(5));
            Assert.That(result.Items.Count(), Is.EqualTo(1));
            Assert.That(result.Page, Is.EqualTo(3));
            Assert.That(result.TotalPages, Is.EqualTo(3));
        });
    }
}
