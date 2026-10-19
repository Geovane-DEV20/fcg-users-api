using Microsoft.AspNetCore.Mvc;

namespace FcgUsersApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Users API is running" });
    }
}
