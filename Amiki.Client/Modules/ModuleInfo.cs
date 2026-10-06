namespace Amiki.Modules;

/// <summary>
/// What a module tells the shell about itself: a nav entry and optional dashboard widgets.
/// Each module registers one of these from its own AddXxxModule() extension, so adding a
/// module never means editing the layout or the dashboard.
/// </summary>
/// <param name="Overlay">Optional component rendered on every page (e.g. Inbox's quick-capture button).</param>
/// <param name="Section">Which heading of the side nav it's listed under.</param>
public sealed record ModuleInfo(
    string Name,
    string Icon,
    string Href,
    int Order = 100,
    IReadOnlyList<WidgetInfo>? Widgets = null,
    Type? Overlay = null,
    NavSection Section = NavSection.Modules);

/// <param name="Span">Width on the 12-column dashboard grid at desktop size.</param>
public sealed record WidgetInfo(Type Component, int Span = 6);
