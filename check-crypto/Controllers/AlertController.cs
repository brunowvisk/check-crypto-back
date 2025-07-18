using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using check_crypto.Services;
using check_crypto.DTOs;

namespace check_crypto.Controllers
{
    [ApiController]
    [Route("api/alerts")]
    [Authorize]
    public class AlertController : ControllerBase
    {
        private readonly IAlertService _alertService;
        private readonly ILogger<AlertController> _logger;

        public AlertController(IAlertService alertService, ILogger<AlertController> logger)
        {
            _alertService = alertService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAlert([FromBody] CreateAlertDto createAlertDto)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var alert = await _alertService.CreateAlertAsync(userId, createAlertDto);
                
                if (alert == null)
                {
                    return BadRequest(new { message = "Minimum price must be lower than maximum price" });
                }

                return CreatedAtAction(nameof(GetAlert), new { id = alert.Id }, alert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAlert(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var alerts = await _alertService.GetUserAlertsAsync(userId);
                var alert = alerts.FirstOrDefault(a => a.Id == id);

                if (alert == null)
                {
                    return NotFound(new { message = "Alert not found" });
                }

                return Ok(alert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alert {AlertId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserAlerts()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var alerts = await _alertService.GetUserAlertsAsync(userId);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user alerts");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetTriggeredAlerts()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var alerts = await _alertService.GetTriggeredAlertsAsync(userId);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting triggered alerts");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAlert(Guid id, [FromBody] UpdateAlertDto updateAlertDto)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var alert = await _alertService.UpdateAlertAsync(userId, id, updateAlertDto);
                
                if (alert == null)
                {
                    return BadRequest(new { message = "Alert not found or minimum price must be lower than maximum price" });
                }

                return Ok(alert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating alert {AlertId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlert(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var success = await _alertService.DeleteAlertAsync(userId, id);
                
                if (!success)
                {
                    return NotFound(new { message = "Alert not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting alert {AlertId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}