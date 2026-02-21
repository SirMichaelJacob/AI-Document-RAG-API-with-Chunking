using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RAG_Doc.Application.Commands;
using RAG_Doc.Application.Handlers;
using RAG_Doc.Application.Utilities;
using Spectre.Console;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using Wolverine;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IMessageBus _messageBus;

    public DocumentsController(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    // ============================================================
    // UPLOAD DOCUMENT (TXT, PDF, DOCX)
    // ============================================================
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status202Accepted)] // Wolverine queues message
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file,CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required.");

        if (file.Length > 50_000_000) // 50 MB
            return BadRequest("File is too large.");

        string content;
        try
        {
            content = await ExtractTextAsync(file, cancellationToken);
        }
        catch (NotSupportedException nsex)
        {
            return BadRequest(nsex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest($"Text extraction failed: {ex.Message}");
        }

        // Create command for Wolverine
        var command = new CreateDocumentCommand
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Content = content
        };

        command.Validate();

        // Send command to Wolverine for async processing
        await _messageBus.InvokeAsync(command, cancellationToken);

        return Accepted(new
        {
            message = "Document queued for processing.",
            fileName = file.FileName
        });
    }

    // ============================================================
    // QUERY DOCUMENTS VIA RAG
    // ============================================================
    [HttpPost("query")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Query(
        [FromBody] QueryDocumentsCommand command,
        CancellationToken cancellationToken)
    {
        if (command == null)
            return BadRequest("Invalid request.");

        command.Validate();

        // Wolverine sends command to handler
        var answer = await _messageBus.InvokeAsync<string>(command, cancellationToken);

        return Ok(new
        {
            question = command.Question,
            answer
        });
    }

    // ============================================================
    // TEXT EXTRACTION
    // ============================================================
    private async Task<string> ExtractTextAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        using var stream = file.OpenReadStream();

        return extension switch
        {
            ".txt" => await ReadTxtAsync(stream, cancellationToken),
            ".pdf" => ReadPdf(stream),
            ".docx" => ReadDocx(stream),
            _ => throw new NotSupportedException(
                    $"File type '{extension}' is not supported. Allowed: .txt, .pdf, .docx")
        };
    }

    private async Task<string> ReadTxtAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private string ReadPdf(Stream stream)
    {
        using var pdf = PdfDocument.Open(stream);
        var sb = new StringBuilder();
        foreach (var page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        return sb.ToString();
    }

    private string ReadDocx(Stream stream)
    {
        using var wordDoc = WordprocessingDocument.Open(stream, false);
        var body = wordDoc.MainDocumentPart?.Document.Body;
        if (body == null) return string.Empty;

        var sb = new StringBuilder();
        foreach (var paragraph in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
        {
            sb.AppendLine(paragraph.InnerText);
        }
        return sb.ToString();
    }
}

