using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;

namespace BTLT07
{
    public partial class MainWindow : Window
    {
        // Đối tượng trạng thái làm DataContext cho toàn bộ cửa sổ
        private readonly PlayerState _state = new PlayerState();

        // Bộ đếm thời gian cập nhật thanh tua mỗi 250ms
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        // Cờ đánh dấu người dùng đang kéo thanh tua
        private bool _isDragging = false;

        // Cờ đánh dấu code đang cập nhật Slider để tránh kích hoạt tua ngược
        private bool _isUpdatingSlider = false;

        public MainWindow()
        {
            InitializeComponent();

            // Gán DataContext và cấu hình Timer
            DataContext = _state;
            _timer.Interval = TimeSpan.FromMilliseconds(250);

            // Đăng ký toàn bộ sự kiện bằng C# thay vì viết trong XAML
            RegisterEvents();
        }

        // Gom toàn bộ việc gán hàm xử lý sự kiện vào một chỗ dễ theo dõi
        private void RegisterEvents()
        {
            Closed += Window_Closed;
            _timer.Tick += Timer_Tick;

            // Sự kiện của MediaElement
            Player.MediaOpened += Player_MediaOpened;
            Player.MediaEnded += Player_MediaEnded;
            Player.MediaFailed += Player_MediaFailed;

            // Sự kiện kéo và click trên thanh tua (Slider)
            SeekSlider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(SeekSlider_DragStarted));
            SeekSlider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(SeekSlider_DragCompleted));
            SeekSlider.ValueChanged += SeekSlider_ValueChanged;

            // Sự kiện các nút điều khiển phát và chọn tốc độ
            PrevButton.Click += PrevButton_Click;
            PlayButton.Click += PlayButton_Click;
            PauseButton.Click += PauseButton_Click;
            StopButton.Click += StopButton_Click;
            NextButton.Click += NextButton_Click;
            SpeedComboBox.SelectionChanged += SpeedComboBox_SelectionChanged;

