/// <summary>
///   Copied from https://github.com/Azure-App-Service/KuduLite/blob/dev/Kudu.Services/Diagnostics/LogStreamManager.cs
/// </summary>

namespace Xylab.Management.LogStream;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

public class LogStreamManager(IFileSystemV2 fileSystem, ILogger logger, LogStreamOptions options) : IDisposable
{
    private const string FilterQueryKey = "filter";

    private static string[] LogFileExtensions = new string[] { ".txt", ".log", ".htm" };

    // Azure 3 mins timeout, heartbeat every mins keep alive.
    private static TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(60);

    private readonly Lock _thisLock = new Lock();
    private string _filter;
    //private readonly IEnvironment _environment;
    // CORE TODO No longer needed, each task can check context.RequestAborted token
    // private readonly List<Task> _results;

    private Dictionary<string, long> _logFiles;
    private IFileSystemWatcher _watcher;
    private Timer _heartbeat;
    private DateTime _lastTraceTime;
    private DateTime _startTime;
    private Stopwatch stopwatch;

    // CORE TODO
    //private ShutdownDetector _shutdownDetector;
    //private CancellationTokenRegistration _cancellationTokenRegistration;

    // CORE TODO bind this class as transient (one per request)

    public async Task ProcessRequest(HttpContext context)
    {
        _startTime = DateTime.UtcNow;
        _lastTraceTime = _startTime;

        DisableResponseBuffering(context);
        stopwatch = Stopwatch.StartNew();
        
        // CORE TODO Shutdown detector registration

        // CORE TODO double check on semantics of this (null vs empty etc);
        _filter = context.Request.Query[FilterQueryKey].ToString();

        // CORE TODO parse from path
        // path route as in logstream/{*path} without query strings
        // string routePath = context.Request.RequestContext.RouteData.Values["path"] as string;

        string path;
        string routePath = "";
        //bool enableTrace;

        // trim '/'
        routePath = string.IsNullOrEmpty(routePath) ? routePath : routePath.Trim('/');

        var firstPath = routePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        // Ensure mounted logFiles dir 
        string mountedLogFilesDir = Path.Combine(options.Path, routePath);

        fileSystem.Directory.EnsureDirectory(mountedLogFilesDir);

        /*
        if (ShouldMonitiorMountedLogsPath(mountedLogFilesDir))
        {
            path = mountedLogFilesDir;
        }
        else
        {
            path = volatileLogsPath;
        }*/
        path = mountedLogFilesDir;

        bool showPreviousTracesTail =
            context.Request.Query.ContainsKey("showPrevious")
            && context.Request.Query["showPrevious"] == "true";

        context.Response.Headers["Content-Type"] = "text/event-stream";

        await WriteInitialMessage(context);

        // CORE TODO Get the fsw and keep it in scope here with a using that ends at the end

        using (_thisLock.EnterScope())
        {
            Initialize(path, context);
        }

        if (showPreviousTracesTail)
        {
            if (_logFiles != null)
            {
                NotifyClientWithLineBreak("Starting Log Tail -n 10 of existing logs ----", context);

                try
                {
                    foreach (string log in _logFiles.Keys)
                    {
                        using var reader = new StreamReader(log, Encoding.ASCII, false, new FileStreamOptions { Share = FileShare.ReadWrite });

                        var vfsPath = GetFileVfsPath(log);

                        var printLine = log + " " + (!string.IsNullOrEmpty(vfsPath) ? " (" + vfsPath + ")" : "");

                        NotifyClientWithLineBreak(string.Format(
                                    CultureInfo.CurrentCulture,
                                    printLine,
                                    DateTime.UtcNow.ToString("s"),
                                    Environment.NewLine), context);

                        foreach (string logLine in Tail(reader, 10))
                        {
                            await context.Response.WriteAsync(logLine);
                            await context.Response.WriteAsync(Environment.NewLine);
                        }

                        await context.Response.WriteAsync(Environment.NewLine);
                    }
                }
                catch (Exception)
                {
                    // best effort to get tail logs
                }

                NotifyClientWithLineBreak("Ending Log Tail of existing logs ---", context);
            }
            else
            {
                logger.LogError("LogStream: No pervious logfiles");
            }

            NotifyClientWithLineBreak("Starting Live Log Stream ---", context);
        }

        // CORE TODO diagnostics setting for enabling app logging            

        while (!context.RequestAborted.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(HeartbeatInterval, context.RequestAborted);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var elapsed = stopwatch.Elapsed;

            if (elapsed >= options.Timeout)
            {
                var timeoutMsg = string.Format(
                    "{0}  Stream terminated due to timeout {1} min(s).{2}",
                    DateTime.UtcNow.ToString("s"),
                    (int)elapsed.TotalMinutes,
                    Environment.NewLine);

                await context.Response.WriteAsync(timeoutMsg);
                return;
            }
            else if (elapsed >= HeartbeatInterval)
            {
                var heartbeatMsg = string.Format(
                    CultureInfo.CurrentCulture,
                    "{0}  No new trace in the past {1} min(s).{2}",
                    DateTime.UtcNow.ToString("s"),
                    (int)elapsed.TotalMinutes,
                    Environment.NewLine);

                await context.Response.WriteAsync(heartbeatMsg);
            }
        }
    }

