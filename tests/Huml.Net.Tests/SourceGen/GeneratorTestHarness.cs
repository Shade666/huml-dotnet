using System.Collections.Immutable;
using Huml.Net.SourceGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Huml.Net.Tests.SourceGen;

/// <summary>
/// Compiles a snippet of user source together with the Huml.Net source generator and reports
/// diagnostics from (a) the generator run and (b) the recompilation of the user source plus the
/// generated output. Lets the G3.2b tests assert that registering exotic type shapes neither
/// crashes the generator nor produces code that fails to compile.
/// </summary>
internal static class GeneratorTestHarness
{
    // Reference the actual runtime assemblies of the test host (matching whatever TFM the test
    // runs under) plus Huml.Net, so the compiled-under-test assembly and Huml.Net agree on the
    // System.Runtime version. Using fixed reference-assembly packs would mismatch a net10 build.
    private static readonly MetadataReference[] References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        .Where(p =>
        {
            var name = Path.GetFileNameWithoutExtension(p);
            return name.StartsWith("System.", StringComparison.Ordinal)
                || name is "System" or "mscorlib" or "netstandard" or "Huml.Net";
        })
        .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
        .ToArray();

    public sealed record Result(
        ImmutableArray<Diagnostic> GeneratorDiagnostics,
        ImmutableArray<Diagnostic> CompilationDiagnostics,
        string GeneratedCode)
    {
        public IEnumerable<Diagnostic> Errors =>
            GeneratorDiagnostics.Concat(CompilationDiagnostics)
                .Where(d => d.Severity == DiagnosticSeverity.Error);

        public bool HasErrors => Errors.Any();
    }

    public static Result Run(string userSource)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTestAssembly",
            syntaxTrees: [CSharpSyntaxTree.ParseText(userSource)],
            references: References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new HumlSerializationGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var genDiagnostics);

        var generated = output.SyntaxTrees
            .Where(t => t.FilePath.EndsWith(".g.cs", StringComparison.Ordinal))
            .Select(t => t.ToString())
            .ToArray();

        var compDiagnostics = output.GetDiagnostics();

        return new Result(genDiagnostics, compDiagnostics, string.Join("\n\n", generated));
    }
}
