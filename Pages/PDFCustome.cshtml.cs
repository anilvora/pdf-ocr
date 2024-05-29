using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Practice.WebApp.Pages
{
    public class PDFCustomeModel : PageModel
    {
        private static IWebHostEnvironment _environment;

        public PDFViewModel pDFView { get; set; }   
		public PDFCustomeModel(IWebHostEnvironment environment)
        {
                _environment = environment;
        }
        public void OnGet()
        {
			TempData["Embed"] = string.Empty;
            
            string path = Path.Combine(_environment.WebRootPath, "GeneratedPDF");
            string pdffile = path + "/TestPrint.pdf";
			byte[] pdfBytes = System.IO.File.ReadAllBytes(pdffile);

			// string path = Path.Combine(_environment.WebRootPath, "Documents");

			using (MemoryStream stream = new MemoryStream())
            {

                System.IO.File.ReadAllBytes(path + "/TestPrint.pdf");

                string embed = "<object data=\"{0}\" type=\"application/pdf\" width=\"500px\" height=\"300px\">";
                embed += "If you are unable to view file, you can download from <a href = \"{0}\">here</a>";
                embed += " or download <a target = \"_blank\" href = \"http://get.adobe.com/reader/\">Adobe PDF Reader</a> to view the file.";
                embed += "</object>";

				TempData["Embed"] = string.Format("", pdfBytes);
            }
        }
        public ActionResult ViewPDF()
        {
            string physicalPath = Path.Combine(_environment.WebRootPath, "GeneratedPDF/TestPrint.pdf");
            byte[] pdfBytes = System.IO.File.ReadAllBytes(physicalPath);
            MemoryStream stream = new MemoryStream(pdfBytes);

            string mimeType = "application/pdf";
            return new FileStreamResult(stream, mimeType)
            {
                FileDownloadName = "AnyNameYouWantToSet.pdf"
            };

        }
		public async Task<IActionResult> OnGetViewPdfURL(string url)
		{
			// Fetch the PDF content from the provided URL
			byte[] pdfContent;
			using (var httpClient = new HttpClient())
			{
				pdfContent = await httpClient.GetByteArrayAsync(url);
			}

			// Return the PDF content as a FileResult
			return File(pdfContent, "application/pdf");
		}
	}
}
