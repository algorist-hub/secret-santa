using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Algorist.App.UI.Components;

/// <summary>
/// Validates an <see cref="EditForm"/> with a function returning translated messages per model property
/// (built from the rules shared with the API), and marks invalid inputs with Bootstrap's <c>is-invalid</c>.
/// </summary>
public sealed class RulesValidator : ComponentBase, IDisposable
{
    private ValidationMessageStore? _messages;
    private EditContext? _context;
    private bool _submitted;

    [CascadingParameter]
    private EditContext? CurrentEditContext { get; set; }

    /// <summary>Returns the current messages, keyed by model property name (only invalid fields).</summary>
    [Parameter, EditorRequired]
    public Func<IReadOnlyDictionary<string, string>> Validate { get; set; } = default!;

    protected override void OnInitialized()
    {
        _context = CurrentEditContext
            ?? throw new InvalidOperationException($"{nameof(RulesValidator)} must be placed inside an {nameof(EditForm)}.");
        _messages = new ValidationMessageStore(_context);
        _context.SetFieldCssClassProvider(new BootstrapFieldCssClassProvider());
        _context.OnValidationRequested += OnValidationRequested;
        _context.OnFieldChanged += OnFieldChanged;
    }

    public void Dispose()
    {
        if (_context is not null)
        {
            _context.OnValidationRequested -= OnValidationRequested;
            _context.OnFieldChanged -= OnFieldChanged;
        }
    }

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        _submitted = true;
        _messages!.Clear();
        foreach (var (field, message) in Validate())
        {
            _messages.Add(_context!.Field(field), message);
        }

        _context!.NotifyValidationStateChanged();
    }

    // After the first submit attempt, re-check a field as soon as it changes so its message clears or updates.
    private void OnFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        if (!_submitted)
        {
            return;
        }

        _messages!.Clear(e.FieldIdentifier);
        if (Validate().TryGetValue(e.FieldIdentifier.FieldName, out var message))
        {
            _messages.Add(e.FieldIdentifier, message);
        }

        _context!.NotifyValidationStateChanged();
    }

    private sealed class BootstrapFieldCssClassProvider : FieldCssClassProvider
    {
        public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier) =>
            editContext.GetValidationMessages(fieldIdentifier).Any() ? "is-invalid" : string.Empty;
    }
}
