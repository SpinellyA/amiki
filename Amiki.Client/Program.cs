using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Amiki;
using Amiki.Data;
using Amiki.Modules.Finance;
using Amiki.Modules.Ideas;
using Amiki.Modules.Inbox;
using Amiki.Modules.Tasks;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomLeft;
    // About 4.5 seconds on screen in total: long enough to hit Undo, short enough to get out of the way.
    config.SnackbarConfiguration.VisibleStateDuration = 4000;
    config.SnackbarConfiguration.ShowTransitionDuration = 200;
    config.SnackbarConfiguration.HideTransitionDuration = 300;
    // MudBlazor keeps snackbars with a button (our "Undo") open until clicked unless told otherwise.
    config.SnackbarConfiguration.RequireInteraction = false;
});

// The API serves this app, so it lives at the same address.
builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<Api>();
builder.Services.AddSingleton<Session>();
builder.Services.AddSingleton<SyncQueue>();
builder.Services.AddSingleton<DataSync>();

// Modules. Each one registers its own services plus a ModuleInfo; the shell picks them up from DI.
builder.Services.AddInboxModule();
builder.Services.AddTasksModule();
builder.Services.AddIdeasModule();
builder.Services.AddFinanceModule();

await builder.Build().RunAsync();
