using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;

namespace CmsLite.Helpers;

/// <summary>
/// Configuration options for PDF validation.
/// </summary>
public class PdfValidationOptions
{
    /// <summary>
    /// Maximum allowed file size in bytes. Default: 8 MB (8388608 bytes).
    /// </summary>
    public int MaxFileSizeBytes { get; set; } = 8388608;

    /// <summary>
    /// Maximum allowed number of pages. Default: 1000.
    /// </summary>
    public int MaxPageCount { get; set; } = 1000;

    /// <summary>
    /// Whether to allow password-protected PDFs. Default: false.
    /// </summary>
    public bool AllowPasswordProtected { get; set; } = false;

    /// <summary>
    /// Whether to scan for embedded files/attachments. Default: true.
    /// </summary>
    public bool ScanForEmbeddedFiles { get; set; } = true;

    /// <summary>
    /// Whether to scan for JavaScript. Default: true.
    /// </summary>
    public bool ScanForJavaScript { get; set; } = true;
}

/// <summary>
/// Result of PDF validation.
/// </summary>
public class PdfValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public PdfValidationWarning[]? Warnings { get; set; }

    public static PdfValidationResult Success() => new() { IsValid = true };
    public static PdfValidationResult Failure(string errorMessage) => new() { IsValid = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Warning information from PDF validation.
/// </summary>
public class PdfValidationWarning
{
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Comprehensive PDF validation service using PdfSharp 6.x.
/// </summary>
public static class PdfValidator
{
    /// <summary>
    /// Validates a PDF file with comprehensive security and structural checks.
    /// </summary>
    /// <param name="data">PDF file bytes</param>
    /// <param name="options">Validation options</param>
    /// <param name="logger">Optional logger for validation details</param>
    /// <returns>Validation result with success status and error message if applicable</returns>
    public static PdfValidationResult ValidatePdf(byte[] data, PdfValidationOptions options, ILogger? logger = null)
    {
        try
        {
            // 1. Check file size first (before any processing)
            var sizeCheck = CheckFileSize(data, options.MaxFileSizeBytes);
            if (!sizeCheck.IsValid)
            {
                logger?.LogInformation("PDF validation failed: File size exceeds limit ({Size} bytes > {MaxSize} bytes)",
                    data.Length, options.MaxFileSizeBytes);
                return sizeCheck;
            }

            // 2. Basic magic byte validation
            if (!Utilities.IsValidPdf(data))
            {
                logger?.LogInformation("PDF validation failed: Invalid PDF header/magic bytes");
                return PdfValidationResult.Failure("Invalid PDF header. File does not appear to be a valid PDF.");
            }

            // 3. Load and parse PDF structure using PdfSharp
            PdfDocument document;
            try
            {
                using var ms = new MemoryStream(data);
                document = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
            }
            catch (PdfReaderException ex)
            {
                logger?.LogWarning(ex, "PDF validation failed: PdfSharp could not parse PDF structure");
                return PdfValidationResult.Failure($"Invalid or corrupted PDF structure: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                logger?.LogWarning(ex, "PDF validation failed: Invalid PDF operation");
                return PdfValidationResult.Failure($"Invalid PDF format: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Unexpected error during PDF validation");
                return PdfValidationResult.Failure($"PDF validation failed: {ex.Message}");
            }

            // 4. Structural validation
            var structureCheck = CheckStructure(document, options.MaxPageCount, logger);
            if (!structureCheck.IsValid)
            {
                return structureCheck;
            }

            // 5. Security validation (password protection)
            if (!options.AllowPasswordProtected)
            {
                var securityCheck = CheckSecurity(document, logger);
                if (!securityCheck.IsValid)
                {
                    return securityCheck;
                }
            }

            // 6. Malicious content detection
            var maliciousContentCheck = CheckMaliciousContent(document, options, logger);
            if (!maliciousContentCheck.IsValid)
            {
                return maliciousContentCheck;
            }

            logger?.LogInformation("PDF validation succeeded: {PageCount} pages, {Size} bytes",
                document.PageCount, data.Length);
            return PdfValidationResult.Success();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error during PDF validation");
            return PdfValidationResult.Failure($"PDF validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if file size is within limits.
    /// </summary>
    private static PdfValidationResult CheckFileSize(byte[] data, int maxSizeBytes)
    {
        if (data == null || data.Length == 0)
        {
            return PdfValidationResult.Failure("PDF file is empty");
        }

        if (data.Length > maxSizeBytes)
        {
            var maxSizeMB = maxSizeBytes / (1024.0 * 1024.0);
            return PdfValidationResult.Failure($"PDF file size ({data.Length} bytes) exceeds maximum allowed size of {maxSizeMB:F1} MB");
        }

        return PdfValidationResult.Success();
    }

    /// <summary>
    /// Checks PDF structural integrity and page count.
    /// </summary>
    private static PdfValidationResult CheckStructure(PdfDocument document, int maxPageCount, ILogger? logger)
    {
        // Check page count
        if (document.PageCount == 0)
        {
            logger?.LogInformation("PDF validation failed: PDF has no pages");
            return PdfValidationResult.Failure("PDF has no pages");
        }

        if (document.PageCount > maxPageCount)
        {
            logger?.LogInformation("PDF validation failed: Page count exceeds limit ({PageCount} > {MaxPageCount})",
                document.PageCount, maxPageCount);
            return PdfValidationResult.Failure($"PDF page count ({document.PageCount}) exceeds maximum allowed pages ({maxPageCount})");
        }

        // Check if document has valid catalog
        if (document.Internals.Catalog == null)
        {
            logger?.LogWarning("PDF validation failed: Missing document catalog");
            return PdfValidationResult.Failure("Invalid PDF structure: Missing document catalog");
        }

        return PdfValidationResult.Success();
    }

    /// <summary>
    /// Checks for password protection and encryption.
    /// </summary>
    private static PdfValidationResult CheckSecurity(PdfDocument document, ILogger? logger)
    {
        try
        {
            // Attempt to access a property that triggers decryption.
            var _ = document.PageCount;
        }
        catch (NotSupportedException ex)
        {
            logger?.LogWarning(ex, "PDF validation failed: Password-protected PDF detected");
            return PdfValidationResult.Failure("Password-protected PDFs are not supported. Please provide an unencrypted PDF.");
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Unexpected error during PDF security check");
            return PdfValidationResult.Failure($"Error during PDF security check: {ex.Message}");
        }

        // Additionally check for encryption flag in the catalog
        if (document.Internals.Catalog?.Elements.ContainsKey("/Encrypt") == true)
        {
            logger?.LogWarning("PDF validation failed: Encrypted PDF detected");
            return PdfValidationResult.Failure("Encrypted PDFs are not supported. Please upload an unencrypted PDF.");
        }

        return PdfValidationResult.Success();
    }

    /// <summary>
    /// Checks for malicious content: JavaScript, embedded files, suspicious actions.
    /// </summary>
    private static PdfValidationResult CheckMaliciousContent(PdfDocument document, PdfValidationOptions options, ILogger? logger)
    {
        try
        {
            // Check for JavaScript at document level
            if (options.ScanForJavaScript && HasJavaScript(document, logger))
            {
                logger?.LogWarning("PDF validation failed: JavaScript detected in PDF");
                return PdfValidationResult.Failure("PDFs containing JavaScript are not allowed for security reasons.");
            }

            // Check for embedded files/attachments
            if (options.ScanForEmbeddedFiles && HasEmbeddedFiles(document, logger))
            {
                logger?.LogWarning("PDF validation failed: Embedded files detected in PDF");
                return PdfValidationResult.Failure("PDFs with embedded files or attachments are not allowed for security reasons.");
            }

            // Check for launch actions (auto-execute on open)
            if (HasLaunchActions(document, logger))
            {
                logger?.LogWarning("PDF validation failed: Launch actions detected in PDF");
                return PdfValidationResult.Failure("PDFs with launch actions are not allowed for security reasons.");
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Error scanning PDF for malicious content");
            // If we can't scan for malicious content, reject the PDF (fail closed for security)
            return PdfValidationResult.Failure("Unable to verify PDF security. Please try a different PDF.");
        }

        return PdfValidationResult.Success();
    }

    /// <summary>
    /// Checks if PDF contains JavaScript.
    /// </summary>
    private static bool HasJavaScript(PdfDocument document, ILogger? logger)
    {
        try
        {
            // Check document-level JavaScript
            var catalog = document.Internals.Catalog;
            if (catalog?.Elements.ContainsKey("/Names") == true)
            {
                var names = catalog.Elements.GetDictionary("/Names");
                if (names?.Elements.ContainsKey("/JavaScript") == true)
                {
                    logger?.LogWarning("JavaScript found in document catalog");
                    return true;
                }
            }

            // Check for JavaScript in document-level actions
            if (catalog?.Elements.ContainsKey("/AA") == true || catalog?.Elements.ContainsKey("/OpenAction") == true)
            {
                var aaDict = catalog.Elements.GetDictionary("/AA");
                var openAction = catalog.Elements.GetDictionary("/OpenAction");

                if (ContainsJavaScriptAction(aaDict) || ContainsJavaScriptAction(openAction))
                {
                    logger?.LogWarning("JavaScript found in document actions");
                    return true;
                }
            }

            // Check each page for JavaScript actions
            foreach (PdfPage page in document.Pages)
            {
                if (page.Elements.ContainsKey("/AA"))
                {
                    var pageAA = page.Elements.GetDictionary("/AA");
                    if (ContainsJavaScriptAction(pageAA))
                    {
                        logger?.LogWarning("JavaScript found in page actions");
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Error checking for JavaScript in PDF");
            // If we can't determine, assume it might have JS (fail closed)
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a PDF dictionary contains JavaScript actions.
    /// </summary>
    private static bool ContainsJavaScriptAction(PdfDictionary? dict)
    {
        if (dict == null) return false;

        foreach (var key in dict.Elements.Keys)
        {
            var element = dict.Elements[key];

            // Check if it's a JavaScript action
            if (element is PdfDictionary actionDict)
            {
                if (actionDict.Elements.ContainsKey("/S"))
                {
                    var actionType = actionDict.Elements.GetName("/S");
                    if (actionType == "/JavaScript")
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if PDF has embedded files or attachments.
    /// </summary>
    private static bool HasEmbeddedFiles(PdfDocument document, ILogger? logger)
    {
        try
        {
            var catalog = document.Internals.Catalog;

            // Check for embedded files in Names dictionary
            if (catalog?.Elements.ContainsKey("/Names") == true)
            {
                var names = catalog.Elements.GetDictionary("/Names");
                if (names?.Elements.ContainsKey("/EmbeddedFiles") == true)
                {
                    logger?.LogWarning("Embedded files found in PDF");
                    return true;
                }
            }

            // Check for file attachments in document catalog
            if (catalog?.Elements.ContainsKey("/EmbeddedFiles") == true)
            {
                logger?.LogWarning("Embedded files found in document catalog");
                return true;
            }

            // Check each page for file attachment annotations
            foreach (PdfPage page in document.Pages)
            {
                if (page.Elements.ContainsKey("/Annots"))
                {
                    var annots = page.Elements.GetArray("/Annots");
                    if (annots != null)
                    {
                        foreach (var annotRef in annots.Elements)
                        {
                            if (annotRef is PdfReference reference)
                            {
                                var annot = reference.Value as PdfDictionary;
                                if (annot?.Elements.GetName("/Subtype") == "/FileAttachment")
                                {
                                    logger?.LogWarning("File attachment annotation found on page");
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Error checking for embedded files in PDF");
            // If we can't determine, assume it might have embedded files (fail closed)
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if PDF has launch actions (auto-execute on open).
    /// </summary>
    private static bool HasLaunchActions(PdfDocument document, ILogger? logger)
    {
        try
        {
            var catalog = document.Internals.Catalog;

            // Check OpenAction for launch actions
            if (catalog?.Elements.ContainsKey("/OpenAction") == true)
            {
                var openAction = catalog.Elements.GetDictionary("/OpenAction");
                if (openAction?.Elements.GetName("/S") == "/Launch")
                {
                    logger?.LogWarning("Launch action found in OpenAction");
                    return true;
                }
            }

            // Check additional actions
            if (catalog?.Elements.ContainsKey("/AA") == true)
            {
                var aaDict = catalog.Elements.GetDictionary("/AA");
                if (ContainsLaunchAction(aaDict))
                {
                    logger?.LogWarning("Launch action found in document actions");
                    return true;
                }
            }

            // Check pages for launch actions
            foreach (PdfPage page in document.Pages)
            {
                if (page.Elements.ContainsKey("/AA"))
                {
                    var pageAA = page.Elements.GetDictionary("/AA");
                    if (ContainsLaunchAction(pageAA))
                    {
                        logger?.LogWarning("Launch action found in page actions");
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Error checking for launch actions in PDF");
            // If we can't determine, assume it might have launch actions (fail closed)
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a PDF dictionary contains launch actions.
    /// </summary>
    private static bool ContainsLaunchAction(PdfDictionary? dict)
    {
        if (dict == null) return false;

        foreach (var key in dict.Elements.Keys)
        {
            var element = dict.Elements[key];

            if (element is PdfDictionary actionDict)
            {
                if (actionDict.Elements.GetName("/S") == "/Launch")
                {
                    return true;
                }
            }
        }

        return false;
    }
}
