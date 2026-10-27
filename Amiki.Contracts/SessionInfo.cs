namespace Amiki.Contracts;

/// <summary>
/// Who's signed in, as returned by GET /auth/me. SignInEnabled is false in local dev without Google.
/// IsOwner: the person who runs this Amiki (can add people and view their data).
/// NoticePending: someone new who hasn't seen "the owner can view your data, except your diary" yet.
/// </summary>
public sealed record SessionInfo(string Email, string Name, bool SignInEnabled, bool IsOwner = true, bool NoticePending = false);
