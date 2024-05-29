using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Practice.WebApp.Pages
{
	public class PDFConvertModel : PageModel
    {
        private readonly IWebHostEnvironment _environment;
        public PDFConvertModel(IWebHostEnvironment environment)
        {
                _environment = environment;
        }
        public void OnGet()
        {
        }
        public IActionResult OnPostConvertToPDF(IList<IFormFile> files)
        {
            byte[] bytes;
			string pdfFileName = $"PDF_{DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")}.pdf";
			string outputPath = Path.Combine(_environment.WebRootPath, "GeneratedPDF", pdfFileName);

			if (files == null || files.Count == 0)
            {
                return BadRequest("No files were uploaded.");
            }
            using (MemoryStream ms = new MemoryStream())
            {
				using var doc = new Document(PageSize.A4);
				using (var writer = PdfWriter.GetInstance(doc, ms))
				{
					doc.Open();
					foreach (var formFile in files)
					{
						if (formFile.Length > 0 && formFile.ContentType.StartsWith("image/"))
						{
							Image image = Image.GetInstance(formFile.OpenReadStream());
							image.Alignment = Element.ALIGN_CENTER;
							image.ScaleToFit(270f, 270f);
							image.IndentationLeft = 9f;
							image.SpacingAfter = 9f;
							image.BorderWidthTop = 2f;

							doc.Add(image);
						}
					}
					doc.Close();
				}

				bytes = ms.ToArray();
			}
            System.IO.File.WriteAllBytes(outputPath, bytes);
            return File(bytes, "application/pdf", pdfFileName);

        }
    }
}
