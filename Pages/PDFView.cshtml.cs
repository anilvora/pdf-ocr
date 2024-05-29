using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Utilities;
using PdfiumViewer;
using Practice.WebApp.Model;
using SkiaSharp;
using Syncfusion.OCRProcessor;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Exporting;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Parsing;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Tesseract;

namespace Practice.WebApp.Pages
{
	public class PDFViewModel : PageModel
	{
		private readonly IWebHostEnvironment _environment;
		private readonly IConfiguration _configuration;

		public List<FileModel> GeneratedPDFFiles = new();
		public List<FileModel> MergedPDFFiles = new();
		public List<FileModel> EditedPDFFiles = new();


		private static readonly string ROOTFOLDER_GENERATED_PDF = "GeneratedPDF";
		private static readonly string ROOTFOLDER_EDITED_PDF = "EditedPDF";
		private static readonly string ROOTFOLDER_MERGED_PDF = "MergedPDF";
		private static readonly string ROOTFOLDER_UPLOADS = "Uploads";
		private static readonly string SUCCESS_MSG = "Your action is successful.";
		public string OcrText { get; set; } = string.Empty;

		public PDFViewModel(IWebHostEnvironment environment, IConfiguration configuration)
		{
			_environment = environment;
			_configuration = configuration;

		}

		#region Working CODE 
		public IActionResult OnGet()
		{
			string apiKey = _configuration["MySettings:ApiKey"] ?? "";
			string baseUrl = _configuration["MySettings:BaseUrl"] ?? "";

			GeneratedPDFFiles = GetGeneratedPDFFiles();
			EditedPDFFiles = GetEditedPDFFiles();
			MergedPDFFiles = GetMergedPDFFiles();
			return Page();
		}

		#region Get PDF List From Root Directory

		private List<FileModel> GetGeneratedPDFFiles()
		{
			List<FileModel> fileList = new List<FileModel>();
			try
			{
				string rootFolder = Path.Combine(_environment.WebRootPath, ROOTFOLDER_GENERATED_PDF);
				string[] filePaths = Directory.GetFiles(rootFolder, "*.*", SearchOption.TopDirectoryOnly);

				foreach (string filePath in filePaths)
				{
					string fileName = Path.GetFileName(filePath);
					fileList.Add(new FileModel { Id = new Random().Next(1, 100), Name = fileName, ContentType = ROOTFOLDER_GENERATED_PDF });
				}
			}
			catch (Exception)
			{
				return new List<FileModel>();
			}
			_ = fileList.OrderByDescending(f => f.Name);
			return fileList;
		}
		private List<FileModel> GetEditedPDFFiles()
		{
			List<FileModel> fileList = new List<FileModel>();
			try
			{
				string rootFolder = Path.Combine(_environment.WebRootPath, ROOTFOLDER_EDITED_PDF);
				string[] filePaths = Directory.GetFiles(rootFolder, "*.*", SearchOption.TopDirectoryOnly);

				foreach (string filePath in filePaths)
				{
					string fileName = Path.GetFileName(filePath);
					fileList.Add(new FileModel { Id = new Random().Next(1, 100), Name = fileName, ContentType = ROOTFOLDER_EDITED_PDF });
				}
			}
			catch (Exception)
			{
				return new List<FileModel>();
			}
			_ = fileList.OrderByDescending(f => f.Name);
			return fileList;
		}
		private List<FileModel> GetMergedPDFFiles()
		{
			List<FileModel> fileList = new List<FileModel>();
			try
			{
				string rootFolder = Path.Combine(_environment.WebRootPath, ROOTFOLDER_MERGED_PDF);
				string[] filePaths = Directory.GetFiles(rootFolder, "*.*", SearchOption.TopDirectoryOnly);

				foreach (string filePath in filePaths)
				{
					string fileName = Path.GetFileName(filePath);
					fileList.Add(new FileModel { Id = new Random().Next(1, 100), Name = fileName, ContentType = ROOTFOLDER_MERGED_PDF });
				}
			}
			catch (Exception)
			{
				return new List<FileModel>();
			}
			_ = fileList.OrderByDescending(f => f.Name);
			return fileList;
		}

		#endregion

		#region Upload and Convert PDF

