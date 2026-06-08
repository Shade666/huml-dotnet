namespace Huml.Net.Serialization;

/// <summary>
/// Determines the naming policy used to convert a .NET property name to a HUML key during
/// serialisation and deserialisation.
/// </summary>
/// <remarks>
/// Use <see cref="KebabCase"/> for HUML documents that use <c>kebab-case</c> keys
/// (the most common HUML convention). When <c>null</c> is used as the policy in
/// <c>HumlOptions.PropertyNamingPolicy</c>, the .NET property name
/// is used as-is (ordinal-exact, PascalCase by default in C#). A
/// <see cref="HumlPropertyAttribute"/> name override always takes precedence over this policy.
/// </remarks>
public abstract class HumlNamingPolicy
{
    /// <summary>Gets the naming policy that converts PascalCase names to kebab-case:
    /// <c>FullName</c> → <c>full-name</c>.</summary>
    /// <remarks>Acronyms split letter-by-letter: <c>URL</c> → <c>u-r-l</c>.
    /// Digits are treated as word boundaries: <c>B2B</c> → <c>b-2-b</c>,
    /// <c>Version2Name</c> → <c>version-2-name</c>.
    /// For acronym-aware conversion, use <see cref="HumlPropertyAttribute"/> directly.</remarks>
    public static HumlNamingPolicy KebabCase { get; } = new KebabCasePolicy();

    /// <summary>Gets the naming policy that converts PascalCase names to snake_case:
    /// <c>FullName</c> → <c>full_name</c>.</summary>
    /// <remarks>Acronyms split letter-by-letter: <c>URL</c> → <c>u_r_l</c>.
    /// Digits are treated as word boundaries: <c>B2B</c> → <c>b_2_b</c>,
    /// <c>Version2Name</c> → <c>version_2_name</c>.</remarks>
    public static HumlNamingPolicy SnakeCase { get; } = new SnakeCasePolicy();

    /// <summary>Gets the naming policy that converts PascalCase names to camelCase:
    /// <c>FullName</c> → <c>fullName</c>.</summary>
    public static HumlNamingPolicy CamelCase { get; } = new CamelCasePolicy();

    /// <summary>Gets the naming policy that ensures the first character is uppercase:
    /// <c>fullName</c> → <c>FullName</c>.</summary>
    public static HumlNamingPolicy PascalCase { get; } = new PascalCasePolicy();

    /// <summary>Initialises a new instance of <see cref="HumlNamingPolicy"/>.</summary>
    protected HumlNamingPolicy() { }

    /// <summary>When overridden in a derived class, converts the specified .NET property name
    /// according to the policy.</summary>
    /// <param name="name">The .NET property name to convert.</param>
    /// <returns>The converted HUML key name.</returns>
    public abstract string ConvertName(string name);

    /// <inheritdoc/>
    /// <remarks>Two <see cref="HumlNamingPolicy"/> instances of the same concrete type are
    /// considered equal. This ensures that custom stateless policy subclasses stored as separate
    /// instances still share a single <see cref="PropertyDescriptor"/> cache entry rather than
    /// producing unbounded cache growth. Override in a derived class if instance identity is
    /// required.</remarks>
    public override bool Equals(object? obj) =>
        obj is HumlNamingPolicy other && GetType() == other.GetType();

    /// <inheritdoc/>
    public override int GetHashCode() => GetType().GetHashCode();

    // ── Private implementations ───────────────────────────────────────────────

    private sealed class KebabCasePolicy : HumlNamingPolicy
    {
        public override string ConvertName(string name) => Separate(name, '-');
    }

    private sealed class SnakeCasePolicy : HumlNamingPolicy
    {
        public override string ConvertName(string name) => Separate(name, '_');
    }

    private sealed class CamelCasePolicy : HumlNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            // Lowercase the first character only; rest of string unchanged.
            if (char.IsUpper(name[0]))
            {
                var sb = new System.Text.StringBuilder(name.Length);
                sb.Append(char.ToLowerInvariant(name[0]));
                sb.Append(name, 1, name.Length - 1);
                return sb.ToString();
            }
            return name;
        }
    }

    private sealed class PascalCasePolicy : HumlNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            // Uppercase the first character only; rest of string unchanged.
            if (char.IsLower(name[0]))
            {
                var sb = new System.Text.StringBuilder(name.Length);
                sb.Append(char.ToUpperInvariant(name[0]));
                sb.Append(name, 1, name.Length - 1);
                return sb.ToString();
            }
            return name;
        }
    }

    // ── Shared separator state machine (KebabCase + SnakeCase) ───────────────

    /// <summary>
    /// Inserts <paramref name="separator"/> at word boundaries (uppercase-after-lowercase/digit),
    /// lowercasing all characters. Each uppercase letter in an acronym sequence is treated as its
    /// own word boundary (STJ-equivalent: URL → u-r-l / u_r_l, B2B → b-2-b / b_2_b).
    /// Non-alphanumeric characters pass through as-is and reset the state machine.
    /// </summary>
    private static string Separate(string name, char separator)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new System.Text.StringBuilder(name.Length + 4);

        bool prevWasAlpha = false; // true when previous output character was a letter or digit

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];

            if (char.IsUpper(c))
            {
                // Each uppercase letter starts its own word segment.
                // Insert separator before any uppercase letter that follows an existing character.
                if (sb.Length > 0 && prevWasAlpha)
                    sb.Append(separator);

                sb.Append(char.ToLowerInvariant(c));
                prevWasAlpha = true;
            }
            else if (char.IsLower(c) || char.IsDigit(c))
            {
                sb.Append(c);
                prevWasAlpha = true;
            }
            else
            {
                // Non-alphanumeric: pass through, reset state.
                sb.Append(c);
                prevWasAlpha = false;
            }
        }

        return sb.ToString();
    }
}
