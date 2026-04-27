// 04-SkillPicker — deterministic skill selection. No LLM, no NuGets.
// Scans ./skills/*.skill.md, parses YAML frontmatter by hand, scores by
// trigger-substring match against a user prompt.

string skillsDir = ResolveSkillsDir();

if (args.Length == 0)
{
    PrintUsage();
    return 0;
}

if (!Directory.Exists(skillsDir))
{
    Console.Error.WriteLine($"⚠️  No skills folder found at {skillsDir}. Set SKILLS_DIR or create the folder.");
    return 1;
}

var skills = LoadSkills(skillsDir);

if (args[0] == "--list")
{
    Console.WriteLine($"Found {skills.Count} skills in {skillsDir}");
    Console.WriteLine();
    int nameW = Math.Max(4, skills.Select(s => s.Name.Length).DefaultIfEmpty(4).Max());
    Console.WriteLine($"{"Skill".PadRight(nameW)}  Description");
    Console.WriteLine($"{new string('-', nameW)}  {new string('-', 40)}");
    foreach (var s in skills.OrderBy(s => s.Name, StringComparer.Ordinal))
        Console.WriteLine($"{s.Name.PadRight(nameW)}  {s.Description}");
    return 0;
}

if (args[0] == "--explain")
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Usage: dotnet run -- --explain \"<prompt>\" <skill-name>");
        return 1;
    }
    string prompt = args[1];
    string target = args[2];
    var skill = skills.FirstOrDefault(s => string.Equals(s.Name, target, StringComparison.OrdinalIgnoreCase));
    if (skill is null)
    {
        Console.Error.WriteLine($"❌ Skill '{target}' not found. Try --list.");
        return 1;
    }
    Explain(skill, prompt);
    return 0;
}

// Default: score all skills against the prompt.
{
    string prompt = args[0];
    Console.WriteLine($"Prompt: \"{prompt}\"");
    Console.WriteLine($"Found {skills.Count} skills in {skillsDir}");
    Console.WriteLine();

    var scored = skills
        .Select(s => (Skill: s, Result: Score(s, prompt)))
        .OrderByDescending(x => x.Result.Score)
        .ThenBy(x => x.Skill.Name, StringComparer.Ordinal)
        .ToList();

    int nameW = Math.Max(5, scored.Select(x => x.Skill.Name.Length).DefaultIfEmpty(5).Max());
    Console.WriteLine($"Score  {"Skill".PadRight(nameW)}  Matched triggers");
    Console.WriteLine($"-----  {new string('-', nameW)}  {new string('-', 32)}");
    foreach (var (s, r) in scored)
    {
        string matched = r.Matched.Count == 0 ? "—" : string.Join(", ", r.Matched);
        Console.WriteLine($"{r.Score,5}  {s.Name.PadRight(nameW)}  {matched}");
    }

    var loaded = scored.Where(x => x.Result.Score >= 1).Select(x => x.Skill.Name).ToList();
    Console.WriteLine();
    Console.WriteLine(loaded.Count > 0
        ? "→ Would load: " + string.Join(", ", loaded)
        : "→ Would load: (nothing — no triggers matched)");
}

return 0;

// ---- Helpers --------------------------------------------------------------

static string ResolveSkillsDir()
{
    string? env = Environment.GetEnvironmentVariable("SKILLS_DIR");
    if (!string.IsNullOrWhiteSpace(env)) return env;

    string nextToExe = Path.Combine(AppContext.BaseDirectory, "skills");
    if (Directory.Exists(nextToExe)) return nextToExe;

    return Path.Combine(Directory.GetCurrentDirectory(), "skills");
}

static void PrintUsage()
{
    Console.WriteLine("04-SkillPicker — deterministic skill selection (no LLM).");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run -- \"<user prompt>\"                 Score all skills against the prompt");
    Console.WriteLine("  dotnet run -- --list                            List all discovered skills");
    Console.WriteLine("  dotnet run -- --explain \"<prompt>\" <skill>     Show why a skill scored what it did");
    Console.WriteLine();
    Console.WriteLine("Skills folder: ./skills/  (override with SKILLS_DIR env var)");
}

static List<Skill> LoadSkills(string dir)
{
    var list = new List<Skill>();
    foreach (var path in Directory.EnumerateFiles(dir, "*.skill.md").OrderBy(p => p, StringComparer.Ordinal))
    {
        var skill = TryParse(path);
        if (skill is not null) list.Add(skill);
    }
    return list;
}

