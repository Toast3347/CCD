using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using ExternalAPI.Interfaces;
using ExternalAPI.Models;
using SkiaSharp;
using System.Text.Json;

namespace ExternalAPI.Services;

public sealed class ImageService : IImageService
{
    private readonly HttpClient _httpClient;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName = "images";

    public ImageService(
        HttpClient httpClient,
        BlobServiceClient blobServiceClient)
    {
        _httpClient = httpClient;
        _blobServiceClient = blobServiceClient;
    }

    public async Task<ImageProcessingResult> GetAndProcessImageAsync(
        WeatherStation station,
        Guid jobId)
    {
        var apiKey = Environment.GetEnvironmentVariable("UnsplashApiKey");

        // debug line for azure deployment. REMOVE AFTER IT WORKS
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "UnsplashApiKey environment variable is missing.");
        }

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.unsplash.com/photos/random?client_id={apiKey}");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();

        using var jsonDoc = JsonDocument.Parse(jsonString);

        string imageUrl = jsonDoc.RootElement
            .GetProperty("urls")
            .GetProperty("regular")
            .GetString()!;

        var imageResponse = await _httpClient.GetAsync(imageUrl);
        imageResponse.EnsureSuccessStatusCode();

        byte[] originalImageBytes =
            await imageResponse.Content.ReadAsByteArrayAsync();

        using var bitmap = SKBitmap.Decode(originalImageBytes);

        if (bitmap == null)
            throw new InvalidOperationException("Could not decode downloaded image.");

        using var canvas = new SKCanvas(bitmap);

        string weatherText =
            $"Station: {station.StationName}\n" +
            $"Temp: {station.Temperature}°C\n" +
            $"Wind: {station.WindSpeed} m/s\n" +
            $"Condition: {station.WeatherDescription}";

        using var typeface = SKTypeface.FromFamilyName(
            "Arial",
            SKFontStyle.Bold);

        using var font = new SKFont(typeface, 24);

        using var paint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true
        };

        var backgroundRect = new SKRect(
            10,
            bitmap.Height - 160,
            bitmap.Width - 10,
            bitmap.Height - 10);

        using var backgroundPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 150)
        };

        canvas.DrawRect(backgroundRect, backgroundPaint);

        string[] lines = weatherText.Split('\n');

        float y = bitmap.Height - 125;

        foreach (string line in lines)
        {
            canvas.DrawText(line, 20, y, font, paint);
            y += 30;
        }

        canvas.Flush();

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 90);

        byte[] processedImage = encoded.ToArray();

        var containerClient =
            _blobServiceClient.GetBlobContainerClient(_containerName);

        await containerClient.CreateIfNotExistsAsync();

        var blobName = $"{jobId}/{station.StationId}.jpg";

        var blobClient = containerClient.GetBlobClient(blobName);

        using var uploadStream = new MemoryStream(processedImage);

        await blobClient.UploadAsync(uploadStream, overwrite: true);

        Uri finalUri = blobClient.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.AddDays(1));

        return new ImageProcessingResult(processedImage, finalUri);
    }
}