    ///<summary>Returns the end of a text reader.</summary>
    ///<param name="reader">The reader to read from.</param>
    ///<param name="lineCount">The number of lines to return.</param>
    ///<returns>The last lineCount lines from the reader.</returns>
    public static string[] Tail(TextReader reader, int lineCount)
    {
        var buffer = new List<string>(lineCount);
        string line;
        for (int i = 0; i < lineCount; i++)
        {
            line = reader.ReadLine();
            if (line == null) return buffer.ToArray();
            buffer.Add(line);
        }

        int lastLine = lineCount - 1;           //The index of the last line read from the buffer.  Everything > this index was read earlier than everything <= this indes

        while (null != (line = reader.ReadLine()))
        {
            lastLine++;
            if (lastLine == lineCount) lastLine = 0;
            buffer[lastLine] = line;
        }

        if (lastLine == lineCount - 1) return buffer.ToArray();
        var retVal = new string[lineCount];
        buffer.CopyTo(lastLine + 1, retVal, 0, lineCount - lastLine - 1);
        buffer.CopyTo(0, retVal, lineCount - lastLine - 1, lastLine + 1);
        return retVal;
    }

    private static Task WriteInitialMessage(HttpContext context)
    {
        var msg = string.Format( 
            CultureInfo.CurrentCulture,
            "{0}  Welcome, you are now connected to log-streaming service.{1}", 
            DateTime.UtcNow.ToString("s"), 
            Environment.NewLine);

        return context.Response.WriteAsync(msg);
    }

    /// <summary>
    /// Determines if Kudu Should Monitor Mounted Logs directory,
    /// or the mounted fs logs dir, if kudu
    /// </summary>
    /// <returns></returns>
    private bool ShouldMonitiorMountedLogsPath(string mountedDirPath)
    {
        int count = 0;
        string dateToday = DateTime.Now.ToString("yyyy_MM_dd");

        if (fileSystem.Directory.Exists(mountedDirPath))
        {
            // if more than two log files present that are generated today, 
            // use this directory; first file for a date is the marker file
            foreach (var file in fileSystem.Directory.GetFiles(mountedDirPath, "*", SearchOption.AllDirectories))
            {
                if (file.Contains(dateToday) && ++count > 1)
                {
                    break;
                }
            }
        }

        return count == 2;
    }

    private void Initialize(string path, HttpContext context)
    {
        Debug.Assert(_watcher == null, "we only allow one manager per request!");

        // initalize _logFiles before the file watcher since file watcher event handlers reference _logFiles
        // this mirrors the Reset() where we stop the file watcher before nulling _logFile.
        if (_logFiles == null)
        {
            var logFiles = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (var ext in LogFileExtensions)
            {
                foreach (var file in fileSystem.Directory.GetFiles(path, "*" + ext, SearchOption.AllDirectories))
                {
                    try
                    {
                        logFiles[file] = fileSystem.FileInfo.FromFileName(file).Length;
                    }
                    catch (Exception ex)
                    {
                        // avoiding racy with providers cleaning up log file
                        logger.LogError(ex, "Error accessing log file");
                    }
                }
            }

            _logFiles = logFiles;
        }

        if (_watcher == null)
        {
            IFileSystemWatcher watcher = OperatingSystem.IsWindows()
                ? new WindowsFileSystemWatcher(path, true)
                : new LinuxFileSystemWatcher(path, LogFileExtensions);
            watcher.Changed += new FileSystemEventHandler(DoSafeAction<object, FileSystemEventArgs, HttpContext>(OnChanged, "LogStreamManager.OnChanged", context));
            watcher.Deleted += new FileSystemEventHandler(DoSafeAction<object, FileSystemEventArgs,HttpContext>(OnDeleted, "LogStreamManager.OnDeleted", context));
            watcher.Renamed += new RenamedEventHandler(DoSafeAction<object, RenamedEventArgs,HttpContext>(OnRenamed, "LogStreamManager.OnRenamed", context));
            //watcher.Error += new ErrorEventHandler(DoSafeAction<object, ErrorEventArgs,HttpContext>(OnError, "LogStreamManager.OnError", context));
            watcher.Error += new ErrorEventHandler(DoSafeAction<object, ErrorEventArgs,HttpContext>(OnError, "LogStreamManager.OnError", context));
            watcher.Start();
            _watcher = watcher;
        }

        if (_heartbeat == null)
        {
            _heartbeat = new Timer(OnHeartbeat, context, HeartbeatInterval, HeartbeatInterval);
        }
    }

