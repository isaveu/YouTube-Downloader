﻿namespace YouTube.Downloader.Helpers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using YouTube.Downloader.EventArgs;

    internal class ProgressMonitor
    {
        private static readonly Regex ProgressReportRegex = new Regex(@"^\[download] (?<ProgressPercentage>[ 1][ 0-9][0-9]\.[0-9])% of .*?(?<TotalDownloadSize>[\d\.]+)?(?<TotalDownloadSizeUnits>.iB) at  (?<DownloadSpeed>.+)(?<DownloadSpeedUnits>.iB)\/s");

#if DEBUG
        private readonly DebugLogger _debugLogger = new DebugLogger();
#endif

        private bool _isPaused;

        private Process _process;

        internal ProgressMonitor(Process process)
        {
            _process = process;
        }

        internal event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;

        internal event EventHandler FinishedMonitoring;

        internal void Run()
        {
            RunMonitoringThread();
        }

        internal void Pause()
        {
            _isPaused = true;
            _process = null;
        }

        internal void Resume(Process process)
        {
            _process = process;
            _isPaused = false;
            RunMonitoringThread();
        }

        private void RunMonitoringThread()
        {
            Task.Run(() =>
            {
                using (StreamReader progressReader = _process.StandardOutput)
                {
                    int stage = 0;
                    double lastProgress = double.MaxValue;

#if DEBUG
                    try
                    {
#endif

                        while (!progressReader.EndOfStream && _process != null)
                        {
                            string line = progressReader.ReadLine();

                            if (line == null)
                            {
                                continue;
                            }

                            Match match = ProgressReportRegex.Match(line);

                            if (!match.Success)
                            {
#if DEBUG
                                _debugLogger.Log(line);
#endif
                                continue;
                            }

#if DEBUG
                            _debugLogger.Log(line);
#endif

                            long GetBytes(double size, string units)
                            {
                                int GetMultiplier()
                                {
                                    switch (units)
                                    {
                                        case "KiB":
                                            return 1024;

                                        case "MiB":
                                            return 1024 * 1024;

                                        case "GiB":
                                            return 1024 * 1024 * 1024;

                                        default:
                                            throw new InvalidOperationException("Invalid units for multiplier.");
                                    }
                                }

                                return (long)(size * GetMultiplier());
                            }

                            long totalDownloadSize = GetBytes(double.Parse(match.Groups["TotalDownloadSize"].Value),
                                                              match.Groups["TotalDownloadSizeUnits"].Value);

                            long downloadSpeed = GetBytes(double.Parse(match.Groups["DownloadSpeed"].Value),
                                                          match.Groups["DownloadSpeedUnits"].Value);

                            double progressPercentage = double.Parse(match.Groups["ProgressPercentage"].Value);

                            if (progressPercentage < lastProgress)
                            {
                                ++stage;
                            }

                            ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(totalDownloadSize, progressPercentage, downloadSpeed, stage));

                            lastProgress = progressPercentage;
                        }
#if DEBUG
                    }
                    finally
                    {
                        _debugLogger.Exit();
                    }
#endif
                }
            }).ContinueWith(task =>
            {
                if (!_isPaused)
                {
                    FinishedMonitoring?.Invoke(this, EventArgs.Empty);
                }
            });
        }
    }
}