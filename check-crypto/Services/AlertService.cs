using Microsoft.EntityFrameworkCore;
using check_crypto.Data;
using check_crypto.Models;
using check_crypto.DTOs;

namespace check_crypto.Services
{
    public interface IAlertService
    {
        Task<AlertDto?> CreateAlertAsync(Guid userId, CreateAlertDto createAlertDto);
        Task<AlertDto?> UpdateAlertAsync(Guid userId, Guid alertId, UpdateAlertDto updateAlertDto);
        Task<bool> DeleteAlertAsync(Guid userId, Guid alertId);
        Task<List<AlertDto>> GetUserAlertsAsync(Guid userId);
        Task<List<AlertHistoryDto>> GetTriggeredAlertsAsync(Guid userId);
        Task CheckAndTriggerAlertsAsync(CryptoData cryptoData);
    }

    public class AlertService : IAlertService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AlertService> _logger;

        public AlertService(AppDbContext context, ILogger<AlertService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AlertDto?> CreateAlertAsync(Guid userId, CreateAlertDto createAlertDto)
        {
            if (createAlertDto.MinPrice >= createAlertDto.MaxPrice)
            {
                return null;
            }

            var alert = new Alert
            {
                UserId = userId,
                CryptoSymbol = createAlertDto.CryptoSymbol.ToUpper(),
                MinPrice = createAlertDto.MinPrice,
                MaxPrice = createAlertDto.MaxPrice,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();

            return new AlertDto
            {
                Id = alert.Id,
                CryptoSymbol = alert.CryptoSymbol,
                MinPrice = alert.MinPrice,
                MaxPrice = alert.MaxPrice,
                IsActive = alert.IsActive,
                CreatedAt = alert.CreatedAt,
                TriggeredAt = alert.TriggeredAt,
                TriggeredPrice = alert.TriggeredPrice,
                TriggeredType = alert.TriggeredType
            };
        }

        public async Task<AlertDto?> UpdateAlertAsync(Guid userId, Guid alertId, UpdateAlertDto updateAlertDto)
        {
            var alert = await _context.Alerts
                .FirstOrDefaultAsync(a => a.Id == alertId && a.UserId == userId);

            if (alert == null)
            {
                return null;
            }

            if (updateAlertDto.MinPrice.HasValue)
            {
                alert.MinPrice = updateAlertDto.MinPrice.Value;
            }

            if (updateAlertDto.MaxPrice.HasValue)
            {
                alert.MaxPrice = updateAlertDto.MaxPrice.Value;
            }

            if (updateAlertDto.IsActive.HasValue)
            {
                alert.IsActive = updateAlertDto.IsActive.Value;
            }

            if (alert.MinPrice >= alert.MaxPrice)
            {
                return null;
            }

            await _context.SaveChangesAsync();

            return new AlertDto
            {
                Id = alert.Id,
                CryptoSymbol = alert.CryptoSymbol,
                MinPrice = alert.MinPrice,
                MaxPrice = alert.MaxPrice,
                IsActive = alert.IsActive,
                CreatedAt = alert.CreatedAt,
                TriggeredAt = alert.TriggeredAt,
                TriggeredPrice = alert.TriggeredPrice,
                TriggeredType = alert.TriggeredType
            };
        }

        public async Task<bool> DeleteAlertAsync(Guid userId, Guid alertId)
        {
            var alert = await _context.Alerts
                .FirstOrDefaultAsync(a => a.Id == alertId && a.UserId == userId);

            if (alert == null)
            {
                return false;
            }

            _context.Alerts.Remove(alert);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<AlertDto>> GetUserAlertsAsync(Guid userId)
        {
            var alerts = await _context.Alerts
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return alerts.Select(a => new AlertDto
            {
                Id = a.Id,
                CryptoSymbol = a.CryptoSymbol,
                MinPrice = a.MinPrice,
                MaxPrice = a.MaxPrice,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                TriggeredAt = a.TriggeredAt,
                TriggeredPrice = a.TriggeredPrice,
                TriggeredType = a.TriggeredType
            }).ToList();
        }

        public async Task<List<AlertHistoryDto>> GetTriggeredAlertsAsync(Guid userId)
        {
            var alerts = await _context.Alerts
                .Where(a => a.UserId == userId && a.TriggeredAt.HasValue)
                .OrderByDescending(a => a.TriggeredAt)
                .ToListAsync();

            return alerts.Select(a => new AlertHistoryDto
            {
                Id = a.Id,
                CryptoSymbol = a.CryptoSymbol,
                MinPrice = a.MinPrice,
                MaxPrice = a.MaxPrice,
                TriggeredAt = a.TriggeredAt!.Value,
                TriggeredPrice = a.TriggeredPrice!.Value,
                TriggeredType = a.TriggeredType!
            }).ToList();
        }

        public async Task CheckAndTriggerAlertsAsync(CryptoData cryptoData)
        {
            try
            {
                var activeAlerts = await _context.Alerts
                    .Where(a => a.IsActive && 
                               a.CryptoSymbol == cryptoData.Symbol.ToUpper() &&
                               !a.TriggeredAt.HasValue)
                    .ToListAsync();

                foreach (var alert in activeAlerts)
                {
                    string? triggeredType = null;

                    if (cryptoData.Price <= alert.MinPrice)
                    {
                        triggeredType = "MIN";
                    }
                    else if (cryptoData.Price >= alert.MaxPrice)
                    {
                        triggeredType = "MAX";
                    }

                    if (!string.IsNullOrEmpty(triggeredType))
                    {
                        alert.TriggeredAt = DateTime.UtcNow;
                        alert.TriggeredPrice = cryptoData.Price;
                        alert.TriggeredType = triggeredType;
                        alert.IsActive = false;

                        _logger.LogInformation("Alert triggered for user {UserId}: {Symbol} {Type} at {Price}", 
                            alert.UserId, alert.CryptoSymbol, triggeredType, cryptoData.Price);
                    }
                }

                if (activeAlerts.Any(a => a.TriggeredAt.HasValue))
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking alerts for {Symbol}", cryptoData.Symbol);
            }
        }
    }
}