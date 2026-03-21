namespace Huml.Net.Parser;

/// <summary>
/// Represents a scalar value.
/// </summary>
/// <param name="Kind">The kind of scalar (string, integer, float, bool, null, nan, or inf).</param>
/// <param name="Value">
/// The runtime value of the scalar, or <c>null</c> for <see cref="ScalarKind.Null"/>,
/// <see cref="ScalarKind.NaN"/>, and <see cref="ScalarKind.Inf"/> scalars.
/// </param>
public sealed record HumlScalar(ScalarKind Kind, object? Value) : HumlNode;
