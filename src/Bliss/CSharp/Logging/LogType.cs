namespace Bliss.CSharp.Logging;

public enum LogType {
    
    /// <summary>
    /// Debug level logging, used for detailed diagnostic messages.
    /// </summary>
    Debug,
    
    /// <summary>
    /// Info level logging, used for general informational messages.
    /// </summary>
    Info,
    
    /// <summary>
    /// Warn level logging, used to indicate potential issues or warnings.
    /// </summary>
    Warn,
    
    /// <summary>
    /// Error level logging, used for error messages that indicate a failure.
    /// </summary>
    Error,
    
    /// <summary>
    /// Fatal level logging, used for critical errors that cause termination.
    /// </summary>
    Fatal
}