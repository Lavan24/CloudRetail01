using Microsoft.AspNetCore.Mvc;

public class ContractController : Controller
{
    private readonly IContractStorageService _contractStorageService;

    public ContractController(IContractStorageService contractStorageService)
    {
        _contractStorageService = contractStorageService;
    }

    public async Task<IActionResult> Index()
    {
        var files = await _contractStorageService.ListContractsAsync();
        return View(files);
    }

    [HttpPost]
    public async Task<IActionResult> UploadContract(IFormFile file)
    {
        if (file == null || file.Length == 0 || !file.FileName.EndsWith(".pdf"))
        {
            TempData["Error"] = "Please select a valid PDF file.";
            return RedirectToAction("Index");
        }

        try
        {
            await _contractStorageService.UploadContractAsync(file);
            TempData["Success"] = $"Contract '{file.FileName}' uploaded!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Download(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            TempData["Error"] = "Invalid file.";
            return RedirectToAction("Index");
        }

        try
        {
            var stream = await _contractStorageService.DownloadContractAsync(fileName);
            return File(stream, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
            return RedirectToAction("Index");
        }
    }
}
