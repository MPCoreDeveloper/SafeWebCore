using System.Diagnostics;
using System.Globalization;

namespace SafeWebCore.Manual;

/// <summary>The outcome of printing the manual.</summary>
/// <param name="Browser">The browser executable that was used.</param>
/// <param name="Path">The file that was written.</param>
/// <param name="Bytes">The size of that file.</param>
/// <param name="HasPdfHeader">Whether the file starts with a PDF header.</param>
internal sealed record PrintResult(string Browser, string Path, long Bytes, bool HasPdfHeader)
{
    /// <summary>Whether the printed file looks like a complete PDF.</summary>
    public bool IsUsable => HasPdfHeader && Bytes > 20_000;
}

/// <summary>
/// Prints an HTML file to PDF with a headless Chromium browser. The browser does one thing only: it lays the pages out.
/// Everything before it is the converter, everything after it is a check of the bytes that were written.
/// </summary>
internal static class ChromiumPrinter
{
    private const string BrowserOverride = "SAFEWEBCORE_CHROME";

    /// <summary>Prints <paramref name="htmlPath"/> to <paramref name="pdfPath"/>.</summary>
    /// <param name="htmlPath">The HTML document to print.</param>
    /// <param name="pdfPath">The PDF to write.</param>
    /// <returns>The outcome, including the browser that produced it.</returns>
    /// <exception cref="FileNotFoundException">No Chrome or Edge installation was found.</exception>
    /// <exception cref="InvalidOperationException">The browser ran and did not produce a usable file.</exception>
    public static PrintResult Print(string htmlPath, string pdfPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);

        var browser = FindBrowser();
        var profile = Path.Combine(Path.GetTempPath(), "safewebcore-manual-" + Guid.NewGuid().ToString("N"));
        var directory = Path.GetDirectoryName(Path.GetFullPath(pdfPath));

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(pdfPath))
        {
            File.Delete(pdfPath);
        }

        var arguments = string.Join(' ',
            "--headless=new",
            "--disable-gpu",
            "--disable-extensions",
            "--no-first-run",
            "--no-pdf-header-footer",
            "--run-all-compositor-stages-before-draw",
            "--virtual-time-budget=20000",
            "--user-data-dir=" + Quote(profile),
            "--print-to-pdf=" + Quote(Path.GetFullPath(pdfPath)),
            Quote(new Uri(Path.GetFullPath(htmlPath)).AbsoluteUri));

        using var process = Process.Start(new ProcessStartInfo(browser, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("The browser process could not be started.");

        process.WaitForExit(milliseconds: 180_000);

        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException("The browser did not finish printing within three minutes.");
        }

        TryDelete(profile);

        var file = new FileInfo(pdfPath);
        if (!file.Exists || file.Length == 0)
        {
            throw new InvalidOperationException(
                "The browser exited with " + process.ExitCode.ToString(CultureInfo.InvariantCulture)
                + " and wrote no PDF. Run with --keep-html and open the HTML to see what it was given.");
        }

        Span<byte> header = stackalloc byte[5];
        using (var stream = file.OpenRead())
        {
            _ = stream.Read(header);
        }

        var hasHeader = header.SequenceEqual("%PDF-"u8);
        return new PrintResult(Path.GetFileName(browser), file.FullName, file.Length, hasHeader);
    }

    private static string FindBrowser()
    {
        var configured = Environment.GetEnvironmentVariable(BrowserOverride);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return File.Exists(configured)
                ? configured
                : throw new FileNotFoundException(
                    "The browser named by " + BrowserOverride + " does not exist: " + configured, configured);
        }

        string[] candidates =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
            "/usr/bin/google-chrome",
            "/usr/bin/chromium",
            "/usr/bin/chromium-browser",
            "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
        ];

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            "No Chrome or Edge installation was found. Install one, or set " + BrowserOverride + " to the executable.");
    }

    private static string Quote(string value) => "\"" + value + "\"";

    private static void TryDelete(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
            // A profile the browser still holds is not worth failing a manual over.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
