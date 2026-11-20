using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace BTLT07
{
    // Lớp chứa trạng thái và dữ liệu bind trực tiếp lên giao diện qua DataContext
    public class PlayerState : INotifyPropertyChanged
    {
        private MediaItem? _selectedItem;
        private MediaItem? _currentItem;
        private string _currentTrackText = "Chưa phát bài nào";
        private string _statusText = "Sẵn sàng. Hãy thêm file để phát.";
        private string _timeText = "00:00 / 00:00";
        private bool _isRepeatOne;
        private double _selectedSpeed = 1.0;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Danh sách phát bind tới ListBox
        public ObservableCollection<MediaItem> Playlist { get; } = new ObservableCollection<MediaItem>();

        // Mức tốc độ phát hiển thị trong ComboBox
        public double[] SpeedOptions { get; } = new double[] { 0.5, 1.0, 1.5, 2.0 };

        // Mục đang được chọn (highlight) trong ListBox
        public MediaItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged();
                }
            }
        }

        // Bài đang được nạp hoặc đang phát hiện tại
        public MediaItem? CurrentItem
        {
            get => _currentItem;
            set
            {
                if (_currentItem != value)
                {
                    _currentItem = value;
                    OnPropertyChanged();
                }
            }
        }

        // Tên file đang phát hiển thị dưới khung video
        public string CurrentTrackText
        {
            get => _currentTrackText;
            set
            {
                if (_currentTrackText != value)
                {
                    _currentTrackText = value;
                    OnPropertyChanged();
                }
            }
        }

        // Trạng thái hiện tại hoặc thông báo lỗi
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        // Đồng hồ hiển thị thời gian phát
        public string TimeText
        {
            get => _timeText;
            set
            {
                if (_timeText != value)
                {
                    _timeText = value;
                    OnPropertyChanged();
                }
            }
        }

        // Trạng thái CheckBox lặp 1 bài
        public bool IsRepeatOne
        {
            get => _isRepeatOne;
            set
            {
                if (_isRepeatOne != value)
                {
                    _isRepeatOne = value;
                    OnPropertyChanged();
                }
            }
        }

        // Tốc độ phát đang được chọn
        public double SelectedSpeed
        {
            get => _selectedSpeed;
            set
            {
                if (!_selectedSpeed.Equals(value))
                {
                    _selectedSpeed = value;
                    OnPropertyChanged();
                }
            }
        }

        // Thêm danh sách đường dẫn file vào playlist và trả về số lượng file đã thêm
        public int AddFiles(IEnumerable<string> filePaths)
        {
            int addedCount = 0;

            foreach (string path in filePaths)
            {
                Playlist.Add(new MediaItem
                {
                    FileName = Path.GetFileName(path),
                    FilePath = path
                });
                addedCount++;
            }

            if (SelectedItem == null && Playlist.Count > 0)
            {
                SelectedItem = Playlist[0];
            }

            return addedCount;
        }

        // Xóa bài đang được chọn trong ListBox và chọn lại bài lân cận nếu còn
        public bool RemoveSelected()
        {
            if (SelectedItem == null)
            {
                return false;
            }

            MediaItem itemToRemove = SelectedItem;
            int index = Playlist.IndexOf(itemToRemove);
            if (index < 0)
            {
                index = 0;
            }

            Playlist.Remove(itemToRemove);

            if (CurrentItem == itemToRemove)
            {
                CurrentItem = null;
            }

            if (Playlist.Count > 0)
            {
                int nextIndex = Math.Min(index, Playlist.Count - 1);
                SelectedItem = Playlist[nextIndex];
            }
            else
            {
                SelectedItem = null;
            }

            return true;
        }

        // Xóa toàn bộ danh sách phát và đặt lại thông tin hiển thị
        public void ClearAll()
        {
            Playlist.Clear();
            SelectedItem = null;
            CurrentItem = null;
            CurrentTrackText = "Chưa phát bài nào";
            TimeText = "00:00 / 00:00";
            StatusText = "Đã xóa toàn bộ danh sách phát.";
        }

        // Lấy bài kế tiếp trong danh sách (trả về null nếu đã ở cuối hoặc danh sách rỗng)
        public MediaItem? GetNextItem()
        {
            if (Playlist.Count == 0)
            {
                return null;
            }

            int index = GetReferenceIndex();
            if (index < 0)
            {
                return Playlist[0];
            }

            return index < Playlist.Count - 1 ? Playlist[index + 1] : null;
        }

        // Lấy bài trước đó trong danh sách (trả về null nếu đang ở đầu hoặc danh sách rỗng)
        public MediaItem? GetPreviousItem()
        {
            if (Playlist.Count == 0)
            {
                return null;
            }

            int index = GetReferenceIndex();
            if (index < 0)
            {
                return Playlist[0];
            }

            return index > 0 ? Playlist[index - 1] : null;
        }

        // Xác định vị trí tham chiếu hiện tại trong playlist để chuyển bài
        private int GetReferenceIndex()
        {
            if (CurrentItem != null)
            {
                return Playlist.IndexOf(CurrentItem);
            }

            if (SelectedItem != null)
            {
                return Playlist.IndexOf(SelectedItem);
            }

            return -1;
        }

        // Thông báo cho giao diện WPF biết thuộc tính đã thay đổi để tự động cập nhật
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
