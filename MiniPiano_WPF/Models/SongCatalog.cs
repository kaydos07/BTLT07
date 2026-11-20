namespace MiniPiano.Models;

public static class SongCatalog
{
    // Các đoạn nhạc ngắn, giọng Đô trưởng (C major), dùng để luyện tập.
    // Beats = 2 nghĩa là giữ nhịp/giai điệu lâu hơn (không phải hợp âm).
    public static IReadOnlyList<PracticeSong> All { get; } = new List<PracticeSong>
    {
        new(
            "1. Twinkle Twinkle Little Star",
            "Hai câu mở đầu - dễ, chậm, phù hợp với người mới bắt đầu.",
            new List<SongNote>
            {
                new("C4"), new("C4"), new("G4"), new("G4"),
                new("A4"), new("A4"), new("G4", 2),
                new("F4"), new("F4"), new("E4"), new("E4"),
                new("D4"), new("D4"), new("C4", 2)
            }),
        new(
            "2. Ode to Joy - Beethoven",
            "Giai điệu mở đầu của Ode to Joy, chuyển sang các nốt đơn giản.",
            new List<SongNote>
            {
                new("E4"), new("E4"), new("F4"), new("G4"),
                new("G4"), new("F4"), new("E4"), new("D4"),
                new("C4"), new("C4"), new("D4"), new("E4"),
                new("E4", 1.5), new("D4", 0.5), new("D4", 2)
            }),
        new(
            "3. Jingle Bells",
            "Giai điệu Jingle Bells mở đầu, phiên bản một tay.",
            new List<SongNote>
            {
                new("E4"), new("E4"), new("E4", 2),
                new("E4"), new("E4"), new("E4", 2),
                new("E4"), new("G4"), new("C4"), new("D4"), new("E4", 2)
            })
    };
}
