using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Platform.Application.DTOs.Account;
using Platform.Application.Enums;
using Platform.Application.Exceptions;
using Platform.Application.Interfaces;
using Platform.Application.Wrappers;
using Platform.Domain.Settings;
using Platform.Infrastructure.Identity.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Mcrio.AspNetCore.Identity.On.RavenDb.Model.Role;
using Mcrio.AspNetCore.Identity.On.RavenDb.Model.User;

namespace Platform.Infrastructure.Identity.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<RavenIdentityUser> _userManager;
        private readonly RoleManager<RavenIdentityRole> _roleManager;
        private readonly SignInManager<RavenIdentityUser> _signInManager;
        private readonly JWTSettings _jwtSettings;
        public AccountService(UserManager<RavenIdentityUser> userManager,
            RoleManager<RavenIdentityRole> roleManager,
            IOptions<JWTSettings> jwtSettings,
            IDateTimeService dateTimeService,
            SignInManager<RavenIdentityUser> signInManager)
        {
            _userManager = userManager;
            _jwtSettings = jwtSettings.Value;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        public async Task<Response<bool>> SeedRolesAndClaimsAsync()
        {
            //basic role
            var basicRole = new RavenIdentityRole()
            {
                Id = Guid.NewGuid().ToString(),
                Name = Roles.Basic.ToString(),
                NormalizedName = "BASIC",
            };
                    
            var basicRoleResult = await _roleManager.CreateAsync(basicRole);
                    
            if (basicRoleResult.Succeeded)
            {
                await _roleManager.AddClaimAsync(basicRole, new Claim(ClaimTypes.AuthorizationDecision, "Add"));   
                await _roleManager.AddClaimAsync(basicRole, new Claim(ClaimTypes.AuthorizationDecision, "Edit"));   
                await _roleManager.AddClaimAsync(basicRole, new Claim(ClaimTypes.AuthorizationDecision, "Delete"));   
                await _roleManager.AddClaimAsync(basicRole, new Claim(ClaimTypes.AuthorizationDecision, "Search"));
            }
            
            var adminRole = new RavenIdentityRole()
            {
                Id = Guid.NewGuid().ToString(),
                Name = Roles.Admin.ToString(),
                NormalizedName = "ADMIN",
            };
                    
            var adminRoleResult = await _roleManager.CreateAsync(adminRole);
                    
            if (adminRoleResult.Succeeded)
            {
                await _roleManager.AddClaimAsync(adminRole, new Claim(ClaimTypes.AuthorizationDecision, "Add"));   
                await _roleManager.AddClaimAsync(adminRole, new Claim(ClaimTypes.AuthorizationDecision, "Edit"));   
                await _roleManager.AddClaimAsync(adminRole, new Claim(ClaimTypes.AuthorizationDecision, "Delete"));   
                await _roleManager.AddClaimAsync(adminRole, new Claim(ClaimTypes.AuthorizationDecision, "Search"));
            }

            return new Response<bool>(true);
        }

        public async Task<Response<AuthenticationResponse>> AuthenticateAsync(AuthenticationRequest request, string ipAddress)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                throw new ApiException($"No Accounts Registered with {request.Email}.");
            }
            var result = await _signInManager.PasswordSignInAsync(user.UserName, request.Password, false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                throw new ApiException($"Invalid Credentials for '{request.Email}'.");
            }
            if (!user.EmailConfirmed)
            {
                throw new ApiException($"Account Not Confirmed for '{request.Email}'.");
            }
            return new Response<AuthenticationResponse>(new AuthenticationResponse()
            {
                Id = user.Id,
                JWToken = new JwtSecurityTokenHandler().WriteToken(await GenerateJWToken(user)),
                Email = user.Email,
                UserName = user.UserName,
                Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                IsVerified = user.EmailConfirmed,
                RefreshToken = GenerateRefreshToken(ipAddress).Token,
            }, $"Authenticated {user.UserName}");
        }

        public async Task<Response<string>> RegisterAsync(RegisterRequest request)
            {
            var userWithSameUserName = await _userManager.FindByNameAsync(request.UserName);
            if (userWithSameUserName != null)
            {
                throw new ApiException($"Username '{request.UserName}' is already taken.");
            }
            var user = new RavenIdentityUser
            {
                Email = request.Email,
                UserName = request.UserName
            };
            var userWithSameEmail = await _userManager.FindByEmailAsync(request.Email);
            if (userWithSameEmail == null)
            {
                var result = await _userManager.CreateAsync(user, request.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user,Roles.First.ToString());

                    var verificationCode = await SendVerificationEmail(user);
                    // for now we will return the verification code with result to make user verify his account locally,
                    return new Response<string>(message: $"User Registered. Please confirm your account by using this userId{user.Id} and this Code {verificationCode}");
                }
                else
                {
                    throw new ApiException($"{result.Errors}");
                }
            }
            else
            {
                throw new ApiException($"Email {request.Email} is already registered.");
            }
        }

        private async Task<JwtSecurityToken> GenerateJWToken(RavenIdentityUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            var roleClaims = new List<Claim>();

            for (int i = 0; i < roles.Count; i++)
            {
                roleClaims.Add(new Claim("roles", roles[i]));
            }

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("uid", user.Id),
            }
            .Union(userClaims)
            .Union(roleClaims);

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
                signingCredentials: signingCredentials);
            return jwtSecurityToken;
        }

        private string RandomTokenString()
        {
            using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            var randomBytes = new byte[40];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            // convert random bytes to hex string
            return BitConverter.ToString(randomBytes).Replace("-", "");
        }

        private async Task<string> SendVerificationEmail(RavenIdentityUser user)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        }

        public async Task<Response<string>> ConfirmEmailAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);

            if (result.Succeeded)
            {
                return new Response<string>(user.Id, message: $"Account Confirmed for {user.Email}. You can now use the /api/Account/authenticate endpoint.");
            }
            else
            {
                throw new ApiException($"An error occured while confirming {user.Email}.");
            }
        }

        private RefreshToken GenerateRefreshToken(string ipAddress)
        {
            return new RefreshToken
            {
                Token = RandomTokenString(),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
        }

        public async Task<Response<string>> ForgotPassword(ForgotPasswordRequest model)
        {
            var account = await _userManager.FindByEmailAsync(model.Email);

            // always return ok response to prevent email enumeration
            if (account == null) return new Response<string>(string.Empty);

            var code = await _userManager.GeneratePasswordResetTokenAsync(account);

            // for now we will return the verification code with result to make user verify his account locally,

            return new Response<string>(message: $"You reset token is - {code}");
        }

        public async Task<Response<string>> ResetPassword(ResetPasswordRequest model)
        {
            var account = await _userManager.FindByEmailAsync(model.Email);
            if (account == null) throw new ApiException($"No Accounts Registered with {model.Email}.");
            var result = await _userManager.ResetPasswordAsync(account, model.Token, model.Password);
            if (result.Succeeded)
            {
                return new Response<string>(model.Email, message: $"Password Resetted.");
            }
            else
            {
                throw new ApiException($"Error occured while reseting the password.");
            }
        }
    }

}
