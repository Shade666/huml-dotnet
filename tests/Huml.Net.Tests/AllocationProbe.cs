namespace Huml.Net.Tests;

/// <summary>
/// Robust per-call allocation measurement for GC-budget tests. Transient effects —
/// tiered-JIT promotion mid-test, TLAB re-priming after a background GC, analyzer or
/// sibling-process load — only ever <em>inflate</em> a measurement, so the minimum
/// across attempts is the true steady-state allocation cost. Added for the G3 audit
/// after a one-off budget failure during a parallel three-TFM run.
/// </summary>
internal static class AllocationProbe
{
    public static long Measure(Action action, int attempts = 3)
    {
        long best = long.MaxValue;
        for (int i = 0; i < attempts; i++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            action();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            best = Math.Min(best, allocated);
        }
        return best;
    }
}
