using Microsoft.AspNetCore.Mvc;
using Mock.Exception;
using Mock.Models.DTO;
using Mock.Services;

namespace Mock.Controller;

[Route("api/[controller]")]
[ApiController]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _appService;

    public AppointmentController(IAppointmentService appService)
    {
        _appService = appService;
    }

    [HttpGet("{appId}")]
    public async Task<IActionResult> GetAppointmentInfoById(int appId)
    {
        try
        {
            var res = await _appService.GetAppointmentInfo(appId);
            return Ok(res);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequestDTO app)
    {
        if (!app.Services.Any())
        {
            return BadRequest("At least one item is required");
        }

        try
        {
            await _appService.AddNewAppointment(app);
        }
        catch (AlreadyExistException e)
        {
            return Conflict(e.Message);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        
        return CreatedAtAction(nameof(GetAppointmentInfoById), new { appId = app.AppointmentId }, app);
    }
}