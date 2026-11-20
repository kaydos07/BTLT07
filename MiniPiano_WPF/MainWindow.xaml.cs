using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MiniPiano.Audio;
using MiniPiano.Models;

namespace MiniPiano;

public partial class MainWindow : Window
{
    private static readonly IReadOnlyDictionary<Key, string> KeyboardMap =
        new Dictionary<Key, string>
        {
            [Key.A] = "C4", [Key.W] = "C#4", [Key.S] = "D4",
            [Key.E] = "D#4", [Key.D] = "E4", [Key.F] = "F4",
            [Key.T] = "F#4", [Key.G] = "G4", [Key.Y] = "G#4",
            [Key.H] = "A4", [Key.U] = "A#4", [Key.J] = "B4",
            [Key.K] = "C5"
        };

    private static readonly IReadOnlyDictionary<string, string> NoteToKey =
        KeyboardMap.ToDictionary(pair => pair.Value, pair => pair.Key.ToString());

    private readonly PianoSoundEngine _sound = new();
    private readonly Dictionary<string, Button> _pianoButtons = new();
    private readonly Brush _normalWhite = Brushes.White;
    private readonly Brush _normalBlack = new SolidColorBrush(Color.FromRgb(31, 41, 55));
    private readonly Brush _activeKey = new SolidColorBrush(Color.FromRgb(147, 197, 253));
    private int _currentIndex;
    private bool _practiceMode = true;
    private bool _isPlayingSample;
    private CancellationTokenSource? _sampleCts;
    private long _flashVersion;

    public MainWindow()
    {
        InitializeComponent();
        RegisterKeys();
        SongCombo.ItemsSource = SongCatalog.All;
        SongCombo.SelectedIndex = 0;
        FeedbackText.Text = "Hãy chơi nốt được tô xanh để bắt đầu!";
    }

    private PracticeSong? CurrentSong => SongCombo.SelectedItem as PracticeSong;

    private void RegisterKeys()
    {
        var buttons = new[]
        {
            PianoC4, PianoCSharp4, PianoD4, PianoDSharp4, PianoE4,
            PianoF4, PianoFSharp4, PianoG4, PianoGSharp4,
            PianoA4, PianoASharp4, PianoB4, PianoC5
        };
        foreach (var button in buttons)
            _pianoButtons.Add((string)button.Tag, button);
    }

