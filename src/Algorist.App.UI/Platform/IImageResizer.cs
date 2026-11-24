using Microsoft.AspNetCore.Components.Forms;

namespace Algorist.App.UI.Platform;

/// <summary>Reduces a chosen image to fit within a size, keeping its format.</summary>
public interface IImageResizer
{
    /// <summary>The resized image bytes, or null when the image could not be read.</summary>
    Task<byte[]?> ResizeAsync(IBrowserFile file, int maxWidth, int maxHeight, long maxBytes, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resizes with the browser (<see cref="BrowserFileExtensions.RequestImageFileAsync"/>), which works in the webapp and
/// in Blazor Hybrid hosts.
/// </summary>
public sealed class BrowserImageResizer : IImageResizer
{
    public async Task<byte[]?> ResizeAsync(IBrowserFile file, int maxWidth, int maxHeight, long maxBytes, CancellationToken cancellationToken = default)
    {
        try
        {
            var resized = await file.RequestImageFileAsync(file.ContentType, maxWidth, maxHeight);
            if (resized.Size > maxBytes)
            {
                return null;
            }

            await using var stream = resized.OpenReadStream(maxBytes, cancellationToken);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            return buffer.ToArray();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Microsoft.JSInterop.JSException)
        {
            return null;
        }
    }
}
