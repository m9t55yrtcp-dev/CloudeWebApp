using ClaudeWebApp.Models;

namespace ClaudeWebApp.UseCases;

public interface ISampleUseCase
{
    Task<IEnumerable<SampleDto>> GetAllSamplesAsync();
    Task<SampleDto?> GetSampleByIdAsync(int id);
    Task<SampleDto> CreateSampleAsync(SampleDto sampleDto);
    Task<bool> UpdateSampleAsync(int id, SampleDto sampleDto);
    Task<bool> DeleteSampleAsync(int id);
}