    private void PianoKey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string pitch)
            PlayUserNote(pitch);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Tránh lặp vô hạn khi giữ phím. Để ComboBox vẫn điều khiển được bằng bàn phím.
        if (e.IsRepeat || SongCombo.IsKeyboardFocusWithin)
            return;
        if (KeyboardMap.TryGetValue(e.Key, out var pitch))
        {
            PlayUserNote(pitch);
            e.Handled = true;
        }
    }

    private void PlayUserNote(string pitch)
    {
        if (_isPlayingSample)
            CancelSample();
        PlaySoundAndFlash(pitch);

        if (!_practiceMode || CurrentSong is null)
        {
            FeedbackText.Text = $"Bạn vừa chơi nốt {pitch} - phím {NoteToKey[pitch]}.";
            return;
        }

        if (_currentIndex >= CurrentSong.Notes.Count)
        {
            FeedbackText.Text = "Bạn đã hoàn thành! Chọn 'Luyện lại từ đầu' để chơi lại.";
            return;
        }

        var expected = CurrentSong.Notes[_currentIndex].Pitch;
        if (pitch == expected)
        {
            _currentIndex++;
            FeedbackText.Text = _currentIndex == CurrentSong.Notes.Count
                ? "Chúc mừng! Bạn đã chơi đúng toàn bộ đoạn nhạc!"
                : $"Chính xác! Nốt tiếp theo: {CurrentSong.Notes[_currentIndex].Pitch} " +
                  $"(phím {NoteToKey[CurrentSong.Notes[_currentIndex].Pitch]}).";
        }
        else
        {
            FeedbackText.Text = $"Bạn vừa chơi {pitch}. Hãy thử nốt {expected} " +
                                $"(phím {NoteToKey[expected]}).";
        }
        RenderNotes();
    }

    private async void PlaySoundAndFlash(string pitch)
    {
        _sound.Play(pitch);
        var version = ++_flashVersion;
        foreach (var (note, button) in _pianoButtons)
            button.Background = note.Contains('#') ? _normalBlack : _normalWhite;
        _pianoButtons[pitch].Background = _activeKey;
        await Task.Delay(170);
        if (version == _flashVersion)
            _pianoButtons[pitch].Background = pitch.Contains('#') ? _normalBlack : _normalWhite;
    }

    private void SongCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CancelSample();
        _practiceMode = true;
        _currentIndex = 0;
        ModeButton.Content = "Chuyển sang chơi tự do";
        SongDescription.Text = CurrentSong?.Description ?? "";
        FeedbackText.Text = "Bắt đầu chơi theo các nốt được hiển thị bên dưới.";
        RenderNotes();
    }

    private void ToggleMode_Click(object sender, RoutedEventArgs e)
    {
        CancelSample();
        _practiceMode = !_practiceMode;
        ModeButton.Content = _practiceMode ? "Chuyển sang chơi tự do" : "Bật luyện tập theo bài";
        FeedbackText.Text = _practiceMode
            ? "Đã bật luyện tập. Nhấn đúng nốt được tô xanh để tiếp tục."
            : "Đã bật chơi tự do. Hãy thử các nốt nhạc tùy thích.";
        RenderNotes();
    }

    private void Restart_Click(object sender, RoutedEventArgs e)
    {
        CancelSample();
        _practiceMode = true;
        _currentIndex = 0;
        ModeButton.Content = "Chuyển sang chơi tự do";
        FeedbackText.Text = "Đã bắt đầu lại bài nhạc. Chơi theo nốt màu xanh nhé!";
        RenderNotes();
    }

    private async void ListenSample_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentSong is null)
            return;
        CancelSample();
        var song = CurrentSong;
        var cts = new CancellationTokenSource();
        _sampleCts = cts;
        _isPlayingSample = true;
        try
        {
            FeedbackText.Text = "Đang phát bài mẫu...";
            for (var i = 0; i < song.Notes.Count; i++)
            {
                cts.Token.ThrowIfCancellationRequested();
                var note = song.Notes[i];
                RenderNotes(i);
                PlaySoundAndFlash(note.Pitch);
                await Task.Delay((int)(note.Beats * 470), cts.Token);
            }
            FeedbackText.Text = "Đã nghe xong bài mẫu. Bạn có thể bắt đầu luyện tập.";
        }
        catch (OperationCanceledException)
        {
            // Người dùng chuyển bài, nhấn Dừng hoặc bắt đầu chơi phím.
        }
        finally
        {
            if (ReferenceEquals(_sampleCts, cts))
            {
                _sampleCts = null;
                _isPlayingSample = false;
                RenderNotes();
            }
            cts.Dispose();
        }
    }

    private void StopSample_Click(object sender, RoutedEventArgs e)
    {
        CancelSample();
        _sound.Stop();
        FeedbackText.Text = "Đã dừng nghe mẫu.";
        RenderNotes();
    }

    private void CancelSample()
    {
        _sampleCts?.Cancel();
        _sampleCts = null;
        _isPlayingSample = false;
    }

    private void RenderNotes(int? previewIndex = null)
    {
        var song = CurrentSong;
        NoteStrip.Children.Clear();
        if (song is null)
            return;

        var target = previewIndex ?? (_practiceMode ? _currentIndex : -1);
        for (var i = 0; i < song.Notes.Count; i++)
        {
            var note = song.Notes[i];
            var completed = _practiceMode && i < _currentIndex;
            var current = i == target;
            var background = current ? Color.FromRgb(219, 234, 254) :
                             completed ? Color.FromRgb(220, 252, 231) :
                             Color.FromRgb(241, 245, 249);
            var border = new Border
            {
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(13, 9, 13, 9),
                MinWidth = 64,
                Background = new SolidColorBrush(background),
                BorderBrush = current ? Brushes.RoyalBlue : Brushes.Transparent,
                BorderThickness = new Thickness(current ? 2 : 1),
                CornerRadius = new CornerRadius(8),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = note.Pitch + (note.Beats >= 2 ? " —" : ""),
                            FontWeight = FontWeights.Bold,
                            FontSize = 15,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = Brushes.DarkSlateGray
                        },
                        new TextBlock
                        {
                            Text = NoteToKey[note.Pitch],
                            FontSize = 13,
                            Foreground = Brushes.SlateGray,
                            HorizontalAlignment = HorizontalAlignment.Center
                        }
                    }
                }
            };
            NoteStrip.Children.Add(border);
        }

        ProgressText.Text = $"Đã chơi đúng: {_currentIndex}/{song.Notes.Count} nốt";
        if (_isPlayingSample && previewIndex is not null)
        {
            TargetText.Text = $"Nghe mẫu - nốt {previewIndex.Value + 1}: {song.Notes[previewIndex.Value].Pitch}";
        }
        else if (!_practiceMode)
        {
            TargetText.Text = "Chế độ chơi tự do";
        }
        else if (_currentIndex >= song.Notes.Count)
        {
            TargetText.Text = "✓ Hoàn thành đoạn nhạc!";
        }
        else
        {
            var expected = song.Notes[_currentIndex].Pitch;
            TargetText.Text = $"Nốt tiếp theo: {expected} - phím {NoteToKey[expected]}";
        }
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        CancelSample();
        _sound.Dispose();
    }
}
