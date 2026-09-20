#requires -Version 5.1
<#
.SYNOPSIS
Generate a source-backed HMS architecture snapshot, or check its freshness.
.DESCRIPTION
Requires the .NET 10 SDK and restored HMS packages (dotnet restore).
Uses SDK-shipped Roslyn; adds no package or project references to HMS.
Only graphify-out/{graph.json,graph.html,GRAPH_REPORT.md} are replaced.
The embedded analyzer is compiled in a unique temporary directory, removed on exit.
Existing Graphify CLI caches and project-local snapshots are left untouched.
Use this generator to refresh this versioned schema, not graphify update.
.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\tools\graphify.ps1
.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\tools\graphify.ps1 -Check
#>
[CmdletBinding()]
param([switch]$Check)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$utf8 = New-Object System.Text.UTF8Encoding($false)
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$work = Join-Path $tempRoot ('hms-graphify-' + [Guid]::NewGuid().ToString('N'))

$analyzer = @'
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class Program
{
    static string Root = "";
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    static readonly HashSet<string> Excluded = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".vs", "TestResults", ".git", "graphify-out", ".agents", ".codex", "node_modules", ".specify" };
    static readonly Dictionary<string, Node> Nodes = new(StringComparer.Ordinal);
    static readonly List<Edge> Edges = [];
    static readonly List<string> Warnings = [];
    static string Rel(string p) => Path.GetRelativePath(Root, p).Replace('\\', '/');
    static string Hash(byte[] b) => Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant();
    static IEnumerable<string> Scan(string directory)
    {
        foreach (var path in Directory.EnumerateFileSystemEntries(directory).Order(StringComparer.Ordinal))
        {
            if (Excluded.Contains(Path.GetFileName(path))) continue;
            var attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.ReparsePoint)) { Warnings.Add($"Skipped linked path: {Rel(path)}"); continue; }
            if (attr.HasFlag(FileAttributes.Directory)) { foreach (var f in Scan(path)) yield return f; }
            else yield return path;
        }
    }
    static string Run(string tool, params string[] args)
    {
        var start = new ProcessStartInfo(tool) { WorkingDirectory = Root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var a in args) start.ArgumentList.Add(a);
        using var p = Process.Start(start) ?? throw new Exception($"Cannot start {tool}");
        var output = p.StandardOutput.ReadToEndAsync(); var error = p.StandardError.ReadToEndAsync();
        p.WaitForExit(); Task.WaitAll(output, error);
        if (p.ExitCode != 0) throw new Exception($"{tool} {string.Join(' ', args)} failed: {error.Result} {output.Result}");
        return output.Result.Trim();
    }
    static string? Git(params string[] args) { try { return Run("git", args); } catch { return null; } }
    static Node Add(Node node) { if (!Nodes.TryAdd(node.Id, node)) return Nodes[node.Id]; return node; }
    static void Link(string a, string b, string kind, string evidence) => Edges.Add(new(a, b, kind, evidence));
    static string TypeId(INamedTypeSymbol t) => "type:" + t.OriginalDefinition.ToDisplayString();
    static bool IsInside(string path) => Path.GetFullPath(path).StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    static List<InputFile> Inputs(IEnumerable<string> paths) => paths.OrderBy(Rel, StringComparer.Ordinal).Select(p => new InputFile(Rel(p), Hash(File.ReadAllBytes(p)))).ToList();
    static string Fingerprint(List<InputFile> inputs) => Hash(Encoding.UTF8.GetBytes(string.Join("\n", inputs.Select(f => f.Path + "\0" + f.Sha256))));
    static int Main(string[] args)
    {
        try { Generate(args); return 0; }
        catch (Exception e) { Console.Error.WriteLine("[FAIL] " + e.Message); return 1; }
    }
    static void Generate(string[] args)
    {
        Root = Path.GetFullPath(args[0]).TrimEnd(Path.DirectorySeparatorChar);
        var allFiles = Scan(Root).ToArray();
        var inputPaths = allFiles.Where(p => new[] { ".cs", ".csproj", ".sln", ".slnx", ".razor", ".props", ".targets" }.Contains(Path.GetExtension(p), StringComparer.OrdinalIgnoreCase)
            || Path.GetFileName(p) is "README.md" or "global.json" or "NuGet.Config" or "packages.lock.json").ToArray();
        var inputs = Inputs(inputPaths); var fingerprint = Fingerprint(inputs);
        var outputDirectory = Path.Combine(Root, "graphify-out");
        if (args[2] == "check")
        {
            using var previous = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDirectory, "graph.json")));
            if (previous.RootElement.GetProperty("sourceFingerprint").GetString() != fingerprint) throw new Exception("Snapshot is stale. Run tools/graphify.ps1 to regenerate.");
            if (previous.RootElement.GetProperty("generatorFingerprint").GetString() != Hash(File.ReadAllBytes(Path.Combine(Root, "tools", "graphify.ps1")))) throw new Exception("Generator changed. Regenerate the snapshot.");
            Console.WriteLine("[PASS] Source and generator fingerprints match. No snapshot files written."); return;
        }
        var solutionFiles = allFiles.Where(p => Path.GetExtension(p) is ".sln" or ".slnx").ToArray();
        if (solutionFiles.Length != 1) throw new Exception($"Expected one solution; found {solutionFiles.Length}.");
        var solution = solutionFiles[0];
        var members = Run("dotnet", "sln", solution, "list").Split('\n').Select(s => s.Trim()).Where(s => s.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(p => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(solution)!, p))).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var projects = new List<Project>();
        foreach (var file in allFiles.Where(p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
        {
            var xml = XDocument.Load(file); var name = Path.GetFileNameWithoutExtension(file);
            var evaluatedText = Run("dotnet", "msbuild", file, "-nologo", "-getProperty:TargetFramework,TargetFrameworks,ImplicitUsings,DefineConstants", "-getItem:Compile,ProjectReference,PackageReference");
            using var evaluated = JsonDocument.Parse(evaluatedText);
            var properties = evaluated.RootElement.GetProperty("Properties"); var items = evaluated.RootElement.GetProperty("Items");
            var framework = properties.GetProperty("TargetFramework").GetString() ?? "";
            if (!string.IsNullOrEmpty(properties.GetProperty("TargetFrameworks").GetString())) throw new Exception($"Multi-target analysis is not supported: {Rel(file)}");
            if (framework != "net10.0") throw new Exception($"This analyzer requires net10.0; found {framework} in {Rel(file)}.");
            var packages = items.GetProperty("PackageReference").EnumerateArray().Select(p => new Package(p.GetProperty("Identity").GetString()!, p.TryGetProperty("Version", out var v) ? v.GetString() ?? "" : "", "MSBuild evaluated PackageReference")).OrderBy(p => p.Name).ToList();
            var source = items.GetProperty("Compile").EnumerateArray().Select(p => p.GetProperty("FullPath").GetString()!).Where(p => !Rel(p).Split('/').Any(Excluded.Contains)).ToList();
            if (source.Any(p => !IsInside(p))) throw new Exception($"External linked sources require manual review: {name}");
            var references = items.GetProperty("ProjectReference").EnumerateArray().Select(p => Rel(p.GetProperty("FullPath").GetString()!)).Order().ToList();
            var project = new Project { Name = name, Id = "project:" + name, Path = Rel(file), TargetFramework = framework, InSolution = members.Contains(file),
                Layer = name.Split('.').Last(), PackageReferences = packages, ProjectReferences = references,
                Sdk = xml.Root?.Attribute("Sdk")?.Value ?? "", SourceFiles = source.Select(Rel).Order().ToList(),
                Defines = properties.GetProperty("DefineConstants").GetString() ?? "", ImplicitUsings = properties.GetProperty("ImplicitUsings").GetString() == "enable" };
            projects.Add(project);
        }
        if (members.Any(m => projects.All(p => p.Path != Rel(m)))) throw new Exception("Solution contains an undiscovered project.");
        var solutionId = "solution:" + Rel(solution);
        Add(new() { Id = solutionId, Name = Path.GetFileName(solution), Kind = "solution", File = Rel(solution) });
        foreach (var (p, index) in projects.Select((p, i) => (p, i)))
        {
            Add(new() { Id = p.Id, Name = p.Name, Kind = "project", Project = p.Name, File = p.Path, Community = index });
            if (p.InSolution) Link(solutionId, p.Id, "contains", Rel(solution));
            foreach (var reference in p.ProjectReferences)
            {
                var target = projects.SingleOrDefault(t => t.Path == reference) ?? throw new Exception($"Unresolved project reference: {reference}");
                Link(p.Id, target.Id, "project-reference", p.Path);
            }
            foreach (var source in p.SourceFiles.Concat(allFiles.Where(f => f.EndsWith(".razor") && f.StartsWith(Path.GetDirectoryName(Path.Combine(Root, p.Path))! + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)).Select(Rel)).Distinct())
            {
                Add(new() { Id = "file:" + p.Name + ":" + source, Name = Path.GetFileName(source), Kind = "file", File = source, Project = p.Name, Community = index });
                Link(p.Id, "file:" + p.Name + ":" + source, "contains", p.Path);
            }
        }

        // Roslyn resolves declarations and type references. It never treats comments
        // or embedded setup-script strings as production C# declarations.
        var metadata = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var p in projects)
        {
            var assets = Path.Combine(Root, Path.GetDirectoryName(p.Path)!, "obj", "project.assets.json");
            if (!File.Exists(assets)) throw new Exception($"Restore packages before generation: missing {Rel(assets)}");
            using var assetDoc = JsonDocument.Parse(File.ReadAllText(assets));
            var a = assetDoc.RootElement; var packageFolders = a.GetProperty("packageFolders").EnumerateObject().Select(x => x.Name).ToArray();
            foreach (var target in a.GetProperty("targets").EnumerateObject()) foreach (var library in target.Value.EnumerateObject())
            {
                if (!library.Value.TryGetProperty("compile", out var compile) || !a.GetProperty("libraries").TryGetProperty(library.Name, out var info) || !info.TryGetProperty("path", out var relative)) continue;
                foreach (var dll in compile.EnumerateObject().Where(x => x.Name.EndsWith(".dll"))) foreach (var folder in packageFolders)
                {
                    var full = Path.Combine(folder, relative.GetString()!, dll.Name);
                    if (File.Exists(full)) metadata.Add(full);
                }
            }
        }
        // Prefer runtime framework references over duplicate NuGet framework facades.
        var referencesMetadata = metadata.GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).Select(g => MetadataReference.CreateFromFile(g.First())).ToList();
        var trees = new List<SyntaxTree>(); var owner = new Dictionary<SyntaxTree, Project>();
        foreach (var p in projects) foreach (var source in p.SourceFiles)
        {
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest).WithPreprocessorSymbols(p.Defines.Split(';', StringSplitOptions.RemoveEmptyEntries).Concat(["NET", "NET10_0", "NET10_0_OR_GREATER"]));
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(Root, source)), options, source);
            var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            if (errors.Length > 0) throw new Exception(string.Join("\n", errors.Select(e => e.ToString())));
            trees.Add(tree); owner[tree] = p;
        }
        // These standard imports reflect the current projects' enabled ImplicitUsings.
        if (projects.Any(p => !p.ImplicitUsings)) throw new Exception("Mixed/disabled implicit usings require per-project analysis; refusing to guess.");
        var globals = CSharpSyntaxTree.ParseText("global using System; global using System.Collections.Generic; global using System.Linq; global using System.Threading.Tasks; global using System.Threading; global using System.IO; global using System.Net.Http;");
        var compilation = CSharpCompilation.Create("ArchitectureSnapshot", trees.Append(globals), referencesMetadata, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var declarations = new List<(INamedTypeSymbol Symbol, BaseTypeDeclarationSyntax Syntax, Node Node)>();
        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree); var p = owner[tree];
            foreach (var declaration in tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                var symbol = model.GetDeclaredSymbol(declaration) as INamedTypeSymbol ?? throw new Exception($"Cannot resolve declaration in {tree.FilePath}");
                var methods = symbol.GetMembers().OfType<IMethodSymbol>().Where(m => m.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() is "Xunit.FactAttribute" or "Xunit.TheoryAttribute")).ToArray();
                var testCases = 0; var unknownTheories = 0;
                foreach (var m in methods)
                {
                    var attrs = m.GetAttributes();
                    if (attrs.Any(a => a.AttributeClass?.ToDisplayString() == "Xunit.FactAttribute")) testCases++;
                    else { var rows = attrs.Count(a => a.AttributeClass?.ToDisplayString() == "Xunit.InlineDataAttribute"); testCases += rows; if (rows == 0 || attrs.Any(a => a.AttributeClass?.Name is "MemberDataAttribute" or "ClassDataAttribute")) unknownTheories++; }
                }
                var kind = symbol.TypeKind == TypeKind.Interface ? "interface" : symbol.TypeKind == TypeKind.Enum ? "enum" : symbol.IsRecord ? "record" : methods.Length > 0 ? "test-class" : symbol.IsAbstract && !symbol.IsStatic ? "abstract-class" : symbol.TypeKind == TypeKind.Struct ? "struct" : "class";
                var node = Add(new() { Id = TypeId(symbol), Name = symbol.Name, FullName = symbol.ToDisplayString(), Kind = kind, Project = p.Name,
                    Namespace = symbol.ContainingNamespace.IsGlobalNamespace ? "" : symbol.ContainingNamespace.ToDisplayString(), File = tree.FilePath,
                    Line = declaration.GetLocation().GetLineSpan().StartLinePosition.Line + 1, Accessibility = symbol.DeclaredAccessibility.ToString().ToLowerInvariant(),
                    Abstract = symbol.IsAbstract, Sealed = symbol.IsSealed, Static = symbol.IsStatic, Community = projects.IndexOf(p),
                    TestMethods = methods.Select(m => m.Name).Order().ToArray(), StaticTestCases = testCases, DynamicTheories = unknownTheories });
                declarations.Add((symbol, declaration, node));
                Link("file:" + p.Name + ":" + tree.FilePath, node.Id, "declares", $"{tree.FilePath}:L{node.Line}");
                var nsId = "namespace:" + p.Name + ":" + node.Namespace;
                Add(new() { Id = nsId, Name = node.Namespace ?? "", Kind = "namespace", Namespace = node.Namespace, Project = p.Name, Community = projects.IndexOf(p) });
                Link(p.Id, nsId, "contains", tree.FilePath); Link(node.Id, nsId, "namespace-membership", tree.FilePath);
                if (symbol.ContainingType is not null) Link(TypeId(symbol.ContainingType), node.Id, "contains", tree.FilePath);
            }
        }
        foreach (var d in declarations)
        {
            void Base(INamedTypeSymbol? t, string relation)
            {
                if (t is null || t.SpecialType == SpecialType.System_Object || t.SpecialType == SpecialType.System_ValueType || t.SpecialType == SpecialType.System_Enum) return;
                if (t.TypeKind == TypeKind.Error) { Warnings.Add($"Unresolved base omitted: {d.Node.FullName} : {t}"); return; }
                var id = TypeId(t);
                if (!Nodes.ContainsKey(id)) Add(new() { Id = id, Name = t.Name, FullName = t.ToDisplayString(), Kind = t.TypeKind == TypeKind.Interface ? "interface" : "class", Namespace = t.ContainingNamespace.ToDisplayString(), External = true, Community = -1 });
                Link(d.Node.Id, id, relation, $"{d.Node.File}:L{d.Node.Line}");
            }
            Base(d.Symbol.BaseType, "inherits");
            foreach (var i in d.Symbol.Interfaces) Base(i, d.Symbol.TypeKind == TypeKind.Interface ? "inherits" : "implements");
            if (d.Node.Kind != "test-class") continue;
            var model = compilation.GetSemanticModel(d.Syntax.SyntaxTree);
            var targets = new HashSet<string>();
            foreach (var syntax in d.Syntax.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                var s = model.GetSymbolInfo(syntax).Symbol;
                var type = s as INamedTypeSymbol ?? s?.ContainingType;
                for (var t = type; t is not null; t = t.BaseType)
                    if (Nodes.TryGetValue(TypeId(t), out var target) && !target.External && target.Project != d.Node.Project && target.Kind is "class" or "abstract-class" or "interface" or "record" or "enum") targets.Add(target.Id);
            }
            foreach (var target in targets) Link(d.Node.Id, target, "test-target", d.Node.File + " (Roslyn-bound type/member use or base of test helper)");
        }
        var edges = Edges.DistinctBy(e => (e.Source, e.Target, e.Type)).OrderBy(e => e.Source, StringComparer.Ordinal).ThenBy(e => e.Type, StringComparer.Ordinal).ThenBy(e => e.Target, StringComparer.Ordinal).ToList();
        if (edges.Any(e => !Nodes.ContainsKey(e.Source) || !Nodes.ContainsKey(e.Target))) throw new Exception("Graph validation failed: dangling edge.");
        var projectEdges = edges.Where(e => e.Type == "project-reference").ToArray();
        var visiting = new HashSet<string>(); var visited = new HashSet<string>();
        bool Cycle(string id) { if (visiting.Contains(id)) return true; if (!visited.Add(id)) return false; visiting.Add(id); foreach (var e in projectEdges.Where(e => e.Source == id)) if (Cycle(e.Target)) return true; visiting.Remove(id); return false; }
        bool cycles = projects.Any(p => Cycle(p.Id));
        var expected = new Dictionary<string, string[]> { ["Core"] = [], ["Application"] = ["Core"], ["Infrastructure"] = ["Core", "Application"], ["Web"] = ["Application", "Infrastructure"], ["Tests"] = ["Core", "Application", "Infrastructure"] };
        var unexpected = projects.Where(p => !expected.ContainsKey(p.Layer) || !projectEdges.Where(e => e.Source == p.Id).Select(e => projects.Single(x => x.Id == e.Target).Layer).Order().SequenceEqual(expected[p.Layer].Order()))
            .Select(p => p.Name + ": observed references differ from documented HMS layer rules").ToList();
        var core = projects.SingleOrDefault(p => p.Layer == "Core");
        var packageNames = projects.SelectMany(p => p.PackageReferences).Select(p => p.Name).ToArray();
        var forbiddenUsings = trees.Where(t => owner[t] == core).SelectMany(t => t.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>())
            .Where(u => u.Name?.ToString().StartsWith("Microsoft.AspNetCore") == true || u.Name?.ToString().StartsWith("Microsoft.EntityFrameworkCore") == true || u.Name?.ToString().StartsWith("ElsheiekhHMS.Application") == true || u.Name?.ToString().StartsWith("ElsheiekhHMS.Infrastructure") == true || u.Name?.ToString().StartsWith("ElsheiekhHMS.Web") == true).Select(u => u.ToString()).ToArray();
        var readme = Path.Combine(Root, "README.md");
        var declaredStatus = File.Exists(readme) ? File.ReadAllLines(readme).Select((line, i) => new { line, number = i + 1 }).Where(x => x.line.StartsWith("> **Current development stage:") || x.line.StartsWith("> **Phase") || x.line.StartsWith("> **Next action:") || x.line.StartsWith("| 0"))
            .Select(x => new { text = x.line, source = $"README.md:L{x.number}" }).ToArray() : [];
        var architecture = new { layers = projects.Select(p => p.Layer), dependencyRules = new { coreIndependent = core is not null && core.ProjectReferences.Count == 0 && core.PackageReferences.Count == 0 && forbiddenUsings.Length == 0,
            circularProjectDependenciesDetected = cycles, unexpectedDependencies = unexpected, forbiddenCoreUsings = forbiddenUsings, ruleSource = "Documented HMS layer policy; observed edges extracted separately" },
            packageChecks = new { efCorePresent = packageNames.Any(n => n.StartsWith("Microsoft.EntityFrameworkCore")), sqlServerPresent = packageNames.Any(n => n.Contains("SqlServer") || n.Contains("SqlClient")), identityPackagePresent = packageNames.Any(n => n.Contains("Identity")) },
            identitySourceReferences = trees.SelectMany(t => t.GetRoot().DescendantNodes().OfType<NameSyntax>()).Where(n => n.ToString().StartsWith("Microsoft.AspNetCore.Identity")).Select(n => n.SyntaxTree.FilePath).Distinct().ToArray(),
            phase = new { statusSource = "README.md declaration, not independently certified completion", declaredStatus } };
        var types = Nodes.Values.Where(n => n.FullName is not null && !n.External).ToArray();
        var tests = types.Where(n => n.Kind == "test-class").ToArray();
        if (projects.Any(p => p.PackageReferences.Any(r => r.Name == "xunit")) && tests.Length == 0) Warnings.Add("No source test classes resolved; verify xUnit reference resolution.");
        var unowned = allFiles.Where(p => p.EndsWith(".cs") && projects.All(project => !project.SourceFiles.Contains(Rel(p)))).Select(Rel).ToArray();
        if (unowned.Length > 0) Warnings.Add("C# files outside evaluated Compile items: " + string.Join(", ", unowned));
        var summary = new { projectCount = projects.Count, sourceFileCount = projects.SelectMany(p => p.SourceFiles).Distinct().Count(), razorFileCount = Nodes.Values.Count(n => n.Kind == "file" && n.File!.EndsWith(".razor")), typeCount = types.Length,
            interfaceCount = types.Count(n => n.Kind == "interface"), testClassCount = tests.Length, testMethodCount = tests.Sum(n => n.TestMethods.Length), testCount = tests.Sum(n => n.StaticTestCases), dynamicTheoryCount = tests.Sum(n => n.DynamicTheories), nodeCount = Nodes.Count, edgeCount = edges.Count };
        var gitStatus = Git("status", "--porcelain=v1", "--untracked-files=all");
        var generated = DateTimeOffset.UtcNow.ToString("O"); var commit = Git("rev-parse", "HEAD"); var branch = Git("branch", "--show-current");
        var graph = new { schemaVersion = "1.0", generatedAtUtc = generated, gitCommit = commit, gitBranch = branch, workingTreeDirty = gitStatus is null ? (bool?)null : gitStatus.Length > 0,
            gitStatusAtGeneration = gitStatus, sourceFingerprint = fingerprint, generatorFingerprint = Hash(File.ReadAllBytes(Path.Combine(Root, "tools", "graphify.ps1"))),
            fingerprintAlgorithm = "SHA-256 of ordinal path + NUL + content SHA-256 records joined with LF; sourceManifest defines inputs; graphify-out excluded everywhere", sourceManifest = inputs,
            repository = new { name = Path.GetFileName(Root), framework = string.Join(", ", projects.Select(p => p.TargetFramework).Distinct()), solutionFile = Rel(solution) }, summary, projects,
            nodes = Nodes.Values.OrderBy(n => n.Id, StringComparer.Ordinal), edges, architecture,
            analysis = new { parser = "SDK Roslyn AST and semantic binding; MSBuild evaluated items plus project XML", scope = "Debug/net10.0 C# Compile items; Razor files indexed without generated component types; implicit Program and generated code excluded",
                limitations = new[] { "Single combined C# analysis compilation; not a replacement for project-by-project compilation.", "Only resolved bases and test targets become edges; unresolved relationships are omitted with warnings.", "testCount is static Fact + InlineData case count, not a test execution result; dynamic theories reported separately.", "Package list is evaluated direct PackageReference only; transitive and SDK auto-references are not claimed as installed direct packages.", "Architecture checks are structural observations, not a full semantic security or dependency audit." },
                warnings = Warnings.Distinct().ToArray(), llmApiCalls = 0, llmTokens = 0 },
            // NetworkX/Graphify node-link compatibility. edges is canonical; links is an identical alias.
            directed = true, multigraph = false, graph = new { name = Path.GetFileName(Root), schemaVersion = "1.0" }, links = edges };
        var json = JsonSerializer.Serialize(graph, Json);
        using var parsed = JsonDocument.Parse(json);
        if (Fingerprint(Inputs(inputPaths)) != fingerprint) throw new Exception("Source changed during generation; retry.");
        var html = File.ReadAllText(args[1]).Replace("__GRAPH_JSON__", json);
        var report = Report(parsed.RootElement);
        Directory.CreateDirectory(outputDirectory);
        if (File.GetAttributes(outputDirectory).HasFlag(FileAttributes.ReparsePoint)) throw new Exception("Refusing to write into a linked output directory.");
        foreach (var name in new[] { "graph.json", "graph.html", "GRAPH_REPORT.md" })
        { var p = Path.Combine(outputDirectory, name); if (File.Exists(p) && File.GetAttributes(p).HasFlag(FileAttributes.ReparsePoint)) throw new Exception("Refusing to replace linked output: " + name); }
        File.WriteAllText(Path.Combine(outputDirectory, "graph.json"), json + "\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(outputDirectory, "graph.html"), html, new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(outputDirectory, "GRAPH_REPORT.md"), report, new UTF8Encoding(false));
        Console.WriteLine($"[PASS] {summary.projectCount} projects, {summary.sourceFileCount} C# files, {summary.typeCount} types, {summary.interfaceCount} interfaces, {summary.testCount} static test cases, {summary.edgeCount} edges.");
        Console.WriteLine($"[PASS] Fingerprint: {fingerprint}");
        foreach (var warning in Warnings.Distinct()) Console.WriteLine("[WARNING] " + warning);
    }
    static string Report(JsonElement g)
    {
        var b = new StringBuilder(); void L(string s = "") => b.AppendLine(s);
        var nodes = g.GetProperty("nodes").EnumerateArray().ToDictionary(n => n.GetProperty("id").GetString()!);
        string Name(string id) => nodes[id].TryGetProperty("fullName", out var f) ? f.GetString()! : nodes[id].GetProperty("name").GetString()!;
        var edges = g.GetProperty("edges").EnumerateArray().ToArray();
        L("# " + g.GetProperty("repository").GetProperty("name").GetString() + " Architecture Graph Report"); L();
        L("## Snapshot"); L();
        foreach (var key in new[] { "generatedAtUtc", "gitBranch", "gitCommit", "workingTreeDirty", "sourceFingerprint" }) L($"- **{key}:** `{g.GetProperty(key)}`");
        L("- Framework: " + g.GetProperty("repository").GetProperty("framework"));
        L("- Structural extraction only: **0 LLM API calls / 0 LLM tokens**. No statistical community clustering is claimed; project ownership supplies the visual groups."); L();
        L("## Solution"); L(); L("`" + g.GetProperty("repository").GetProperty("solutionFile") + "`"); L();
        foreach (var p in g.GetProperty("projects").EnumerateArray()) L($"- [{p.GetProperty("name") }](../{p.GetProperty("path")}) — {p.GetProperty("targetFramework")}; solution member: {p.GetProperty("inSolution")}");
        L(); L("## Project Dependencies"); L(); L("Arrows mean references; extracted from evaluated ProjectReference items."); L(); L("```text");
        foreach (var p in g.GetProperty("projects").EnumerateArray()) { var refs = edges.Where(e => e.GetProperty("type").GetString() == "project-reference" && e.GetProperty("source").GetString() == p.GetProperty("id").GetString()).Select(e => Name(e.GetProperty("target").GetString()!)); L(p.GetProperty("name") + " -> " + (refs.Any() ? string.Join(", ", refs) : "(none)")); } L("```");
        L(); L("## Core Foundation"); L();
        foreach (var n in nodes.Values.Where(n => n.TryGetProperty("project", out var p) && p.GetString()!.EndsWith(".Core") && n.TryGetProperty("fullName", out _))) L($"- [{n.GetProperty("fullName")}](../{n.GetProperty("file")}) — {n.GetProperty("kind")}, {n.GetProperty("accessibility")}");
        L(); L("## Inheritance and Exceptions"); L(); L("Derived -> base (includes private test helper types and resolved external bases):"); L();
        foreach (var e in edges.Where(e => e.GetProperty("type").GetString() == "inherits")) L($"- `{Name(e.GetProperty("source").GetString()!)}` -> `{Name(e.GetProperty("target").GetString()!)}` — {e.GetProperty("evidence")}");
        L(); L("## Interfaces"); L();
        foreach (var n in nodes.Values.Where(n => n.GetProperty("kind").GetString() == "interface")) { L("- `" + Name(n.GetProperty("id").GetString()!) + "`"); var impl = edges.Where(e => e.GetProperty("type").GetString() == "implements" && e.GetProperty("target").GetString() == n.GetProperty("id").GetString()).ToArray(); L("  - Implementations: " + (impl.Length == 0 ? "none discovered" : string.Join(", ", impl.Select(e => Name(e.GetProperty("source").GetString()!))))); }
        L(); L("## Tests"); L(); L($"{g.GetProperty("summary").GetProperty("testClassCount")} classes; {g.GetProperty("summary").GetProperty("testMethodCount")} methods; {g.GetProperty("summary").GetProperty("testCount")} statically enumerable cases. This is discovery from source, not an execution result."); L();
        foreach (var n in nodes.Values.Where(n => n.GetProperty("kind").GetString() == "test-class")) { L($"- [{Name(n.GetProperty("id").GetString()!)}](../{n.GetProperty("file")}) — {n.GetProperty("staticTestCases")} cases"); foreach (var e in edges.Where(e => e.GetProperty("type").GetString() == "test-target" && e.GetProperty("source").GetString() == n.GetProperty("id").GetString())) L("  - Uses/tests `" + Name(e.GetProperty("target").GetString()!) + "` (source type/member binding; includes inherited foundation dependencies)"); }
        L(); L("## Packages"); L(); L("Evaluated direct PackageReference items only. Framework/SDK auto-references and transitives are outside this list."); L();
        foreach (var p in g.GetProperty("projects").EnumerateArray()) { var pkgs = p.GetProperty("packageReferences").EnumerateArray().Select(x => $"{x.GetProperty("name")} {x.GetProperty("version")}").ToArray(); L($"- **{p.GetProperty("name")}:** " + (pkgs.Length == 0 ? "none" : string.Join("; ", pkgs))); }
        L(); L("## Architecture Checks"); L();
        foreach (var rule in g.GetProperty("architecture").GetProperty("dependencyRules").EnumerateObject()) L($"- {rule.Name}: `{rule.Value}`");
        foreach (var rule in g.GetProperty("architecture").GetProperty("packageChecks").EnumerateObject()) L($"- {rule.Name}: `{rule.Value}`");
        L("- Identity source references: `" + g.GetProperty("architecture").GetProperty("identitySourceReferences") + "`");
        L(); L("## Current Development Phase"); L(); L("Declared by README; not inferred from the existence of classes:"); L();
        foreach (var s in g.GetProperty("architecture").GetProperty("phase").GetProperty("declaredStatus").EnumerateArray()) L("- " + s.GetProperty("text").GetString()!.Replace("|", " / ") + " — " + s.GetProperty("source"));
        L(); L("## Important Files"); L();
        foreach (var n in nodes.Values.Where(n => n.GetProperty("kind").GetString() == "file")) L($"- [{n.GetProperty("file")}](../{n.GetProperty("file")}) — {n.GetProperty("project")}");
        L(); L("## Graph Freshness"); L(); L("`generatedAtUtc` and Git metadata describe generation time. Dirty status includes unrelated local changes. Git commit alone cannot describe uncommitted source.");
        L("`sourceManifest` hashes source, project, solution, Razor, build configuration and README inputs. Each sorted record is relative path + NUL + SHA-256; records are joined with LF and hashed again. All graphify-out directories and build output are excluded. `generatorFingerprint` also detects tooling changes.");
        L(); L("```powershell"); L("powershell -ExecutionPolicy Bypass -File .\\tools\\graphify.ps1 -Check"); L("powershell -ExecutionPolicy Bypass -File .\\tools\\graphify.ps1"); L("```");
        L(); L("## Usage and Limits"); L(); L("- `graph.json`: machine-readable Codex architecture context. `edges` is canonical; `links` is an identical node-link compatibility alias."); L("- `graph.html`: offline, self-contained interactive explorer with project, Core, test and full-structure views."); L("- `GRAPH_REPORT.md`: human navigation index generated from the same JSON data.");
        L("- Read README and this index first. Graphify is an architectural index, NOT authoritative source code. Read the relevant source before reasoning about implementation or editing it. Regenerate if fingerprints differ.");
        L("- Refresh these outputs with tools/graphify.ps1. The generic Graphify CLI updater uses another schema and may replace this custom metadata. Existing CLI caches and project-local snapshots have not been refreshed or removed.");
        foreach (var limit in g.GetProperty("analysis").GetProperty("limitations").EnumerateArray()) L("- " + limit.GetString());
        foreach (var warning in g.GetProperty("analysis").GetProperty("warnings").EnumerateArray()) L("- WARNING: " + warning.GetString());
        return b.ToString();
    }
}
internal record InputFile(string Path, string Sha256);
internal record Package(string Name, string Version, string Evidence);
internal record Edge(string Source, string Target, string Type, string Evidence)
{
    public string Relation => Type;
    public string Confidence => "EXTRACTED";
    public int Weight => 1;
}
internal sealed class Project
{
    public string Id { get; set; } = ""; public string Name { get; set; } = ""; public string Type => "project";
    public string Path { get; set; } = ""; public string TargetFramework { get; set; } = ""; public string Sdk { get; set; } = "";
    public string Layer { get; set; } = ""; public bool InSolution { get; set; }
    public List<string> ProjectReferences { get; set; } = []; public List<Package> PackageReferences { get; set; } = []; public List<string> SourceFiles { get; set; } = [];
    [JsonIgnore] public string Defines { get; set; } = ""; [JsonIgnore] public bool ImplicitUsings { get; set; }
}
internal sealed class Node
{
    public string Id { get; set; } = ""; public string Name { get; set; } = ""; public string Label => Name;
    public string Kind { get; set; } = ""; public string Type => Kind; public string? FullName { get; set; }
    public string? Project { get; set; } public string? Namespace { get; set; } public string? File { get; set; }
    [JsonPropertyName("source_file")] public string? SourceFile => File;
    [JsonPropertyName("source_location")] public string? SourceLocation => Line is null ? null : "L" + Line;
    public int? Line { get; set; } public string? Accessibility { get; set; }
    public bool? Abstract { get; set; } public bool? Sealed { get; set; } public bool? Static { get; set; }
    public bool External { get; set; } public int Community { get; set; }
    [JsonPropertyName("community_name")] public string CommunityName => Project ?? "Solution / external";
    public string[] TestMethods { get; set; } = []; public int StaticTestCases { get; set; } public int DynamicTheories { get; set; }
}
'@

