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
    private string? _errorMessage;
    private bool _isSaving;
    private bool _isDeleting;

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

    // Fires when the Firestore listener itself fails -- most commonly because a signed-in
    // account isn't the one allowed by firestore.rules. Without this the table would just
    // silently stay empty with a console.error nobody sees.
    [JSInvokable]
    public async Task OnApplicationsError(JsResult result)
    {
        _applications = new();
        _errorMessage = FriendlyError(result);
        await InvokeAsync(StateHasChanged);
    }

    // Maps Firestore/Firebase Auth error codes to messages worth showing a user, instead of
    // raw SDK text like "Missing or insufficient permissions."
    private static string FriendlyError(JsResult result) => result.Code switch
    {
        "permission-denied" =>
            "You don't have access to this data. This tracker is restricted to one Google account.",
        "unavailable" or "network-request-failed" or "auth/network-request-failed" =>
            "Network error — check your connection and try again.",
        "auth/popup-blocked" =>
            "Your browser blocked the sign-in popup. Allow popups for this site and try again.",
        _ => string.IsNullOrWhiteSpace(result.Message) ? "Something went wrong. Please try again." : result.Message,
    };

    private void DismissError() => _errorMessage = null;

    // Recognized by exact (case-insensitive) company name so the applications table can show
    // a real brand mark instead of plain text. Only icons confirmed present in the Font
    // Awesome Free build this project loads (see index.html) belong here; an unmatched
    // company falls back to a generic building icon rather than showing nothing.
    private static readonly Dictionary<string, string> CompanyIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Google"] = "fa-brands fa-google",
        ["Alphabet"] = "fa-brands fa-google",
        ["Microsoft"] = "fa-brands fa-microsoft",
        ["Amazon"] = "fa-brands fa-amazon",
        ["AWS"] = "fa-brands fa-aws",
        ["Apple"] = "fa-brands fa-apple",
        ["Meta"] = "fa-brands fa-meta",
        ["Facebook"] = "fa-brands fa-facebook",
        ["Airbnb"] = "fa-brands fa-airbnb",
        ["Slack"] = "fa-brands fa-slack",
        ["Spotify"] = "fa-brands fa-spotify",
        ["Salesforce"] = "fa-brands fa-salesforce",
        ["Atlassian"] = "fa-brands fa-atlassian",
        ["Dropbox"] = "fa-brands fa-dropbox",
        ["GitHub"] = "fa-brands fa-github",
        ["GitLab"] = "fa-brands fa-gitlab",
        ["PayPal"] = "fa-brands fa-paypal",
        ["Stripe"] = "fa-brands fa-stripe",
        ["Uber"] = "fa-brands fa-uber",
        ["eBay"] = "fa-brands fa-ebay",
    };

    private static string CompanyIconClass(string company) =>
        CompanyIcons.TryGetValue(company.Trim(), out var iconClass) ? iconClass : "fa-solid fa-building";

    private async Task SignIn()
    {
        _errorMessage = null;
        var result = await JS.InvokeAsync<JsResult>("jobTracker.signIn");
        // Ignore the two "codes" that just mean the user closed the popup themselves.
        if (!result.Ok && result.Code is not ("auth/popup-closed-by-user" or "auth/cancelled-popup-request"))
        {
            _errorMessage = FriendlyError(result);
        }
    }

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
        _isSaving = true;
        _errorMessage = null;
        try
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

            var result = _isEditing && _draft.Id is not null
                ? await JS.InvokeAsync<JsResult>("jobTracker.updateApplication", _draft.Id, payload)
                : await JS.InvokeAsync<JsResult>("jobTracker.addApplication", payload);

            if (result.Ok)
            {
                _showForm = false;
            }
            else
            {
                // Leave the form open on failure so the draft isn't lost.
                _errorMessage = FriendlyError(result);
            }
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task DeleteApplication(string id)
    {
        _isDeleting = true;
        _errorMessage = null;
        try
        {
            var result = await JS.InvokeAsync<JsResult>("jobTracker.deleteApplication", id);
            if (!result.Ok)
            {
                _errorMessage = FriendlyError(result);
            }
        }
        finally
        {
            _isDeleting = false;
        }
    }

    public ValueTask DisposeAsync()
    {
        _selfRef?.Dispose();
        return ValueTask.CompletedTask;
    }
}
