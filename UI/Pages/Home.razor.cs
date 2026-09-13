using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using UI.Models;

namespace UI.Pages;

public partial class Home : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private AppUser? _user;
    private List<JobApplication> _applications = new();
    private DotNetObjectReference<Home>? _selfRef;
    private bool _showForm;
    private bool _isEditing;
    private JobApplication _draft = new();

    protected override async Task OnInitializedAsync()
    {
        _selfRef = DotNetObjectReference.Create(this);
        await JS.InvokeVoidAsync("jobTracker.registerAuthCallback", _selfRef);
    }

    [JSInvokable]
    public async Task OnAuthStateChanged(AppUser? user)
    {
        _user = user;
        if (_user is not null)
        {
            await JS.InvokeVoidAsync("jobTracker.registerApplicationsListener", _selfRef);
        }
        else
        {
            _applications = new();
            _showForm = false;
        }
        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public async Task OnApplicationsChanged(string json)
    {
        _applications = JsonSerializer.Deserialize<List<JobApplication>>(json, JsonOptions) ?? new();
        await InvokeAsync(StateHasChanged);
    }

    private async Task SignIn() => await JS.InvokeVoidAsync("jobTracker.signIn");

    private async Task SignOutUser() => await JS.InvokeVoidAsync("jobTracker.signOutUser");

    private void StartAdd()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        _draft = new JobApplication { DateApplied = today, LastUpdate = today };
        _isEditing = false;
        _showForm = true;
    }

    private void StartEdit(JobApplication app)
    {
        _draft = new JobApplication
        {
            Id = app.Id,
            Company = app.Company,
            Role = app.Role,
            JobLink = app.JobLink,
            Status = app.Status,
            DateApplied = app.DateApplied,
            LastUpdate = app.LastUpdate,
            Notes = app.Notes,
        };
        _isEditing = true;
        _showForm = true;
    }

    private void CancelForm() => _showForm = false;

    private async Task SaveDraft()
    {
        _draft.LastUpdate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var payload = new Dictionary<string, object?>
        {
            ["company"] = _draft.Company,
            ["role"] = _draft.Role,
            ["jobLink"] = _draft.JobLink,
            ["status"] = _draft.Status,
            ["dateApplied"] = _draft.DateApplied,
            ["lastUpdate"] = _draft.LastUpdate,
            ["notes"] = _draft.Notes,
        };

        if (_isEditing && _draft.Id is not null)
        {
            await JS.InvokeVoidAsync("jobTracker.updateApplication", _draft.Id, payload);
        }
        else
        {
            await JS.InvokeVoidAsync("jobTracker.addApplication", payload);
        }

        _showForm = false;
    }

    private async Task DeleteApplication(string id) => await JS.InvokeVoidAsync("jobTracker.deleteApplication", id);

    public ValueTask DisposeAsync()
    {
        _selfRef?.Dispose();
        return ValueTask.CompletedTask;
    }
}
