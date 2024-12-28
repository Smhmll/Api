using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using Drivers.Api.Configurations;
using Drivers.Api.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Drivers.Api.Controllers;

 
[ApiController]
[Route("api/[controller]")]
public class AuthManagementController : ControllerBase
{
    private readonly ILogger<AuthManagementController> _logger;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly JwtConfig _jwtConfig;

    public AuthManagementController(
        ILogger<AuthManagementController> logger,
        UserManager<IdentityUser> userManager,
        IOptionsMonitor<JwtConfig> optionsMonitor)
    {
        _logger = logger;
        _userManager = userManager;
        _jwtConfig = optionsMonitor.CurrentValue;
    }

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto requestDto)
    {
        if (ModelState.IsValid)
        {
            // Check if email exists
            var emailExist = await _userManager.FindByEmailAsync(requestDto.Email);
            if (emailExist != null)
                return BadRequest(new { Error = "Email already exists" });

            var newUser = new IdentityUser()
            {
                Email = requestDto.Email,
                
            };

            var isCreated = await _userManager.CreateAsync(newUser, requestDto.Password);
            if (isCreated.Succeeded)
            {
                // Generate Token
                var token = GenerateJwtToken(newUser);
                return Ok(new RegistrationRequestResponse()
                {
                    Result = true,
                    Token = token
                });
            }

            return BadRequest(new { Error = "Error creating the user, please try again later" });
        }

        return BadRequest(new { Error = "Invalid request payload" });
    }

    private string GenerateJwtToken(IdentityUser user)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);

        // Define token descriptor
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(type:"Id", value:user.Id),
                new Claim(type:JwtRegisteredClaimNames.Sub, value: user.Email),
                new Claim(type:JwtRegisteredClaimNames.Email, value: user.Email),
                new Claim(type:JwtRegisteredClaimNames.Jti, value:Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(6), // Token expiration time
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha512)
        };

        // Generate token
         var token = jwtTokenHandler.CreateToken(tokenDescriptor);
        return jwtTokenHandler.WriteToken(token);
    }
}
