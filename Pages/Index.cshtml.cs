using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Practice.WebApp.Model;
using System.Linq.Expressions;
using static System.Net.Mime.MediaTypeNames;

namespace Practice.WebApp.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public IFormFile UploadedFile { get; set; }
        private readonly ILogger<IndexModel> _logger;
        private readonly IWebHostEnvironment _environment;

        public Employee employee { get; set; } = new();
        [BindProperty]
        public List<string> ImagePathLists { get; set; }
        public List<string> ImageFiles { get; set; }
        public List<string> ImagePaths { get; set; }
        public List<string> ImageNames { get; set; }

        public string Header { get; set; } = string.Empty;
        public string TestMessage { get; set; } = string.Empty;
        public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public IActionResult OnGet()
        {
            Header = "Welcome";
            // OnPostGetImages();
            MyAppSetting appSetting = new MyAppSetting();
            var test = appSetting;

			return Page();
        }
        public void OnPostUpload(List<IFormFile> postedFiles)
        {
            if (postedFiles.Count == 0)
            {
                return;
            }
            string wwwPath = _environment.WebRootPath;
            string contentPath = _environment.ContentRootPath;

            string path = Path.Combine(this._environment.WebRootPath, "Uploads");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            List<string> uploadedFiles = new List<string>();
            foreach (IFormFile postedFile in postedFiles)
            {
                string fileName = Path.GetFileName(postedFile.FileName);
                using FileStream stream = new(Path.Combine(path, fileName), FileMode.Create);
                postedFile.CopyTo(stream);
                uploadedFiles.Add(fileName);
                TestMessage += string.Format("<b>{0}</b> uploaded.<br />", fileName);
            }
        }
        public IActionResult OnPostGetImages()
        {
            string rootFolder = Path.Combine(_environment.WebRootPath, "Uploads");

            // Get all image files from the root folder
            ImageFiles = Directory.GetFiles(rootFolder, "*.*", SearchOption.TopDirectoryOnly).ToList();

            ImageNames = Directory.GetFiles(rootFolder).Select(Path.GetFileName).ToList();

            return Page();
        }
        public IActionResult OnPostDeleteImage(string fileName)
        {
            try
            {
                string filePath = Path.Combine(_environment.WebRootPath, "Uploads", fileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    OnPostGetImages();
                }
                return new JsonResult(new { success = true, message = "Image deleted successfully" });
            }
            catch
            {
                return new JsonResult(new { success = false, message = "Error in Delete Img" });
            }
        }
    }
}