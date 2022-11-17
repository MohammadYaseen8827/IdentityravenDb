using Microsoft.AspNetCore.Mvc;
using Platform.Application.Interfaces;
using Platform.Application.DTOs.Account;

namespace Platform.API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost("SeedRolesAndClaims")]
    public async Task<IActionResult> SeedRolesAnaClaimsAsync()
    {
        return Ok(await _accountService.SeedRolesAndClaimsAsync());
    }
    [HttpPost("authenticate")]
    public async Task<IActionResult> AuthenticateAsync(AuthenticationRequest request)
    {
        return Ok(await _accountService.AuthenticateAsync(request, GenerateIPAddress()));
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync(RegisterRequest request)
    {
        return Ok(await _accountService.RegisterAsync(request));
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmailAsync([FromQuery] string userId, [FromQuery] string code)
    {
        return Ok(await _accountService.ConfirmEmailAsync(userId, code));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest model)
    {
        return Ok(await _accountService.ForgotPassword(model));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest model)
    {

        return Ok(await _accountService.ResetPassword(model));
    }

    private string GenerateIPAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"];
        else
            return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
    }

    [HttpGet]
    [Route("currentuser")]
    public IActionResult GetCurrentUser()
    {
        var currentUser = User.Identity.IsAuthenticated ? User.Identity.Name : null;
        return Ok(currentUser);
    }
}