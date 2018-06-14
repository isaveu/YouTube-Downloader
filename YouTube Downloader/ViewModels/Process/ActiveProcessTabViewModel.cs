﻿namespace YouTube.Downloader.ViewModels.Process
{
    using System;
    using System.ComponentModel;

    using Caliburn.Micro;

    using YouTube.Downloader.Core;
    using YouTube.Downloader.Models.Download;
    using YouTube.Downloader.Services.Interfaces;
    using YouTube.Downloader.ViewModels.Interfaces.Process;

    internal abstract class ActiveProcessTabViewModel : ProcessTabViewModel<IActiveProcessViewModel>, IActiveProcessTabViewModel
    {
        private protected ActiveProcessTabViewModel(IEventAggregator eventAggregator, IProcessDispatcherService processDispatcherService, Predicate<ProcessTransferType> processTransferFilter)
                : base(eventAggregator, processDispatcherService, processTransferFilter)
        {
        }

        private protected override void OnProcessesAdded(IActiveProcessViewModel[] processViewModels)
        {
            foreach (IActiveProcessViewModel activeProcessViewModel in processViewModels)
            {
                void ProcessExited(object sender, EventArgs e)
                {
                    activeProcessViewModel.Process.Exited -= ProcessExited;
                    activeProcessViewModel.PropertyChanged -= DownloadStatusPropertyChanged;

                    SelectedProcesses.Remove(activeProcessViewModel);
                    Processes.Remove(activeProcessViewModel);

                    OnProcessExited(activeProcessViewModel);
                }

                void DownloadStatusPropertyChanged(object sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == nameof(DownloadStatus.DownloadState))
                    {
                        Processes.Refresh();
                    }
                }

                activeProcessViewModel.Process.Exited += ProcessExited;
                activeProcessViewModel.PropertyChanged += DownloadStatusPropertyChanged;
            }
        }
    }
}