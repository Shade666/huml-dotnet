using Microsoft.CodeAnalysis;

namespace Huml.Net.SourceGeneration;

/// <summary>
/// Incremental source generator for Huml.Net. For each type registered via
/// <c>[HumlSerializable(typeof(T))]</c> on a <c>HumlGeneratedContext</c> subclass,
/// emits a concrete <c>HumlTypeInfo&lt;T&gt;</c> and the corresponding dispatch override.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class HumlSerializationGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Phase 67: ForAttributeWithMetadataName pipeline, equatable models, and code emission.
    }
}
