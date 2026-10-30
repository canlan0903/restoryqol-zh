using Mono.Cecil;
using Mono.Cecil.Cil;

// 只读的游戏程序集探针：不需要 ILSpy / dnSpy，一台装了 .NET SDK 的机器就能查
// 「这个类有哪些成员」「这个方法到底调了什么」「谁引用了 OnXxx 事件」。
//
//   dotnet run --project tools/asm-probe -- <assembly.dll> types   <substring>
//   dotnet run --project tools/asm-probe -- <assembly.dll> members <TypeName>
//   dotnet run --project tools/asm-probe -- <assembly.dll> il      <TypeName> [MethodName]
//   dotnet run --project tools/asm-probe -- <assembly.dll> callers <symbolSubstring>
//   dotnet run --project tools/asm-probe -- <assembly.dll> strings <textSubstring>

if (args.Length < 3)
{
    Console.Error.WriteLine("usage: asm-probe <assembly.dll> <types|members|il|callers|strings> <arg> [method]");
    return 2;
}

var asmPath = args[0];
var cmd = args[1].ToLowerInvariant();
var needle = args[2];
var methodFilter = args.Length > 3 ? args[3] : null;

if (!File.Exists(asmPath))
{
    Console.Error.WriteLine($"not found: {asmPath}");
    return 2;
}

using var asm = AssemblyDefinition.ReadAssembly(asmPath);
var types = asm.Modules.SelectMany(m => m.Types).ToList();

static bool Match(string s, string n) => s.Contains(n, StringComparison.OrdinalIgnoreCase);

TypeDefinition? FindType(string name) =>
    types.FirstOrDefault(t => t.Name == name || t.FullName == name)
    ?? types.FirstOrDefault(t => Match(t.Name, name));

switch (cmd)
{
    case "types":
        foreach (var t in types.Where(t => Match(t.FullName, needle)))
            Console.WriteLine($"{t.FullName}  ({t.Fields.Count} fields, {t.Methods.Count} methods)");
        break;

    case "members":
    {
        var t = FindType(needle);
        if (t == null) { Console.WriteLine($"(no type matching '{needle}')"); return 1; }
        Console.WriteLine($"== {t.FullName} : {t.BaseType?.FullName}");
        foreach (var f in t.Fields) Console.WriteLine($"  field {VisF(f)} {f.FieldType.Name} {f.Name}");
        foreach (var p in t.Properties) Console.WriteLine($"  prop  {p.PropertyType.Name} {p.Name}");
        foreach (var m in t.Methods)
            Console.WriteLine($"  meth  {Vis(m)} {m.ReturnType.Name} {m.Name}({string.Join(", ", m.Parameters.Select(x => x.ParameterType.Name + " " + x.Name))})");
        break;
    }

    case "il":
    {
        var t = FindType(needle);
        if (t == null) { Console.WriteLine($"(no type matching '{needle}')"); return 1; }
        foreach (var m in t.Methods)
        {
            if (methodFilter != null && !Match(m.Name, methodFilter)) continue;
            Console.WriteLine($"\n--- {t.Name}.{m.Name}");
            if (!m.HasBody) { Console.WriteLine("      (no body)"); continue; }
            foreach (var i in m.Body.Instructions)
                Console.WriteLine($"      {i.OpCode,-10} {i.Operand}");
        }
        break;
    }

    case "callers":
        foreach (var t in types)
        {
            foreach (var m in t.Methods)
            {
                if (!m.HasBody) continue;
                var hits = m.Body.Instructions
                    .Select(i => i.Operand?.ToString() ?? "")
                    .Where(s => Match(s, needle))
                    .Distinct()
                    .ToList();
                if (hits.Count == 0) continue;
                Console.WriteLine($"\n== {t.Name}.{m.Name}");
                foreach (var h in hits.Take(5)) Console.WriteLine($"   <- {h}");
            }
        }
        break;

    case "strings":
        foreach (var t in types)
        {
            foreach (var m in t.Methods)
            {
                if (!m.HasBody) continue;
                foreach (var i in m.Body.Instructions)
                {
                    if (i.OpCode.Code != Code.Ldstr) continue;
                    var s = i.Operand as string ?? "";
                    if (Match(s, needle)) Console.WriteLine($"{t.Name}.{m.Name} :: \"{s}\"");
                }
            }
        }
        break;

    default:
        Console.Error.WriteLine($"unknown command '{cmd}'");
        return 2;
}

return 0;

static string Vis(MethodDefinition m) => m.IsPublic ? "pub" : m.IsPrivate ? "prv" : "prt";
static string VisF(FieldDefinition f) => f.IsPublic ? "pub" : f.IsPrivate ? "prv" : "prt";
