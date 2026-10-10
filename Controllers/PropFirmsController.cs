using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeJournal.Api.Models;
using TradeJournal.Api.Services;

namespace TradeJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropFirmsController : ControllerBase
{
    private readonly PropFirmService _propFirmService;
    private readonly PropFirmTradeService _propFirmTradeService;

    public PropFirmsController(
        PropFirmService propFirmService,
        PropFirmTradeService propFirmTradeService)
    {
        _propFirmService = propFirmService;
        _propFirmTradeService = propFirmTradeService;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private ObjectResult Forbidden() =>
        StatusCode(
            StatusCodes.Status403Forbidden,
            new { message = "You do not have permission to access this resource." });

    private static bool TryNormalizeStatus(
        string? raw,
        string fallback,
        out string status,
        out string? error)
    {
        status = fallback;
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
            return true;

        var value = raw.Trim();

        if (value.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            status = "Active";
            return true;
        }

        if (value.Equals("Failed", StringComparison.OrdinalIgnoreCase))
        {
            status = "Failed";
            return true;
        }

        if (value.Equals("Passed", StringComparison.OrdinalIgnoreCase))
        {
            status = "Passed";
            return true;
        }

        error = "Status must be Active, Failed or Passed.";
        return false;
    }

    private static bool TryValidateAmountSpent(PropFirm propFirm, out string? error)
    {
        error = null;

        if (double.IsNaN(propFirm.AmountSpent) ||
            double.IsInfinity(propFirm.AmountSpent) ||
            propFirm.AmountSpent < 0)
        {
            error = "Amount spent must be zero or greater.";
            return false;
        }

        return true;
    }

    private static bool TryValidatePayouts(PropFirm propFirm, out string? error)
    {
        error = null;

        if (double.IsNaN(propFirm.Payouts) ||
            double.IsInfinity(propFirm.Payouts) ||
            propFirm.Payouts < 0)
        {
            error = "Payouts must be zero or greater.";
            return false;
        }

        return true;
    }

    [HttpGet]
    public async Task<IActionResult> GetPropFirms()
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Invalid or missing authentication token." });

        var propFirms = await _propFirmService.GetPropFirmsAsync(userId);
        return Ok(propFirms);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPropFirm(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Invalid or missing authentication token." });

        var propFirm = await _propFirmService.GetPropFirmAsync(id);

        if (propFirm == null)
            return NotFound(new { message = "Prop firm not found." });

        if (propFirm.UserId != userId)
            return Forbidden();

        return Ok(propFirm);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePropFirm(PropFirm propFirm)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Invalid or missing authentication token." });

        if (propFirm == null || string.IsNullOrWhiteSpace(propFirm.FirmName))
            return BadRequest(new { message = "Prop firm name is required." });

        if (!TryValidateAmountSpent(propFirm, out var amountSpentError))
            return BadRequest(new { message = amountSpentError });

        if (!TryValidatePayouts(propFirm, out var payoutsError))
            return BadRequest(new { message = payoutsError });

        if (!TryNormalizeStatus(propFirm.Status, "Active", out var status, out var statusError))
            return BadRequest(new { message = statusError });

        // Ownership always comes from the verified token, never from the request body.
        propFirm.UserId = userId;
        propFirm.Status = status;
        propFirm.Id = string.IsNullOrWhiteSpace(propFirm.Id)
            ? Guid.NewGuid().ToString()
            : propFirm.Id;

        var now = DateTime.UtcNow;
        propFirm.CreatedAt = now;
        propFirm.UpdatedAt = now;

        await _propFirmService.AddPropFirmAsync(propFirm);

        return Ok(new { message = "Prop firm saved successfully", id = propFirm.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePropFirm(string id, PropFirm propFirm)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Invalid or missing authentication token." });

        var existing = await _propFirmService.GetPropFirmAsync(id);

        if (existing == null)
            return NotFound(new { message = "Prop firm not found." });

        if (existing.UserId != userId)
            return Forbidden();

        if (propFirm == null || string.IsNullOrWhiteSpace(propFirm.FirmName))
            return BadRequest(new { message = "Prop firm name is required." });

        if (!TryValidateAmountSpent(propFirm, out var updateAmountError))
            return BadRequest(new { message = updateAmountError });

        if (!TryValidatePayouts(propFirm, out var updatePayoutsError))
            return BadRequest(new { message = updatePayoutsError });

        var fallbackStatus = string.IsNullOrWhiteSpace(existing.Status)
            ? "Active"
            : existing.Status;

        if (!TryNormalizeStatus(propFirm.Status, fallbackStatus, out var updateStatus, out var updateStatusError))
            return BadRequest(new { message = updateStatusError });

        propFirm.Id = id;
        propFirm.UserId = userId; // editing cannot change ownership
        propFirm.Status = updateStatus;
        propFirm.CreatedAt = existing.CreatedAt;
        propFirm.UpdatedAt = DateTime.UtcNow;

        await _propFirmService.UpdatePropFirmAsync(id, propFirm);

        return Ok(new { message = "Prop firm updated successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePropFirm(string id, [FromQuery] bool force = false)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Invalid or missing authentication token." });

        var existing = await _propFirmService.GetPropFirmAsync(id);

        if (existing == null)
            return NotFound(new { message = "Prop firm not found." });

        if (existing.UserId != userId)
            return Forbidden();

        var tradeCount = await _propFirmTradeService.CountTradesAsync(id, userId);

        // Never silently delete associated trades: block until explicitly confirmed.
        if (tradeCount > 0 && !force)
        {
            return Conflict(new
            {
                message = $"This prop firm has {tradeCount} trade(s). " +
                          "Deleting it will also delete all of its trades.",
                tradeCount
            });
        }

        var deletedTrades = 0;
        if (tradeCount > 0)
            deletedTrades = await _propFirmTradeService.DeleteTradesForFirmAsync(id, userId);

        await _propFirmService.DeletePropFirmAsync(id);

        return Ok(new
        {
            message = "Prop firm deleted successfully",
            deletedTrades
        });
    }
}
