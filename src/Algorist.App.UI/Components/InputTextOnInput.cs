using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Algorist.App.UI.Components;

/// <summary>An <see cref="InputText"/> that updates its value on every keystroke (e.g. for a live password checklist).</summary>
public sealed class InputTextOnInput : InputText
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttributeIfNotNullOrEmpty(2, "name", NameAttributeValue);
        builder.AddAttributeIfNotNullOrEmpty(3, "class", CssClass);
        builder.AddAttribute(4, "value", CurrentValueAsString);
        builder.AddAttribute(5, "oninput", EventCallback.Factory.CreateBinder<string?>(this, value => CurrentValueAsString = value, CurrentValueAsString));
        builder.AddElementReferenceCapture(6, element => Element = element);
        builder.CloseElement();
    }
}

internal static class RenderTreeBuilderExtensions
{
    public static void AddAttributeIfNotNullOrEmpty(this RenderTreeBuilder builder, int sequence, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            builder.AddAttribute(sequence, name, value);
        }
    }
}
