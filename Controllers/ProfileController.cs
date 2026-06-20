using Microsoft.AspNetCore.Mvc;
using TradeJournal.Api.Models;
using TradeJournal.Api.Services;

namespace TradeJournal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : Controller
    {
        private readonly FirestoreService _firestoreService;

        public ProfileController(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        [HttpPost]
        public async Task<IActionResult> SaveProfile(UserProfile profile)
        {
            await _firestoreService.SaveUserProfileAsync(profile);
            return Ok(new { message = "Profile saved successfully" });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfile(string id)
        {
            var profile = await _firestoreService.GetUserProfileAsync(id);

            if (profile == null)
                return NotFound();

            return Ok(profile);
        }
    }
}
