using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeJournal.Api.Models;
using TradeJournal.Api.Services;

namespace TradeJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropFirmTradesController : ControllerBase
{
    private readonly PropFirmService _propFirmService;
    private readonly PropFirmTradeService _propFirmTradeService;

    public PropFirmTradesController(
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

    /// <summary>
    /// Resolves the requested prop firm and guarantees it belongs to the authenticated user.
    /// </summary>
    private async Task<(PropFirm? firm, IActionResult? failure)> ResolveFirmAsync(string propFirmId)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return (null, Unauthorized(new { message = "Invalid or missing authentication token." }));

        var firm = await _propFirmService.GetPropFirmAsync(propFirmId);

        if (firm == null)
            return (null, NotFound(new { message = "Prop firm not found." }));

        if (firm.UserId != userId)
            return (null, Forbidden());

        return (firm, null);
    }

    [HttpGet("{propFirmId}")]
    public async Task<IActionResult> GetTrades(string propFirmId)
    {
        var (firm, failure) = await ResolveFirmAsync(propFirmId);
        if (failure != null) return failure;

        var trades = await _propFirmTradeService.GetTradesAsync(firm!.Id, GetUserId());
        return Ok(trades);
    }

    [HttpGet("{propFirmId}/{tradeId}")]
    public async Task<IActionResult> GetTrade(string propFirmId, string tradeId)
    {
        var (_, failure) = await ResolveFirmAsync(propFirmId);
        if (failure != null) return failure;

        var trade = await _propFirmTradeService.GetTradeAsync(propFirmId, tradeId);

        if (trade == null || trade.UserId != GetUserId())
            return NotFound(new { message = "Trade not found." });

        return Ok(trade);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTrade(PropFirmTrade trade)
    {
        if (trade == null || string.IsNullOrWhiteSpace(trade.PropFirmId))
            return BadRequest(new { message = "PropFirmId is required." });

        var (firm, failure) = await ResolveFirmAsync(trade.PropFirmId);
        if (failure != null) return failure;

        var userId = GetUserId();

        trade.UserId = userId;
        trade.PropFirmId = firm!.Id;
        trade.Id = string.IsNullOrWhiteSpace(trade.Id)
            ? Guid.NewGuid().ToString()
            : trade.Id;

        var now = DateTime.UtcNow;
        trade.CreatedAt = now;
        trade.UpdatedAt = now;

        await _propFirmTradeService.AddTradeAsync(trade);

        return Ok(new { message = "Trade saved successfully", id = trade.Id });
    }

    [HttpPut("{propFirmId}/{tradeId}")]
    public async Task<IActionResult> UpdateTrade(
        string propFirmId,
        string tradeId,
        PropFirmTrade trade)
    {
        var (_, failure) = await ResolveFirmAsync(propFirmId);
        if (failure != null) return failure;

        var userId = GetUserId();

        var existing = await _propFirmTradeService.GetTradeAsync(propFirmId, tradeId);

        if (existing == null)
            return NotFound(new { message = "Trade not found." });

        if (existing.UserId != userId)
            return Forbidden();

        if (trade == null)
            return BadRequest(new { message = "Trade payload is required." });

        trade.Id = tradeId;
        trade.UserId = userId;                 // editing cannot change ownership
        trade.PropFirmId = existing.PropFirmId; // editing cannot move a trade to another firm
        trade.CreatedAt = existing.CreatedAt;
        trade.UpdatedAt = DateTime.UtcNow;

        await _propFirmTradeService.UpdateTradeAsync(tradeId, trade);

        return Ok(new { message = "Trade updated successfully" });
    }

    [HttpDelete("{propFirmId}/{tradeId}")]
    public async Task<IActionResult> DeleteTrade(string propFirmId, string tradeId)
    {
        var (_, failure) = await ResolveFirmAsync(propFirmId);
        if (failure != null) return failure;

        var userId = GetUserId();

        var existing = await _propFirmTradeService.GetTradeAsync(propFirmId, tradeId);

        if (existing == null)
            return NotFound(new { message = "Trade not found." });

        if (existing.UserId != userId)
            return Forbidden();

        await _propFirmTradeService.DeleteTradeAsync(tradeId);

        return Ok(new { message = "Trade deleted successfully" });
    }
}
