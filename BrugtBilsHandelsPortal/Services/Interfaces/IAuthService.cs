using BrugtBilsHandelsPortal.DTOs.Authentication;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

public interface IAuthService
{
    Task<AuthenticationResult> LoginAsync(
        LoginRequest request);
    
    Task<AuthenticationResult> RegisterAsync(
        RegisterRequest request);
}