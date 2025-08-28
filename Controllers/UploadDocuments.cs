using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Retail3.Controllers
{
    public class UploadDocuments : Controller
    {
        private readonly string _uploadPath;

        public UploadDocuments(IWebHostEnvironment env)
        {
            _uploadPath = Path.Combine(env.WebRootPath, "uploads");
            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);
        }

        public IActionResult Contacts()
        {
            var files = Directory.GetFiles(_uploadPath)
                .Select(Path.GetFileName)
                .ToList();
            ViewBag.Files = files;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile contractFile)
        {
            if (contractFile != null && contractFile.Length > 0)
            {
                var filePath = Path.Combine(_uploadPath, Path.GetFileName(contractFile.FileName));
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await contractFile.CopyToAsync(stream);
                }
            }
            return RedirectToAction("Contacts");
        }

        public IActionResult ViewImage(string fileName)
        {
            var filePath = Path.Combine(_uploadPath, fileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var contentType = "image/" + Path.GetExtension(fileName).TrimStart('.').ToLower();
            return PhysicalFile(filePath, contentType);
        }

        public IActionResult Download(string fileName)
        {
            var filePath = Path.Combine(_uploadPath, fileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var contentType = "application/octet-stream";
            return PhysicalFile(filePath, contentType, fileName);
        }
    }
}
