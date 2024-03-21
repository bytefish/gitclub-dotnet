using GitClub.Infrastructure.Errors;
using GitClub.Models;
using GitClub.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GitClub.Controllers
{
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;

        private readonly ExceptionToApplicationErrorMapper _exceptionToApplicationErrorMapper;

        public AuthController(ILogger<AuthController> logger, ExceptionToApplicationErrorMapper exceptionToODataErrorMapper)
        {
            _logger = logger;
            _exceptionToApplicationErrorMapper = exceptionToODataErrorMapper;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromServices] UserService userService, [FromBody] Credentials credentials, CancellationToken cancellationToken)
        {
            try
            {
                // create a claim for each request scope
                var userClaims = await userService
                    .GetClaimsAsync(credentials.Email, credentials.Roles, cancellationToken)
                    .ConfigureAwait(false);

                // Create the ClaimsPrincipal
                var claimsIdentity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                // It's a valid ClaimsPrincipal, sign in
                await HttpContext.SignInAsync(claimsPrincipal, new AuthenticationProperties { IsPersistent = credentials.RememberMe });

                return Ok();
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok();
        }
    }
}