using System;
using System.Diagnostics;
using System.Text;

/// <summary>
/// Utility class for launching external processes.
/// This class wraps around <see cref="Process"/> such that the launched process
/// provides error logging using redirection.
/// </summary>
public class ExternalProcess
{
	/// <summary>
	/// The process start info needed to launch the process
	/// </summary>
	private readonly ProcessStartInfo startInfo;

	/// <summary>
	/// Reference to the started process.
	/// This will be <c>null</c> unless <see cref="Start"/> succeeds.
	/// </summary>
	private Process process;

	/// <summary>
	/// Error output redirected from process
	/// </summary>
	private readonly StringBuilder errorLog;

	/// <summary>
	/// The error log of the process
	/// </summary>
	public string ErrorLog
	{
		get => errorLog.ToString();
	}

	/// <summary>
	/// Creates a process (not started) using the path and arguments.
	/// </summary>
	/// <param name="pathToExecutable">The path to the executable</param>
	/// <param name="arguments">Arguments passed to the executable</param>
	public ExternalProcess(string pathToExecutable, string arguments)
	{
		startInfo = new ProcessStartInfo(
					fileName: pathToExecutable,
					arguments: arguments
				)
		{
			// Necessary to redirect output
			UseShellExecute = false,
			RedirectStandardError = true,
		};

		errorLog = new StringBuilder();
		errorLog.AppendFormat("{0} {1}{2}{2}", pathToExecutable, arguments, Environment.NewLine);

	}

	/// <summary>
	/// If <c>true</c>, this will hide the window (if any) of the started process
	/// </summary>
	public bool Hide
	{
		set
		{
			startInfo.CreateNoWindow = value;
		}
	}

	/// <summary>
	/// The working directory of the started process
	/// </summary>
	public string WorkingDirectory
	{
		get => startInfo.WorkingDirectory;

		set {
			startInfo.WorkingDirectory = value;
		}
	}

	/// <summary>
	/// <c>true</c> if the process was started successfully, <c>false</c> otherwise
	/// </summary>
	public bool HasStarted
	{
		get => process != null;
	}

	/// <summary>
	/// <c>true</c> if the process was started successfully, and then exited, <c>false</c> otherwise
	/// </summary>
	public bool HasExited
	{
		get => process?.HasExited ?? false;
	}

	/// <summary>
	/// Attempts to start the process
	/// </summary>
	/// <remarks>
	/// This does not guarantee that the process will start successfully.
	/// Use <see cref="HasStarted"/> to verify if the process actually started.
	/// </remarks>
	/// <example>
	/// <code>
	/// process.Start();
	/// if (process.HasStarted)
	/// {
	///    // Do something with the process
	/// }
	/// </code>
	/// </example>
	public void Start()
	{
		try
		{
			process = Process.Start(startInfo);
		}
		catch (Exception e)
		{
			errorLog.AppendLine(e.Message);
		}

		if (process != null)
		{
			process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
			{
				errorLog.AppendLine(e.Data);
			};
			process.BeginErrorReadLine();
		}
	}

	/// <summary>
	/// Closes the process.
	/// </summary>
	/// <remarks>
	/// Note that this is not safe to call on an already closed process.
	/// Use <see cref="HasStarted"/> and <see cref="HasExited"/> to verify if this call is safe.
	/// </remarks>
	/// <example>
	/// To ensure that this call is safe, call it so:
	/// <code>
	/// if (process.HasStarted && !process.HasExited)
	/// {
	///		process.Close();
	///	}
	/// </code>
	/// </example>
	public void Close()
	{
		errorLog.Clear();

		process.CancelErrorRead();

		process.CloseMainWindow();
		process.Close();
	}
}
