// Benchmarks of the Model-Driven Harness: the same tasks, done by Claude Code with Burgr
// (a YAML model, a template set and its harness) and without it (C# written by hand).
//
//   dotnet run bench.cs -- prepare <case> <variant> <start|reference> <directory>
//   dotnet run bench.cs -- build-image
//   dotnet run bench.cs -- verify [--case <case>]
//   dotnet run bench.cs -- run [--case <case>] [--variant <variant>] [--runs <n>] [--out <directory>] --yes
//   dotnet run bench.cs -- report <results directory>
//
// See README.md for the protocol.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

var root = FindRoot();
var settings = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "bench.json")))!;
var model = (string)settings["model"]!;
var claudeCodeVersion = (string)settings["claudeCodeVersion"]!;
var image = $"burgr-benchmarks:claude-code-{claudeCodeVersion}";

try
{
    return args switch
    {
        ["prepare", var caseName, var variant, var state, var directory] => Prepare(caseName, variant, state, directory),
        ["build-image", ..] => BuildImage(),
        ["verify", .. var rest] => Verify(Option(rest, "--case")),
        ["run", .. var rest] => RunPasses(rest),
        ["report", var directory] => Report(directory),
        _ => Usage(),
    };
}
catch (BenchException e)
{
    Console.Error.WriteLine(e.Message);
    return 1;
}

int Usage()
{
    Console.Error.WriteLine("""
        usage:
          dotnet run bench.cs -- prepare <case> <variant> <start|reference> <directory>
          dotnet run bench.cs -- build-image
          dotnet run bench.cs -- verify [--case <case>]
          dotnet run bench.cs -- run [--case <case>] [--variant <variant>] [--runs <n>] [--out <directory>] --yes
          dotnet run bench.cs -- report <results directory>
        """);
    return 2;
}

// ---------------------------------------------------------------- cases and states

IEnumerable<string> CaseNames() =>
    Directory.GetDirectories(Path.Combine(root, "cases")).Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal);

JsonNode CaseOf(string name)
{
    var path = Path.Combine(root, "cases", name, "case.json");
    if (!File.Exists(path))
    {
        throw new BenchException($"unknown case '{name}': {string.Join(", ", CaseNames())}");
    }
    return JsonNode.Parse(File.ReadAllText(path))!;
}

string[] VariantsOf(JsonNode @case) => [$"burgr-{(string)@case["templateSet"]!}", "hand"];

int Prepare(string caseName, string variant, string state, string directory)
{
    Materialize(caseName, variant, state, Path.GetFullPath(directory));
    Console.WriteLine($"{caseName}/{variant}/{state} -> {Path.GetFullPath(directory)}");
    return 0;
}

// The state of a case for a variant: the Burgr state as is, or the hand state derived from it.
void Materialize(string caseName, string variant, string state, string target)
{
    var @case = CaseOf(caseName);
    if (!VariantsOf(@case).Contains(variant))
    {
        throw new BenchException($"unknown variant '{variant}' for {caseName}: {string.Join(", ", VariantsOf(@case))}");
    }
    if (state is not ("start" or "reference"))
    {
        throw new BenchException($"unknown state '{state}': start or reference");
    }
    var source = Path.Combine(root, "cases", caseName, state);
    if (!Directory.Exists(source) && state == "reference")
    {
        // a case that changes nothing (entry) has no reference of its own
        source = Path.Combine(root, "cases", caseName, "start");
    }
    if (Directory.Exists(target))
    {
        Directory.Delete(target, recursive: true);
    }
    Directory.CreateDirectory(target);
    if (variant == "hand")
    {
        StripBurgr(source, target, (string)@case["templateSet"]!);
    }
    else
    {
        CopyTree(source, target, (_, relative) => relative);
    }
}

