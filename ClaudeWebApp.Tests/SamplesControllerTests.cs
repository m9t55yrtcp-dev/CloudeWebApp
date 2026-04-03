using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using ClaudeWebApp.Data;
using ClaudeWebApp.DTOs;
using ClaudeWebApp.Models;
using ClaudeWebApp.UseCases;

namespace ClaudeWebApp.Tests;

[TestFixture]
public class SamplesControllerTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private ISampleUseCase _mockUseCase;
    private SqliteConnection _keepAliveConnection;

    [SetUp]
    public void Setup()
    {
        _keepAliveConnection = new SqliteConnection("DataSource=:memory:");
        _keepAliveConnection.Open();

        _mockUseCase = Substitute.For<ISampleUseCase>();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var dbDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (dbDescriptor != null) services.Remove(dbDescriptor);
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlite(_keepAliveConnection));

                    var useCaseDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(ISampleUseCase));
                    if (useCaseDescriptor != null) services.Remove(useCaseDescriptor);
                    services.AddScoped(_ => _mockUseCase);
                });
            });

        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
        _keepAliveConnection.Dispose();
    }

    // -----------------------------------------------------------------------
    // GET /api/samples
    // -----------------------------------------------------------------------

    [Test]
    public async Task GetSamples_ReturnsOk()
    {
        _mockUseCase
            .GetAllSamplesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<string?>())
            .Returns(new PagedResult<SampleResponse> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 });

        var response = await _client.GetAsync("/api/samples");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // -----------------------------------------------------------------------
    // GET /api/samples/{id}
    // -----------------------------------------------------------------------

    [Test]
    public async Task GetSample_ExistingId_ReturnsOk()
    {
        _mockUseCase.GetSampleByIdAsync(1)
            .Returns(new SampleResponse { Id = 1, Name = "Test", Description = "" });

        var response = await _client.GetAsync("/api/samples/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetSample_NonExistingId_ReturnsNotFound()
    {
        _mockUseCase.GetSampleByIdAsync(999)
            .Returns((SampleResponse?)null);

        var response = await _client.GetAsync("/api/samples/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // POST /api/samples
    // -----------------------------------------------------------------------

    [Test]
    public async Task PostSample_ValidRequest_ReturnsCreated()
    {
        _mockUseCase.CreateSampleAsync(Arg.Any<SampleRequest>())
            .Returns(new SampleResponse { Id = 1, Name = "Test", Description = "Desc" });

        var response = await _client.PostAsJsonAsync("/api/samples",
            new SampleRequest { Name = "Test", Description = "Desc" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // -----------------------------------------------------------------------
    // PUT /api/samples/{id}
    // -----------------------------------------------------------------------

    [Test]
    public async Task PutSample_ExistingId_ReturnsNoContent()
    {
        _mockUseCase.UpdateSampleAsync(1, Arg.Any<SampleRequest>())
            .Returns(true);

        var response = await _client.PutAsJsonAsync("/api/samples/1",
            new SampleRequest { Name = "Updated", Description = "" });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task PutSample_NonExistingId_ReturnsNotFound()
    {
        _mockUseCase.UpdateSampleAsync(999, Arg.Any<SampleRequest>())
            .Returns(false);

        var response = await _client.PutAsJsonAsync("/api/samples/999",
            new SampleRequest { Name = "Updated", Description = "" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // DELETE /api/samples/{id}
    // -----------------------------------------------------------------------

    [Test]
    public async Task DeleteSample_ExistingId_ReturnsNoContent()
    {
        _mockUseCase.DeleteSampleAsync(1)
            .Returns(true);

        var response = await _client.DeleteAsync("/api/samples/1");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteSample_NonExistingId_ReturnsNotFound()
    {
        _mockUseCase.DeleteSampleAsync(999)
            .Returns(false);

        var response = await _client.DeleteAsync("/api/samples/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
