using Platform.Application.DTOs.Account;
using Platform.Application.Wrappers;

namespace Platform.Application.Interfaces
{
    public interface IAccountService
    {
        Task<Response<bool>> SeedRolesAndClaimsAsync();
        Task<Response<AuthenticationResponse>> AuthenticateAsync(AuthenticationRequest request, string ipAddress);
        Task<Response<string>> RegisterAsync(RegisterRequest request);
        Task<Response<string>> ConfirmEmailAsync(string userId, string code);
        Task<Response<string>> ForgotPassword(ForgotPasswordRequest model);
        Task<Response<string>> ResetPassword(ResetPasswordRequest model);
    }
}
