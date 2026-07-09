using System.IO;
using System.Text.Json;
using System.Windows.Controls;

namespace DahuaUserManager.UI.Services;

public static class DataGridLayoutService
{
    private sealed class ColumnLayout
    {
        public string Header { get; set; } = "";
        public double Width { get; set; }
        public int DisplayIndex { get; set; }
    }

    private static string GetFilePath(string gridKey)
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DahuaUserManager",
            "Layouts");

        Directory.CreateDirectory(folder);

        return Path.Combine(folder, gridKey + ".json");
    }

    public static void Save(DataGrid grid, string gridKey)
    {
        var items = grid.Columns
            .Select(c => new ColumnLayout
            {
                Header = c.Header?.ToString() ?? "",
                Width = c.ActualWidth,
                DisplayIndex = c.DisplayIndex
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Header))
            .ToList();

        string json = JsonSerializer.Serialize(
            items,
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(GetFilePath(gridKey), json);
    }

    public static void Load(DataGrid grid, string gridKey)
    {
        string path = GetFilePath(gridKey);

        if (!File.Exists(path))
            return;

        var items = JsonSerializer.Deserialize<List<ColumnLayout>>(
            File.ReadAllText(path));

        if (items == null)
            return;

        foreach (ColumnLayout item in items)
        {
            DataGridColumn? column = grid.Columns.FirstOrDefault(c =>
                (c.Header?.ToString() ?? "") == item.Header);

            if (column == null)
                continue;

            if (item.Width > 20)
                column.Width = new DataGridLength(item.Width);

            if (item.DisplayIndex >= 0 && item.DisplayIndex < grid.Columns.Count)
                column.DisplayIndex = item.DisplayIndex;
        }
    }
}