// The hand variant: generated (.g) and framework (.f) files become ordinary files without
// the template markers, the model and the harness of Burgr go, the hand AGENTS.md comes.
void StripBurgr(string source, string target, string templateSet)
{
    var burgrOnly = new[] { ".claude/", "specs/", "AGENTS.md", "CLAUDE.md", "dotnet-tools.json" };
    CopyTree(source, target, (_, relative) =>
    {
        if (burgrOnly.Any(prefix => relative == prefix || relative.StartsWith(prefix, StringComparison.Ordinal))
            || relative.EndsWith(".puml", StringComparison.Ordinal)
            || relative.Split('/').Any(part => part.EndsWith(".Diagrams", StringComparison.Ordinal)))
        {
            return null;
        }
        var plain = GeneratedSuffix().Replace(relative, "$1$2");
        // a hand-written part of the same partial class keeps its name: UMDbContext.g.cs -> UMDbContext.Core.cs
        return plain != relative && File.Exists(Path.Combine(source, plain)) ? GeneratedSuffix().Replace(relative, "$1.Core$2") : plain;
    }, (relative, content) => relative.EndsWith(".cs", StringComparison.Ordinal) ? TemplateMarker().Replace(content, "") : content);

    var agents = Path.Combine(root, "harness", "hand", templateSet, "AGENTS.md");
    File.Copy(agents, Path.Combine(target, "AGENTS.md"));
    File.WriteAllText(Path.Combine(target, "CLAUDE.md"), "@AGENTS.md\n");
}

