using H5P_Samkør_Web_Api.Models;

namespace H5P_Samkør_Web_Api.Services;

public interface ITokenService
{
    string CreateToken(User user, IList<string> roles);
}
