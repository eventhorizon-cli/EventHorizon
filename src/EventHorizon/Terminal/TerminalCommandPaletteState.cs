namespace EventHorizon.Terminal;

public sealed class TerminalCommandPaletteState
{
    public bool IsOpen { get; private set; }
    public string Query { get; private set; } = string.Empty;
    public int SelectedIndex { get; private set; }

    public void Open(string? initialQuery = null)
    {
        IsOpen = true;
        Query = initialQuery ?? string.Empty;
        SelectedIndex = 0;
    }

    public void Close()
    {
        IsOpen = false;
        Query = string.Empty;
        SelectedIndex = 0;
    }

    public void SetQuery(string query)
    {
        Query = query;
        SelectedIndex = 0;
    }

    public void MoveSelection(int offset, int itemCount)
    {
        if (itemCount <= 0)
        {
            SelectedIndex = 0;
            return;
        }

        int next = SelectedIndex + offset;
        if (next < 0)
        {
            next = itemCount - 1;
        }
        else if (next >= itemCount)
        {
            next = 0;
        }

        SelectedIndex = next;
    }
}

