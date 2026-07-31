using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ChaosChess.AI.Stockfish
{
    public sealed class StockfishProcess : IStockfishProcess
    {
        private readonly StockfishEngineOptions _options;
        private readonly BlockingCollection<string> _outputLines = new BlockingCollection<string>();
        private Process? _process;

        public StockfishProcess(StockfishEngineOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public bool HasExited => _process == null || _process.HasExited;

        public void Start()
        {
            if (_process != null)
            {
                throw new InvalidOperationException("Stockfish process has already been started.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _options.EnginePath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            try
            {
                if (!_process.Start())
                {
                    throw new StockfishEngineException(
                        StockfishEngineErrorCode.HandshakeFailed,
                        "Failed to start Stockfish process.");
                }
            }
            catch (Win32Exception exception)
            {
                throw new StockfishEngineException(
                    StockfishEngineErrorCode.HandshakeFailed,
                    "Failed to start Stockfish process: " + exception.Message);
            }

            Task.Run(() => DrainOutput(_process));
            Task.Run(() => DrainError(_process));
        }

        public void WriteLine(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentException("Command cannot be empty.", nameof(command));
            }

            if (_process == null || _process.HasExited)
            {
                throw new StockfishEngineException(
                    StockfishEngineErrorCode.ProcessExited,
                    "Cannot write to a stopped Stockfish process.");
            }

            _process.StandardInput.WriteLine(command);
            _process.StandardInput.Flush();
        }

        public string? ReadLine(TimeSpan timeout)
        {
            return _outputLines.TryTake(out string? line, timeout)
                ? line
                : null;
        }

        public void ClearOutput()
        {
            while (_outputLines.TryTake(out _))
            {
            }
        }

        public void Kill()
        {
            if (_process == null || _process.HasExited)
            {
                return;
            }

            _process.Kill(entireProcessTree: true);
        }

        public void Dispose()
        {
            if (_process == null)
            {
                return;
            }

            try
            {
                if (!_process.HasExited)
                {
                    _process.StandardInput.WriteLine("quit");
                    _process.StandardInput.Flush();
                }
            }
            catch (InvalidOperationException)
            {
            }

            _process.Dispose();
            _outputLines.Dispose();
        }

        private void DrainOutput(Process process)
        {
            try
            {
                while (!process.HasExited)
                {
                    string? line = process.StandardOutput.ReadLine();

                    if (line == null)
                    {
                        break;
                    }

                    _outputLines.Add(line);
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static void DrainError(Process process)
        {
            try
            {
                while (!process.HasExited && process.StandardError.ReadLine() != null)
                {
                }
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