void CopyTree(string source, string target, Func<string, string, string?> map, Func<string, string, string>? transform = null)
{
    foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
    {
        var relative = Path.GetRelativePath(source, file).Replace('\\', '/');
        if (relative.Split('/').Any(part => part is "bin" or "obj" or ".vs") || relative.EndsWith(".db", StringComparison.Ordinal))
        {
            continue;
        }
        var mapped = map(file, relative);
        if (mapped is null)
        {
            continue;
        }
        var destination = Path.Combine(target, mapped);
        if (File.Exists(destination))
        {
            throw new BenchException($"{relative}: {mapped} already exists in {target}");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (transform is null)
        {
            File.Copy(file, destination);
        }
        else
        {
            File.WriteAllText(destination, transform(relative, File.ReadAllText(file)), new UTF8Encoding(false));
        }
    }
}

// ---------------------------------------------------------------- image

int BuildImage()
{
    var context = Path.Combine(Path.GetTempPath(), $"burgr-benchmarks-image-{Guid.NewGuid():N}");
    try
    {
        CopyTree(Path.Combine(root, "docker"), context, (_, relative) => relative == "warm.sh" ? "warm/warm.sh" : relative);
        foreach (var caseName in CaseNames())
        {
            var @case = CaseOf(caseName);
            Materialize(caseName, VariantsOf(@case)[0], "reference", Path.Combine(context, "warm", "states", caseName));
            if (Directory.Exists(Path.Combine(root, "cases", caseName, "acceptance")))
            {
                CopyTree(Path.Combine(root, "cases", caseName, "acceptance"), Path.Combine(context, "warm", "cases", caseName, "acceptance"), (_, relative) => relative);
            }
        }
        CopyTree(Path.Combine(root, "shared"), Path.Combine(context, "warm", "shared"), (_, relative) => relative);
        Docker("build", "-t", image,
            "--build-arg", $"CLAUDE_CODE_VERSION={claudeCodeVersion}",
            "--build-arg", $"DOTNET_SDK_IMAGE={(string)settings["dotnetSdkImage"]!}",
            context);
        Console.WriteLine($"built {image}");
        return 0;
    }
    finally
    {
        Directory.Delete(context, recursive: true);
    }
}

// ---------------------------------------------------------------- verify and run

// Judges the reference state of every variant (expected: everything passes) and the start
// state (expected: the acceptance suite fails), without calling Claude.
int Verify(string? onlyCase)
{
    var failures = 0;
    foreach (var caseName in onlyCase is null ? CaseNames() : [onlyCase])
    {
        var @case = CaseOf(caseName);
        foreach (var variant in VariantsOf(@case))
        {
            foreach (var state in new[] { "reference", "start" })
            {
                var output = Path.Combine(Path.GetTempPath(), $"burgr-benchmarks-verify-{Guid.NewGuid():N}");
                try
                {
                    var judge = Pass(caseName, variant, state, output, withClaude: false)["judge"]!;
                    var line = $"{caseName,-16} {variant,-12} {state,-10} {Describe(judge)}";
                    var expected = state == "reference" ? Succeeded(judge) : !Succeeded(judge) || (string)@case["judge"]! == "unchanged";
                    Console.WriteLine((expected ? "ok    " : "WRONG ") + line);
                    failures += expected ? 0 : 1;
                }
                finally
                {
                    if (Directory.Exists(output))
                    {
                        Directory.Delete(output, recursive: true);
                    }
                }
            }
        }
    }
    Console.WriteLine(failures == 0 ? "verify: every state judged as expected" : $"verify: {failures} state(s) judged unexpectedly");
    return failures == 0 ? 0 : 1;
}

int RunPasses(string[] options)
{
    var onlyCase = Option(options, "--case");
    var onlyVariant = Option(options, "--variant");
    var runs = int.Parse(Option(options, "--runs") ?? ((int)settings["runs"]!).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmm", CultureInfo.InvariantCulture);
    var output = Path.GetFullPath(Option(options, "--out") ?? Path.Combine(root, "results", $"{stamp}-{model}"));

    var plan = (onlyCase is null ? CaseNames() : [onlyCase])
        .SelectMany(caseName => VariantsOf(CaseOf(caseName)).Where(v => onlyVariant is null || v == onlyVariant).Select(variant => (caseName, variant)))
        .SelectMany(cell => Enumerable.Range(1, runs).Select(run => (cell.caseName, cell.variant, run)))
        .ToList();
    Console.WriteLine($"{plan.Count} pass(es) of {model} with Claude Code {claudeCodeVersion}, into {output}:");
    foreach (var group in plan.GroupBy(p => (p.caseName, p.variant)))
    {
        Console.WriteLine($"  {group.Key.caseName} / {group.Key.variant}: {group.Count()}");
    }
    if (!options.Contains("--yes"))
    {
        Console.WriteLine("Each pass calls the Anthropic API and is billed. Add --yes to start.");
        return 0;
    }
    if (Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") is null && Environment.GetEnvironmentVariable("CLAUDE_CODE_OAUTH_TOKEN") is null)
    {
        throw new BenchException("set ANTHROPIC_API_KEY (or CLAUDE_CODE_OAUTH_TOKEN) first");
    }

    foreach (var (caseName, variant, run) in plan)
    {
        var directory = Path.Combine(output, caseName, variant, run.ToString("00", CultureInfo.InvariantCulture));
        if (File.Exists(Path.Combine(directory, "result.json")))
        {
            Console.WriteLine($"skip  {caseName} {variant} #{run}: already done");
            continue;
        }
        var result = Pass(caseName, variant, "start", directory, withClaude: true);
        result["run"] = run;
        File.WriteAllText(Path.Combine(directory, "result.json"), result.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        var claude = result["claude"]!;
        Console.WriteLine($"done  {caseName} {variant} #{run}: ${(double?)claude["costUsd"]:0.00}, {(int?)claude["turns"]} turns, {Describe(result["judge"]!)}");
    }
    Report(output);
    return 0;
}

// One pass in a fresh container: the state, Claude Code when asked, then the judge.
JsonObject Pass(string caseName, string variant, string state, string output, bool withClaude)
{
    var @case = CaseOf(caseName);
    var work = Path.Combine(Path.GetTempPath(), $"burgr-benchmarks-state-{Guid.NewGuid():N}");
    var container = $"burgr-bench-{Guid.NewGuid():N}"[..24];
    Directory.CreateDirectory(output);
    try
    {
        Materialize(caseName, variant, state, work);
        Docker("run", "-d", "--name", container, image, "sleep", "infinity");
        Docker("cp", work + "/.", $"{container}:/work");
        Docker("cp", Path.Combine(root, "cases", caseName, "prompt.md"), $"{container}:/bench/prompt.md");
        Docker("exec", "-u", "root", container, "chown", "-R", "bench:bench", "/work", "/bench");

        if (withClaude)
        {
            Docker("exec",
                "-e", $"BENCH_MODEL={model}",
                "-e", $"BENCH_MAX_TURNS={(int)@case["maxTurns"]!}",
                "-e", $"BENCH_TIMEOUT_MINUTES={(int)@case["timeoutMinutes"]!}",
                "-e", "ANTHROPIC_API_KEY", "-e", "CLAUDE_CODE_OAUTH_TOKEN",
                container, "bash", "/opt/bench/pass.sh");
        }

        // The acceptance suite only enters the container once Claude is done.
        var suite = Path.Combine(Path.GetTempPath(), $"burgr-benchmarks-suite-{Guid.NewGuid():N}");
        try
        {
            if (Directory.Exists(Path.Combine(root, "cases", caseName, "acceptance")))
            {
                CopyTree(Path.Combine(root, "cases", caseName, "acceptance"), Path.Combine(suite, "cases", caseName, "acceptance"), (_, relative) => relative);
            }
            CopyTree(Path.Combine(root, "shared"), Path.Combine(suite, "shared"), (_, relative) => relative);
            Docker("cp", suite + "/.", $"{container}:/judge");
        }
        finally
        {
            Directory.Delete(suite, recursive: true);
        }
        Docker("exec", "-u", "root", container, "chown", "-R", "bench:bench", "/judge");

        var jwt = @case["jwt"];
        Docker("exec",
            "-e", $"BENCH_CASE={caseName}",
            "-e", $"BENCH_SOLUTION={(string)@case["solution"]!}",
            "-e", $"BENCH_API={(string)@case["api"]!}",
            "-e", $"BENCH_CONNECTION={(string)@case["connectionString"]!}",
            "-e", $"BENCH_JUDGE={(string)@case["judge"]!}",
            "-e", $"BENCH_JWT_KEY={(string?)jwt?["key"] ?? ""}",
            "-e", $"BENCH_JWT_ISSUER={(string?)jwt?["issuer"] ?? ""}",
            container, "bash", "/opt/bench/judge.sh");

        Docker("cp", $"{container}:/bench/out/.", output);
    }
    finally
    {
        DockerCall(false, ["rm", "-f", container]);
        if (Directory.Exists(work))
        {
            Directory.Delete(work, recursive: true);
        }
    }

    return new JsonObject
    {
        ["case"] = caseName,
        ["variant"] = variant,
        ["state"] = state,
        ["model"] = model,
        ["claudeCodeVersion"] = ReadText(output, "claude.version") ?? claudeCodeVersion,
        ["startedAt"] = ReadText(output, "started"),
        ["endedAt"] = ReadText(output, "ended"),
        ["claude"] = withClaude ? ClaudeMetrics(output) : null,
        ["iterations"] = withClaude ? IterationMetrics(output) : null,
        ["written"] = WrittenMetrics(output, variant),
        ["judge"] = JudgeMetrics(output, (string)@case["judge"]!),
    };
}

// ---------------------------------------------------------------- metrics

static string? ReadText(string directory, string name)
{
    var path = Path.Combine(directory, name);
    return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
}

static int? ReadExit(string directory, string name) =>
    int.TryParse(ReadText(directory, name), CultureInfo.InvariantCulture, out var code) ? code : null;

static IEnumerable<JsonNode> Transcript(string directory)
{
    var path = Path.Combine(directory, "transcript.jsonl");
    if (!File.Exists(path))
    {
        yield break;
    }
    foreach (var line in File.ReadLines(path))
    {
        JsonNode? node = null;
        try
        {
            node = string.IsNullOrWhiteSpace(line) ? null : JsonNode.Parse(line);
        }
        catch (JsonException)
        {
        }
        if (node is not null)
        {
            yield return node;
        }
    }
}

static JsonObject ClaudeMetrics(string directory)
{
    var result = Transcript(directory).LastOrDefault(n => (string?)n["type"] == "result");
    var usage = result?["usage"];
    long Tokens(string name) => (long?)usage?[name] ?? 0;
    return new JsonObject
    {
        ["exitCode"] = ReadExit(directory, "claude.exit"),
        ["subtype"] = (string?)result?["subtype"],
        ["isError"] = (bool?)result?["is_error"] ?? true,
        ["turns"] = (int?)result?["num_turns"],
        ["durationMs"] = (long?)result?["duration_ms"],
        ["durationApiMs"] = (long?)result?["duration_api_ms"],
        ["costUsd"] = (double?)result?["total_cost_usd"],
        ["inputTokens"] = Tokens("input_tokens"),
        ["cacheCreationInputTokens"] = Tokens("cache_creation_input_tokens"),
        ["cacheReadInputTokens"] = Tokens("cache_read_input_tokens"),
        ["totalInputTokens"] = Tokens("input_tokens") + Tokens("cache_creation_input_tokens") + Tokens("cache_read_input_tokens"),
        ["outputTokens"] = Tokens("output_tokens"),
        ["modelUsage"] = result?["modelUsage"]?.DeepClone(),
    };
}

// Builds, tests and generations launched by Claude, from the tool calls of the transcript.
static JsonObject IterationMetrics(string directory)
{
    var commands = new Dictionary<string, string>();
    var tools = new SortedDictionary<string, int>(StringComparer.Ordinal);
    var outcomes = new List<(string Kind, bool Failed)>();
    var refusals = 0;
    foreach (var node in Transcript(directory))
    {
        foreach (var item in (node["message"]?["content"] as JsonArray) ?? [])
        {
            switch ((string?)item?["type"])
            {
                case "tool_use":
                    var name = (string?)item!["name"] ?? "?";
                    tools[name] = tools.GetValueOrDefault(name) + 1;
                    if (name == "Bash" && (string?)item["input"]?["command"] is { } command && (string?)item["id"] is { } id)
                    {
                        commands[id] = command;
                    }
                    break;
                case "tool_result":
                    var text = ResultText(item!["content"]);
                    var isError = (bool?)item["is_error"] ?? false;
                    if (isError && text.Contains("Refused:", StringComparison.Ordinal))
                    {
                        refusals++;
                    }
                    if ((string?)item["tool_use_id"] is { } useId && commands.TryGetValue(useId, out var run) && KindOf(run) is { } kind)
                    {
                        outcomes.Add((kind, isError || FailureMark().IsMatch(text)));
                    }
                    break;
            }
        }
    }

    bool? First(string kind) => outcomes.Where(o => o.Kind == kind || (kind == "build" && o.Kind == "test")).Select(o => (bool?)!o.Failed).FirstOrDefault();
    return new JsonObject
    {
        ["builds"] = outcomes.Count(o => o.Kind == "build"),
        ["failedBuilds"] = outcomes.Count(o => o.Kind == "build" && o.Failed),
        ["tests"] = outcomes.Count(o => o.Kind == "test"),
        ["failedTests"] = outcomes.Count(o => o.Kind == "test" && o.Failed),
        ["generations"] = outcomes.Count(o => o.Kind == "generate"),
        ["failedGenerations"] = outcomes.Count(o => o.Kind == "generate" && o.Failed),
        ["firstBuildSucceeded"] = First("build"),
        ["firstTestSucceeded"] = First("test"),
        ["toolCalls"] = new JsonObject(tools.Select(t => KeyValuePair.Create<string, JsonNode?>(t.Key, t.Value))),
        ["harnessRefusals"] = refusals,
    };

    static string? KindOf(string command) =>
        command.Contains("dotnet burgr", StringComparison.Ordinal) ? "generate"
        : command.Contains("dotnet test", StringComparison.Ordinal) ? "test"
        : command.Contains("dotnet build", StringComparison.Ordinal) ? "build"
        : null;

    static string ResultText(JsonNode? content) => content switch
    {
        JsonValue value => value.ToString(),
        JsonArray parts => string.Join("\n", parts.Select(p => (string?)p?["text"] ?? "")),
        _ => "",
    };
}

// Lines written in the pass, outside the files Burgr writes (.g and .f).
static JsonObject WrittenMetrics(string directory, string variant)
{
    var path = Path.Combine(directory, "numstat.txt");
    var lines = File.Exists(path) ? File.ReadAllLines(path) : [];
    int files = 0, added = 0, removed = 0, generated = 0;
    foreach (var line in lines)
    {
        var parts = line.Split('\t');
        if (parts.Length < 3)
        {
            continue;
        }
        var name = parts[2];
        if (variant != "hand" && GeneratedSuffix().IsMatch(name))
        {
            generated += int.TryParse(parts[0], out var g) ? g : 0;
            continue;
        }
        files++;
        added += int.TryParse(parts[0], out var a) ? a : 0;
        removed += int.TryParse(parts[1], out var r) ? r : 0;
    }
    return new JsonObject { ["files"] = files, ["addedLines"] = added, ["removedLines"] = removed, ["generatedLines"] = generated };
}

static JsonObject JudgeMetrics(string directory, string judge)
{
    if (judge == "unchanged")
    {
        var status = ReadText(directory, "status.txt") ?? "";
        return new JsonObject { ["kind"] = judge, ["unchanged"] = status.Length == 0 };
    }
    var trx = Directory.Exists(directory) ? Directory.GetFiles(directory, "*.trx", SearchOption.AllDirectories).FirstOrDefault() : null;
    var counters = trx is null ? null : Counters().Match(File.ReadAllText(trx));
    int? Counter(string name) => counters is { Success: true } && int.TryParse(Regex.Match(counters.Value, $"\\b{name}=\"(\\d+)\"").Groups[1].Value, out var n) ? n : null;
    return new JsonObject
    {
        ["kind"] = judge,
        ["build"] = ReadExit(directory, "build.exit") == 0,
        ["tests"] = ReadExit(directory, "test.exit") == 0,
        ["acceptancePassed"] = Counter("passed") ?? 0,
        ["acceptanceTotal"] = Counter("total") ?? Counter("executed") ?? 0,
    };
}

static bool Succeeded(JsonNode judge) => (string)judge["kind"]! == "unchanged"
    ? (bool)judge["unchanged"]!
    : (bool)judge["build"]! && (bool)judge["tests"]! && (int)judge["acceptanceTotal"]! > 0 && (int)judge["acceptancePassed"]! == (int)judge["acceptanceTotal"]!;

static string Describe(JsonNode judge) => (string)judge["kind"]! == "unchanged"
    ? ((bool)judge["unchanged"]! ? "unchanged" : "CHANGED")
    : $"build {Mark((bool)judge["build"]!)}, own tests {Mark((bool)judge["tests"]!)}, acceptance {(int)judge["acceptancePassed"]!}/{(int)judge["acceptanceTotal"]!}";

static string Mark(bool ok) => ok ? "ok" : "FAILED";

// ---------------------------------------------------------------- report

int Report(string directory)
{
    directory = Path.GetFullPath(directory);
    var results = Directory.Exists(directory)
        ? Directory.GetFiles(directory, "result.json", SearchOption.AllDirectories).Select(f => JsonNode.Parse(File.ReadAllText(f))!).ToList()
        : [];
    if (results.Count == 0)
    {
        throw new BenchException($"no result.json under {directory}");
    }

    var report = new StringBuilder();
    report.AppendLine($"# Results: {Path.GetFileName(directory)}");
    report.AppendLine();
    var first = results[0];
    report.AppendLine($"Model `{(string?)first["model"]}`, Claude Code `{(string?)first["claudeCodeVersion"]}`, {results.Count} passes. " +
                      "Each cell: median (min–max) over the passes of the row. Input tokens include the cache reads and writes.");
    report.AppendLine();
    report.AppendLine("| Case | Variant | Passes | Accepted | Acceptance | Cost (USD) | Input tokens | Output tokens | Turns | Minutes | Builds (failed) | First build ok | Lines written |");
    report.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
    foreach (var cell in results.GroupBy(r => ((string)r["case"]!, (string)r["variant"]!)).OrderBy(g => g.Key.Item1, StringComparer.Ordinal).ThenBy(g => g.Key.Item2, StringComparer.Ordinal))
    {
        var rows = cell.ToList();
        double[] Values(Func<JsonNode, double?> pick) => rows.Select(pick).OfType<double>().ToArray();
        var accepted = rows.Count(r => Succeeded(r["judge"]!));
        var acceptance = (string)rows[0]["judge"]!["kind"]! == "unchanged"
            ? "n/a"
            : Stat(Values(r => (int)r["judge"]!["acceptanceTotal"]! == 0 ? 0 : 100.0 * (int)r["judge"]!["acceptancePassed"]! / (int)r["judge"]!["acceptanceTotal"]!), "0") + " %";
        var firstBuilds = rows.Select(r => (bool?)r["iterations"]?["firstBuildSucceeded"]).OfType<bool>().ToList();
        report.AppendLine(string.Join(" | ",
            "", cell.Key.Item1, cell.Key.Item2, rows.Count.ToString(CultureInfo.InvariantCulture),
            $"{accepted}/{rows.Count}",
            acceptance,
            Stat(Values(r => (double?)r["claude"]?["costUsd"]), "0.00"),
            Stat(Values(r => (double?)r["claude"]?["totalInputTokens"]), "N0"),
            Stat(Values(r => (double?)r["claude"]?["outputTokens"]), "N0"),
            Stat(Values(r => (double?)r["claude"]?["turns"]), "0"),
            Stat(Values(r => (double?)r["claude"]?["durationMs"] / 60000.0), "0.0"),
            Stat(Values(r => (double?)r["iterations"]?["builds"] + (double?)r["iterations"]?["tests"]), "0") + " (" + Stat(Values(r => (double?)r["iterations"]?["failedBuilds"] + (double?)r["iterations"]?["failedTests"]), "0") + ")",
            firstBuilds.Count == 0 ? "n/a" : $"{firstBuilds.Count(b => b)}/{firstBuilds.Count}",
            Stat(Values(r => (double?)r["written"]?["addedLines"]), "0"),
            "").Trim());
    }

    report.AppendLine();
    report.AppendLine("## Burgr against hand, per case (ratio of the medians)");
    report.AppendLine();
    report.AppendLine("| Case | Cost | Input tokens | Output tokens | Minutes |");
    report.AppendLine("|---|---|---|---|---|");
    foreach (var caseGroup in results.GroupBy(r => (string)r["case"]!).OrderBy(g => g.Key, StringComparer.Ordinal))
    {
        var burgr = caseGroup.Where(r => ((string)r["variant"]!).StartsWith("burgr-", StringComparison.Ordinal)).ToList();
        var hand = caseGroup.Where(r => (string)r["variant"]! == "hand").ToList();
        if (burgr.Count == 0 || hand.Count == 0)
        {
            continue;
        }
        string Ratio(Func<JsonNode, double?> pick)
        {
            var b = Median(burgr.Select(pick).OfType<double>().ToArray());
            var h = Median(hand.Select(pick).OfType<double>().ToArray());
            return b is null || h is null || h == 0 ? "n/a" : $"{b / h:0.00}× ({(b / h - 1) * 100:+0;-0} %)";
        }
        report.AppendLine($"| {caseGroup.Key} | {Ratio(r => (double?)r["claude"]?["costUsd"])} | {Ratio(r => (double?)r["claude"]?["totalInputTokens"])} | {Ratio(r => (double?)r["claude"]?["outputTokens"])} | {Ratio(r => (double?)r["claude"]?["durationMs"])} |");
    }

    File.WriteAllText(Path.Combine(directory, "README.md"), report.ToString(), new UTF8Encoding(false));
    Console.WriteLine(report);
    return 0;

    static double? Median(double[] values)
    {
        if (values.Length == 0)
        {
            return null;
        }
        var sorted = values.Order().ToArray();
        return sorted.Length % 2 == 1 ? sorted[sorted.Length / 2] : (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2;
    }

    static string Stat(double[] values, string format)
    {
        var median = Median(values);
        if (median is null)
        {
            return "n/a";
        }
        var text = median.Value.ToString(format, CultureInfo.InvariantCulture);
        return values.Length == 1 ? text : $"{text} ({values.Min().ToString(format, CultureInfo.InvariantCulture)}–{values.Max().ToString(format, CultureInfo.InvariantCulture)})";
    }
}

// ---------------------------------------------------------------- helpers

static string FindRoot()
{
    for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
    {
        if (File.Exists(Path.Combine(directory.FullName, "bench.json")))
        {
            return directory.FullName;
        }
    }
    throw new BenchException("bench.json not found: run from the burgr-benchmarks repository");
}

static string? Option(string[] options, string name)
{
    var index = Array.IndexOf(options, name);
    return index >= 0 && index + 1 < options.Length ? options[index + 1] : null;
}

static void Docker(params string[] arguments) => DockerCall(true, arguments);

static void DockerCall(bool check, string[] arguments)
{
    var start = new ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var argument in arguments)
    {
        start.ArgumentList.Add(argument);
    }
    using var process = Process.Start(start)!;
    var output = process.StandardOutput.ReadToEndAsync();
    var error = process.StandardError.ReadToEndAsync();
    process.WaitForExit();
    if (arguments[0] == "build")
    {
        Console.Error.Write(error.Result);
    }
    if (check && process.ExitCode != 0)
    {
        throw new BenchException($"docker {string.Join(' ', arguments.Take(3))}… failed ({process.ExitCode}): {error.Result.Trim()}");
    }
}

sealed class BenchException(string message) : Exception(message);

partial class Program
{
    [GeneratedRegex(@"^(.*)\.[gf](\.[A-Za-z0-9]+)$")]
    private static partial Regex GeneratedSuffix();

    // the markers Burgr's templates leave in generated code: "// Property [S][NO][NAR]", "// Service "
    [GeneratedRegex(@"^[ \t]*// (Object|Property|Dependency|Rule|Service|Method|Event|Factory|Enum|Value)\b[^a-z\r\n]*\r?\n", RegexOptions.Multiline)]
    private static partial Regex TemplateMarker();

    [GeneratedRegex(@"error CS\d+|error MSB\d+|Build FAILED|Failed!|\[FAIL\]")]
    private static partial Regex FailureMark();

    [GeneratedRegex(@"<Counters [^>]*>")]
    private static partial Regex Counters();
}
