using ClaudeWebApp.Models;

namespace ClaudeWebApp.UseCases;

public interface ISampleUseCase
{
    Task<PagedResult<SampleResponse>> GetAllSamplesAsync(
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool descending = false,
        string? nameFilter = null);
    Task<SampleResponse?> GetSampleByIdAsync(int id);
    Task<SampleResponse> CreateSampleAsync(SampleRequest request);
    Task<bool> UpdateSampleAsync(int id, SampleRequest request);
    Task<bool> DeleteSampleAsync(int id);
}
