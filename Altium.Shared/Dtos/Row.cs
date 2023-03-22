namespace Altium.Shared.Dtos;

public record Row : IComparable<Row>
{
    public Row()
    {

    }

    public Row(uint number, string text)
    {
        Number = number;
        Text = text;
    }

    public uint Number { get; init; }
    public string Text { get; init; }
    public int StreamReader { get; init; }

    public int CompareTo(Row other)
    {
        if (other == null)
            return 1;

        var compareText = Text.CompareTo(other.Text);
        if (compareText != 0) return compareText;

        return Number.CompareTo(other.Number);
    }
}

public static class RowDtoExtensions
{
    public static Row Parse(this string text)
    {
        //if (text is null || text.Length is 0)
        //    return null;

        var span = text.AsSpan();
        var splitAt = span.IndexOf(stackalloc char[] { '.', ' ' });

        return new Row(
             uint.Parse(span.Slice(0, splitAt)),
             span.Slice(splitAt + 2).ToString()
            );
    }

    public static Row Parse(this string text, int streamReader)
    {
        return text.Parse() with { StreamReader = streamReader };
    }
}
