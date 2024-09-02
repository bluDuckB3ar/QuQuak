using CsvHelper;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Concurrent; // Change to use ConcurrentBag
using System.Collections.Generic;
using Accord.MachineLearning;
using Accord.Math;
using System.Threading.Tasks; // Required for Parallel.ForEach

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var csvPath = Path.Combine("C:/QulorQuAK/image/CSV", $"out_{timestamp}.csv");

            var imagesDirectory = "C:/QulorQuAK/image/imgur";
            string[] imageNames = Directory.GetFiles(imagesDirectory, "*.*")
                                           .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                                       f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                                       f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                                                       f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                                                       f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                                           .ToArray();

            var imageRecords = new ConcurrentBag<ImageRecord>();

            Parallel.ForEach(imageNames, id =>
            {
                using (var image = Image.FromFile(id))
                {
                    var colorPalette = GetColorPalette(image, 5); // Get 5-layer color palette
                    var record = new ImageRecord
                    {
                        TimeName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_U",
                        ImageName = Path.GetFileName(id),
                        ImageType = Path.GetExtension(id).TrimStart('.').ToUpper(),
                        Width = image.Width,
                        Height = image.Height,
                        Colors = string.Join(";", colorPalette.Select(c => $"{c.R},{c.G},{c.B}")),
                        HexColors = string.Join(";", colorPalette.Select(c => $"#{c.R:X2}{c.G:X2}{c.B}"))
                    };
                    imageRecords.Add(record);

                    // Print colors to console with background
                    Console.WriteLine($"Image: {record.ImageName}");
                    foreach (var color in colorPalette)
                    {
                        PrintColorBlock(color);
                    }
                    Console.WriteLine(); // New line after each image
                }
            });

            Directory.CreateDirectory(Path.GetDirectoryName(csvPath));

            using (var writer = new StreamWriter(csvPath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(imageRecords);
            }

            Console.WriteLine(File.ReadAllText(csvPath));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    // Method to extract a 5-layer color palette using K-means clustering
    public static List<Color> GetColorPalette(Image image, int numColors)
    {
        var colors = new List<Color>();

        using (var bitmap = new Bitmap(image))
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    colors.Add(bitmap.GetPixel(x, y));
                }
            }
        }

        // Convert colors to double array for K-means
        double[][] pixelData = colors.Select(c => new double[] { c.R, c.G, c.B }).ToArray();

        // Apply K-means clustering to reduce the number of colors
        var kmeans = new KMeans(numColors);
        KMeansClusterCollection clusters = kmeans.Learn(pixelData);

        // Get centroids which represent the average color of each cluster
        double[][] centroids = clusters.Centroids;

        // Convert centroids back to Color objects
        return centroids.Select(c => Color.FromArgb((int)c[0], (int)c[1], (int)c[2])).ToList();
    }

    // Method to print a colored block in the console
    public static void PrintColorBlock(Color color)
    {
        string rgb = $"{color.R};{color.G};{color.B}";
        Console.Write($"\x1b[48;2;{rgb}m   \x1b[0m "); // ANSI escape code for background color
    }

    public class ImageRecord
    {
        public string? TimeName { get; set; }
        public string? ImageName { get; set; }
        public string? ImageType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string? Colors { get; set; }
        public string? HexColors { get; set; }
    }
}