    // Suppress exception on callback to not crash the process.
    private Action<T1, T2> DoSafeAction<T1, T2, T3>(Action<T1, T2, T3> func, string eventName, T3 context)
    {
        return (t1, t2) =>
        {
            try
            {
                try
                {
                    func(t1, t2, context);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Step: {eventName}", eventName);
                }
            }
            catch
            {
                // no-op
            }
        };
    }
    
    private void DisableResponseBuffering(HttpContext context)
    {
        IHttpResponseBodyFeature bufferingFeature = context.Features.Get<IHttpResponseBodyFeature>();
        if (bufferingFeature != null)
        {
            bufferingFeature.DisableBuffering();
        }
    }

    private void Reset()
    {
        if (_watcher != null)
        {
            _watcher.Stop();
            // dispose is blocked till all change request handled, 
            // this could lead to deadlock as we share the same lock
            // http://stackoverflow.com/questions/73128/filesystemwatcher-dispose-call-hangs
            // in the meantime, let GC handle it
            // _watcher.Dispose();
            _watcher = null;
        }

        if (_heartbeat != null)
        {
            _heartbeat.Dispose();
            _heartbeat = null;
        }

        _logFiles = null;
    }
    
    private void OnHeartbeat(object state)
    {
        try
        {
            try
            {
                HttpContext context = (HttpContext) state;
                TimeSpan ts = DateTime.UtcNow.Subtract(_startTime);
                if (ts >= options.Timeout)
                {
                    TerminateClient(string.Format(
                        "{0}  Stream terminated due to timeout {1} min(s).{2}",
                        DateTime.UtcNow.ToString("s"),
                        (int)ts.TotalMinutes,
                        Environment.NewLine), context);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Step: LogStreamManager.OnHeartbeat");
            }
        }
        catch
        {
            // no-op
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs e, HttpContext context)
    {
        if (e.ChangeType == WatcherChangeTypes.Changed && MatchFilters(e.FullPath))
        {
            // reading the delta of file changed, retry if failed.
            IEnumerable<string> lines = null;
            OperationManager.Attempt(() =>
            {
                lines = GetChanges(e);
            }, 3, 100);

            if (lines.Count() > 0)
            {
                _lastTraceTime = DateTime.UtcNow;
                stopwatch = Stopwatch.StartNew();
                NotifyClient(lines,context);
            }
        }
    }

    private async void NotifyClientWithLineBreak(string line, HttpContext context)
    {
        if (!context.RequestAborted.IsCancellationRequested)
        {
            try
            {
                await context.Response.WriteAsync(Environment.NewLine);
                await context.Response.WriteAsync(line);
                await context.Response.WriteAsync(Environment.NewLine);
            }
            catch (Exception)
            {
                logger.LogError("Error notifying client");
            }
        }
    }

    /*
     * CORE TODO : Rest API to watch a particular file by LogStream
     * /
    private string ParseRequest(HttpContext context)
    {
        _filter = context.Request.QueryString[FilterQueryKey];

        // path route as in logstream/{*path} without query strings
        string routePath = context.Request.RequestContext.RouteData.Values["path"] as string;
        
        // trim '/'
        routePath = String.IsNullOrEmpty(routePath) ? routePath : routePath.Trim('/');

        // logstream at root
        if (String.IsNullOrEmpty(routePath))
        {
            _enableTrace = true;
            return options.Path;
        }

        var firstPath = routePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.Equals(firstPath, "Application", StringComparison.OrdinalIgnoreCase))
        {
            _enableTrace = true;
        }

        return FileSystemHelpers.EnsureDirectory(Path.Combine(options.Path, routePath));
    }
    */

    private static bool MatchFilters(string fileName)
    {
        if (!string.IsNullOrEmpty(fileName))
        {
            foreach (string ext in LogFileExtensions)
            {
                if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void NotifyClient(string text, HttpContext context)
    {
        NotifyClient(new string[] { text },context);
    }

    private async void NotifyClient(IEnumerable<string> lines, HttpContext context)
    {
        if (!context.RequestAborted.IsCancellationRequested)
        {
            try
            {
                await context.Response.WriteAsync(System.Environment.NewLine);
                foreach (var line in lines)
                {
                    await context.Response.WriteAsync(line);
                }
            }
            catch (Exception)
            {
                logger.LogError("Error notifying client");
            }
        }
    }

    private IEnumerable<string> GetChanges(FileSystemEventArgs e)
    {
        using (_thisLock.EnterScope())
        {
            // do no-op if races between idle timeout and file change event
            /*
            if (_results.Count == 0)
            {
                return Enumerable.Empty<string>();
            }
            */
            try
            { 
                long offset = 0;
                if (!_logFiles.TryGetValue(e.FullPath, out offset))
                {
                    _logFiles[e.FullPath] = 0;
                }

                using (FileStream fs = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long length = fs.Length;

                    // file was truncated
                    if (offset > length)
                    {
                        _logFiles[e.FullPath] = offset = 0;
                    }

                    // multiple events
                    if (offset == length)
                    {
                        return Enumerable.Empty<string>();
                    }

                    if (offset != 0)
                    {
                        fs.Seek(offset, SeekOrigin.Begin);
                    }

                    List<string> changes = new List<string>();

                    StreamReader reader = new StreamReader(fs);
                    while (!reader.EndOfStream)
                    {
                        string line = ReadLine(reader);
                        if (String.IsNullOrEmpty(_filter) || line.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            changes.Add(line);
                        }
                    }

                    // Adjust offset and return changes
                    _logFiles[e.FullPath] = reader.BaseStream.Position;

                    return changes;
                }
            }
            catch(FileNotFoundException)
            {
                // if no log is produced for a long time,
                // lwas Log4Net maintains an open file-handle for an 
                // old log file, workaround till that is fixed
                return Enumerable.Empty<string>();
            }
        }
    }

    private void OnDeleted(object sender, FileSystemEventArgs e, HttpContext context)
    {
        if (e.ChangeType == WatcherChangeTypes.Deleted)
        {
            using (_thisLock.EnterScope())
            {
                _logFiles.Remove(e.FullPath);
            }
        }
    }

    private void OnRenamed(object sender, RenamedEventArgs e, HttpContext context)
    {
        if (e.ChangeType == WatcherChangeTypes.Renamed)
        {
            using (_thisLock.EnterScope())
            {
                _logFiles.Remove(e.OldFullPath);
            }
        }
    }

    private void OnError(object sender, ErrorEventArgs e, HttpContext context)
    {
        try
        {
            using (_thisLock.EnterScope())
            {
                if (_watcher != null)
                {
                    string path = _watcher.Path;
                    Reset();
                    Initialize(path,context);
                }
            }
        }
        catch (Exception ex)
        {
            OnCriticalError(ex, context);
        }
    }

    private void OnCriticalError(Exception ex, HttpContext context)
    {
        TerminateClient(string.Format("{0}{1}  Error has occurred and stream is terminated. {2}{0}", Environment.NewLine, DateTime.UtcNow.ToString("s"), ex.Message), context);
    }

    private void TerminateClient(string text, HttpContext context)
    {
        NotifyClient(text, context);
        using (_thisLock.EnterScope())
        {
            // Proactively cleanup resources
            Reset();
        }
        /*
        using (_thisLock.EnterScope())
        {
            foreach (ProcessRequestAsyncResult result in _results)
            {
                //CORE CHECK
                result.Complete(false);
            }

            _results.Clear();

            // Proactively cleanup resources
            Reset();
        }
        */
    }

    // this has the same performance and implementation as StreamReader.ReadLine()
    // they both account for '\n' or '\r\n' as new line chars.  the difference is 
    // this returns the result with preserved new line chars.
    // without this, logstream can only guess whether it is '\n' or '\r\n' which is 
    // subjective to each log providers/files.
    private static string ReadLine(StreamReader reader)
    {
        var strb = new StringBuilder();
        int val;
        while ((val = reader.Read()) >= 0)
        {
            char ch = (char)val;
            strb.Append(ch);
            switch (ch)
            {
                case '\r':
                case '\n':
                    if (ch == '\r' && (char)reader.Peek() == '\n')
                    {
                        ch = (char)reader.Read();
                        strb.Append(ch);
                    }
                    return strb.ToString();
                default:
                    break;
            }
        }

        return strb.ToString();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private string GetFileVfsPath(string filePath)
    {
        string relativePath = filePath.IndexOf("LogFiles") > 0 ? filePath.Substring(filePath.IndexOf("LogFiles")).Replace("\\", "/") : "";
        string fileName = Uri.EscapeDataString(relativePath.Substring(relativePath.LastIndexOf("/")+1));
        string encodedUrl = relativePath.Substring(0, relativePath.LastIndexOf("/")+1) + fileName;

        return filePath.Contains("LogFiles") ? 
            options.VfsApiBase.TrimEnd('/') + "/" + encodedUrl : "";
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Reset();
        }
    }
}