            // Sự kiện quản lý playlist
            PlaylistBox.MouseDoubleClick += PlaylistBox_MouseDoubleClick;
            AddFileButton.Click += AddFileButton_Click;
            RemoveSelectedButton.Click += RemoveSelectedButton_Click;
            ClearAllButton.Click += ClearAllButton_Click;
        }

        // Nạp và phát một mục trong danh sách phát
        private void PlayItem(MediaItem item)
        {
            _state.CurrentItem = item;
            _state.SelectedItem = item;
            _state.CurrentTrackText = $"Đang phát: {item.FileName}";

            try
            {
                Uri mediaUri = new Uri(item.FilePath, UriKind.Absolute);

                // Nếu phát lại đúng file đang nạp sẵn, đưa về đầu và phát lại
                if (Player.Source != null && Player.Source.Equals(mediaUri))
                {
                    Player.Stop();
                    Player.Position = TimeSpan.Zero;
                    Player.SpeedRatio = _state.SelectedSpeed;
                    Player.Play();

                    _isUpdatingSlider = true;
                    SeekSlider.Value = 0;
                    _isUpdatingSlider = false;

                    string totalText = Player.NaturalDuration.HasTimeSpan
                        ? FormatTime(Player.NaturalDuration.TimeSpan)
                        : "00:00";
                    _state.TimeText = $"00:00 / {totalText}";
                }
                else
                {
                    // Nạp file mới để phát
                    Player.Stop();
                    ResetSeekSlider();
                    Player.Source = mediaUri;
                    Player.Play();
                }

                _state.StatusText = "Đang phát";
                _timer.Start();
            }
            catch (Exception ex)
            {
                _timer.Stop();
                _state.StatusText = $"Không thể mở file: {ex.Message}";
            }
        }

        // Đưa thanh tua và thời gian hiển thị về trạng thái ban đầu
        private void ResetSeekSlider()
        {
            _isUpdatingSlider = true;
            SeekSlider.Minimum = 0;
            SeekSlider.Maximum = 1;
            SeekSlider.Value = 0;
            _isUpdatingSlider = false;

            _state.TimeText = "00:00 / 00:00";
        }

        // Dừng hẳn media hiện tại và cập nhật trạng thái
        private void StopAndClearMedia(string statusMessage)
        {
            Player.Stop();
            Player.Source = null;
            _timer.Stop();
            ResetSeekSlider();
            _state.CurrentTrackText = "Chưa phát bài nào";
            _state.StatusText = statusMessage;
        }

        // Định dạng TimeSpan thành mm:ss hoặc hh:mm:ss
        private static string FormatTime(TimeSpan time)
        {
            return time.TotalHours >= 1
                ? time.ToString(@"hh\:mm\:ss")
                : time.ToString(@"mm\:ss");
        }

        // Khi nạp xong file: đọc tổng thời lượng để cài đặt thanh tua
        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (Player.NaturalDuration.HasTimeSpan)
            {
                TimeSpan total = Player.NaturalDuration.TimeSpan;

                _isUpdatingSlider = true;
                SeekSlider.Minimum = 0;
                SeekSlider.Maximum = total.TotalSeconds;
                SeekSlider.Value = 0;
                _isUpdatingSlider = false;

                _state.TimeText = $"00:00 / {FormatTime(total)}";
            }

            Player.SpeedRatio = _state.SelectedSpeed;
            _timer.Start();
        }

        // Khi phát hết bài: lặp lại nếu bật Lặp 1 bài, ngược lại chuyển bài kế tiếp
        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (_state.IsRepeatOne)
            {
                Player.Position = TimeSpan.Zero;
                Player.Play();
                return;
            }

            MediaItem? nextItem = _state.GetNextItem();
            if (nextItem != null)
            {
                PlayItem(nextItem);
                return;
            }

            Player.Stop();
            _timer.Stop();

            _isUpdatingSlider = true;
            SeekSlider.Value = 0;
            _isUpdatingSlider = false;

            string totalText = Player.NaturalDuration.HasTimeSpan
                ? FormatTime(Player.NaturalDuration.TimeSpan)
                : "00:00";
            _state.TimeText = $"00:00 / {totalText}";
            _state.StatusText = "Đã phát hết danh sách.";
        }

        // Khi phát bị lỗi: hiển thị thông báo lỗi ra giao diện
        private void Player_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            _timer.Stop();
            string message = e.ErrorException?.Message ?? "Không xác định được lỗi.";
            _state.StatusText = $"Lỗi phát media: {message}";
        }

        // Cập nhật vị trí thanh tua và đồng hồ mỗi 250ms
        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_isDragging || Player.Source == null || !Player.NaturalDuration.HasTimeSpan)
            {
                return;
            }

            TimeSpan current = Player.Position;
            TimeSpan total = Player.NaturalDuration.TimeSpan;

            _isUpdatingSlider = true;
            SeekSlider.Value = current.TotalSeconds;
            _isUpdatingSlider = false;

            _state.TimeText = $"{FormatTime(current)} / {FormatTime(total)}";
        }

        // Bắt đầu kéo thanh tua: khóa cập nhật từ Timer
        private void SeekSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
            _isDragging = true;
        }

        // Thả chuột sau khi kéo: tua media đến đúng vị trí mới
        private void SeekSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _isDragging = false;

            if (Player.Source != null && Player.NaturalDuration.HasTimeSpan)
            {
                TimeSpan newPosition = TimeSpan.FromSeconds(SeekSlider.Value);
                Player.Position = newPosition;
                _state.TimeText = $"{FormatTime(newPosition)} / {FormatTime(Player.NaturalDuration.TimeSpan)}";
            }
        }

        // Hỗ trợ click trực tiếp lên thanh tua mà không bị giật
        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingSlider || Player.Source == null || !Player.NaturalDuration.HasTimeSpan)
            {
                return;
            }

            TimeSpan target = TimeSpan.FromSeconds(SeekSlider.Value);
            _state.TimeText = $"{FormatTime(target)} / {FormatTime(Player.NaturalDuration.TimeSpan)}";

            if (!_isDragging)
            {
                Player.Position = target;
            }
        }

        // Nút Play: phát bài đang chọn, phát tiếp bài tạm dừng hoặc phát bài đầu tiên
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_state.Playlist.Count == 0)
            {
                _state.StatusText = "Danh sách phát đang trống. Hãy thêm file để phát.";
                return;
            }

            if (_state.SelectedItem != null && _state.SelectedItem != _state.CurrentItem)
            {
                PlayItem(_state.SelectedItem);
                return;
            }

            if (Player.Source != null && _state.CurrentItem != null)
            {
                Player.SpeedRatio = _state.SelectedSpeed;
                Player.Play();
                _timer.Start();
                _state.StatusText = "Đang phát";
            }
            else
            {
                MediaItem targetItem = _state.SelectedItem ?? _state.Playlist[0];
                PlayItem(targetItem);
            }
        }

        // Nút Pause: tạm dừng phát
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null)
            {
                return;
            }

            Player.Pause();
            _timer.Stop();
            _state.StatusText = "Đã tạm dừng.";
        }

        // Nút Stop: dừng phát và đưa thanh tua về đầu
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null)
            {
                return;
            }

            Player.Stop();
            _timer.Stop();

            _isUpdatingSlider = true;
            SeekSlider.Value = 0;
            _isUpdatingSlider = false;

            string totalText = Player.NaturalDuration.HasTimeSpan
                ? FormatTime(Player.NaturalDuration.TimeSpan)
                : "00:00";
            _state.TimeText = $"00:00 / {totalText}";
            _state.StatusText = "Đã dừng phát.";
        }

        // Nút Prev: chuyển về bài trước nếu có
        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            MediaItem? previousItem = _state.GetPreviousItem();
            if (previousItem != null)
            {
                PlayItem(previousItem);
            }
        }

        // Nút Next: chuyển sang bài tiếp theo nếu có
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            MediaItem? nextItem = _state.GetNextItem();
            if (nextItem != null)
            {
                PlayItem(nextItem);
            }
        }

        // Thay đổi tốc độ phát khi người dùng chọn ComboBox
        private void SpeedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SpeedComboBox.SelectedItem is double speed)
            {
                _state.SelectedSpeed = speed;
                Player.SpeedRatio = speed;
            }
        }

        // Double-click vào một mục trong danh sách để phát ngay
        private void PlaylistBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_state.SelectedItem != null)
            {
                PlayItem(_state.SelectedItem);
            }
        }

        // Nút Thêm file...: mở hộp thoại chọn nhiều file nhạc/video
        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Chọn file âm thanh hoặc video",
                Multiselect = true,
                Filter = "Media Files (*.mp4;*.mp3;*.wav;*.wmv;*.avi)|*.mp4;*.mp3;*.wav;*.wmv;*.avi|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                int addedCount = _state.AddFiles(dialog.FileNames);
                if (Player.Source == null)
                {
                    _state.StatusText = $"Đã thêm {addedCount} file vào danh sách.";
                }
            }
        }

        // Nút Xóa bài chọn: xóa mục đang chọn và dừng phát nếu đang phát chính bài đó
        private void RemoveSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (_state.SelectedItem == null)
            {
                return;
            }

            bool isRemovingCurrent = _state.SelectedItem == _state.CurrentItem;
            _state.RemoveSelected();

            if (isRemovingCurrent)
            {
                StopAndClearMedia("Đã xóa bài đang phát.");
            }
            else
            {
                _state.StatusText = "Đã xóa bài khỏi danh sách phát.";
            }
        }

        // Nút Xóa hết: dừng phát và dọn sạch playlist
        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            Player.Stop();
            Player.Source = null;
            _timer.Stop();
            ResetSeekSlider();
            _state.ClearAll();
        }

        // Dừng Timer khi đóng cửa sổ
        private void Window_Closed(object? sender, EventArgs e)
        {
            _timer.Stop();
        }
    }
}
