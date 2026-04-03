using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shouldly;
using ClaudeWebApp.Data;
using ClaudeWebApp.DTOs;
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

        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _sampleUseCase = new SampleUseCase(_context, cache);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // -----------------------------------------------------------------------
    // GetAllSamplesAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task GetAllSamplesAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _sampleUseCase.GetAllSamplesAsync();

        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Test]
    public async Task GetAllSamplesAsync_MultipleSamples_ReturnsPaginatedResult()
    {
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Sample 1", Description = "Desc 1" });
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Sample 2", Description = "Desc 2" });

        var result = await _sampleUseCase.GetAllSamplesAsync(page: 1, pageSize: 20);

        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(2);
        result.Items.ShouldContain(s => s.Name == "Sample 1");
        result.Items.ShouldContain(s => s.Name == "Sample 2");
    }

    [Test]
    public async Task GetAllSamplesAsync_WithNameFilter_ReturnsFilteredResult()
    {
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Apple", Description = "" });
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Banana", Description = "" });

        var result = await _sampleUseCase.GetAllSamplesAsync(nameFilter: "App");

        result.TotalCount.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Apple");
    }

    [Test]
    public async Task GetAllSamplesAsync_WithPagination_ReturnsCorrectPage()
    {
        for (int i = 1; i <= 5; i++)
            await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = $"Sample {i}", Description = "" });

        var result = await _sampleUseCase.GetAllSamplesAsync(page: 2, pageSize: 2);

        result.ShouldSatisfyAllConditions(
            () => result.TotalCount.ShouldBe(5),
            () => result.Items.Count().ShouldBe(2),
            () => result.Page.ShouldBe(2),
            () => result.TotalPages.ShouldBe(3)
        );
    }

    [Test]
    public async Task GetAllSamplesAsync_LastPage_ReturnsRemainingItems()
    {
        for (int i = 1; i <= 5; i++)
            await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = $"Sample {i}", Description = "" });

        var result = await _sampleUseCase.GetAllSamplesAsync(page: 3, pageSize: 2);

        result.ShouldSatisfyAllConditions(
            () => result.TotalCount.ShouldBe(5),
            () => result.Items.Count().ShouldBe(1),
            () => result.Page.ShouldBe(3),
            () => result.TotalPages.ShouldBe(3)
        );
    }

    [Test]
    public async Task GetAllSamplesAsync_WithSortByName_ReturnsSortedResult()
    {
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Zebra", Description = "" });
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Alpha", Description = "" });

        var result = await _sampleUseCase.GetAllSamplesAsync(sortBy: "name");
        var names = result.Items.Select(s => s.Name).ToList();

        names.ShouldBe(new[] { "Alpha", "Zebra" });
    }

    [Test]
    public async Task GetAllSamplesAsync_WithSortByNameDescending_ReturnsSortedResult()
    {
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Alpha", Description = "" });
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Zebra", Description = "" });

        var result = await _sampleUseCase.GetAllSamplesAsync(sortBy: "name", descending: true);
        var names = result.Items.Select(s => s.Name).ToList();

        names.ShouldBe(new[] { "Zebra", "Alpha" });
    }

    [Test]
    public async Task GetAllSamplesAsync_WithSortByCreatedAt_ReturnsSortedResult()
    {
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "First", Description = "" });
        await Task.Delay(1100); // MySQL DATETIME 秒精度の差をつける
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Second", Description = "" });

        var result = await _sampleUseCase.GetAllSamplesAsync(sortBy: "createdAt");
        var names = result.Items.Select(s => s.Name).ToList();

        names.ShouldBe(new[] { "First", "Second" });
    }

    [Test]
    public async Task GetAllSamplesAsync_WithSortByCreatedAtDescending_ReturnsSortedResult()
    {
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "First", Description = "" });
        await Task.Delay(1100); // MySQL DATETIME 秒精度の差をつける
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Second", Description = "" });

        var result = await _sampleUseCase.GetAllSamplesAsync(sortBy: "createdAt", descending: true);
        var names = result.Items.Select(s => s.Name).ToList();

        names.ShouldBe(new[] { "Second", "First" });
    }

    [Test]
    public async Task GetAllSamplesAsync_SecondCall_ReturnsCachedResult()
    {
        await _sampleUseCase.CreateSampleAsync(new SampleRequest { Name = "Cached", Description = "" });

        var first = await _sampleUseCase.GetAllSamplesAsync();

        // DBを直接書き換えてもキャッシュから古い値が返ることを確認
        await _context.Samples
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Name, "DirectUpdate"));

        var second = await _sampleUseCase.GetAllSamplesAsync();

        second.Items.First().Name.ShouldBe(first.Items.First().Name);
    }

    // -----------------------------------------------------------------------
    // GetSampleByIdAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task GetSampleByIdAsync_ExistingId_ReturnsSample()
    {
        var created = await _sampleUseCase.CreateSampleAsync(
            new SampleRequest { Name = "Test Sample", Description = "Test Description" });

        var result = await _sampleUseCase.GetSampleByIdAsync(created.Id);

        result.ShouldNotBeNull();
        result.ShouldSatisfyAllConditions(
            () => result.Id.ShouldBe(created.Id),
            () => result.Name.ShouldBe(created.Name),
            () => result.Description.ShouldBe(created.Description)
        );
    }

    [Test]
    public async Task GetSampleByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _sampleUseCase.GetSampleByIdAsync(999);

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetSampleByIdAsync_SecondCall_ReturnsCachedResult()
    {
        var created = await _sampleUseCase.CreateSampleAsync(
            new SampleRequest { Name = "Cached", Description = "" });

        var first = await _sampleUseCase.GetSampleByIdAsync(created.Id);

        // DBを直接書き換えてもキャッシュから古い値が返ることを確認
        await _context.Samples
            .Where(s => s.Id == created.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Name, "DirectUpdate"));

        var second = await _sampleUseCase.GetSampleByIdAsync(created.Id);

        second!.Name.ShouldBe(first!.Name);
    }

    // -----------------------------------------------------------------------
    // CreateSampleAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateSampleAsync_ValidRequest_ReturnsCreatedSample()
    {
        var request = new SampleRequest { Name = "Test Sample", Description = "Test Description" };

        var result = await _sampleUseCase.CreateSampleAsync(request);

        result.ShouldNotBeNull();
        result.ShouldSatisfyAllConditions(
            () => result.Id.ShouldBeGreaterThan(0),
            () => result.Name.ShouldBe("Test Sample"),
            () => result.Description.ShouldBe("Test Description"),
            () => result.CreatedAt.ShouldNotBe(default),
            () => result.DeletedAt.ShouldBeNull()
        );
    }

    [Test]
    public async Task CreateSampleAsync_ValidRequest_UpdatedAtEqualsCreatedAt()
    {
        var result = await _sampleUseCase.CreateSampleAsync(
            new SampleRequest { Name = "Test", Description = "" });

        result.UpdatedAt.ShouldBe(result.CreatedAt, tolerance: TimeSpan.FromSeconds(1));
    }

    // -----------------------------------------------------------------------
    // UpdateSampleAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task UpdateSampleAsync_ExistingId_ReturnsTrue()
    {
        var created = await _sampleUseCase.CreateSampleAsync(
            new SampleRequest { Name = "Original", Description = "Original Desc" });

        var success = await _sampleUseCase.UpdateSampleAsync(created.Id,
            new SampleRequest { Name = "Updated", Description = "Updated Desc" });

        success.ShouldBeTrue();

        var updated = await _sampleUseCase.GetSampleByIdAsync(created.Id);
        updated.ShouldNotBeNull();
        updated.ShouldSatisfyAllConditions(
            () => updated.Name.ShouldBe("Updated"),
            () => updated.Description.ShouldBe("Updated Desc"),
            () => updated.CreatedAt.ShouldBe(created.CreatedAt, tolerance: TimeSpan.FromSeconds(1))
        );
    }

    [Test]
    public async Task UpdateSampleAsync_ExistingId_UpdatedAtChanges()
    {
        var created = await _sampleUseCase.CreateSampleAsync(
            new SampleRequest { Name = "Original", Description = "" });

        await Task.Delay(1100); // 秒精度の差をつける
        await _sampleUseCase.UpdateSampleAsync(created.Id,
            new SampleRequest { Name = "Updated", Description = "" });

        var updated = await _sampleUseCase.GetSampleByIdAsync(created.Id);
        updated!.UpdatedAt.ShouldBeGreaterThan(created.UpdatedAt);
        updated.CreatedAt.ShouldBe(created.CreatedAt, tolerance: TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task UpdateSampleAsync_InvalidatesCache()
    {
        var created = await _sampleUseCase.CreateSampleAsync(
            new SampleRequest { Name = "Before", Description = "" });

        await _sampleUseCase.GetSampleByIdAsync(created.Id);

        await _sampleUseCase.UpdateSampleAsync(created.Id,
            new SampleRequest { Name = "After", Description = "" });

        var result = await _sampleUseCase.GetSampleByIdAsync(created.Id);
        result!.Name.ShouldBe("After");
    }

    [Test]
    public async Task UpdateSampleAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _sampleUseCase.UpdateSampleAsync(999,
            new SampleRequest { Name = "Updated", Description = "Updated Desc" });

        result.ShouldBeFalse();
    }

    // -----------------------------------------------------------------------
    // DeleteSampleAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task DeleteSampleAsync_ExistingId_ReturnsTrue()
    {
        var created = await _sampleUseCase.CreateSampleAsync(
            new SampleRequest { Name = "Test Sample", Description = "Test Description" });

        var success = await _sampleUseCase.DeleteSampleAsync(created.Id);

        success.ShouldBeTrue();
    }

    [Test]
    public async Task DeleteSampleAsync_ExistingId_SetsDeletedAt()
    {
        var created = await _sampleUseCase.CreateSampleAsync(
            new SampleRequest { Name = "Test Sample", Description = "" });

        await _sampleUseCase.DeleteSampleAsync(created.Id);

        var raw = await _context.Samples
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == created.Id);

        raw.ShouldNotBeNull();
        raw!.DeletedAt.ShouldNotBeNull();
    }

    [Test]
    public async Task DeleteSampleAsync_ExistingId_NotReturnedByGetById()
    {
        var created = await _sampleUseCase.CreateSampleAsync(
            new SampleRequest { Name = "Test Sample", Description = "" });

        await _sampleUseCase.DeleteSampleAsync(created.Id);

        (await _sampleUseCase.GetSampleByIdAsync(created.Id)).ShouldBeNull();
    }

    [Test]
    public async Task DeleteSampleAsync_ExistingId_ExcludedFromGetAll()
    {
        var created = await _sampleUseCase.CreateSampleAsync(
            new SampleRequest { Name = "ToDelete", Description = "" });
        await _sampleUseCase.CreateSampleAsync(
            new SampleRequest { Name = "ToKeep", Description = "" });

        await _sampleUseCase.DeleteSampleAsync(created.Id);

        var result = await _sampleUseCase.GetAllSamplesAsync();
        result.TotalCount.ShouldBe(1);
        result.Items.ShouldAllBe(s => s.Name != "ToDelete");
    }

    [Test]
    public async Task DeleteSampleAsync_InvalidatesCache()
    {
        var created = await _sampleUseCase.CreateSampleAsync(
            new SampleRequest { Name = "Test", Description = "" });

        await _sampleUseCase.GetSampleByIdAsync(created.Id);
        await _sampleUseCase.DeleteSampleAsync(created.Id);

        (await _sampleUseCase.GetSampleByIdAsync(created.Id)).ShouldBeNull();
    }

    [Test]
    public async Task DeleteSampleAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _sampleUseCase.DeleteSampleAsync(999);

        result.ShouldBeFalse();
    }
}
