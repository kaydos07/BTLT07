namespace MiniPiano.Models;

public sealed record PracticeSong(
    string Title,
    string Description,
    IReadOnlyList<SongNote> Notes)
{
    public override string ToString() => Title;
}
