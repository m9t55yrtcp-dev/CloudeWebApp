using Microsoft.AspNetCore.Mvc;
using ClaudeWebApp.UseCases;
using ClaudeWebApp.Models;

namespace ClaudeWebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SamplesController : ControllerBase
{
    private readonly ISampleUseCase _sampleUseCase;

    public SamplesController(ISampleUseCase sampleUseCase)
    {
        _sampleUseCase = sampleUseCase;
    }

    // GET: api/samples?page=1&pageSize=20&sortBy=name&descending=false&nameFilter=foo
    [HttpGet]
    public async Task<ActionResult<PagedResult<SampleResponse>>> GetSamples(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = false,
        [FromQuery] string? nameFilter = null)
    {
        var result = await _sampleUseCase.GetAllSamplesAsync(page, pageSize, sortBy, descending, nameFilter);
        return Ok(result);
    }

    // GET: api/samples/5
    [HttpGet("{id}")]
    public async Task<ActionResult<SampleResponse>> GetSample(int id)
    {
        var sample = await _sampleUseCase.GetSampleByIdAsync(id);
        if (sample == null) return NotFound();
        return sample;
    }

    // POST: api/samples
    [HttpPost]
    public async Task<ActionResult<SampleResponse>> PostSample(SampleRequest request)
    {
        var created = await _sampleUseCase.CreateSampleAsync(request);
        return CreatedAtAction(nameof(GetSample), new { id = created.Id }, created);
    }

    // PUT: api/samples/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutSample(int id, SampleRequest request)
    {
        var success = await _sampleUseCase.UpdateSampleAsync(id, request);
        if (!success) return NotFound();
        return NoContent();
    }

    // DELETE: api/samples/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSample(int id)
    {
        var success = await _sampleUseCase.DeleteSampleAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}