		public IActionResult OnPostUploadPDFForView(IFormFile formFile)
		{
			try
			{
				byte[] bytes;
				string rootFolder = Path.Combine(_environment.WebRootPath, ROOTFOLDER_GENERATED_PDF);
				string pdfFileName = $"PDF_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.pdf";

				string desinationPath = Path.Combine(rootFolder, pdfFileName);

				if (formFile == null || formFile.Length == 0)
				{
					return BadRequest("No files were uploaded.");
				}
				if (formFile.Length > 0 && formFile.ContentType.StartsWith("image/"))
				{
					bytes = ConvertTOPDF(formFile);
				}
				else
				{
					using MemoryStream ms = new();
					formFile.CopyTo(ms);
					bytes = ms.ToArray();
				}
				System.IO.File.WriteAllBytes(desinationPath, bytes);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
			GeneratedPDFFiles = GetGeneratedPDFFiles();
			EditedPDFFiles = GetEditedPDFFiles();
			MergedPDFFiles = GetMergedPDFFiles();
			return Page();
		}
		public byte[] ConvertTOPDF(IFormFile formFile)
		{
			byte[] bytes;
			using MemoryStream ms = new MemoryStream();
			using var doc = new Document(PageSize.A4);
			using (var writer = PdfWriter.GetInstance(doc, ms))
			{
				doc.Open();
				iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(formFile.OpenReadStream());
				image.Alignment = Element.ALIGN_CENTER;
				image.ScaleToFit(270f, 270f);
				image.IndentationLeft = 9f;
				image.SpacingAfter = 9f;
				image.BorderWidthTop = 2f;
				doc.Add(image);
				doc.Close();
			}

			bytes = ms.ToArray();

			return bytes;
		}

		#endregion

		#region View Selected PDF

		public IActionResult OnPostViewPDF(string fileName, string folderType)
		{
			try
			{
				if (string.IsNullOrEmpty(fileName))
					return ErrorResult("File not uploaded");

				string fullFilePath = string.Empty;
				string currentDirectory = Directory.GetCurrentDirectory();
				if (!string.IsNullOrEmpty(folderType))
				{
					string folderPath = Path.Combine(_environment.WebRootPath, folderType);
					fullFilePath = Path.Combine(folderPath, fileName);
				}
				else
				{
					string[] fullFilePathArray = Directory.GetFiles(currentDirectory, fileName, SearchOption.AllDirectories);
					fullFilePath = (fullFilePathArray.Length > 0) ? string.Join("", fullFilePathArray[0]) : string.Empty;
				}

				if (!System.IO.File.Exists(fullFilePath))
					return ErrorResult("File not found");

				string viewFileContent = fullFilePath.Substring(_environment.WebRootPath.Length + 1);
				string virtualPath = Url.Content(viewFileContent);
				string serializedString = JsonConvert.SerializeObject(virtualPath);
				return new JsonResult(new { success = true, Data = serializedString, Message = JsonConvert.SerializeObject(SUCCESS_MSG) });
			}
			catch (Exception)
			{
				return ErrorResult("Something went wrong");
			}
		}

		#endregion

		#region Customize PDF - Select Page 

		public IActionResult OnPostCustomizePDF(string fileName, string pageRange)
		{
			try
			{
				string serializedString = string.Empty;
				if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(pageRange))
					return ErrorResult("File or Page range is not provided.");

				string folderPath = Path.Combine(_environment.WebRootPath, Path.GetDirectoryName(fileName) ?? "");
				string selectedFile = Path.Combine(folderPath, Path.GetFileName(fileName));

				if (!System.IO.File.Exists(selectedFile))
					return ErrorResult("File not uploaded");

				string outFileName = $"Edited_{Path.GetFileName(fileName)}";
				string outFileFullDestination = Path.Combine(_environment.WebRootPath, ROOTFOLDER_EDITED_PDF, outFileName);

				bool IsSuccess = CustomizePages(selectedFile, pageRange, outFileFullDestination);
				if (IsSuccess)
				{
					string virtualPath = Url.Content("~/EditedPDF/") + outFileName;
					return SuccessResult(virtualPath);
				}
				else
				{
					return ErrorResult("Error in Page Customization");
				}
			}
			catch (Exception)
			{
				return ErrorResult("Something went wrong");
			}
		}
		public bool CustomizePages(string inputPdf, string pageSelection, string outputPdf)
		{
			try
			{
				using PdfReader reader = new(inputPdf);
				reader.SelectPages(pageSelection);

				using PdfStamper stamper = new(reader, System.IO.File.Create(outputPdf));
				stamper.Close();
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		#endregion

		#region Merge PDF

		public IActionResult OnPostMergePDF(string existingFile, string fileToMerge)
		{
			try
			{
				if (string.IsNullOrEmpty(existingFile) || string.IsNullOrEmpty(fileToMerge))
					return ErrorResult("File not uploaded");

				string existingFilePath = Path.Combine(_environment.WebRootPath, existingFile);
				string mergeFilePath = Path.Combine(_environment.WebRootPath, ROOTFOLDER_MERGED_PDF);
				string uploadFilePath = Path.Combine(_environment.WebRootPath, ROOTFOLDER_UPLOADS);

				string generatedFileName = Path.GetFileNameWithoutExtension(existingFilePath);
				string outFileName = $"Merged_{generatedFileName}_{fileToMerge}.pdf";
				string outFilePath = Path.Combine(mergeFilePath, outFileName);

				string selectFilesToMerge = Directory.GetFiles(uploadFilePath, $"*{fileToMerge}*", SearchOption.AllDirectories).FirstOrDefault() ?? "";

				if (!System.IO.File.Exists(selectFilesToMerge) || !System.IO.File.Exists(existingFilePath))
					return ErrorResult("File Not Found");

				List<string> mergedPdfs = new() { existingFilePath, selectFilesToMerge };
				using (var stream = new FileStream(outFilePath, FileMode.Create))
				{
					var writer = new iText.Kernel.Pdf.PdfWriter(stream);
					var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
					var document = new iText.Layout.Document(pdf);

					foreach (var pdfPath in mergedPdfs)
					{
						var reader = new iText.Kernel.Pdf.PdfReader(pdfPath);
						var sourceDocument = new iText.Kernel.Pdf.PdfDocument(reader);
						sourceDocument.CopyPagesTo(1, sourceDocument.GetNumberOfPages(), pdf);
						reader.Close();
					}
					document.Close();
				}
				string virtualPath = Url.Content($"~/MergedPDF/{outFileName}");
				return SuccessResult(virtualPath);
			}
			catch (Exception)
			{
				return ErrorResult("Something went wrong");
			}
		}

		#endregion

		#region Add New Page At Page Number

		public IActionResult OnPostMergeAtPageNumber(string existingFile, string newPdfFile, int pageNumber)
		{
			try
			{
				if (string.IsNullOrEmpty(existingFile) || string.IsNullOrEmpty(newPdfFile) || pageNumber <= 0)
					return ErrorResult("Invalid inputs.");

				string existingFilePath = Path.Combine(_environment.WebRootPath, existingFile);
				string mergeFileFolderPath = Path.Combine(_environment.WebRootPath, ROOTFOLDER_MERGED_PDF);
				string uploadFileFolderPath = Path.Combine(_environment.WebRootPath, ROOTFOLDER_UPLOADS);

				string generatedFileName = Path.GetFileNameWithoutExtension(existingFilePath);
				string outFileName = $"MergedPages_{generatedFileName}_{newPdfFile}";
				string outFilePath = Path.Combine(mergeFileFolderPath, outFileName);

				string selectFilesToMerge = Directory.GetFiles(uploadFileFolderPath, $"*{newPdfFile}*", SearchOption.AllDirectories).FirstOrDefault() ?? string.Empty;


				if (!System.IO.File.Exists(selectFilesToMerge) || !System.IO.File.Exists(existingFilePath))
					return ErrorResult("File Not Found.");

				List<string> mergedPdfs = new() { existingFilePath, selectFilesToMerge };

				using (FileStream outputStream = new(outFilePath, FileMode.Create, FileAccess.Write))
				{
					using Document document = new Document();
					{
						using PdfCopy copy = new PdfCopy(document, outputStream);
						{
							document.Open();

							using PdfReader existingReader = new PdfReader(existingFilePath);
							{
								if (existingReader.NumberOfPages == 0)
									return ErrorResult("The existing PDF file has no pages.");

								using PdfReader newReader = new PdfReader(selectFilesToMerge);
								{
									if (newReader.NumberOfPages == 0)
										return ErrorResult("The new PDF file has no pages to Add");

									// Loop through the existing document and insert new page at specified position
									for (int i = 1; i <= existingReader.NumberOfPages; i++)
									{
										// Insert new page at specified position
										if (i == pageNumber)
										{
											copy.AddDocument(newReader);
										}
										copy.AddPage(copy.GetImportedPage(existingReader, i));
									}
								}
							}
						}
					}
				}
				string virtualPath = Url.Content($"~/MergedPDF/{outFileName}");
				return SuccessResult(virtualPath);
			}
			catch (Exception)
			{
				return ErrorResult("Something went wrong");
			}
		}

		#endregion

		#region Add WaterMark

		public IActionResult OnPostAddWatermark(string existingFile, string watermarText, string watermarkImg)
		{
			try
			{
				string existingFilePath = string.Empty;
				bool successImgWatermark = false;
				bool successTextWatermark = false;


				if (string.IsNullOrEmpty(existingFile))
					return ErrorResult("File not uploaded");

				existingFilePath = Path.Combine(_environment.WebRootPath, existingFile);

				if (!System.IO.File.Exists(existingFilePath))
					return ErrorResult("File not found");

				if (string.IsNullOrEmpty(watermarText))
					return ErrorResult("Watermark text is empty");

				successTextWatermark = TextWatermark(existingFilePath, watermarText);

				if (string.IsNullOrEmpty(watermarkImg))
					return ErrorResult("Watermark image is empty");

				string uploadFolder = Path.Combine(_environment.WebRootPath, ROOTFOLDER_UPLOADS);
				string watermarkImagePath = Directory.GetFiles(uploadFolder, $"*{watermarkImg}*", SearchOption.AllDirectories).FirstOrDefault() ?? string.Empty;

				if (!System.IO.File.Exists(watermarkImagePath))
					return ErrorResult("Watermark image not found");

				successImgWatermark = ImgWatermark(existingFilePath, watermarkImagePath);

				if (successImgWatermark || successTextWatermark)
				{
					string viewFileContent = existingFilePath.Substring(_environment.WebRootPath.Length + 1);
					string virtualPath = Url.Content(viewFileContent);
					return SuccessResult(virtualPath);
				}
				else
				{
					return ErrorResult("Error in add Watermark");
				}
			}
			catch
			{
				return ErrorResult("Something went wrong");
			}
		}
		public bool TextWatermark(string inputFile, string watermarkText)
		{
			bool success = false;
			try
			{
				string tempFilePath = Path.GetTempFileName();

				using (var reader = new PdfReader(inputFile))
				{
					using (var output = new FileStream(tempFilePath, FileMode.Create))
					{
						using (var stamper = new PdfStamper(reader, output))
						{
							int pageCount = reader.NumberOfPages;
							for (int i = 1; i <= pageCount; i++)
							{
								// Get the content and dimensions of each page
								PdfContentByte content = stamper.GetOverContent(i);
								iTextSharp.text.Rectangle pageSize = reader.GetPageSize(i);

								//float fontSize = pageSize.Width / (watermarkText.Length * 2);
								//float fontSize = Math.Min(pageSize.Width, pageSize.Height) / (float)(watermarkText.Length); // Adjust divisor as needed
								//float fontSize = 2f; // Adjust the divisor as needed
								//float fontSize = (pageSize.Width + pageSize.Height) / 2; // Adjust the divisor as needed
								//float fontSize = pageSize.Width * 1f;  // Adjust the divisor as needed

								float fontSize1 = Math.Min(pageSize.Width, pageSize.Height) * 0.5f;

								float diagonal = (float)Math.Sqrt(pageSize.Width * pageSize.Width + pageSize.Height * pageSize.Height);
								float fontSize = diagonal * 0.03f; // Adjust the multiplier as needed

								// Create a new font for the watermark
								iTextSharp.text.pdf.BaseFont baseFont = iTextSharp.text.pdf.BaseFont.CreateFont(iTextSharp.text.pdf.BaseFont.HELVETICA, iTextSharp.text.pdf.BaseFont.CP1252, iTextSharp.text.pdf.BaseFont.NOT_EMBEDDED);
								content.SetColorFill(new BaseColor(128, 128, 128, 150));
								content.BeginText();
								content.SetFontAndSize(baseFont, fontSize);

								// Add watermark text at the center of the page
								//ColumnText.ShowTextAligned(content, iTextSharp.text.Element.ALIGN_CENTER, new Phrase(watermarkText), pageSize.Width / 2, pageSize.Height / 2, 45);

								ColumnText.ShowTextAligned(content, iTextSharp.text.Element.ALIGN_CENTER, new Phrase(watermarkText), pageSize.Width / 2, pageSize.Height - (pageSize.Height * 0.05f), 0); // Adjust the Y position as needed

								content.EndText();
							}
						}
					}
				}
				System.IO.File.Delete(inputFile);
				System.IO.File.Move(tempFilePath, inputFile);
				success = true;
			}
			catch (Exception ex)
			{
				var eee = ex.Message;
			}
			return success;
		}
		public bool ImgWatermark(string inputFile, string watermarkImagePath)
		{
			bool success = false;
			try
			{
				string tempFilePath = Path.GetTempFileName();

				iTextSharp.text.Image watermarkImage = iTextSharp.text.Image.GetInstance(watermarkImagePath);

				// Create a reader for the existing PDF
				using (iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(inputFile))
				{
					// Create a stamper to modify the existing PDF
					using (iTextSharp.text.pdf.PdfStamper stamper = new iTextSharp.text.pdf.PdfStamper(reader, new FileStream(tempFilePath, FileMode.Create)))
					{
						// Iterate through each page and add the watermark behind the existing content
						for (int i = 1; i <= reader.NumberOfPages; i++)
						{
							// Get the page size
							iTextSharp.text.Rectangle pageSize = reader.GetPageSizeWithRotation(i);

							//PdfContentByte contentByte = stamper.GetUnderContent(i);    //--- For add water mark under the content (behind the Img)
							iTextSharp.text.pdf.PdfContentByte contentByte = stamper.GetOverContent(i);       //--  For add water mark over the content (Over the Img)

							// Add the watermark image to the page
							iTextSharp.text.pdf.PdfTemplate template = contentByte.CreateTemplate(pageSize.Width, pageSize.Height);

							float watermarkWidth = pageSize.Width * 0.50f; // 50% width of the page  -- Exp: 0.20f = 20%, 0.50f = 50%, 1.0f = 100% ...
							float watermarkHeight = watermarkWidth * (watermarkImage.Height / watermarkImage.Width);

							// -- Center
							float xPosition = (pageSize.Width - watermarkWidth) / 2; // Center horizontally
							float yPosition = (pageSize.Height - watermarkHeight) / 2; // Center vertically

							// -- Top
							//float topXPosition = (pageSize.Width - watermarkWidth) / 2; // Center horizontally
							//float topYPosition = pageSize.Height - watermarkHeight - 20; // 20 pixels from top margin

							// -- Bottom
							//float bottomXPosition = (pageSize.Width - watermarkWidth) / 2; // Center horizontally
							//float bottomYPosition = 20; // 20 pixels from bottom margin

							// -- Top-left
							//float topLeftXPosition = 50; // 20 pixels from left margin
							//float topLeftYPosition = pageSize.Height - watermarkHeight - 20;

							// -- Top-right
							//float topRightXPosition = pageSize.Width - watermarkWidth - 20; // 20 pixels from right margin
							//float topRightYPosition = pageSize.Height - watermarkHeight - 20; // 20 pixels from top margin


							// -- Bottom-left
							//float bottomLeftXPosition = 20; // 20 pixels from left margin
							//float bottomLeftYPosition = 20; // 20 pixels from bottom margin

							// -- Bottom-right
							//float bottomRightXPosition = pageSize.Width - watermarkWidth - 60; // 60 pixels from right margin
							//float bottomRightYPosition = 60; // 60 pixels from bottom margin

							// Set the transparency for the watermark image
							iTextSharp.text.pdf.PdfGState gstate = new()
							{
								FillOpacity = 0.25f // 25%  Set opacity as (opacity / 100)f. Exp: 0.20f = 20%, 0.50f = 50%, 1.0f = 100% ...
							};
							contentByte.SetGState(gstate);

							// Add the watermark image to the page
							watermarkImage.ScaleToFit(watermarkWidth, watermarkHeight);
							watermarkImage.SetAbsolutePosition(xPosition, yPosition);
							contentByte.AddImage(watermarkImage);
						}
					}
				}
				// Replace the original PDF file with the modified PDF
				System.IO.File.Delete(inputFile);
				System.IO.File.Move(tempFilePath, inputFile);
				success = true;
			}
			catch (Exception ex)
			{
				var eee = ex.Message;
			}
			return success;
		}

		#endregion

		#region PDF To OCR

		public async Task<IActionResult> OnPostTestOCR(IFormFile formFile)
		{
			//FileStream fileStream = new FileStream(existingFilePath, FileMode.Open, FileAccess.Read);

			// Create a new instance of FormFile
			var filePath = Path.Combine(_environment.WebRootPath, ROOTFOLDER_GENERATED_PDF, formFile.FileName);

			if (formFile != null && formFile.Length > 0)
			{
				try
				{
					using (var pdfStream = new MemoryStream())
					{
						await formFile.CopyToAsync(pdfStream);

						byte[] pdfBytes = pdfStream.ToArray();

						// Extract text from PDF using iTextSharp
						//string pdfText = ExtractTextFromPdf(stream);

						// Perform OCR on any image-based text using Tesseract
						var ocrFileStream = SyncPerformOCR(filePath, formFile.FileName);

						// Combine extracted text from iTextSharp and OCR
						//OcrText = $"{pdfText}\n\n=== OCR Result ===\n{ocrText}";

						return ocrFileStream;
					}
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"An error occurred: {ex.Message}");
				}
			}

			return Page();
		}

		public IActionResult SyncPerformOCR(string docPath, string fileName)
		{
			try
			{
				//string docPath = Path.GetFullPath("../../Data/Input.pdf");
				//Initialize the OCR processor by providing the path of tesseract binaries(SyncfusionTesseract.dll and liblept168.dll).
				using (Syncfusion.OCRProcessor.OCRProcessor processor = new())
				{
					FileStream fileStream = new FileStream(docPath, FileMode.Open, FileAccess.Read);
					//Load a PDF document.
					PdfLoadedDocument lDoc = new PdfLoadedDocument(fileStream);
					//Set OCR language to process.
					processor.Settings.Language = Syncfusion.OCRProcessor.Languages.English;
					//Process OCR by providing the PDF document and Tesseract data.
					processor.PerformOCR(lDoc);
					//Create memory stream.
					MemoryStream stream = new MemoryStream();
					//Save the document to memory stream.
					lDoc.Save(stream);
					lDoc.Close();
					//Set the position as '0'
					stream.Position = 0;

					//string pdfText = ExtractTextFromPdf(stream);    // -- Read OCR Text From PDF

					byte[] ocrBytes = stream.ToArray();

					//Download the PDF document in the browser.
					FileStreamResult fileStreamResult = new FileStreamResult(stream, "application/pdf");
					fileStreamResult.FileDownloadName = $@"OCR_{fileName}";
					return fileStreamResult;
				}
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", $"An error occurred: {ex.Message}");
			}
			return null;
		}


		private string ExtractTextFromPdf(Stream pdfStream)
		{
			pdfStream.Seek(0, SeekOrigin.Begin);

			using (var pdfReader = new iText.Kernel.Pdf.PdfReader(pdfStream))
			{
				using (var pdfDocument = new iText.Kernel.Pdf.PdfDocument(pdfReader))
				{
					var pageCount = pdfDocument.GetNumberOfPages();
					var extractedText = "";

					for (int i = 1; i <= pageCount; i++)
					{
						var page = pdfDocument.GetPage(i);
						extractedText += PdfTextExtractor.GetTextFromPage(page).Trim() + "\n";
					}
					return extractedText;
				}
			}

		}


		//-- Multiple in File Result 

		public async Task<IActionResult> OnPostTestOCRMultiple(IFormFile formFile)
		{
			//FileStream fileStream = new FileStream(existingFilePath, FileMode.Open, FileAccess.Read);

			// Create a new instance of FormFile
			var filePath = Path.Combine(_environment.WebRootPath, ROOTFOLDER_GENERATED_PDF, formFile.FileName);

			if (formFile != null && formFile.Length > 0)
			{
				try
				{
					using (var pdfStream = new MemoryStream())
					{
						await formFile.CopyToAsync(pdfStream);

						byte[] pdfBytes = pdfStream.ToArray();

						// Extract text from PDF using iTextSharp
						//string pdfText = ExtractTextFromPdf(stream);

						// Perform OCR on any image-based text using Tesseract
						var ocrBytesList = SyncPerformOCRMultiple(filePath);

						//byte[] concatenatedBytes = ocrBytesList.SelectMany(bytes => bytes).ToArray();

						//// Create a memory stream from the concatenated byte array
						//using (var memoryStream = new MemoryStream(concatenatedBytes))
						//{
						//	// Reset memory stream position
						//	memoryStream.Position = 0;

						//	// Return the memory stream as a file stream result
						//	return File(memoryStream, "application/octet-stream", "outputfile.pdf");
						//}
						return ocrBytesList;
					}
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"An error occurred: {ex.Message}");
				}
			}

			return Page();
		}
		public IActionResult SyncPerformOCRMultiple(string docPath)
		{
			var ocrBytesList = new List<byte[]>(); // List to store OCR bytes for each page

			using (var processor = new OCRProcessor()) // Assuming OCRProcessor is defined
			{
				using (var fileStream = new FileStream(docPath, FileMode.Open, FileAccess.Read))
				{
					using (var imageConverter = new Syncfusion.PdfToImageConverter.PdfToImageConverter())
					{
						imageConverter.Load(fileStream);

						// Loop through each page of the PDF document
						for (int i = 0; i < imageConverter.PageCount; i++)
						{
							using (var imageStream = imageConverter.Convert(i, true, true)) // Convert specific page
							{
								// Perform OCR on the current page image
								processor.Settings.Language = Languages.English; // Set OCR language
								var ocrText = processor.PerformOCR(imageStream, processor.TessDataPath);

								// Convert OCR text to bytes (assuming specific encoding is needed)
								var encoding = Encoding.UTF8; // Adjust encoding as needed
								var ocrBytes = encoding.GetBytes(ocrText);

								ocrBytesList.Add(ocrBytes);
							}
						}
					}
					FileStreamResult fileStreamResult = new FileStreamResult(fileStream, "application/pdf");
					fileStreamResult.FileDownloadName = $@"OCRM_{Path.GetFileName(docPath)}";
					return fileStreamResult;
				}
			}

			// Concatenate OCR byte arrays into a single byte array
			//byte[] concatenatedBytes = ocrBytesList.SelectMany(bytes => bytes).ToArray();

			// Create a memory stream from the concatenated byte array
			//using (var memoryStream = new MemoryStream(concatenatedBytes))
			//{
			//	// Reset memory stream position
			//	memoryStream.Position = 0;

			//	// Return the memory stream as a file stream result
			//	FileStreamResult fileStreamResult = new FileStreamResult(fileStream, "application/pdf");
			//	fileStreamResult.FileDownloadName = $@"OCR_{Path.GetFileName(docPath)}";
			//	return fileStreamResult;
			//}
		}


