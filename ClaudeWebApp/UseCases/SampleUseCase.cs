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

    public async Task<PagedResult<SampleResponse>> GetAllSamplesAsync(
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool descending = false,
        string? nameFilter = null)
    {
        var query = _context.Samples.AsQueryable();

        if (!string.IsNullOrWhiteSpace(nameFilter))
            query = query.Where(s => s.Name.Contains(nameFilter));

        query = sortBy?.ToLower() switch
        {
            "name" => descending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
            "createdat" => descending ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt),
            _ => query.OrderBy(s => s.Id)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SampleResponse
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<SampleResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SampleResponse?> GetSampleByIdAsync(int id)
    {
        var sample = await _context.Samples.FindAsync(id);
        if (sample == null) return null;

        return new SampleResponse
        {
            Id = sample.Id,
            Name = sample.Name,
            Description = sample.Description,
            CreatedAt = sample.CreatedAt
        };
    }

    public async Task<SampleResponse> CreateSampleAsync(SampleRequest request)
    {
        var sample = new Sample
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        return new SampleResponse
        {
            Id = sample.Id,
            Name = sample.Name,
            Description = sample.Description,
            CreatedAt = sample.CreatedAt
        };
    }

    public async Task<bool> UpdateSampleAsync(int id, SampleRequest request)
    {
        var sample = await _context.Samples.FindAsync(id);
        if (sample == null) return false;

        sample.Name = request.Name;
        sample.Description = request.Description;

        await _context.SaveChangesAsync();
        return true;
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
