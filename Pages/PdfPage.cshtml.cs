using iText.IO.Image;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Practice.WebApp.Model;
using System.Text.Json;
using Document = iTextSharp.text.Document;
using LayoutDocument = iText.Layout.Document;

namespace Practice.WebApp.Pages
{
    public class PdfPageModel : PageModel
    {
        public List<string> ImageNames { get; set; } = new List<string>();
        public List<string> ImageFiles { get; set; } = new List<string>();

        private readonly IWebHostEnvironment _environment;
        [BindProperty]
		public List<FileModel> Files { get; set; }
        public PdfPageModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }


		public IActionResult OnGet()
		{
			Files = GetFiles();
			return Page();
		}

		public IActionResult OnPost()
		{
			var postedFile = Request.Form.Files["postedFile"];
			byte[] bytes;
			using (BinaryReader br = new BinaryReader(postedFile.OpenReadStream()))
			{
				bytes = br.ReadBytes((int)postedFile.Length);
			}
			string fileName = Path.GetFileName(postedFile.FileName);
			string pdfFileName = $"PDF_{DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")}.pdf";


			string filePath = Path.Combine(_environment.WebRootPath, "GeneratedPDF", pdfFileName);
			if (System.IO.File.Exists(filePath))
			{
				System.IO.File.Delete(filePath);
			}
            System.IO.File.Create(filePath);

			Files = GetFiles();
			return Page();
		}
		public JsonResult OnPostGetPDF(string file)
		{
			byte[] fileBytes;
			string fileName;


			string rootFolder = Path.Combine(_environment.WebRootPath, "GeneratedPDF");


			var rootFolderFiles = Directory.GetFiles(rootFolder).Select(Path.GetFullPath).ToList();

            fileName = rootFolderFiles.FirstOrDefault(x => x.Contains(file, StringComparison.OrdinalIgnoreCase));

			fileBytes = System.IO.File.ReadAllBytes(fileName ?? "");

			string base64Content = Convert.ToBase64String(fileBytes);


			JsonResult jsonResult = new(new { FileName = fileName, ContentType = "application/pdf", Data = base64Content });
			//var serializer = new JsonSerializerOptions(); // Using JavaScriptSerializer to set MaxJsonLength
			return jsonResult;
		}

		private List<FileModel> GetFiles()
		{
            List<FileModel> fileList = new List<FileModel>();

			string rootFolder = Path.Combine(_environment.WebRootPath, "GeneratedPDF");

			// Get all image files from the root folder
			ImageFiles = Directory.GetFiles(rootFolder, "*.*", SearchOption.TopDirectoryOnly).ToList();

			ImageNames = Directory.GetFiles(rootFolder).Select(Path.GetFileName).ToList();

			fileList = ImageNames.Select(p => new FileModel { Id = new Random().Next(), Name = p }).ToList();

            // Return Name and Id
			return fileList;
		}

        /// <summary>
        ///  END NEW TEST
        /// </summary>
        /// <returns></returns>


		#region OLD Test Code

		// COMMENTS
		//public List<string> ImageNames { get; set; } = new List<string>();
		//public List<string> ImageFiles { get; set; } = new List<string>();

		//private readonly IWebHostEnvironment _environment;
		//public List<FileModel> Files { get; set; }
		//public PdfPageModel(IWebHostEnvironment environment)
		//{
		//	_environment = environment;
		//}

		//public IActionResult OnGet()
		//{

		//}

		public List<string> GetImagesForPDF()
        {
            string rootFolder = Path.Combine(_environment.WebRootPath, "Uploads");

            // Get all image files from the root folder
            ImageFiles = Directory.GetFiles(rootFolder, "*.*", SearchOption.TopDirectoryOnly).ToList();

            for (int i = 0; i < ImageFiles.Count; i++)
            {
                ImageFiles[i] = ImageFiles[i].Replace('\\', '/');
            }

            ImageNames = Directory.GetFiles(rootFolder).Select(Path.GetFileName).ToList();
            return ImageFiles;
        }
        public IActionResult OnPostItext7()
        {
            try
            {
                // Generate PDF content here
                //var htmlContent1 = "<html><body><img src='/images/image1.jpg' /><img src='/images/image2.jpg' /></body></html>";

                ImageFiles = GetImagesForPDF();
                var htmlContent = GetHtmlContent(ImageFiles);

                // Convert HTML to PDF using iTextSharp
                var pdfStream = new MemoryStream();
                pdfStream.Position = 0;

                var writer = new iText.Kernel.Pdf.PdfWriter(pdfStream);
                var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
                var document = new LayoutDocument(pdf);

                //var htmlContent = "<html><body><p>Hello, World!</p></body></html>";


                //var html = HtmlConverter.ConvertToElements(htmlContent);
                //foreach (var element in html)
                //{
                //    document.Add((IBlockElement)element);
                //}

                var imagePaths = ExtractImagePathsFromHtml(htmlContent);

                foreach (var imagePath in imagePaths)
                {
                    var image = new iText.Layout.Element.Image(ImageDataFactory.Create(imagePath));
                    document.Add(image);
                }

                using (var memoryStream = new MemoryStream())
                    // Save or return the generated PDF
                    pdfStream.CopyTo(memoryStream);
                document.Close();


                return new FileStreamResult(pdfStream, "application/pdf")
                {
                    FileDownloadName = "GeneratedPDF.pdf"
                };
                //document.Close();
            }
            catch (Exception ex)
            {
                // Handle exceptions, log, or return an error response
                Console.WriteLine($"Error generating PDF: {ex.Message}");
                return new StatusCodeResult(500); // Internal Server Error
            }
        }

        public static List<string> ExtractImagePathsFromHtml(string htmlContent)
        {
            var imagePaths = new List<string>();

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlContent);

            var imgNodes = doc.DocumentNode.SelectNodes("//img/@src");
            if (imgNodes != null)
            {
                foreach (var imgNode in imgNodes)
                {
                    var imagePath = imgNode.GetAttributeValue("src", "");
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        imagePaths.Add(imagePath);
                    }
                }
            }

            return imagePaths;

        }
        private static string GetHtmlContent(List<string> imagePaths)
        {
            // Generate HTML content with image tags
            var htmlContent = "<html><body>";

            foreach (var imagePath in imagePaths)
            {
                htmlContent += $"<img src='{imagePath}' />";
            }

            htmlContent += "</body></html>";

            return htmlContent;
        }

        #region ITextSharp

        //public void GeneratePdfFromImagesa(List<string> imagePaths, string outputPath)
        //{
        //    // Create a document with A4 page size
        //    Document document = new  Document(PageSize.A4);

        //    using (var fs = new FileStream(outputPath, FileMode.Create))
        //    {
        //        iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, new FileStream(outputPath, FileMode.Create));
        //        document.Open();

        //        foreach (var imagePath in imagePaths)
        //        {
        //            // Add image to the document
        //            var image = iTextSharp.text.Image.GetInstance(imagePath);
        //            image.ScaleToFit(document.PageSize.Width - document.LeftMargin - document.RightMargin, document.PageSize.Height - document.TopMargin - document.BottomMargin);
        //            image.Alignment = iTextSharp.text.Image.ALIGN_CENTER;
        //            document.Add(image);

        //            // Add text below the image
        //            var phrase = new Phrase("Description or additional information here", new Font(Font.FontFamily.TIMES_ROMAN, 12f, Font.NORMAL, BaseColor.BLACK));
        //            var chunk = new Chunk(phrase);
        //            var paragraph = new iTextSharp.text.Paragraph(chunk);
        //            paragraph.Alignment = Element.ALIGN_CENTER;
        //            document.Add(paragraph);

        //            // Add a new page for the next image
        //            document.NewPage();
        //        }
        //    }

        //    document.Close();
        //}

        public void OnPostITextSharp()
        {
            ImageFiles = GetImagesForPDF();

            string folderName = "GeneratedPDF";
            GeneratePdfFromImages(ImageFiles, folderName);
        }
        public void GeneratePdfFromImages(List<string> imagePaths, string folderName)
        {
            // Create a new document with A4 page size
            Document document = new Document(PageSize.A4);

            // Get the root folder path of the application
            string rootFolder = Path.Combine(_environment.WebRootPath, folderName);

            // Combine the root folder path with the specified folder name
            string outputPath = Path.Combine(rootFolder);

            // Create the directory if it doesn't exist
            Directory.CreateDirectory(outputPath);

            // Generate a unique file name for the PDF
            string fileName = $"GeneratedPDF_{DateTime.Now:yyyyMMddHHmmss}.pdf";

            // Combine the output path with the file name
            string filePath = Path.Combine(outputPath, fileName);

            // Create a PdfWriter instance
            iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, new FileStream(outputPath, FileMode.Create));

            // Open the document
            document.Open();

            // Add each image to the document
            foreach (string imagePath in imagePaths)
            {
                // Create an image instance
                iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(imagePath);

                // Set the image's position and size
                image.SetAbsolutePosition(0, 0);
                image.ScaleToFit(document.PageSize.Width, document.PageSize.Height);

                // Add the image to the document
                document.Add(image);

                // Add a new page
                document.NewPage();
            }

            // Close the document
            document.Close();
        }
		#endregion

		#endregion

	}
}
