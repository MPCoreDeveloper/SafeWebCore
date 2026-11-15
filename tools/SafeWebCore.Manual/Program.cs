using System.Globalization;
using System.Text.RegularExpressions;

namespace SafeWebCore.Manual;

/// <summary>The command line of the manual builder.</summary>
internal static class Program
{
    private const string Product = "SafeWebCore";
    private const string SolutionFile = "SafeWebCore.slnx";

    private static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception exception) when (
            exception is IOException
            or InvalidOperationException
            or FileNotFoundException
            or UnauthorizedAccessException
            or DirectoryNotFoundException)
        {
            Console.Error.WriteLine("manual: " + exception.Message);
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        var options = Options.Parse(args);
        if (options.ShowHelp)
        {
            Options.PrintHelp();
            return 0;
        }

        var root = options.Root ?? FindRoot();
        var version = options.Version ?? ReadVersion(root);
        var sourceDirectory = Path.Combine(root, "docs", "manual", "src");
        var stylesheet = Path.Combine(root, "docs", "manual", "manual.css");
        var logo = Path.Combine(root, "icon.png");
        var buildDirectory = Path.Combine(root, "docs", "manual", "build");
        var htmlPath = Path.Combine(buildDirectory, "manual.html");
        var pdfPath = options.Output ?? Path.Combine(root, "docs", Product + "-Manual-" + version + ".pdf");

        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException("No chapter directory at " + sourceDirectory + ".");
        }

        var usedAnchors = new HashSet<string>(StringComparer.Ordinal) { "contents" };
        var chapters = new List<Chapter>();

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*.md").OrderBy(static path => path, StringComparer.Ordinal))
        {
            chapters.Add(MarkdownConverter.Convert(File.ReadAllText(file), usedAnchors));
            Console.WriteLine("  chapter  " + Path.GetFileName(file));
        }

        var identity = new ManualIdentity(version, Product, "The " + Product + " Manual, version " + version);
        var html = ManualHtml.Assemble(chapters, identity, stylesheet, logo);

        Directory.CreateDirectory(buildDirectory);
        File.WriteAllText(htmlPath, html);

        var headings = chapters.Sum(static chapter => chapter.Headings.Count);
        Console.WriteLine(FormattableString.Invariant(
            $"  chapters {chapters.Count}, headings {headings}, html {html.Length / 1024} KB"));

        if (options.NoPrint)
        {
            Console.WriteLine("  html     " + htmlPath);
            return 0;
        }

        var result = ChromiumPrinter.Print(htmlPath, pdfPath);

        Console.WriteLine("  browser  " + result.Browser);
        Console.WriteLine(FormattableString.Invariant($"  pdf      {result.Path} ({result.Bytes / 1024} KB)"));
        Console.WriteLine("  check    " + (result.IsUsable ? "ok" : "FAILED: the printed file is not a usable PDF"));

        if (!options.KeepHtml && result.IsUsable)
        {
            File.Delete(htmlPath);
        }
        else
        {
            Console.WriteLine("  html     " + htmlPath);
        }

        return result.IsUsable ? 0 : 1;
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFile)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find " + SolutionFile + " above " + AppContext.BaseDirectory
            + ". Pass --root with the repository root.");
    }

    private static string ReadVersion(string root)
    {
        var project = Path.Combine(root, "src", Product, Product + ".csproj");
        if (!File.Exists(project))
        {
            throw new FileNotFoundException("The version was not given and " + project + " does not exist.", project);
        }

        var match = Regex.Match(
            File.ReadAllText(project),
            @"<Version>(?<version>[^<]+)</Version>",
            RegexOptions.None,
            TimeSpan.FromSeconds(2));

        return match.Success
            ? match.Groups["version"].Value.Trim()
            : throw new InvalidOperationException("No <Version> element in " + project + ".");
    }

    /// <summary>The parsed command line.</summary>
    private sealed class Options
    {
        public string? Root { get; private set; }

        public string? Output { get; private set; }

        public string? Version { get; private set; }

        public bool KeepHtml { get; private set; }

        public bool NoPrint { get; private set; }

        public bool ShowHelp { get; private set; }

        public static Options Parse(string[] args)
        {
            var options = new Options();

            for (var index = 0; index < args.Length; index++)
            {
                switch (args[index])
                {
                    case "--root":
                        options.Root = Next(args, ref index);
                        break;
                    case "--out":
                        options.Output = Next(args, ref index);
                        break;
                    case "--version":
                        options.Version = Next(args, ref index);
                        break;
                    case "--keep-html":
                        options.KeepHtml = true;
                        break;
                    case "--no-print":
                        options.NoPrint = true;
                        break;
                    case "--help" or "-h" or "/?":
                        options.ShowHelp = true;
                        break;
                    default:
                        throw new InvalidOperationException("Unknown argument '" + args[index] + "'. Use --help.");
                }
            }

            return options;
        }

        public static void PrintHelp() => Console.WriteLine(
            """
            Builds the SafeWebCore manual from docs/manual/src into docs/SafeWebCore-Manual-<version>.pdf.

              --root <path>       the repository root; found from the tool's location when omitted
              --out <path>        the PDF to write; docs/SafeWebCore-Manual-<version>.pdf by default
              --version <x.y.z>   the edition the cover states; read from src/SafeWebCore/SafeWebCore.csproj
              --keep-html         keep docs/manual/build/manual.html after printing
              --no-print          stop after the HTML is written
              --help              this text

            Chrome or Edge is the only prerequisite beyond the .NET SDK. SAFEWEBCORE_CHROME points the printer at a
            browser installed somewhere else.
            """);

        private static string Next(string[] args, ref int index)
        {
            if (index + 1 >= args.Length)
            {
                throw new InvalidOperationException("'" + args[index] + "' needs a value.");
            }

            index++;
            return args[index];
        }
    }
}
