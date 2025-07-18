using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using check_crypto.Services;
using check_crypto.DTOs;

namespace check_crypto.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var alert = await _alertService.CreateAlertAsync(userId, createAlertDto);
                
                if (alert == null)
                {
                    return BadRequest(new { message = "Preço mínimo deve ser menor que o preço máximo" });
                }

                return CreatedAtAction(nameof(GetAlert), new { id = alert.Id }, alert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAlert(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var alerts = await _alertService.GetUserAlertsAsync(userId);
                var alert = alerts.FirstOrDefault(a => a.Id == id);

                if (alert == null)
                {
                    return NotFound(new { message = "Alerta não encontrado" });
                }

                return Ok(alert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alert {AlertId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserAlerts()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var alerts = await _alertService.GetUserAlertsAsync(userId);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user alerts");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetTriggeredAlerts()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var alerts = await _alertService.GetTriggeredAlertsAsync(userId);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting triggered alerts");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAlert(int id, [FromBody] UpdateAlertDto updateAlertDto)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var alert = await _alertService.UpdateAlertAsync(userId, id, updateAlertDto);
                
                if (alert == null)
                {
                    return BadRequest(new { message = "Alerta não encontrado ou preço mínimo deve ser menor que o preço máximo" });
                }

                return Ok(alert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating alert {AlertId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlert(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var success = await _alertService.DeleteAlertAsync(userId, id);
                
                if (!success)
                {
                    return NotFound(new { message = "Alerta não encontrado" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting alert {AlertId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }
    }
}