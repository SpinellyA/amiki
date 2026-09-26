namespace Amiki.Core;

/// <summary>Who's signed in, as returned by GET /auth/me. SignInEnabled is false in local dev without Google.</summary>
public sealed record SessionInfo(string Email, string Name, bool SignInEnabled);