		// -- END


		/// -- OCR Approaches 
		public string OCR_T1(string returnText)
		{
			string tessDataPath = Path.Combine(_environment.WebRootPath, "Tesseract", "tessdata");
			try
			{

				// Initialize Tesseract engine and perform OCR on extracted text
				using (var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default))
				{
					using (var memoryStream = new MemoryStream())
					{
						// Convert extracted text to byte array and load as TIFF image
						using (var writer = new StreamWriter(memoryStream))
						{
							writer.Write(returnText);
							writer.Flush();
							memoryStream.Position = 0;

							// Load the image for OCR processing
							//using (var image = new Pix(memoryStream.ToArray()))
							//{
							//	// Process the image using Tesseract
							//	using (var page = engine.ProcessText(image))
							//	{
							//		returnText = page.GetText();
							//	}
							//}
						}
					}
				}
			}
			catch (Exception ex)
			{
				// Handle exceptions
				var msg = ex.Message;
			}

			return returnText;

		}

		public string OCR_T2(string extractedText)
		{
			string ocrText = string.Empty;
			string tessDataPath = Path.Combine(_environment.WebRootPath, "Tesseract", "tessdata");

			try
			{
				using (var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default))
				{
					if (engine != null)
					{
						using (var page = engine.Process(null, extractedText))
						{
							ocrText = page.GetText();
						}
					}
					else
					{
						Console.WriteLine("Tesseract engine initialization failed.");
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
			}
			return ocrText;
		}

		public string OCR_T3(string pdfFilePath)
		{
			// Path to the Tesseract data directory
			string tessDataPath = Path.Combine(_environment.WebRootPath, "Tesseract", "tessdata");

			try
			{
				using (var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default))
				{
					using (PdfiumViewer.PdfDocument pdfDocument = PdfiumViewer.PdfDocument.Load(pdfFilePath))
					{
						string ocrText = "";

						for (int i = 0; i < pdfDocument.PageCount; i++)
						{
							using (System.Drawing.Bitmap bitmap = (System.Drawing.Bitmap)pdfDocument.Render(i, 300, 300, true))
							{
								using (Pix pix = PixConverter.ToPix(bitmap))
								{
									using (Tesseract.Page page = engine.Process(pix))
									{
										ocrText += page.GetText();
									}
								}
							}
						}

						return ocrText;
					}

				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return null;
			}

		}

		public string OCR_T4(string pdfFilePath)
		{
			// Path to the Tesseract data directory
			string tessDataPath = Path.Combine(_environment.WebRootPath, "Tesseract", "tessdata");

			try
			{
				using (var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default))
				{

					using (PdfiumViewer.PdfDocument pdfDocument = PdfiumViewer.PdfDocument.Load(pdfFilePath))
					{
						string ocrText = "";

						for (int i = 0; i < pdfDocument.PageCount; i++)
						{
							using (System.Drawing.Bitmap bitmap = (System.Drawing.Bitmap)pdfDocument.Render(i, 300, 300, true))
							{
								using (Pix pix = PixConverter.ToPix(bitmap))
								{
									using (Tesseract.Page page = engine.Process(pix))
									{
										ocrText += page.GetText();
									}
								}

								// Create new bitmap with desired format
								//System.Drawing.Bitmap newBitmap = new System.Drawing.Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
								//using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(newBitmap))
								//{
								//	// Draw original bitmap onto new bitmap with desired format
								//	g.DrawImage(bitmap, new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height));
								//}

								//// Convert new bitmap to Tesseract Pix
								//using (Pix pix = PixConverter.ToPix(newBitmap))
								//{
								//	using (Tesseract.Page page = engine.Process(pix))
								//	{
								//		ocrText += page.GetText();
								//	}
								//}

							}
						}

						return ocrText;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return "";
			}
		}

		private string OCR_T5(Stream pdfStream, string filePath)
		{
			string ocrText = string.Empty;
			string tessDataPath = Path.Combine(_environment.WebRootPath, "Tesseract", "tessdata");
			try
			{
				using (var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default))
				{


					using (var memoryStream = new MemoryStream())
					{
						pdfStream.CopyTo(memoryStream);
						memoryStream.Position = 0;

						MemoryStream memStream = new MemoryStream();
						var ff = Pix.LoadFromFile(filePath);
						Pix ImagefromMemory = Pix.LoadFromFile(filePath);


						// Load the image for OCR processing
						using (var image = Pix.LoadFromMemory(memoryStream.ToArray()))
						{
							// Process the image using Tesseract
							using (var page = engine.Process(image))
							{
								ocrText = page.GetText();
							}
						}

						//using (var image = new System.Drawing.Bitmap(memoryStream))
						//{
						//	using (var pix = PixConverter.ToPix(image))
						//	{
						//		using (var page = engine.Process(pix))
						//		{
						//			var ttt = String.Format("{0:P}", page.GetMeanConfidence());
						//			ocrText = page.GetText();
						//		}
						//	}
						//}

					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return "";
			}
			return ocrText;
		}

		private string OCR_T6(Stream stream, string pdfFilePath)
		{
			string ocrText = string.Empty;
			string tessDataPath = Path.Combine(_environment.WebRootPath, "Tesseract", "tessdata");


			try
			{
				var test = ExtractImages(pdfFilePath);
				stream.Seek(0, SeekOrigin.Begin);
				using (iTextSharp.text.pdf.PdfReader reader = new(pdfFilePath))
				{
					// Create Tesseract engine
					using (TesseractEngine engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default))
					{
						// Iterate over each page in the PDF document
						for (int i = 1; i <= reader.NumberOfPages; i++)
						{
							// Render the PDF page as an image
							using (System.Drawing.Bitmap image = RenderPdfPageAsImage(reader, i))
							{
								// Perform OCR on the image
								ocrText = PerformOCROperation(engine, image);

								// Do something with the OCR text
								return ocrText;
							}
						}
					}
				}
			}
			catch (Exception)
			{

				throw;
			}
			return ocrText;
		}
		// Render PDF page as an image
		static Bitmap RenderPdfPageAsImage(PdfReader reader, int pageNumber)
		{
			PdfDictionary page = reader.GetPageN(pageNumber);

			if (page != null)
			{
				PdfDictionary resources = page.GetAsDict(PdfName.RESOURCES);

				if (resources != null)
				{
					PdfDictionary xObject = resources.GetAsDict(PdfName.XOBJECT);

					if (xObject != null)
					{
						foreach (PdfName key in xObject.Keys)
						{
							PdfObject obj = xObject.GetDirectObject(key);
							if (obj is PdfStream stream && stream.Get(PdfName.SUBTYPE).Equals(PdfName.IMAGE))
							{
								byte[] bytes = stream.GetBytes();
								using (MemoryStream ms = new MemoryStream(bytes))
								{
									return new Bitmap(ms);
								}
							}
						}
					}
				}
			}

			return null;
		}

		public static List<iTextSharp.text.Image> ExtractImages(string pdfPath)
		{
			List<iTextSharp.text.Image> images = new();
			PdfReader reader = new PdfReader(pdfPath);

			for (int i = 1; i <= reader.NumberOfPages; i++)
			{
				PdfDictionary page = reader.GetPageN(i);
				PdfDictionary resources = page.GetAsDict(PdfName.RESOURCES);

				if (resources != null)
				{
					PdfDictionary xObject = resources.GetAsDict(PdfName.XOBJECT);

					if (xObject != null)
					{
						foreach (PdfName key in xObject.Keys)
						{
							PdfObject obj = xObject.GetDirectObject(key);

							if (obj is PdfStream stream)
							{
								try
								{
									PdfName subtype = stream.GetAsName(PdfName.SUBTYPE);
									if (subtype.Equals(PdfName.IMAGE))
									{
										byte[] imageData = stream.GetBytes();
										iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(imageData);
										images.Add(image);
									}
								}
								catch (Exception)
								{
									// Handle potential exceptions during image processing
								}
							}
						}
					}
				}
			}

			reader.Close();
			return images;
		}


		// Perform OCR on the image
		static string PerformOCROperation(TesseractEngine engine, Bitmap image)
		{
			if (image is null)
			{
				return string.Empty;
			}
			using (Tesseract.Page page = engine.Process(image))
			{
				return page.GetText();
			}
		}
		#endregion


		#region Syncfusion OCR 

		public IActionResult OnPostPerformOCR(string fileName)
		{
			try
			{
				if (string.IsNullOrEmpty(fileName))
					return ErrorResult("File is not provided.");

				string folderPath = Path.Combine(_environment.WebRootPath, Path.GetDirectoryName(fileName) ?? "");
				string selectedFile = Path.Combine(folderPath, Path.GetFileName(fileName));

				if (!System.IO.File.Exists(selectedFile))
					return ErrorResult("File not found");

				//var resultStream = PerformOcrAndGetBytesForMultiplePages(selectedFile);
				var resultStream = OCRFinal(selectedFile);
				
				if (resultStream is not null)
				{
					// Correctly concatenate base64 strings
					//string pdfData = "";
					//foreach (byte[] pageData in resultStream)	
					//{
					//	string base64String = Convert.ToBase64String(pageData);
					//	pdfData += base64String;
					//}

					// Build the data URI
					//string dataUri = "data:application/pdf;base64," + pdfData;

					// Address escaping if using server-side rendering
					//string unescapedDataUri = (yourFrameworkContext.GetHtmlHelper()).Raw(dataUri); // Replace with relevant syntax for your framework


					byte[] combinedPdfData = resultStream.SelectMany(x => x).ToArray();
					string base64String = Convert.ToBase64String(combinedPdfData);

					// -- 1 
					//string serializedBytes = JsonConvert.SerializeObject(resultStream);
					return new JsonResult(new { success = true, Data = base64String, Message = JsonConvert.SerializeObject(SUCCESS_MSG) });
				}
				else
					return ErrorResult("Error in perform OCR");
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", $"An error occurred: {ex.Message}");
				return ErrorResult("Something went wrong");
			}
		}

		public byte[] PerformOCRSyncfusion(string docPath)
		{
			try
			{
				//Initialize the OCR processor by providing the path of tesseract binaries(SyncfusionTesseract.dll and liblept168.dll).
				using (Syncfusion.OCRProcessor.OCRProcessor processor = new())
				{
					FileStream fileStream = new FileStream(docPath, FileMode.Open, FileAccess.Read);
					//Load a PDF document.
					PdfLoadedDocument lDoc = new PdfLoadedDocument(fileStream);
					//Set OCR language to process.
					processor.Settings.Language = Syncfusion.OCRProcessor.Languages.English;

					//Process OCR by providing the PDF document and Tesseract data.
					processor.PerformOCR(lDoc);
					//Create memory stream.
					MemoryStream stream = new MemoryStream();
					//Save the document to memory stream.
					lDoc.Save(stream);
					lDoc.Close();
					//Set the position as '0'
					stream.Position = 0;

					byte[] ocrBytes = stream.ToArray();


					//string pdfText = ExtractTextFromPdf(stream);    // -- Read OCR Text From PDF

					//Download the PDF document in the browser.
					FileStreamResult fileStreamResult = new FileStreamResult(stream, "application/pdf");
					fileStreamResult.FileDownloadName = $@"OCR_";
					//return fileStreamResult;


					return ocrBytes;
				}
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("PerformOCR", $"An error occurred: {ex.Message}");
			}
			return null;
		}

		public List<byte[]> PerformOcrAndGetBytesForPages(string docPath)
		{
			List<byte[]> ocrBytesList = new List<byte[]>();

			try
			{
				// Initialize OCR processor
				using (Syncfusion.OCRProcessor.OCRProcessor processor = new Syncfusion.OCRProcessor.OCRProcessor())
				{
					using (FileStream fileStream = new FileStream(docPath, FileMode.Open, FileAccess.Read))
					{
						// Load PDF document
						using (PdfLoadedDocument lDoc = new PdfLoadedDocument(fileStream))
						{
							// Set OCR language to process
							processor.Settings.Language = Syncfusion.OCRProcessor.Languages.English;

							// Loop through each page of the PDF document
							for (int i = 0; i < lDoc.Pages.Count; i++)
							{
								// Get the current page
								Syncfusion.Pdf.PdfPageBase page = lDoc.Pages[i];

								// Create memory stream to save the OCR processed page
								using (MemoryStream stream = new MemoryStream())
								{
									// Perform OCR on the page
									processor.PerformOCR(stream);

									// Save the OCR processed page to memory stream
									//page.Document.Save(stream);

									// Set the position to the beginning of the stream
									stream.Position = 0;

									// Convert the OCR processed page to byte array
									byte[] ocrBytes = stream.ToArray();

									// Add the byte array to the list
									ocrBytesList.Add(ocrBytes);
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				// Handle exceptions here
				Console.WriteLine($"Error: {ex.Message}");
			}


			return ocrBytesList;
		}

		public List<byte[]> PerformOcrAndGetBytesForMultiplePages(string docPath)
		{
			// Initialize OCR processor
			using (Syncfusion.OCRProcessor.OCRProcessor processor = new())
			{
				using (FileStream fileStream = new FileStream(docPath, FileMode.Open, FileAccess.Read))
				using (PdfLoadedDocument lDoc = new PdfLoadedDocument(fileStream))
				{
					// Set OCR language to process
					processor.Settings.Language = Syncfusion.OCRProcessor.Languages.English;

					List<byte[]> ocrBytesList = new List<byte[]>();  // List to store OCR bytes for each page

					// Loop through each page of the PDF document
					for (int i = 0; i < lDoc.Pages.Count; i++)
					{
						PdfRenderer pdfExportImage = new();

						using (MemoryStream stream = new MemoryStream())
						{
							//string tempImagePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".png");


							// Convert page to a compatible format (adjust method based on documentation)
							//var Image = page.ExportAsImage(stream, ImageFormat.Png);  // Or ExportAsJpeg, etc. (check documentation)

							//Bitmap bitmap = page.Expo (0, page.Size.Width, page.Size.Height);


							//Bitmap[] bitmapimage = pdfExportImage.ExportAsImage(0, pdfExportImage.PageCount - 1, 200, 200);


							stream.Position = 0;
							processor.PerformOCR(stream);

							// Extract OCR processed bytes from the stream
							byte[] ocrBytes = stream.ToArray();
							ocrBytesList.Add(ocrBytes);  // Add bytes for this page to the list
						}
					}

					return ocrBytesList;  // Return the list of OCR bytes for all pages
				}
			}
		}
		//public List<byte[]> ZZZZZZ(string docPath)
		//{
		//	using (Syncfusion.OCRProcessor.OCRProcessor processor = new Syncfusion.OCRProcessor.OCRProcessor())
		//	{
		//		Syncfusion.PdfToImageConverter.PdfToImageConverter imageConverter = new Syncfusion.PdfToImageConverter.PdfToImageConverter();

		//		using (FileStream fileStream = new FileStream(docPath, FileMode.Open, FileAccess.Read))
		//		{
		//			// Load PDF document
		//			PdfLoadedDocument lDoc = new PdfLoadedDocument(fileStream);

		//			// Set OCR language to process
		//			processor.Settings.Language = Syncfusion.OCRProcessor.Languages.English;

		//			List<byte[]> ocrBytesList = new List<byte[]>();  // List to store OCR bytes for each page

		//			// Loop through each page of the PDF document
		//			for (int i = 0; i < lDoc.Pages.Count; i++)
		//			{
		//				// Get the current page
		//				PdfPageBase page = lDoc.Pages[i];

		//				// Export the page as an image
		//				//Bitmap image = page.ExportAsImage();
		//				Bitmap image = page.ExportAsImage();

		//				using (MemoryStream stream = new MemoryStream())
		//				{
		//					// Save the image to the memory stream
		//					image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

		//					// Set the position to the beginning of the stream
		//					stream.Position = 0;

		//					// Perform OCR on the page
		//					processor.PerformOCR(stream);

		//					// Extract OCR processed bytes from the stream
		//					byte[] ocrBytes = stream.ToArray();
		//					ocrBytesList.Add(ocrBytes);  // Add bytes for this page to the list
		//				}

		//				// Dispose of the image
		//				image.Dispose();
		//			}

		//			// Dispose of the PDF document after processing all pages
		//			lDoc.Dispose();

		//			return ocrBytesList;  // Return the list of OCR bytes for all pages
		//		}
		//	}
		//}

		public List<byte[]> FinalTest(string docPath)
		{

			PdfiumViewer.PdfDocument document = PdfiumViewer.PdfDocument.Load(docPath);

			// Render the page to a bitmap
			//Bitmap image = page.Render((int)page.Width, (int)page.Height);


			// Initialize OCR processor
			//using (Syncfusion.OCRProcessor.OCRProcessor processor = new Syncfusion.OCRProcessor.OCRProcessor())
			//{
			//	Syncfusion.PdfToImageConverter.PdfToImageConverter imageConverter = new Syncfusion.PdfToImageConverter.PdfToImageConverter();

			//	using (FileStream fileStream = new FileStream(docPath, FileMode.Open, FileAccess.Read))
			//	{
			//		using (PdfLoadedDocument lDoc = new PdfLoadedDocument(fileStream))
			//		{
			//			// Set OCR language to process
			//			processor.Settings.Language = Syncfusion.OCRProcessor.Languages.English;

			//			List<byte[]> ocrBytesList = new List<byte[]>(); // List to store OCR bytes for each page

			//			// Loop through each page of the PDF document
			//			for (int i = 0; i < lDoc.Pages.Count; i++)
			//			{
			//				// Get the current page
			//				Syncfusion.Pdf.PdfPage page = (Syncfusion.Pdf.PdfPage)lDoc.Pages[i]; // Use Syncfusion.Pdf.PdfPage

			//				// Export the page as an image
			//				Bitmap image1 = page.ExportAsImage();
			//				//PdfBitmap pdfBitmap = new PdfBitmap(image1);

			//				using (MemoryStream stream = new MemoryStream())
			//				{
			//					//******
			//					image1.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

			//					// Set the position to the beginning of the stream
			//					stream.Position = 0;

			//					// Perform OCR on the page
			//					processor.PerformOCR(stream);

			//					// Extract OCR processed bytes from the stream
			//					byte[] ocrBytes = stream.ToArray();
			//					ocrBytesList.Add(ocrBytes);

			//					// Add bytes for this page to the list
			//					//****


			//				}
			//			}

			//			return ocrBytesList; // Return the list of OCR bytes for all pages
			//		}
			//	}
			//}

			var ocrBytesList = new List<byte[]>(); // List to store OCR bytes for each page

			using (var processor = new OCRProcessor()) // Assuming OCRProcessor is defined
			{
				using (var fileStream = new FileStream(docPath, FileMode.Open, FileAccess.Read))
				{
					using (var imageConverter = new Syncfusion.PdfToImageConverter.PdfToImageConverter())
					{
						imageConverter.Load(fileStream);

						// Loop through each page of the PDF document
						for (int i = 0; i < imageConverter.PageCount; i++)
						{
							using (var imageStream = imageConverter.Convert(i, true, true)) // Convert specific page
							{
								// Perform OCR on the current page image
								processor.Settings.Language = Syncfusion.OCRProcessor.Languages.English;  // Set OCR language
								var ocrText = processor.PerformOCR(imageStream, processor.TessDataPath);

								// Convert OCR text to bytes (assuming specific encoding is needed)
								var encoding = Encoding.UTF8; // Adjust encoding as needed
								var ocrBytes = encoding.GetBytes(ocrText);
								

								ocrBytesList.Add(ocrBytes);
							}
						}

						return ocrBytesList;	
					}
				}
			}





			//using (Syncfusion.OCRProcessor.OCRProcessor processor = new Syncfusion.OCRProcessor.OCRProcessor())
			//{
			//	Syncfusion.PdfToImageConverter.PdfToImageConverter imageConverter = new Syncfusion.PdfToImageConverter.PdfToImageConverter();

			//	using (FileStream fileStream = new FileStream(docPath, FileMode.Open, FileAccess.Read))
			//	{
			//		using (PdfLoadedDocument lDoc = new PdfLoadedDocument(fileStream))
			//		{
			//			// Set OCR language to process
			//			processor.Settings.Language = Syncfusion.OCRProcessor.Languages.English;

			//			List<byte[]> ocrBytesList = new List<byte[]>(); // List to store OCR bytes for each page

			//			// Loop through each page of the PDF document
			//			for (int i = 0; i < lDoc.Pages.Count; i++)
			//			{
			//				// Get the current page
			//				PdfPage page = lDoc.Pages[i];

			//				// **Export the entire document as image (modify if needed)**
			//				Bitmap image = lDoc.ExportAsImage();

			//				using (MemoryStream stream = new MemoryStream())
			//				{
			//					// Save the image to stream
			//					image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

			//					// Set the position to the beginning of the stream
			//					stream.Position = 0;

			//					// Perform OCR on the entire document (modify for page-level OCR)
			//					processor.PerformOCR(stream);

			//					// Extract OCR processed bytes from the stream
			//					byte[] ocrBytes = stream.ToArray();
			//					ocrBytesList.Add(ocrBytes);

			//					//****
			//				}
			//			}

			//			return ocrBytesList; // Return the list of OCR bytes for all pages (might need adjustment)
			//		}
			//	}
			//}
		}

		public List<byte[]> OCRFinal(string docPath)
		{
			var ocrBytesList = new List<byte[]>(); // List to store OCR bytes for each page

			using (var processor = new OCRProcessor()) // Assuming OCRProcessor is defined
			{
				using (var fileStream = new FileStream(docPath, FileMode.Open, FileAccess.Read))
				{
					using (var imageConverter = new Syncfusion.PdfToImageConverter.PdfToImageConverter())
					{
						imageConverter.Load(fileStream);

						// Loop through each page of the PDF document
						for (int i = 0; i < imageConverter.PageCount; i++)
						{
							using (var imageStream = imageConverter.Convert(i, true, true)) // Convert specific page
							{
								// Perform OCR on the current page image
								processor.Settings.Language = Languages.English; // Set OCR language
								var ocrText = processor.PerformOCR(imageStream, processor.TessDataPath);

								// Convert OCR text to bytes (assuming specific encoding is needed)
								var encoding = Encoding.UTF8; // Adjust encoding as needed
								var ocrBytes = encoding.GetBytes(ocrText);
								//var ArrayocrBytes = imageStream.ReadByte();

								ocrBytesList.Add(ocrBytes);
							}
						}
					}
				}
			}
			return ocrBytesList;
		}


		// Convert page to image  ***********
		//Bitmap[] bitmapImages = imageConverter.ConvertToImage(fileStream, imageConverter.PageCount - 1, 200, 200); ***

		//Stream imageStream = imageConverter.Convert(0, true, true);
		//PdfBitmap pdfBitmap = new PdfBitmap(imageStream);

		//// Process OCR on each image
		//foreach (var bitmapImage in bitmapImages)
		//{
		//	// Save image to memory stream
		//	bitmapImage.Save(stream, ImageFormat.Png);
		//	stream.Position = 0;

		//	// Perform OCR on the stream
		//	processor.PerformOCR(stream);

		//	// Extract OCR processed bytes from the stream
		//	byte[] ocrBytes = stream.ToArray();
		//	ocrBytesList.Add(ocrBytes);  // Add bytes for this page to the list
		//}  
		//*****

		

		#endregion

		#region Json Result Common Method

		public JsonResult ErrorResult(string errorMessage)
		{
			return new JsonResult(new { success = false, Data = string.Empty, Message = JsonConvert.SerializeObject(errorMessage) });
		}
		public JsonResult SuccessResult(string virtualPath)
		{
			string serializedString = JsonConvert.SerializeObject(virtualPath);
			return new JsonResult(new { success = true, Data = serializedString, Message = JsonConvert.SerializeObject(SUCCESS_MSG) });
		}

		#endregion


		#endregion    //-- Working Code Region End


		#region // -- OLD CODE With AJAX UPLOAD

		public IActionResult OnPostUploadPDF(IList<IFormFile> files)
		{
			byte[] bytes;
			string pdfFileName = $"PDF_{DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")}.pdf";
			string outputPath = Path.Combine(_environment.WebRootPath, ROOTFOLDER_GENERATED_PDF, pdfFileName);

			// Check if any files were uploaded
			if (files == null || files.Count == 0)
			{
				return BadRequest("No files were uploaded.");
			}
			using (MemoryStream ms = new MemoryStream())
			{
				using (var doc = new Document(PageSize.A4))
				{
					using (var writer = PdfWriter.GetInstance(doc, ms))
					{
						doc.Open();
						foreach (var formFile in files)
						{
							if (formFile.Length > 0 && formFile.ContentType.StartsWith("image/"))
							{
								iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(formFile.OpenReadStream());
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
			}
			//System.IO.File.WriteAllBytes(outputPath, bytes);
			return File(bytes, "application/pdf", pdfFileName);
		}

		public IActionResult OnPostPDFUpload(IFormFile file)
		{
			if (file != null && file.Length > 0)
			{
				using (var memoryStream = new MemoryStream())
				{
					file.CopyTo(memoryStream);
					memoryStream.Position = 0;

					// Generate PDF content here (for demonstration, let's assume it's already generated)
					// byte[] pdfContent = GeneratePDF(memoryStream);

					byte[] pdfData = memoryStream.ToArray();

					string base64Content = Convert.ToBase64String(pdfData);

					// Return the base64-encoded content
					return Content(base64Content);

					// Return the PDF content as a file
					//return File(pdfContent, "application/pdf");
				}
			}
			else
			{
				// Handle case when no file is uploaded
				return Content("No file uploaded.");
			}
		}

		private byte[] GeneratePDF(MemoryStream fileStream)
		{
			byte[] bytes;

			using (var doc = new Document(PageSize.A4))
			{
				using (var writer = PdfWriter.GetInstance(doc, fileStream))
				{
					doc.Open();

					if (fileStream.Length > 0)
					{
						iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(fileStream);
						image.Alignment = Element.ALIGN_CENTER;
						image.ScaleToFit(270f, 270f);
						image.IndentationLeft = 9f;
						image.SpacingAfter = 9f;
						image.BorderWidthTop = 2f;

						doc.Add(image);
					}
					doc.Close();
				}

				bytes = fileStream.ToArray();
			}

			return bytes;
		}

		public void SelectPages(string inputPdf, string pageSelection, string outputPdf)
		{
			using PdfReader reader = new PdfReader(inputPdf);
			reader.SelectPages(pageSelection);

			using PdfStamper stamper = new PdfStamper(reader, System.IO.File.Create(outputPdf));
			stamper.Close();
		}

		#endregion

		#region // -- MERGE PDF CODE SAMPLE

		//public IActionResult OnPostMergePDFHandler()  // --- Test Merge Code --- //
		//{
		//	var mergedPDF = "D:/Work/Demo Project/Practice.WebApp/wwwroot/MergedPDF/TestMergePDF.pdf";
		//	var PDF1 = "C:/Users/Anil/Desktop/TestImages/Nature.pdf";
		//	var PDF2 = "C:/Users/Anil/Desktop/TestImages/Nature4.pdf";
		//	var PDF3 = "C:/Users/Anil/Desktop/TestImages/beautifulsky.pdf";

		//	var pdfsToMerge = new string[] { PDF1, PDF2, PDF3 }; // Paths to PDFs to merge

		//	using (var stream = new FileStream(mergedPDF, FileMode.Create))
		//	{
		//		var writer = new iText.Kernel.Pdf.PdfWriter(stream);
		//		var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
		//		var document = new iText.Layout.Document(pdf);

		//		foreach (var pdfPath in pdfsToMerge)
		//		{
		//			var reader = new iText.Kernel.Pdf.PdfReader(pdfPath);
		//			var sourceDocument = new iText.Kernel.Pdf.PdfDocument(reader);
		//			sourceDocument.CopyPagesTo(1, sourceDocument.GetNumberOfPages(), pdf);
		//			reader.Close();
		//		}
		//		document.Close();
		//	}
		//	return File(mergedPDF, "application/pdf", "TestMergePDF.pdf");
		//}

		#endregion

		#region // -- ADD WATERMARK CODE SAMPLE

		// -- *** WATERMARK CODE SAMPLE *** -- //
		//-- Test Watermark Text
		public void AddWatermarkText(string inputFile, string outputFile, string watermarkText)
		{
			//inputFile = "D:/Work/Demo Project/Practice.WebApp/wwwroot/GeneratedPDF/TestPrint.pdf";
			//outputFile = "D:/Work/Demo Project/Practice.WebApp/wwwroot/GeneratedPDF/TestPrint.pdf";

			string fileName = "TestPrintWaterMark1.pdf";
			string folderPath = Path.Combine(_environment.WebRootPath, ROOTFOLDER_GENERATED_PDF);
			inputFile = Path.Combine(folderPath, fileName);
			//outputFile = Path.Combine(folderPath, "AMC_tenderTest.pdf");
			string tempFilePath = Path.GetTempFileName();

			try
			{
				watermarkText = "Test Watermark";
				using (var reader = new PdfReader(inputFile))
				{
					using (var output = new FileStream(tempFilePath, FileMode.Create))
					{
						using (var stamper = new PdfStamper(reader, output))
						{
							int pageCount = reader.NumberOfPages;
							for (int i = 1; i <= pageCount; i++)
							{
								// Get the content and dimensions of each page
								PdfContentByte content = stamper.GetOverContent(i);
								iTextSharp.text.Rectangle pageSize = reader.GetPageSize(i);

								//float fontSize = pageSize.Width / (watermarkText.Length * 2);
								//float fontSize = Math.Min(pageSize.Width, pageSize.Height) / (float)(watermarkText.Length); // Adjust divisor as needed
								//float fontSize = 2f; // Adjust the divisor as needed
								//float fontSize = (pageSize.Width + pageSize.Height) / 2; // Adjust the divisor as needed
								//float fontSize = pageSize.Width * 1f;  // Adjust the divisor as needed

								float fontSize1 = Math.Min(pageSize.Width, pageSize.Height) * 0.5f;

								float diagonal = (float)Math.Sqrt(pageSize.Width * pageSize.Width + pageSize.Height * pageSize.Height);
								float fontSize = diagonal * 0.03f; // Adjust the multiplier as needed

								// Create a new font for the watermark
								iTextSharp.text.pdf.BaseFont baseFont = iTextSharp.text.pdf.BaseFont.CreateFont(iTextSharp.text.pdf.BaseFont.HELVETICA, iTextSharp.text.pdf.BaseFont.CP1252, iTextSharp.text.pdf.BaseFont.NOT_EMBEDDED);
								content.SetColorFill(new BaseColor(128, 128, 128, 150));
								content.BeginText();
								content.SetFontAndSize(baseFont, fontSize);

								// Add watermark text at the center of the page
								//ColumnText.ShowTextAligned(content, iTextSharp.text.Element.ALIGN_CENTER, new Phrase(watermarkText), pageSize.Width / 2, pageSize.Height / 2, 45);

								ColumnText.ShowTextAligned(content, iTextSharp.text.Element.ALIGN_CENTER, new Phrase(watermarkText), pageSize.Width / 2, pageSize.Height - (pageSize.Height * 0.05f), 0); // Adjust the Y position as needed

								content.EndText();
							}
						}
					}
				}
				System.IO.File.Delete(inputFile);
				System.IO.File.Move(tempFilePath, inputFile);
			}
			catch (Exception ex)
			{
				var eee = ex.Message;
			}
		}

		//-- Test Watermark Image
		public void AddWatermarkImg(string inputFilePath, string watermarkImagePath)
		{

			// Create a new document
			Document doc = new Document();
			string fileName = "AMC_tender.pdf";
			watermarkImagePath = Path.Combine(_environment.WebRootPath, ROOTFOLDER_UPLOADS, "AMC_logo.png");
			//watermarkText = "Confidential";
			string folderPath = Path.Combine(_environment.WebRootPath, ROOTFOLDER_GENERATED_PDF);
			inputFilePath = Path.Combine(folderPath, fileName);
			try
			{
				string tempFilePath = Path.GetTempFileName();

				// Load the watermark image
				iTextSharp.text.Image watermarkImage = iTextSharp.text.Image.GetInstance(watermarkImagePath);

				//watermarkImage.SetAbsolutePosition(watermarkImage.Width / 2, watermarkImage.Height / 2); // Set the position of the watermark image
				//watermarkImage.ScaleToFit(watermarkImage.Width / 2, watermarkImage.Height / 2); // Set the position of the watermark image
				//watermarkImage.ScalePercent(80);



				// Create a reader for the existing PDF
				using (PdfReader reader = new PdfReader(inputFilePath))
				{
					// Create a stamper to modify the existing PDF
					using (PdfStamper stamper = new PdfStamper(reader, new FileStream(tempFilePath, FileMode.Create)))
					{
						// Iterate through each page and add the watermark behind the existing content
						for (int i = 1; i <= reader.NumberOfPages; i++)
						{
							// Get the page size
							iTextSharp.text.Rectangle pageSize = reader.GetPageSizeWithRotation(i);

							// Get the content byte for the current page


							//PdfContentByte contentByte = stamper.GetUnderContent(i);    //--- For add water mark under the content (behind the Img)
							PdfContentByte contentByte = stamper.GetOverContent(i);       //--  For add water mark over the content (Over the Img)

							// Add the watermark image to the page
							iTextSharp.text.pdf.PdfTemplate template = contentByte.CreateTemplate(pageSize.Width, pageSize.Height);

							float watermarkWidth = pageSize.Width * 0.50f; // 50% width of the page  -- Exp: 0.20f = 20%, 0.50f = 50%, 1.0f = 100% ...
							float watermarkHeight = watermarkWidth * (watermarkImage.Height / watermarkImage.Width);

							// -- Center
							float xPosition = (pageSize.Width - watermarkWidth) / 2; // Center horizontally
							float yPosition = (pageSize.Height - watermarkHeight) / 2; // Center vertically

							// -- Top
							//float topXPosition = (pageSize.Width - watermarkWidth) / 2; // Center horizontally
							//float topYPosition = pageSize.Height - watermarkHeight - 20; // 20 pixels from top margin

							// -- Bottom
							//float bottomXPosition = (pageSize.Width - watermarkWidth) / 2; // Center horizontally
							//float bottomYPosition = 20; // 20 pixels from bottom margin

							// -- Top-left
							//float topLeftXPosition = 50; // 20 pixels from left margin
							//float topLeftYPosition = pageSize.Height - watermarkHeight - 20;

							// -- Top-right
							//float topRightXPosition = pageSize.Width - watermarkWidth - 20; // 20 pixels from right margin
							//float topRightYPosition = pageSize.Height - watermarkHeight - 20; // 20 pixels from top margin


							// -- Bottom-left
							//float bottomLeftXPosition = 20; // 20 pixels from left margin
							//float bottomLeftYPosition = 20; // 20 pixels from bottom margin

							// -- Bottom-right
							//float bottomRightXPosition = pageSize.Width - watermarkWidth - 60; // 60 pixels from right margin
							//float bottomRightYPosition = 60; // 60 pixels from bottom margin

							// Set the transparency for the watermark image
							PdfGState gstate = new()
							{
								FillOpacity = 0.25f // 25%  Set opacity as (opacity / 100)f. Exp: 0.20f = 20%, 0.50f = 50%, 1.0f = 100% ...
							};
							contentByte.SetGState(gstate);

							// Add the watermark image to the page
							watermarkImage.ScaleToFit(watermarkWidth, watermarkHeight);
							watermarkImage.SetAbsolutePosition(xPosition, yPosition);
							contentByte.AddImage(watermarkImage);
						}
					}
				}

				// Replace the original PDF file with the modified PDF
				System.IO.File.Delete(inputFilePath);
				System.IO.File.Move(tempFilePath, inputFilePath);

			}
			catch (Exception)
			{

			}
			finally
			{
				// Close the document
				doc.Close();
			}
		}
		// -- *** WATERMARK CODE SAMPLE END *** -- //

		#endregion
	}
}
