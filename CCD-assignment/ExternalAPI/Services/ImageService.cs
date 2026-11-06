using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using ExternalAPI.Interfaces;
using ExternalAPI.Models;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using System.Text.Json;

namespace ExternalAPI.Services;

public sealed class ImageService : IImageService
{
    private readonly HttpClient _httpClient;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName = "images";

    public ImageService(HttpClient httpClient, BlobServiceClient blobServiceClient)
    {
        _httpClient = httpClient;
        _blobServiceClient = blobServiceClient;
    }

    public async Task<ImageProcessingResult> GetAndProcessImageAsync(WeatherStation station, Guid jobId)
    {
        var apiKey = Environment.GetEnvironmentVariable("UnsplashApiKey");
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.unsplash.com/photos/random?client_id={apiKey}");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonString);
        string imageUrl = jsonDoc.RootElement.GetProperty("urls").GetProperty("regular").GetString();

        var imageResponse = await _httpClient.GetAsync(imageUrl);
        imageResponse.EnsureSuccessStatusCode();

        using var stream = await imageResponse.Content.ReadAsStreamAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        byte[] originalImageBytes = ms.ToArray();

        using var image = Image.Load(originalImageBytes);

        // System.I0.Path because 'Path' is an ambiguous reference between 'SixLabors.ImageSharp.Drawing.Path' and 'System.IO.Path'. Ugly but necessary to avoid ambiguity.
        string fontPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "../../../Fonts/Arial.ttf"));

        FontCollection collection = new();

        FontFamily family = collection.Add(fontPath);

        Font font = family.CreateFont(
            24,
            new FontVariation(KnownVariationAxes.Weight, 700),
            new FontVariation(KnownVariationAxes.Width, 85),
            new FontVariation(KnownVariationAxes.OpticalSize, 16));

        string weatherText = $"Station: {station.StationName}\n" +
                             $"Temp: {station.Temperature}°C\n" +
                             $"Wind: {station.WindSpeed} m/s\n" +
                             $"Condition: {station.WeatherDescription}";

        RichTextOptions textOptions = new(font)
        {
            Origin = new PointF(20, image.Height - 150),
            WrappingLength = image.Width - 40
        };

        image.Mutate(x => x.Paint(canvas =>
        {
            canvas.DrawText(textOptions, weatherText, Brushes.Solid(Color.White), pen:null);
        }));

        using var outputStream = new MemoryStream();
        await image.SaveAsJpegAsync(outputStream);
        byte[] processedImage = outputStream.ToArray();

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobName = $"{jobId}/{station.StationId}.jpg";
        var blobClient = containerClient.GetBlobClient(blobName);

        using var uploadStream = new MemoryStream(processedImage);
        await blobClient.UploadAsync(uploadStream, overwrite: true);

        Uri finalUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddDays(1));

        return new ImageProcessingResult(processedImage, finalUri);
    }
}
