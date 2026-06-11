namespace Huml.Net.Parser;

/// <summary>
/// Iterative structural equality and hashing for the AST node graph. Used by the node
/// records' <c>Equals</c>/<c>GetHashCode</c> overrides so that two nodes parsed from
/// different source positions compare equal (the contract documented on <see cref="HumlNode"/>),
/// and so that pathologically deep graphs do not overflow the stack the way the
/// compiler-generated recursive equality would. <see cref="HumlNode.Line"/> and
/// <see cref="HumlNode.Column"/> are intentionally excluded.
/// </summary>
internal static class HumlNodeEquality
{
    public static bool DeepEquals(HumlNode? x, HumlNode? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        var stack = new Stack<(HumlNode, HumlNode)>();
        stack.Push((x, y));

        while (stack.Count > 0)
        {
            var (a, b) = stack.Pop();
            if (ReferenceEquals(a, b)) continue;
            if (a.GetType() != b.GetType()) return false;

            switch (a)
            {
                case HumlScalar sa when b is HumlScalar sb:
                    if (sa.Kind != sb.Kind || !Equals(sa.Value, sb.Value)) return false;
                    break;

                case HumlMapping ma when b is HumlMapping mb:
                    if (!string.Equals(ma.Key, mb.Key, StringComparison.Ordinal)) return false;
                    stack.Push((ma.Value, mb.Value));
                    break;

                case HumlSequence qa when b is HumlSequence qb:
                    if (!PushChildren(qa.Items, qb.Items, stack)) return false;
                    break;

                case HumlDocument da when b is HumlDocument db:
                    if (da.DetectedVersion != db.DetectedVersion) return false;
                    if (!PushChildren(da.Entries, db.Entries, stack)) return false;
                    break;

                case HumlInlineMapping ia when b is HumlInlineMapping ib:
                    if (!PushChildren(ia.Entries, ib.Entries, stack)) return false;
                    break;

                default:
                    return false;
            }
        }
        return true;
    }

    private static bool PushChildren(
        IReadOnlyList<HumlNode> a, IReadOnlyList<HumlNode> b, Stack<(HumlNode, HumlNode)> stack)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            stack.Push((a[i], b[i]));
        return true;
    }

    public static int DeepHashCode(HumlNode root)
    {
        // Order-sensitive structural hash computed with an explicit stack so deep graphs
        // cannot overflow. Each node contributes its discriminating fields; children are
        // folded in document order.
        var hash = new HashCode();
        var stack = new Stack<HumlNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            switch (node)
            {
                case HumlScalar s:
                    hash.Add(0);
                    hash.Add(s.Kind);
                    hash.Add(s.Value);
                    break;
                case HumlMapping m:
                    hash.Add(1);
                    hash.Add(m.Key, StringComparer.Ordinal);
                    stack.Push(m.Value);
                    break;
                case HumlSequence q:
                    hash.Add(2);
                    hash.Add(q.Items.Count);
                    PushReversed(q.Items, stack);
                    break;
                case HumlDocument d:
                    hash.Add(3);
                    hash.Add(d.Entries.Count);
                    PushReversed(d.Entries, stack);
                    break;
                case HumlInlineMapping inl:
                    hash.Add(4);
                    hash.Add(inl.Entries.Count);
                    PushReversed(inl.Entries, stack);
                    break;
            }
        }
        return hash.ToHashCode();
    }

    private static void PushReversed(IReadOnlyList<HumlNode> items, Stack<HumlNode> stack)
    {
        // Push in reverse so children are popped in document order (keeps the hash stable
        // and order-sensitive).
        for (int i = items.Count - 1; i >= 0; i--)
            stack.Push(items[i]);
    }
}
