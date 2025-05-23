namespace fetcher_tester;

/// <summary>
/// Endpoints for serving various media types.
/// </summary>
public class MediaEndpoints
{
    public const string AviLabel = "avi";
    public const string Mp3Label = "mp3";
    public const string DelayedMp3Label = "delayed-mp3";
    public const string WavLabel = "wav";
    public const string OggLabel = "ogg";
    public const string Mp4Label = "mp4";
    public const string MovLabel = "mov";
    public const string PngLabel = "png";
    public const string TiffLabel = "tiff";
    public const string JpgLabel = "jpg";
    public const string PdfLabel = "pdf";

    public const string LabelPrefix = "media";

    private const string TestAviFilename = "file_example_AVI_480_750kB.avi";
    private const string TestMp3Filename = "file_example_MP3_1MG.mp3";
    private const string TestWavFilename = "file_example_WAV_1MG.wav";
    private const string TestOggFilename = "file_example_OOG_1MG.ogg";
    private const string TestMp4Filename = "file_example_MP4_480_1_5MG.mp4";
    private const string TestMovFilename = "file_example_MOV_480_700kB.mov";
    private const string TestPngFilename = "file_example_PNG_1MB.png";
    private const string TestTiffFilename = "file_example_TIFF_1MB.tiff";
    private const string TestJpgFilename = "file_example_JPG_1MB.jpg";
    private const string TestPdfFilename = "file_example_PDF_1MB.pdf";

    public MediaEndpoints()
    {
    }

    public Dictionary<string, RequestDelegate> GetEndpoints()
    {
        return new() {
            { $"{LabelPrefix}/{AviLabel}", AviFilehost },
            { $"{LabelPrefix}/{PdfLabel}", PdfFilehost },
            { $"{LabelPrefix}/{TiffLabel}", TiffFilehost },
            { $"{LabelPrefix}/{Mp3Label}", Mp3Filehost },
            { $"{LabelPrefix}/{WavLabel}", WavFilehost },
            { $"{LabelPrefix}/{Mp4Label}", Mp4Filehost },
            { $"{LabelPrefix}/{JpgLabel}", JpgFilehost },
            { $"{LabelPrefix}/{PngLabel}", PngFilehost },
            { $"{LabelPrefix}/{OggLabel}", OggFilehost },
            { $"{LabelPrefix}/{MovLabel}", MovFilehost },
            { $"{LabelPrefix}/{DelayedMp3Label}", DelayedMp3Filehost}
        };
    }

    async Task DelayedMp3Filehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "audio/mpeg3";

        await Task.Delay(TimeSpan.FromSeconds(5));
        await context.Response.SendFileAsync("media/" + TestMp3Filename);
    }

    async Task Mp3Filehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "audio/mpeg3";

        await context.Response.SendFileAsync("media/" + TestMp3Filename);
    }

    async Task WavFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "audio/wav";

        await context.Response.SendFileAsync("media/" + TestWavFilename);
    }

    async Task AviFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "video/avi";

        await context.Response.SendFileAsync("media/" + TestAviFilename);
    }

    async Task Mp4Filehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "video/mp4";

        await context.Response.SendFileAsync("media/" + TestMp4Filename);
    }

    async Task MovFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "video/quicktime";

        await context.Response.SendFileAsync("media/" + TestMovFilename);
    }

    async Task OggFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "audio/ogg";

        await context.Response.SendFileAsync("media/" + TestOggFilename);
    }

    async Task PdfFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "audio/mpeg3";

        await context.Response.SendFileAsync("media/" + TestPdfFilename);
    }

    async Task PngFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "image/png";

        await context.Response.SendFileAsync("media/" + TestPngFilename);
    }

    async Task JpgFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "image/jpg";

        await context.Response.SendFileAsync("media/" + TestJpgFilename);
    }

    async Task TiffFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "image/tiff";

        await context.Response.SendFileAsync("media/" + TestTiffFilename);
    }
}
