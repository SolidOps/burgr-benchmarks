# burgr-benchmarks

> *The AI writes the model, a deterministic generator writes the code, the tests decide.*

Benchmarks of the **Model-Driven Harness** of [Burgr](https://github.com/SolidOps/burgr):
how much does it cost an AI agent to build or change an API **with Burgr** (it writes a
YAML model and the extension points, Burgr generates the rest, a harness keeps the agent
on that path) and **without** (it writes the same architecture by hand)?

Everything needed to run them again is here: the tasks, the start states, the hidden
acceptance suites, the container and the script. With your own API key you get your own
numbers.

## Protocol

- **Agent:** [Claude Code](https://docs.claude.com/en/docs/claude-code) in headless mode,
  version and model fixed in [`bench.json`](bench.json) (`claude-sonnet-5-5`).
- **Isolation:** every pass runs in a fresh container ([`docker/`](docker)) with the .NET
  10 SDK, an empty Claude Code configuration (no memory, no user `CLAUDE.md`, no personal
  setting) and permissions bypassed, which is acceptable only because the container is
  thrown away. The NuGet and Burgr caches are warmed in the image: a pass measures work,
  not downloads.
- **Same prompt for both variants.** What differs is the repository the agent starts in.
- **Several passes per case** (5 by default) for the variance; the tables give the median
  and the range.
- **Judged in black box:** once the agent is done, a hidden acceptance suite enters the
  container, the judge builds the solution, runs the agent's own tests, starts the API and
  checks the contract of the prompt over HTTP. Black box keeps the suite valid for future
  baselines in other languages.

## Variants

| Variant | Start state | What the agent writes |
|---|---|---|
| `burgr-mini`, `burgr-onion` | Burgr pinned in `dotnet-tools.json`, `specs/burgr.json`, the harness of the template set (`AGENTS.md`, `CLAUDE.md`, a hook refusing reads and edits of generated files), the project files | the YAML model, the extension points, its tests |
| `hand` | the same project files, an `AGENTS.md` describing the same architecture, conventions and way of testing | everything |

The `hand` state is **derived** from the Burgr state by a single transform
(`dotnet run bench.cs -- prepare <case> hand <start|reference> <directory>`): generated
(`*.g.*`) and framework (`*.f.*`) files become ordinary files without the template markers,
the model and the Burgr harness are removed, the hand `AGENTS.md` of
[`harness/hand/`](harness/hand) is added. On an existing project both variants therefore
start from the same code; only the tooling differs.

## Cases

| Case | Project | Variants | What it shows |
|---|---|---|---|
| [`simple-create`](cases/simple-create) | Warehouse: 2 aggregates, an enum, a reference, a calculated property, validation rules | `burgr-mini`, `hand` | creating a simple API |
| [`complex-create`](cases/complex-create) | Ticketing, onion architecture: 3 aggregates, an entity edited with its parent, a value object, rules, a service, rights, JWT | `burgr-onion`, `hand` | creating a complex API |
| [`complex-evolve`](cases/complex-evolve) | User management (the Onion sample): add teams with synchronized members, two rules, a right | `burgr-onion`, `hand` | changing a complex API |
| [`simple-fix`](cases/simple-fix) | Library (the Mini sample): a one-character bug in a calculated property | `burgr-mini`, `hand` | unfavorable: a small fix, nothing to generate |
| [`entry`](cases/entry) | User management: a question about the code, no change | `burgr-onion`, `hand` | unfavorable: the entry cost of the harness |

Each case holds `case.json` (solution, API project, limits), `prompt.md`, `start/` (the
Burgr start state), `reference/` (a solution, used to check the judge) and `acceptance/`
(the hidden suite, sharing [`shared/acceptance`](shared/acceptance)).

The create cases use domains of their own: the harness of each template set shows the
model of its own sample as an example, which would give the Burgr variant a head start on
those domains.

## Measures

Per pass, in `result.json`:

- tokens: input (with cache reads and writes) and output, per model; cost
  (`total_cost_usd` of Claude Code); turns; duration;
- iterations: `dotnet build` / `dotnet test` / `dotnet burgr` launched by the agent, how
  many failed, whether the first build and the first test run succeeded, tool calls,
  refusals by the harness hook;
- lines written by the agent, outside the files Burgr writes;
- judge: build, the agent's tests, acceptance tests passed out of the total (or "unchanged"
  for `entry`).

The transcript (`transcript.jsonl`) and the diff (`changes.patch`) of every pass are kept
next to it.

## Run them

Requires Docker and the .NET 10 SDK. A full run (5 cases × 2 variants × 5 passes) is
billed to your API key.

```sh
dotnet run bench.cs -- build-image        # the container, with Claude Code and warm caches
dotnet run bench.cs -- verify             # judges the references and start states, no API call
export ANTHROPIC_API_KEY=...
dotnet run bench.cs -- run --runs 1       # shows the plan; add --yes to start
dotnet run bench.cs -- run --yes          # every case, every variant, 5 passes
dotnet run bench.cs -- report results/<run>
```

`--case` and `--variant` narrow a run. A pass already done (its `result.json` exists) is
skipped, so an interrupted run resumes where it stopped.

## Results

Under [`results/`](results), one directory per run, with its `README.md` of tables.

## Limits

- One model, one agent, one language (C#). The protocol admits more without change: a
  baseline in another language is one more variant judged by the same HTTP suite.
- The template sets are pinned (Burgr 1.10.0). Two limitations of Onion 1.10.0 shape the
  cases: deleting an aggregate whose list navigation is loaded raises an error instead of
  a conflict (the references use a `before_remove` rule), and a synchronized child cannot
  reference another aggregate (team members are identified by email).
- The costs are the ones Claude Code reports; with a subscription token they are an
  estimate, not a bill.

## License

[MIT](LICENSE).
