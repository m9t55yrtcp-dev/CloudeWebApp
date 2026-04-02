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

    // GET: api/samples
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SampleDto>>> GetSamples()
    {
        var samples = await _sampleUseCase.GetAllSamplesAsync();
        return Ok(samples);
    }

    // GET: api/samples/5
    [HttpGet("{id}")]
    public async Task<ActionResult<SampleDto>> GetSample(int id)
    {
        var sample = await _sampleUseCase.GetSampleByIdAsync(id);

        if (sample == null)
        {
            return NotFound();
        }

        return sample;
    }

    // POST: api/samples
    [HttpPost]
    public async Task<ActionResult<SampleDto>> PostSample(SampleDto sampleDto)
    {
        var createdSample = await _sampleUseCase.CreateSampleAsync(sampleDto);
        return CreatedAtAction(nameof(GetSample), new { id = createdSample.Id }, createdSample);
    }

    // PUT: api/samples/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutSample(int id, SampleDto sampleDto)
    {
        var success = await _sampleUseCase.UpdateSampleAsync(id, sampleDto);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    // DELETE: api/samples/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSample(int id)
    {
        var success = await _sampleUseCase.DeleteSampleAsync(id);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}