$html = @'
<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>HMS Architecture Explorer</title>
<style>
:root{color-scheme:light dark;--bg:#f3f5f8;--surface:#fff;--text:#17243a;--muted:#546378;--line:#d4dce8;--accent:#255ecb;--selected:#edf3ff}*{box-sizing:border-box}body{margin:0;font:15px/1.5 system-ui,sans-serif;background:var(--bg);color:var(--text)}header{padding:28px 32px 20px;border-bottom:1px solid var(--line);background:var(--surface)}h1{margin:3px 0 8px;font-size:28px;letter-spacing:-.7px}.eyebrow{color:var(--accent);font-size:12px;font-weight:750;letter-spacing:2px}p{margin:8px 0}.muted{color:var(--muted)}.stats{display:flex;gap:24px;flex-wrap:wrap;margin-top:18px}.stats strong{font-size:23px;margin-right:6px}.toolbar{padding:18px 32px;display:flex;gap:12px;flex-wrap:wrap;align-items:end}label{display:grid;gap:5px;font-size:12px;font-weight:650}input,select,button{font:inherit;color:inherit}input,select{padding:10px 12px;border:1px solid var(--line);border-radius:7px;background:var(--surface);min-height:43px}input{width:280px}button{cursor:pointer}button:focus-visible,input:focus-visible,select:focus-visible{outline:3px solid var(--accent);outline-offset:3px}.reset{padding:10px 16px;border:1px solid var(--line);border-radius:7px;background:var(--surface);height:43px}.workspace{display:grid;grid-template-columns:minmax(0,1fr) 340px;gap:18px;padding:0 32px 28px}.panel{background:var(--surface);border:1px solid var(--line);border-radius:12px;overflow:hidden}.panel-title{padding:16px 20px;border-bottom:1px solid var(--line);display:flex;justify-content:space-between;gap:12px;align-items:center}h2{font-size:17px;margin:0}#canvas{overflow:auto;padding:12px;min-height:350px}svg{display:block;width:100%;min-width:600px}svg .node rect{fill:var(--surface);stroke:var(--line);stroke-width:1.5}svg .node:hover rect,svg .node:focus rect,svg .node.selected rect{fill:var(--selected);stroke:var(--accent);stroke-width:2.5}svg text{fill:var(--text);font-family:system-ui}svg .edge{stroke:var(--muted);stroke-width:1.4;fill:none;opacity:.65}svg .edge-label{font-size:10px;fill:var(--muted)}svg .node{cursor:pointer}.detail{padding:20px;overflow-wrap:anywhere}.detail h3{margin:0 0 15px}dt{font-size:11px;text-transform:uppercase;letter-spacing:1px;color:var(--muted);margin-top:15px}dd{margin:3px 0 0}ul{padding-left:18px}.link{border:0;background:none;color:var(--accent);text-align:left;padding:3px 0;text-decoration:underline}.list{padding:0 16px 18px;display:grid;gap:8px;grid-template-columns:repeat(auto-fill,minmax(240px,1fr))}.card{border:1px solid var(--line);border-left:4px solid var(--project);border-radius:8px;padding:12px;background:var(--surface);text-align:left;overflow-wrap:anywhere}.card:hover,.card.selected{background:var(--selected)}.card small{display:block;color:var(--muted);font-size:11px;margin-top:5px}.note{padding:0 20px 16px;font-size:12px;color:var(--muted)}.legend{display:flex;gap:16px;flex-wrap:wrap;padding:0 20px 16px;font-size:12px}.dot{display:inline-block;width:9px;height:9px;border-radius:50%;margin-right:5px}#freshness{font-size:12px;overflow-wrap:anywhere}footer{padding:0 32px 28px;color:var(--muted);font-size:12px}#empty{padding:30px}a{color:var(--accent)}
@media(prefers-color-scheme:dark){:root{--bg:#101622;--surface:#182132;--text:#e0e8f5;--muted:#a5b5cc;--line:#344259;--accent:#8bb3ff;--selected:#233755}}
@media(max-width:850px){.workspace{grid-template-columns:1fr}.toolbar,header{padding:18px}.workspace{padding:0 18px 18px}input{width:min(280px,85vw)}.detail{min-height:160px}footer{padding:0 18px 18px}.stats{gap:15px}}
</style></head><body>
<header><div class="eyebrow">REPOSITORY INTELLIGENCE / SOURCE SNAPSHOT</div><h1>HMS Architecture Explorer</h1><p class="muted">Follow project dependencies, inspect the Core foundation, and trace tests to source.</p><div id="stats" class="stats"></div><p id="freshness" class="muted"></p></header>
<div class="toolbar"><label>View<select id="view"><option value="projects">Project architecture</option><option value="core">Core domain foundation</option><option value="tests">Test relationships</option><option value="full">Full structure</option></select></label><label>Project<select id="project"><option value="">All projects</option></select></label><label>Node type<select id="kind"><option value="">All types</option><option value="project">Project</option><option value="class">Class</option><option value="abstract-class">Abstract class</option><option value="interface">Interface</option><option value="enum">Enum</option><option value="record">Record</option><option value="test-class">Test</option><option value="file">File</option><option value="namespace">Namespace</option><option value="solution">Solution</option></select></label><label>Search<input id="search" type="search" placeholder="Type, namespace, file, project…"></label><button id="reset" class="reset">Reset</button></div>
<main class="workspace"><section class="panel" aria-label="Architecture graph"><div class="panel-title"><h2 id="heading">Project architecture</h2><span id="count" class="muted" aria-live="polite"></span></div><div id="canvas"></div><div id="list" class="list"></div><div id="legend" class="legend"></div><div class="note" id="viewnote"></div></section><aside class="panel" aria-label="Node details"><div class="panel-title"><h2>Node details</h2></div><div class="detail" id="details" aria-live="polite"><p class="muted">Select a node to inspect its source and relationships.</p></div></aside></main>
<footer>Architectural index, not authoritative source. Check freshness with <code>tools/graphify.ps1 -Check</code> before relying on this snapshot. No network requests or external libraries.</footer>
<script id="graph-data" type="application/json">__GRAPH_JSON__</script>
<script>
'use strict';
const graph=JSON.parse(document.getElementById('graph-data').textContent), byId=new Map(graph.nodes.map(n=>[n.id,n])), $=id=>document.getElementById(id);
const palette=['#6578e7','#1b9a91','#bd813a','#b266bc','#d65d72'];let selected=null;
const color=n=>palette[Math.max(0,n.community)%palette.length];
function el(tag,text,cls){const e=document.createElement(tag);if(text!==undefined)e.textContent=text;if(cls)e.className=cls;return e}
function svg(tag,attrs={}){const e=document.createElementNS('http://www.w3.org/2000/svg',tag);for(const [k,v] of Object.entries(attrs))e.setAttribute(k,v);return e}
for(const [key,label] of [['projectCount','projects'],['sourceFileCount','C# files'],['typeCount','types'],['testCount','static test cases']]){const s=el('span');s.append(el('strong',graph.summary[key]),el('span',label));$('stats').append(s)}
$('freshness').textContent=`Generated ${graph.generatedAtUtc} · ${graph.gitBranch||'No branch'} @ ${(graph.gitCommit||'unavailable').slice(0,10)} · ${graph.workingTreeDirty?'dirty working tree':'clean working tree'} · SHA-256 ${graph.sourceFingerprint.slice(0,16)}…`;
for(const p of graph.projects){const o=el('option',p.layer);o.value=p.name;$('project').append(o);const l=el('span');const d=el('i',undefined,'dot');d.style.background=color(byId.get(p.id));l.append(d,document.createTextNode(p.layer));$('legend').append(l)}
function detail(n){selected=n.id;const d=$('details');d.replaceChildren(el('h3',n.name));const dl=el('dl');for(const [label,value] of [['Kind',n.kind],['Project',n.project],['Namespace',n.namespace],['File',n.file?`${n.file}${n.line?':L'+n.line:''}`:n.external?'External framework type':'—'],['Full name',n.fullName],['Modifiers',[n.accessibility,n.abstract?'abstract':'',n.sealed?'sealed':'',n.static?'static':''].filter(Boolean).join(' ')]]){if(value){dl.append(el('dt',label),el('dd',value))}}d.append(dl,el('h4','Relationships'));const ul=el('ul');for(const e of graph.edges.filter(e=>e.source===n.id||e.target===n.id)){const outgoing=e.source===n.id,other=byId.get(outgoing?e.target:e.source);const li=el('li'),b=el('button',`${outgoing?'→':'←'} ${e.type}: ${other.name}`,'link');b.onclick=()=>{detail(other);highlight()};li.append(b);ul.append(li)}d.append(ul);if(!ul.children.length)d.append(el('p','No relationships recorded.','muted'));highlight()}
function highlight(){document.querySelectorAll('[data-node]').forEach(e=>e.classList.toggle('selected',e.dataset.node===selected))}
function render(){const mode=$('view').value,project=$('project').value,kind=$('kind').value,q=$('search').value.toLowerCase().trim();let nodes;
if(mode==='projects')nodes=graph.nodes.filter(n=>n.kind==='project');else if(mode==='core')nodes=graph.nodes.filter(n=>(n.project?.endsWith('.Core')&&n.fullName)||n.external);else if(mode==='tests'){const ids=new Set(graph.edges.filter(e=>e.type==='test-target').flatMap(e=>[e.source,e.target]));nodes=graph.nodes.filter(n=>ids.has(n.id)||n.kind==='test-class')}else nodes=graph.nodes;
nodes=nodes.filter(n=>(!project||n.project===project)&&(!kind||n.kind===kind)&&(!q||[n.name,n.fullName,n.namespace,n.file,n.project].filter(Boolean).join(' ').toLowerCase().includes(q)));
const ids=new Set(nodes.map(n=>n.id)),edges=graph.edges.filter(e=>ids.has(e.source)&&ids.has(e.target)&&(mode==='projects'?e.type==='project-reference':mode==='core'?['inherits','implements'].includes(e.type):mode==='tests'?e.type==='test-target':false));
$('heading').textContent=$('view').selectedOptions[0].textContent;$('count').textContent=`${nodes.length} nodes${mode==='full'?'':` · ${edges.length} relationships`}`;$('canvas').replaceChildren();$('list').replaceChildren();
$('viewnote').textContent=mode==='full'?'Full structure uses a searchable card index. Select any card for all inbound and outbound relationships.':mode==='tests'?'Arrows link tests to types used in source, including base types of private test helpers. This is not a coverage measurement.':'Arrows point from dependent/derived nodes toward referenced/base nodes. Use the details panel for source evidence.';
if(!nodes.length){$('canvas').append(el('p','No matching nodes. Change the filters or search.'));return}
if(mode==='full'){for(const n of nodes){const b=el('button',n.name,'card');b.dataset.node=n.id;b.style.setProperty('--project',color(n));b.append(el('small',`${n.kind} · ${n.project||'repository'}`),el('small',n.file||n.namespace||''));b.onclick=()=>detail(n);$('list').append(b)}$('canvas').style.minHeight='0';highlight();return}
$('canvas').style.minHeight='350px';const width=1000,row=98,boxWidth=220,boxHeight=66,positions=new Map();
if(mode==='tests'){const left=nodes.filter(n=>n.kind==='test-class'),right=nodes.filter(n=>n.kind!=='test-class');left.forEach((n,i)=>positions.set(n.id,{x:30,y:30+i*row}));right.forEach((n,i)=>positions.set(n.id,{x:700,y:30+i*row}))}
else{const rank=new Map();function depth(id,trail=new Set()){if(rank.has(id))return rank.get(id);if(trail.has(id))return 0;const next=new Set(trail);next.add(id);const targets=edges.filter(e=>e.source===id).map(e=>e.target);const d=targets.length?1+Math.max(...targets.map(t=>depth(t,next))):0;rank.set(id,d);return d}nodes.forEach(n=>depth(n.id));const max=Math.max(...rank.values(),0),columns=new Map();for(const n of nodes){const r=rank.get(n.id);if(!columns.has(r))columns.set(r,[]);columns.get(r).push(n)}for(const [r,group]of columns)group.forEach((n,i)=>positions.set(n.id,{x:max?(max-r)*(width-boxWidth-60)/max+30:30,y:30+i*row}))}
const height=Math.max(330,...[...positions.values()].map(p=>p.y+boxHeight+30)),s=svg('svg',{viewBox:`0 0 ${width} ${height}`,role:'group','aria-label':$('heading').textContent});const defs=svg('defs'),marker=svg('marker',{id:'arrow',viewBox:'0 0 10 10',refX:9,refY:5,markerWidth:7,markerHeight:7,orient:'auto-start-reverse'});marker.append(svg('path',{d:'M 0 0 L 10 5 L 0 10 z',fill:'var(--muted)'}));defs.append(marker);s.append(defs);
for(const e of edges){const a=positions.get(e.source),b=positions.get(e.target),x1=a.x+boxWidth,y1=a.y+boxHeight/2,x2=b.x,y2=b.y+boxHeight/2;const path=svg('path',{d:`M${x1},${y1} C${x1+36},${y1} ${x2-36},${y2} ${x2},${y2}`,class:'edge','marker-end':'url(#arrow)'});const title=svg('title');title.textContent=e.type+': '+e.evidence;path.append(title);s.append(path)}
for(const n of nodes){const p=positions.get(n.id),g=svg('g',{transform:`translate(${p.x},${p.y})`,class:'node',tabindex:'0',role:'button','aria-label':`${n.name}, ${n.kind}`,'data-node':n.id});g.append(svg('rect',{width:boxWidth,height:boxHeight,rx:8}),svg('rect',{width:5,height:boxHeight-16,x:1,y:8,rx:2,style:`fill:${color(n)};stroke:none`}));const t=svg('text',{x:15,y:27,'font-size':13,'font-weight':650});const display=n.kind==='project'?n.name.replace('ElsheiekhHMS.',''):n.name;t.textContent=display.length>31?display.slice(0,29)+'…':display;const sub=svg('text',{x:15,y:47,'font-size':10});sub.textContent=n.kind+(n.external?' · external':'');const title=svg('title');title.textContent=n.fullName||n.name;g.append(t,sub,title);g.onclick=()=>detail(n);g.onkeydown=e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();detail(n)}};s.append(g)}$('canvas').append(s);highlight()}
for(const id of ['view','project','kind'])$(id).addEventListener('change',render);$('search').addEventListener('input',render);$('reset').onclick=()=>{$('project').value='';$('kind').value='';$('search').value='';render()};render();
</script></body></html>
'@

Push-Location $root
try {
    $sdkVersion = (& dotnet --version).Trim()
    if ($LASTEXITCODE -ne 0 -or $sdkVersion -notmatch '^10\.') { throw 'An installed .NET 10 SDK is required.' }
    $sdkLines = @(& dotnet --list-sdks)
    $sdkLine = $sdkLines | Where-Object { $_.StartsWith($sdkVersion + ' ') } | Select-Object -First 1
    if (!$sdkLine -or $sdkLine -notmatch '\[(.+)\]') { throw 'Cannot locate the selected SDK.' }
    $roslyn = Join-Path (Join-Path $Matches[1] $sdkVersion) 'Roslyn\bincore'
    foreach ($dll in @('Microsoft.CodeAnalysis.dll', 'Microsoft.CodeAnalysis.CSharp.dll')) {
        if (!(Test-Path -LiteralPath (Join-Path $roslyn $dll))) { throw "Missing SDK Roslyn assembly: $dll" }
    }
    [IO.Directory]::CreateDirectory($work) | Out-Null
    $escapedRoslyn = [Security.SecurityElement]::Escape($roslyn)
    $project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup>
  <ItemGroup>
    <Reference Include="Microsoft.CodeAnalysis"><HintPath>$escapedRoslyn\Microsoft.CodeAnalysis.dll</HintPath></Reference>
    <Reference Include="Microsoft.CodeAnalysis.CSharp"><HintPath>$escapedRoslyn\Microsoft.CodeAnalysis.CSharp.dll</HintPath></Reference>
  </ItemGroup>
</Project>
"@
    [IO.File]::WriteAllText((Join-Path $work 'Analyzer.csproj'), $project, $utf8)
    [IO.File]::WriteAllText((Join-Path $work 'Program.cs'), $analyzer, $utf8)
    [IO.File]::WriteAllText((Join-Path $work 'template.html'), $html, $utf8)
    [IO.File]::WriteAllText((Join-Path $work 'NuGet.Config'), '<configuration><packageSources><clear /></packageSources></configuration>', $utf8)
    & dotnet restore (Join-Path $work 'Analyzer.csproj') --configfile (Join-Path $work 'NuGet.Config') --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Temporary analyzer restore failed.' }
    $mode = if ($Check) { 'check' } else { 'generate' }
    & dotnet run --project (Join-Path $work 'Analyzer.csproj') --no-restore -- $root (Join-Path $work 'template.html') $mode
    if ($LASTEXITCODE -ne 0) { throw 'Architecture analysis failed; see diagnostics above.' }
} finally {
    Pop-Location
    if (Test-Path -LiteralPath $work) {
        $resolved = (Resolve-Path -LiteralPath $work).Path
        if ($resolved -ne [IO.Path]::GetFullPath($work) -or !$resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or
            [IO.Path]::GetFileName($resolved) -notmatch '^hms-graphify-[0-9a-f]{32}$') { throw 'Refusing unexpected temporary cleanup target.' }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
