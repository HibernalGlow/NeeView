using NeeLaboratory.ComponentModel;
using NeeLaboratory.Windows.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace NeeView
{
    /// <summary>
    /// 浮动控制面板控制模式
    /// </summary>
    public enum FloatingControlMode
    {
        Auto,   // 自动识别
        Image,  // 图片模式
        Video   // 视频模式
    }

    /// <summary>
    /// 浮动控制面板 ViewModel
    /// </summary>
    public class FloatingControlPanelViewModel : BindableBase, IDisposable
    {
        private static FloatingControlPanelViewModel? _current;
        public static FloatingControlPanelViewModel? Current => _current;

        private readonly DisposableCollection _disposables = new();
        private readonly DispatcherTimer _timer;
        private FloatingControlMode _mode = FloatingControlMode.Auto;
        private bool _isCollapsed = true; // 默认隐藏
        private bool _isSettingsVisible = false;
        private double _mediaPosition;
        private string _currentTime = "00:00";
        private string _totalTime = "00:00";
        private double _playbackSpeed = 1.0;
        private double _panelOpacity = 0.85;
        private bool _isDraggable = true;
        private double _offsetX = 0;
        private double _offsetY = 0;
        private bool _disposedValue;

        public FloatingControlPanelViewModel()
        {
            _current = this;
            // 订阅书籍变化事件
            _disposables.Add(BookOperation.Current.SubscribeBookChanged((s, e) => UpdateMode()));

            // 订阅页面变化事件  
            _disposables.Add(BookOperation.Current.SubscribePropertyChanged(
                nameof(BookOperation.Current.Address),
                (s, e) => UpdateMode()));

            // 定时器更新媒体状态
            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            UpdateMode();
        }

        #region Properties

        public FloatingControlMode Mode
        {
            get => _mode;
            set
            {
                if (SetProperty(ref _mode, value))
                {
                    RaisePropertyChanged(nameof(IsImageMode));
                    RaisePropertyChanged(nameof(IsVideoMode));
                    RaisePropertyChanged(nameof(IsAutoMode));
                    UpdateVisibility();
                }
            }
        }

        public bool IsCollapsed
        {
            get => _isCollapsed;
            set
            {
                if (SetProperty(ref _isCollapsed, value))
                {
                    RaisePropertyChanged(nameof(IsExpanded));
                }
            }
        }

        public bool IsExpanded => !IsCollapsed;

        public bool IsSettingsVisible
        {
            get => _isSettingsVisible;
            set => SetProperty(ref _isSettingsVisible, value);
        }

        public double PanelOpacity
        {
            get => _panelOpacity;
            set => SetProperty(ref _panelOpacity, value);
        }

        public bool IsDraggable
        {
            get => _isDraggable;
            set
            {
                if (SetProperty(ref _isDraggable, value))
                {
                    RaisePropertyChanged(nameof(DragCursor));
                }
            }
        }

        public Cursor DragCursor => IsDraggable ? Cursors.SizeAll : Cursors.Arrow;

        public double OffsetX
        {
            get => _offsetX;
            set => SetProperty(ref _offsetX, value);
        }

        public double OffsetY
        {
            get => _offsetY;
            set => SetProperty(ref _offsetY, value);
        }

        public int DefaultModeIndex
        {
            get => (int)_mode;
            set
            {
                if (value >= 0 && value <= 2)
                {
                    Mode = (FloatingControlMode)value;
                }
            }
        }

        public bool IsImageMode
        {
            get => _mode == FloatingControlMode.Image;
            set { if (value) Mode = FloatingControlMode.Image; }
        }

        public bool IsVideoMode
        {
            get => _mode == FloatingControlMode.Video;
            set { if (value) Mode = FloatingControlMode.Video; }
        }

        public bool IsAutoMode
        {
            get => _mode == FloatingControlMode.Auto;
            set { if (value) Mode = FloatingControlMode.Auto; }
        }

        public bool ShowImageControls
        {
            get
            {
                if (_mode == FloatingControlMode.Image) return true;
                if (_mode == FloatingControlMode.Auto)
                {
                    var mediaOperator = MediaPlayerOperator.CurrentMediaOperator;
                    return mediaOperator == null || (!mediaOperator.HasVideo && !mediaOperator.HasAudio);
                }
                return false;
            }
        }

        public bool ShowVideoControls
        {
            get
            {
                if (_mode == FloatingControlMode.Video) return true;
                if (_mode == FloatingControlMode.Auto)
                {
                    var mediaOperator = MediaPlayerOperator.CurrentMediaOperator;
                    return mediaOperator != null && (mediaOperator.HasVideo || mediaOperator.HasAudio);
                }
                return false;
            }
        }

        public double MediaPosition
        {
            get => _mediaPosition;
            set
            {
                if (SetProperty(ref _mediaPosition, value))
                {
                    var mediaOperator = MediaPlayerOperator.CurrentMediaOperator;
                    if (mediaOperator != null)
                    {
                        mediaOperator.Position = value;
                    }
                }
            }
        }

        public bool CanSeek => MediaPlayerOperator.CurrentMediaOperator?.ScrubbingEnabled ?? false;

        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public string TotalTime
        {
            get => _totalTime;
            set => SetProperty(ref _totalTime, value);
        }

        public string PlayPauseIcon
        {
            get
            {
                var mediaOperator = MediaPlayerOperator.CurrentMediaOperator;
                return mediaOperator?.IsPlaying == true ? "⏸" : "▶";
            }
        }

        public double PlaybackSpeed
        {
            get => _playbackSpeed;
            set
            {
                if (SetProperty(ref _playbackSpeed, value))
                {
                    var mediaOperator = MediaPlayerOperator.CurrentMediaOperator;
                    if (mediaOperator != null)
                    {
                        mediaOperator.Rate = value;
                    }
                }
            }
        }

        public List<double> PlaybackSpeeds { get; } = new List<double> 
        { 
            0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0, 2.5, 3.0 
        };

        #endregion

        #region Commands

        private RelayCommand? _collapseCommand;
        public RelayCommand CollapseCommand =>
            _collapseCommand ??= new RelayCommand(() => IsCollapsed = true);

        private RelayCommand? _expandCommand;
        public RelayCommand ExpandCommand =>
            _expandCommand ??= new RelayCommand(() => IsCollapsed = false);

        private RelayCommand? _showSettingsCommand;
        public RelayCommand ShowSettingsCommand =>
            _showSettingsCommand ??= new RelayCommand(() => IsSettingsVisible = true);

        private RelayCommand? _closeSettingsCommand;
        public RelayCommand CloseSettingsCommand =>
            _closeSettingsCommand ??= new RelayCommand(() => IsSettingsVisible = false);

        // 图片控制命令
        private RelayCommand? _prevPageCommand;
        public RelayCommand PrevPageCommand =>
            _prevPageCommand ??= new RelayCommand(
                () => BookOperation.Current.Control.MovePrev(this));

        private RelayCommand? _nextPageCommand;
        public RelayCommand NextPageCommand =>
            _nextPageCommand ??= new RelayCommand(
                () => BookOperation.Current.Control.MoveNext(this));

        private RelayCommand? _prevBookCommand;
        public RelayCommand PrevBookCommand =>
            _prevBookCommand ??= new RelayCommand(
                async () => await BookshelfFolderList.Current.PrevFolder(false));

        private RelayCommand? _nextBookCommand;
        public RelayCommand NextBookCommand =>
            _nextBookCommand ??= new RelayCommand(
                async () => await BookshelfFolderList.Current.NextFolder(false));

        // 媒体控制命令
        private RelayCommand? _mediaTogglePlayCommand;
        public RelayCommand MediaTogglePlayCommand =>
            _mediaTogglePlayCommand ??= new RelayCommand(
                () => MediaPlayerOperator.CurrentMediaOperator?.TogglePlay(),
                () => MediaPlayerOperator.CurrentMediaOperator != null);

        private RelayCommand? _mediaFirstCommand;
        public RelayCommand MediaFirstCommand =>
            _mediaFirstCommand ??= new RelayCommand(
                () => MediaPlayerOperator.CurrentMediaOperator?.SetPositionFirst(),
                () => MediaPlayerOperator.CurrentMediaOperator != null);

        private RelayCommand? _mediaLastCommand;
        public RelayCommand MediaLastCommand =>
            _mediaLastCommand ??= new RelayCommand(
                () => MediaPlayerOperator.CurrentMediaOperator?.SetPositionLast(),
                () => MediaPlayerOperator.CurrentMediaOperator != null);

        private RelayCommand? _mediaRewindCommand;
        public RelayCommand MediaRewindCommand =>
            _mediaRewindCommand ??= new RelayCommand(
                () => MediaPlayerOperator.CurrentMediaOperator?.AddPosition(TimeSpan.FromSeconds(-5)),
                () => MediaPlayerOperator.CurrentMediaOperator != null);

        private RelayCommand? _mediaForwardCommand;
        public RelayCommand MediaForwardCommand =>
            _mediaForwardCommand ??= new RelayCommand(
                () => MediaPlayerOperator.CurrentMediaOperator?.AddPosition(TimeSpan.FromSeconds(5)),
                () => MediaPlayerOperator.CurrentMediaOperator != null);

        #endregion

        #region Methods

        private void UpdateMode()
        {
            UpdateVisibility();
            UpdateMediaInfo();
        }

        private void UpdateVisibility()
        {
            RaisePropertyChanged(nameof(ShowImageControls));
            RaisePropertyChanged(nameof(ShowVideoControls));
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_disposedValue) return;

            UpdateMediaInfo();
            UpdateCommandStates();
        }

        private void UpdateMediaInfo()
        {
            var mediaOperator = MediaPlayerOperator.CurrentMediaOperator;
            if (mediaOperator != null)
            {
                // 更新进度
                _mediaPosition = mediaOperator.Position;
                RaisePropertyChanged(nameof(MediaPosition));

                // 更新时间
                if (mediaOperator.Duration.HasTimeSpan)
                {
                    var current = mediaOperator.Duration.TimeSpan.Multiply(mediaOperator.Position);
                    var total = mediaOperator.Duration.TimeSpan;
                    CurrentTime = FormatTime(current);
                    TotalTime = FormatTime(total);
                }

                // 更新播放/暂停图标
                RaisePropertyChanged(nameof(PlayPauseIcon));

                // 更新播放速度
                if (Math.Abs(_playbackSpeed - mediaOperator.Rate) > 0.01)
                {
                    _playbackSpeed = mediaOperator.Rate;
                    RaisePropertyChanged(nameof(PlaybackSpeed));
                }

                // 更新可拖动状态
                RaisePropertyChanged(nameof(CanSeek));
            }
        }

        private void UpdateCommandStates()
        {
            PrevPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
            PrevBookCommand.RaiseCanExecuteChanged();
            NextBookCommand.RaiseCanExecuteChanged();
            MediaTogglePlayCommand.RaiseCanExecuteChanged();
            MediaFirstCommand.RaiseCanExecuteChanged();
            MediaLastCommand.RaiseCanExecuteChanged();
            MediaRewindCommand.RaiseCanExecuteChanged();
            MediaForwardCommand.RaiseCanExecuteChanged();
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
            {
                return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
            }
            return $"{time.Minutes:D2}:{time.Seconds:D2}";
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _timer?.Stop();
                    _disposables?.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
