using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using ClaudeWebApp.Data;
using ClaudeWebApp.Models;

namespace ClaudeWebApp.UseCases;

public class SampleUseCase : ISampleUseCase
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;

    private static readonly DistributedCacheEntryOptions ItemCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };
    private static readonly DistributedCacheEntryOptions ListCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
    };
    private static readonly DistributedCacheEntryOptions VersionCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
    };

    private const string VersionKey = "samples:version";

    public SampleUseCase(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    private async Task<long> GetVersionAsync()
    {
        var val = await _cache.GetStringAsync(VersionKey);
        return long.TryParse(val, out var ver) ? ver : 0;
    }

    private async Task BumpVersionAsync()
    {
        var ver = await GetVersionAsync();
        await _cache.SetStringAsync(VersionKey, (ver + 1).ToString(), VersionCacheOptions);
    }

    public async Task<PagedResult<SampleResponse>> GetAllSamplesAsync(
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool descending = false,
        string? nameFilter = null)
    {
        var version = await GetVersionAsync();
        var cacheKey = $"samples:all:{version}:{page}:{pageSize}:{sortBy}:{descending}:{nameFilter}";

        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
            return JsonSerializer.Deserialize<PagedResult<SampleResponse>>(cached)!;

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

        var result = new PagedResult<SampleResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), ListCacheOptions);
        return result;
    }

    public async Task<SampleResponse?> GetSampleByIdAsync(int id)
    {
        var cacheKey = $"sample:{id}";

        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
            return JsonSerializer.Deserialize<SampleResponse>(cached);

        var sample = await _context.Samples.FindAsync(id);
        if (sample == null) return null;

        var response = new SampleResponse
        {
            Id = sample.Id,
            Name = sample.Name,
            Description = sample.Description,
            CreatedAt = sample.CreatedAt
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), ItemCacheOptions);
        return response;
    }

    public async Task<SampleResponse> CreateSampleAsync(SampleRequest request)
    {
        var sample = new Sample
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.Now
        };

        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        await BumpVersionAsync();

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

        await _cache.RemoveAsync($"sample:{id}");
        await BumpVersionAsync();

        return true;
    }

    public async Task<bool> DeleteSampleAsync(int id)
    {
        var sample = await _context.Samples.FindAsync(id);
        if (sample == null) return false;

        _context.Samples.Remove(sample);
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync($"sample:{id}");
        await BumpVersionAsync();

        return true;
    }
}