static Skill? TryParse(string path)
{
    string[] lines;
    try { lines = File.ReadAllLines(path); }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"⚠️  Could not read {Path.GetFileName(path)}: {ex.Message}");
        return null;
    }

    if (lines.Length == 0 || lines[0].Trim() != "---")
    {
        Console.Error.WriteLine($"⚠️  {Path.GetFileName(path)}: missing YAML frontmatter — skipped.");
        return null;
    }

    int end = -1;
    for (int i = 1; i < lines.Length; i++)
        if (lines[i].Trim() == "---") { end = i; break; }

    if (end < 0)
    {
        Console.Error.WriteLine($"⚠️  {Path.GetFileName(path)}: unterminated frontmatter — skipped.");
        return null;
    }

    string name = "", description = "";
    var triggers = new List<string>();

    for (int i = 1; i < end; i++)
    {
        string raw = lines[i];
        if (string.IsNullOrWhiteSpace(raw)) continue;
        int colon = raw.IndexOf(':');
        if (colon < 0) continue;
        string key = raw[..colon].Trim().ToLowerInvariant();
        string val = raw[(colon + 1)..].Trim();

        switch (key)
        {
            case "name":        name        = StripQuotes(val); break;
            case "description": description = StripQuotes(val); break;
            case "triggers":    triggers    = ParseTriggers(val); break;
        }
    }

    if (string.IsNullOrWhiteSpace(name))
    {
        Console.Error.WriteLine($"⚠️  {Path.GetFileName(path)}: missing 'name' — skipped.");
        return null;
    }
    if (triggers.Count == 0)
    {
        Console.Error.WriteLine($"⚠️  {Path.GetFileName(path)} ({name}): empty 'triggers' — skipped.");
        return null;
    }

    return new Skill(name, description, triggers, path);
}

static string StripQuotes(string s)
{
    if (s.Length >= 2 && (s[0] == '"' || s[0] == '\'') && s[^1] == s[0])
        return s[1..^1];
    return s;
}

static List<string> ParseTriggers(string val)
{
    // Supports inline array form: [a, b, "c d"]   or single value: foo
    val = val.Trim();
    if (val.StartsWith('[') && val.EndsWith(']'))
        val = val[1..^1];

    var parts = val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var list = new List<string>(parts.Length);
    foreach (var p in parts)
    {
        var t = StripQuotes(p).Trim();
        if (t.Length > 0) list.Add(t);
    }
    return list;
}

static (int Score, List<string> Matched, List<string> NotMatched, bool NameMatch) Score(Skill skill, string prompt)
{
    string lowered = prompt.ToLowerInvariant();
    // Tokenize-aware: build a normalized prompt by replacing punctuation with spaces.
    char[] sep = { ' ', '\t', '\r', '\n', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '"', '\'', '-', '/', '\\' };
    string normalized = " " + string.Join(' ', lowered.Split(sep, StringSplitOptions.RemoveEmptyEntries)) + " ";

    var matched = new List<string>();
    var notMatched = new List<string>();
    foreach (var trig in skill.Triggers)
    {
        string t = trig.ToLowerInvariant().Trim();
        // For multi-word triggers, also normalize internal whitespace.
        string tNorm = string.Join(' ', t.Split(sep, StringSplitOptions.RemoveEmptyEntries));
        if (tNorm.Length > 0 && normalized.Contains(" " + tNorm + " ", StringComparison.Ordinal))
            matched.Add(trig);
        else
            notMatched.Add(trig);
    }

    bool nameMatch = !string.IsNullOrEmpty(skill.Name) &&
                     normalized.Contains(" " + skill.Name.ToLowerInvariant() + " ", StringComparison.Ordinal);

    int score = matched.Count + (nameMatch ? 1 : 0);
    return (score, matched, notMatched, nameMatch);
}

static void Explain(Skill skill, string prompt)
{
    var r = Score(skill, prompt);
    Console.WriteLine($"Skill: {skill.Name}");
    Console.WriteLine($"Triggers ({skill.Triggers.Count}): {string.Join(", ", skill.Triggers)}");
    Console.WriteLine($"Prompt:   \"{prompt}\"");
    Console.WriteLine($"Matched ({r.Matched.Count}):     {(r.Matched.Count == 0 ? "—" : string.Join(", ", r.Matched))}");
    Console.WriteLine($"Not matched ({r.NotMatched.Count}): {(r.NotMatched.Count == 0 ? "—" : string.Join(", ", r.NotMatched))}");
    Console.WriteLine($"Name match: {(r.NameMatch ? "yes (+1)" : "no")}");
    Console.WriteLine($"Final score: {r.Score}");
}

// ---- Types ----------------------------------------------------------------

internal sealed record Skill(string Name, string Description, List<string> Triggers, string Path);
