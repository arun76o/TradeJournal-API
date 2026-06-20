using Microsoft.AspNetCore.Mvc;
using TradeJournal.Api.Models;
using TradeJournal.Api.Services;

namespace TradeJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradesController : ControllerBase
{
    private readonly FirestoreService _firestoreService;

    public TradesController(FirestoreService firestoreService)
    {
        _firestoreService = firestoreService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTrade(Trade trade)
    {
        await _firestoreService.AddTradeAsync(trade);
        return Ok(new { message = "Trade saved successfully" });
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetTrades(string userId)
    {
        var trades = await _firestoreService.GetTradesAsync(userId);
        return Ok(trades);
    }
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteTrade(string id)
    {
        await _firestoreService.DeleteTradeAsync(id);

        return Ok(new
        {
            message = "Trade deleted successfully"
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTrade(
        string id,
        Trade trade)
    {
        await _firestoreService.UpdateTradeAsync(
            id,
            trade
        );

        return Ok(new
        {
            message = "Trade updated successfully"
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTradeById(string id)
    {
        var trade = await _firestoreService.GetTradeByIdAsync(id);

        if (trade == null)
            return NotFound();

        return Ok(trade);
    }

}