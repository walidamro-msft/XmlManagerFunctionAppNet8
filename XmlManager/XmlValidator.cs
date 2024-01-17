using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace XmlManager
{
    public class XmlValidator
    {
        private readonly ILogger _logger;
        public static bool hasError = false;

        public XmlValidator(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<XmlValidator>();
        }

        [Function("XmlValidator")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Validating XML against its XSD...");

            // get the XML body from the request body
            string? reqBody = await req.ReadAsStringAsync();


            if (string.IsNullOrEmpty(reqBody))
            {
                _logger.LogError("XML body is empty");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var xmlBody = reqBody.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? await LoadFromUrl(reqBody)
                : reqBody;

            _logger.LogInformation($"XML Body:\n{xmlBody}");

            // get the XSD schema from the request header of x-xsd-schema
            string? xsdSchemaUrl = req.Headers.GetValues("x-xsd-schema").FirstOrDefault();


            if (string.IsNullOrEmpty(xsdSchemaUrl))
            {
                _logger.LogError("Header [x-xsd-schema] for XSD Schema URL is empty");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            _logger.LogInformation($"XSD Schema URL: {xsdSchemaUrl}");



            XmlReaderSettings settings = new XmlReaderSettings();
            settings.Schemas.Add(null, xsdSchemaUrl); // This line of code will only load the first XSD and will not load the leaf XSD, so it will show only 1 schema (which is the root cause of the bug) -- in .NET 4.8 this method loads both Schemas and it will show 2 schemas.
            settings.ValidationType = ValidationType.Schema;

            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);

            bool hasError = false;

            // Convert xmlContent to Stream
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlBody)))


            using (XmlReader reader = XmlReader.Create(stream, settings)) // This where the code generates the exception as it validates the XML againt the XSD schemas, so it only finds 1 schema in the settings.Schemas object.
            {
                try
                {
                    while (reader.Read()) { }
                    if (!hasError)
                    {
                        _logger.LogInformation("XML is valid");
                        return req.CreateResponse(HttpStatusCode.OK);
                    }
                }
                catch (XmlException ex)
                {
                    _logger.LogError($"XML is not weel-formed! Error: {ex.Message}");
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error: {ex.Message}");
                    return req.CreateResponse(HttpStatusCode.InternalServerError);
                }
            }

            _logger.LogError("XML is not valid");
            return req.CreateResponse(HttpStatusCode.BadRequest);

        }

        private static void ValidationCallBack(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
                Console.WriteLine($"\tWarning: {args.Message}");
            else if (args.Severity == XmlSeverityType.Error)
            {
                Console.WriteLine($"\tError: {args.Message}");
                hasError = true;
            }
        }

        private static async Task<string> LoadFromUrl(string url)
        {
            using (var httpClient = new HttpClient())
            {
                return await httpClient.GetStringAsync(url);
            }

        }

        private static async Task<string> LoadFromFile(string filePath)
        {
            return await Task.Run(() => File.ReadAllText(filePath));
        }


    }
}
