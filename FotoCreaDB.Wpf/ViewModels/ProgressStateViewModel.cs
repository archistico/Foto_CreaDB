namespace FotoCreaDB.Wpf.ViewModels
{
    public class ProgressStateViewModel : ViewModelBase
    {
        private string _title = string.Empty;
        private int _processedItems;
        private int _totalItems;
        private int _percentage;
        private string _currentItem = string.Empty;
        private string _currentStep = string.Empty;
        private bool _isRunning;
        private bool _isDiscoveringFiles;
        private int _discoveredItems;

        public bool IsDiscoveringFiles
        {
            get => _isDiscoveringFiles;
            set
            {
                if (SetProperty(ref _isDiscoveringFiles, value))
                {
                    OnPropertyChanged(nameof(SummaryText));
                    OnPropertyChanged(nameof(IsIndeterminate));
                }
            }
        }

        public int DiscoveredItems
        {
            get => _discoveredItems;
            set
            {
                if (SetProperty(ref _discoveredItems, value))
                    OnPropertyChanged(nameof(SummaryText));
            }
        }

        public bool IsIndeterminate => IsRunning && (IsDiscoveringFiles || TotalItems <= 0);

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public int ProcessedItems
        {
            get => _processedItems;
            set
            {
                if (SetProperty(ref _processedItems, value))
                {
                    OnPropertyChanged(nameof(SummaryText));
                }
            }
        }

        public int TotalItems
        {
            get => _totalItems;
            set
            {
                if (SetProperty(ref _totalItems, value))
                {
                    OnPropertyChanged(nameof(SummaryText));
                    OnPropertyChanged(nameof(IsIndeterminate));
                }
            }
        }

        public int Percentage
        {
            get => _percentage;
            set
            {
                if (SetProperty(ref _percentage, value))
                {
                    OnPropertyChanged(nameof(SummaryText));
                }
            }
        }

        public string CurrentItem
        {
            get => _currentItem;
            set => SetProperty(ref _currentItem, value);
        }

        public string CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    OnPropertyChanged(nameof(SummaryText));
                }
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                    OnPropertyChanged(nameof(IsIndeterminate));
            }
        }

        public string SummaryText
        {
            get
            {
                if (IsDiscoveringFiles)
                    return CurrentStep + " " + DiscoveredItems.ToString("N0") + " file individuati";
                if (TotalItems > 0)
                {
                    return ProcessedItems + " / " + TotalItems + " (" + Percentage + "%)";
                }

                return CurrentStep;
            }
        }

        public void Reset(string title)
        {
            Title = title;
            ProcessedItems = 0;
            TotalItems = 0;
            Percentage = 0;
            CurrentItem = string.Empty;
            CurrentStep = string.Empty;
            DiscoveredItems = 0;
            IsDiscoveringFiles = false;
            IsRunning = false;
        }
    }
}
