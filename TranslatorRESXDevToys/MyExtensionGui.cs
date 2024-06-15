using DevToys.Api;
using TranslatorRESXDevToys;
using System.ComponentModel.Composition;
using static DevToys.Api.GUI;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Xml.Linq;
using TranslatorRESXDevToys.Services;

namespace TranslatorRESXDevToys;

[Export(typeof(IGuiTool))]
[Name("TranslatorRESXDevToys")] // A unique, internal name of the tool.
[ToolDisplayInformation(
    IconFontName = "FluentSystemIcons", // This font is available by default in DevToys
    IconGlyph = '\uE670', // An icon that represents a pizza
    GroupName = PredefinedCommonToolGroupNames.Converters, // The group in which the tool will appear in the side bar.
    ResourceManagerAssemblyIdentifier = nameof(MyResourceAssemblyIdentifier), // The Resource Assembly Identifier to use
    ResourceManagerBaseName =
        "TranslatorRESXDevToys.TranslatorRESXDevToys", // The full name (including namespace) of the resource file containing our localized texts
    ShortDisplayTitleResourceName =
        nameof(ExtensionText.ShortDisplayTitle), // The name of the resource to use for the short display title
    LongDisplayTitleResourceName = nameof(ExtensionText.LongDisplayTitle),
    DescriptionResourceName = nameof(ExtensionText.Description),
    AccessibleNameResourceName = nameof(ExtensionText.AccessibleName))]
internal sealed class MyExtensionGui : IGuiTool
{
    private SandboxedFileReader[]? _selectedFiles;

    [Import]
    private IFileStorage _fileStorage = null!;

    private string _fromLanguage = "en";
    private string _toLanguage = "es";
    private string _region = "northeurope";
    private string _key = "YOUR_KEY_HERE";


    private static AzureTranslatorService _azureTranslatorService;

    
    public UIToolView View
    {
        get
        {
            IUIStack verticalSection = Stack()
                .Vertical()
                .WithChildren(
                    FileSelector()
                        .CanSelectManyFiles()
                        .LimitFileTypesTo(".resx")
                        .OnFilesSelected(OnFilesSelected),
                    Button()
                        .Text("Click me")
                        .OnClick(OnButtonClickAsync)
                );

            IUIStack horizontalSection = Stack()
                .Horizontal()
                .WithChildren(
                    SingleLineTextInput()
                        .Title("From Language")
                        .Text("en")
                        .OnTextChanged(OnFromTextChanged),
                    SingleLineTextInput()
                        .Title("To Language")
                        .Text("es")
                        .OnTextChanged(OnToTextChanged)
                );
            
            IUIStack horizontalSection2 = Stack()
                .Horizontal()
                .WithChildren(
                    SingleLineTextInput()
                        .Title("From Language")
                        .Text("en")
                        .OnTextChanged(OnFromTextChanged),
                    SingleLineTextInput()
                        .Title("To Language")
                        .Text("es")
                        .OnTextChanged(OnToTextChanged)
                );

            IUIStack rootElement = Stack()
                .Vertical()
                .WithChildren(horizontalSection, verticalSection, horizontalSection2);

            return new UIToolView(true, rootElement);
        }
    }

    
    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
        // Handle Smart Detection.
    }

    private async ValueTask OnButtonClickAsync()
    {
        
        if (_selectedFiles is not null && _selectedFiles.Any())
        {
            var file = _selectedFiles.FirstOrDefault();
            if (file is not null)
            {
                await using Stream stream = await file.GetNewAccessToFileContentAsync(CancellationToken.None);
                StreamReader streamReader = new StreamReader(stream);

                await using FileStream  result = await _fileStorage.PickSaveFileAsync(".resx");
                StreamWriter writer = new StreamWriter(result);
                

                _azureTranslatorService = new AzureTranslatorService(_region, _key);
                
                XDocument xmlDoc = XDocument.Load(streamReader);

                await TranslateXmlValues(xmlDoc, _toLanguage, _fromLanguage);

                xmlDoc.Save(writer.BaseStream);
                
            }
        }
    }
    private static async Task TranslateXmlValues(XDocument xmlDoc, string targetLanguage, string fromLanguage)
    {
        var valueElements = xmlDoc.Descendants("data").Elements("value").ToList();

        foreach (var valueElement in valueElements)
        {
            string originalContent = valueElement.Value;
            valueElement.Value = await _azureTranslatorService.Translator(fromLanguage, targetLanguage, originalContent);
        }
    }

    private void OnFilesSelected(SandboxedFileReader[] files)
    {
        _selectedFiles = files;
    }
    private void OnFromTextChanged(string text)
    {
        _fromLanguage = text;
    }

    private void OnToTextChanged(string text)
    {
        _toLanguage = text;
    }
}