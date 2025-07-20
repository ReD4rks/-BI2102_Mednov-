using Microsoft.AspNetCore.Mvc;
using Test1.Models;
using Test1.Services;

namespace Test1.Controllers;

[ApiController]
[Route("api/fields")]
public class FieldsController : ControllerBase
{
    private readonly IKmlService _kmlService;

    public FieldsController(IKmlService kmlService)
    {
        _kmlService = kmlService;
    }

    [HttpGet]
    public IActionResult GetAllFields()
    {
        var fields = _kmlService.GetAllFields();
        var result = fields.Select(f => new
        {
            id = f.Id,
            name = f.Name,
            size = f.Size,
            locations = new
            {
                Center = f.Center != null ? new[] { f.Center.Latitude, f.Center.Longitude } : null,
                Polygon = f.Polygon.Select(p => new[] { p.Latitude, p.Longitude }).ToArray()
            }
        });

        return Ok(result);
    }

    [HttpGet("{id}/size")]
    public IActionResult GetFieldSize(int id)
    {
        var size = _kmlService.GetFieldSize(id);
        return size.HasValue ? Ok(new { size }) : NotFound();
    }

  
    [HttpPost("distance")]
    public IActionResult CalculateDistance([FromBody] DistanceRequest request)
    {
        var distance = _kmlService.CalculateDistance(request.FieldId, request.Latitude, request.Longitude);
        return distance.HasValue ? Ok(new { distance }) : NotFound();
    }

    [HttpPost("contains")]
    public IActionResult FindFieldContainingPoint([FromBody] PointInPolygonRequest request)
    {
        var field = _kmlService.FindFieldContainingPoint(request.Latitude, request.Longitude);
        return field.HasValue
            ? Ok(new { id = field.Value.id, name = field.Value.name })
            : Ok(false);
    }
}