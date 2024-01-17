# XML Manager Function App Net 8.0 Nested XSDs Bug Reproduction
This Azure Function App in .NET 8 that will demo how to fail nested XSD validation.

You can deploy this Function App to either Windows or Linux and the issue occurs on both platforms.

Please run the Integration Tests in the Unit Test Project.

## XML File for Testing:
https://salybpubliclogicapps.blob.core.windows.net/xsds/test-las.xml

## Main XSD for Testing:
https://salybpubliclogicapps.blob.core.windows.net/xsds/rfcInvoiceImageRfc.xsd

## Included XSD that is called from the Main XSD:
https://salybpubliclogicapps.blob.core.windows.net/xsds/rfcInvoiceImageTypesRfc.xsd
