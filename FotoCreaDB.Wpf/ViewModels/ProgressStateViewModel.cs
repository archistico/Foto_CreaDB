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
            set => SetProperty(ref _isRunning, value);
        }

        public string SummaryText
        {
            get
            {
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
            IsRunning = false;
        }
    }
}