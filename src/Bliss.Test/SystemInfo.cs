namespace Bliss.Test;

public static class SystemInfo {

    /// <summary>
    /// Gets the CPU identifier string or "Unknown" if not available.
    /// </summary>
    public static string Cpu => Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown";

    /// <summary>
    /// Gets the total available memory size in gigabytes (GB).
    /// </summary>
    public static int MemorySize => (int) Math.Ceiling(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0F * 1024.0F * 1024.0F));
    
    /// <summary>
    /// Gets the number of available processor threads.
    /// </summary>
    public static int Threads => Environment.ProcessorCount;

    /// <summary>
    /// Gets the operating system version string.
    /// </summary>
    public static string Os => Environment.OSVersion.VersionString;
}