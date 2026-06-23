using System;
using System.Collections.Generic;
using System.Text;

namespace WaslX.Application.Features.Auth.Dtos;

public record AuthResponse(
    string UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    string AccessToken,
    DateTime AccessTokenExpiresOn,
    string RefreshToken,
    bool IsDisabled,
    bool IsEmailConfirmed);
