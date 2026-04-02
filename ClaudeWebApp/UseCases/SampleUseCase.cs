using Microsoft.EntityFrameworkCore;
using ClaudeWebApp.Data;
using ClaudeWebApp.Models;

namespace ClaudeWebApp.UseCases;

public class SampleUseCase : ISampleUseCase
{
    private readonly ApplicationDbContext _context;

    public SampleUseCase(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SampleDto>> GetAllSamplesAsync()
    {
        var samples = await _context.Samples.ToListAsync();
        return samples.Select(s => new SampleDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            CreatedAt = s.CreatedAt
        });
    }

    public async Task<SampleDto?> GetSampleByIdAsync(int id)
    {
        var sample = await _context.Samples.FindAsync(id);
        if (sample == null) return null;

        return new SampleDto
        {
            Id = sample.Id,
            Name = sample.Name,
            Description = sample.Description,
            CreatedAt = sample.CreatedAt
        };
    }

    public async Task<SampleDto> CreateSampleAsync(SampleDto sampleDto)
    {
        var sample = new Sample
        {
            Name = sampleDto.Name,
            Description = sampleDto.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        sampleDto.Id = sample.Id;
        sampleDto.CreatedAt = sample.CreatedAt;
        return sampleDto;
    }

    public async Task<bool> UpdateSampleAsync(int id, SampleDto sampleDto)
    {
        var sample = await _context.Samples.FindAsync(id);
        if (sample == null) return false;

        sample.Name = sampleDto.Name;
        sample.Description = sampleDto.Description;
        sample.CreatedAt = DateTime.UtcNow; // Update timestamp

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteSampleAsync(int id)
    {
        var sample = await _context.Samples.FindAsync(id);
        if (sample == null) return false;

        _context.Samples.Remove(sample);
        await _context.SaveChangesAsync();
        return true;
    }
}