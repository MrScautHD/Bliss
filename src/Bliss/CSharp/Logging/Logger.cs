using System.Runtime.CompilerServices;

namespace Bliss.CSharp.Logging;

public class Logger {
    
    /// <summary>
    /// A delegate used to handle logging messages.
    /// </summary>
    /// <param name="type">The type of the log message, represented by the <see cref="LogType"/> enum.</param>
    /// <param name="msg">The log message content.</param>
    /// <param name="color">The console color to display the message in.</param>
    /// <param name="sourceFilePath">The file path of the caller. Automatically set by the compiler.</param>
    /// <param name="memberName">The name of the calling member. Automatically set by the compiler.</param>
    /// <param name="sourceLineNumber">The line number of the call within the source file. Automatically set by the compiler.</param>
    /// <returns>Returns a boolean value indicating whether the message has been handled.</returns>
    public delegate bool OnMessage(LogType type, string msg, ConsoleColor color, string sourceFilePath, string memberName, int sourceLineNumber);
    
    /// <summary>
    /// An event that is triggered when a log message is generated.
    /// </summary>
    public static event OnMessage? Message;
    
    /// <summary>
    /// Logs a debug message if the build configuration is set to DEBUG.
    /// </summary>
    /// <param name="msg">The debug message to log.</param>
    /// <param name="sourceFilePath">The file path of the caller. Automatically set by the compiler.</param>
    /// <param name="memberName">The name of the calling member. Automatically set by the compiler.</param>
    /// <param name="sourceLineNumber">The line number of the call within the source file. Automatically set by the compiler.</param>
    public static void Debug(string msg, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
#if DEBUG
        Log(LogType.Debug, msg, ConsoleColor.Gray, sourceFilePath, memberName, sourceLineNumber);
#endif
    }
    
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="msg">The informational message to log.</param>
    /// <param name="sourceFilePath">The file path of the caller. Automatically set by the compiler.</param>
    /// <param name="memberName">The name of the calling member. Automatically set by the compiler.</param>
    /// <param name="sourceLineNumber">The line number of the call within the source file. Automatically set by the compiler.</param>
    public static void Info(string msg, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Log(LogType.Info, msg, ConsoleColor.Cyan, sourceFilePath, memberName, sourceLineNumber);
    }
    
    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="msg">The warning message to log.</param>
    /// <param name="sourceFilePath">The file path of the caller. Automatically set by the compiler.</param>
    /// <param name="memberName">The name of the calling member. Automatically set by the compiler.</param>
    /// <param name="sourceLineNumber">The line number of the call within the source file. Automatically set by the compiler.</param>
    public static void Warn(string msg, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Log(LogType.Warn, msg, ConsoleColor.Yellow, sourceFilePath, memberName, sourceLineNumber);
    }
    
    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="msg">The error message to log.</param>
    /// <param name="sourceFilePath">The file path of the caller. Automatically set by the compiler.</param>
    /// <param name="memberName">The name of the calling member. Automatically set by the compiler.</param>
    /// <param name="sourceLineNumber">The line number of the call within the source file. Automatically set by the compiler.</param>
    public static void Error(string msg, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Log(LogType.Error, msg, ConsoleColor.Red, sourceFilePath, memberName, sourceLineNumber);
    }
    
    /// <summary>
    /// Logs a fatal error message.
    /// </summary>
    /// <param name="msg">The fatal error message to log.</param>
    /// <param name="sourceFilePath">The file path of the caller. Automatically set by the compiler.</param>
    /// <param name="memberName">The name of the calling member. Automatically set by the compiler.</param>
    /// <param name="sourceLineNumber">The line number of the call within the source file. Automatically set by the compiler.</param>
    public static void Fatal(string msg, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Log(LogType.Fatal, msg, ConsoleColor.Red, sourceFilePath, memberName, sourceLineNumber);
        throw new Exception(msg);
    }
    
    /// <summary>
    /// Logs a fatal error message.
    /// </summary>
    /// <param name="exception">The exception to log and rethrow.</param>
    /// <param name="sourceFilePath">The file path of the caller. Automatically set by the compiler.</param>
    /// <param name="memberName">The name of the calling member. Automatically set by the compiler.</param>
    /// <param name="sourceLineNumber">The line number of the call within the source file. Automatically set by the compiler.</param>
    public static void Fatal(Exception exception, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Log(LogType.Fatal, exception.Message, ConsoleColor.Red, sourceFilePath, memberName, sourceLineNumber);
        throw exception;
    }
    
    /// <summary>
    /// Logs a message.
    /// </summary>
    /// <param name="type">The type of log (e.g., Debug, Info, Warn, Error, Fatal).</param>
    /// <param name="msg">The message content to log.</param>
    /// <param name="color">The console color to display the log message.</param>
    /// <param name="sourceFilePath">The file path of the caller. Automatically set by the compiler.</param>
    /// <param name="memberName">The name of the calling member. Automatically set by the compiler.</param>
    /// <param name="sourceLineNumber">The line number of the call within the source file. Automatically set by the compiler.</param>
    private static void Log(LogType type, string msg, ConsoleColor color, string sourceFilePath, string memberName, int sourceLineNumber) {
        OnMessage? message = Message;
        
        if (message != null) {
            if (message.Invoke(type, msg, color, sourceFilePath, memberName, sourceLineNumber)) {
                return;
            }
        }
        
        string fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string metaInfo = $"[{timestamp} | {fileName}::{memberName}({sourceLineNumber})] ";
        
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(metaInfo);
        
        Console.ForegroundColor = color;
        Console.WriteLine($"{msg}");
        
        Console.ResetColor();
    }
}