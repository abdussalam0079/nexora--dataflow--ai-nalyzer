using DataFlow.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DataFlow.UI.Helpers;

public static class DashboardExportHelper
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Formatting = Formatting.Indented,
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        }
    };

    public static void ExportLayoutJson(DashboardLayoutDocument layout, string filePath)
    {
        var json = JsonConvert.SerializeObject(layout, JsonSettings);
        File.WriteAllText(filePath, json);
    }

    public static void ExportPng(Control surface, string filePath)
    {
        var bounds = surface.ClientRectangle;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            bounds = new Rectangle(0, 0, Math.Max(800, surface.Width), Math.Max(600, surface.Height));

        using var bmp = new Bitmap(bounds.Width, bounds.Height);
        surface.DrawToBitmap(bmp, bounds);
        bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
    }
}
