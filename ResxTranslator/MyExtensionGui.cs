using DevToys.Api;
using System.ComponentModel.Composition;
using static DevToys.Api.GUI;
using System.Xml.Linq;
using ResxTranslator.Services;

namespace ResxTranslator;

[Export(typeof(IGuiTool))]
[Name("ResxTranslator")] // A unique, internal name of the tool.
[ToolDisplayInformation(
    IconFontName = "FluentSystemIcons", // This font is available by default in DevToys
    IconGlyph = '\uF658',
    GroupName = PredefinedCommonToolGroupNames.Converters, // The group in which the tool will appear in the side bar.
    ResourceManagerAssemblyIdentifier = nameof(ResxTranslatorResourceAssemblyIdentifier), // The Resource Assembly Identifier to use
    ResourceManagerBaseName =
        "ResxTranslator.ExtensionText", // The full name (including namespace) of the resource file containing our localized texts
    ShortDisplayTitleResourceName =
        nameof(ExtensionText.ShortDisplayTitle), // The name of the resource to use for the short display title
    LongDisplayTitleResourceName = nameof(ExtensionText.LongDisplayTitle),
    DescriptionResourceName = nameof(ExtensionText.Description),
    AccessibleNameResourceName = nameof(ExtensionText.AccessibleName))]
internal sealed class MyExtensionGui : IGuiTool
{
    private static AzureTranslatorService _azureTranslatorService;

    [Import] private ISettingsProvider _settingsProvider = null!;
    [Import] private IFileStorage _fileStorage = null!;
    
    private SandboxedFileReader[]? _selectedFiles;

    private string _fromLanguage = "en";
    private string _toLanguage = "es";
    
    private readonly IUIProgressRing _progressRing = ProgressRing();
    private readonly UIToolView _view = new UIToolView();
    
    private static readonly SettingDefinition<string> TranslatorKey =
        new(name: $"{nameof(MyExtensionGui)}.{nameof(TranslatorKey)}", defaultValue: "");

    private static readonly SettingDefinition<string> TranslatorRegion =
        new(name: $"{nameof(MyExtensionGui)}.{nameof(TranslatorRegion)}", defaultValue: "");

    public UIToolView View
    {
        get
        {
            IUISplitGrid azureKeySection = SplitGrid()
                .Vertical()
                        .LeftPaneLength(new UIGridLength(2, UIGridUnitType.Fraction))
                        .RightPaneLength(new UIGridLength(2, UIGridUnitType.Fraction))
                        .WithLeftPaneChild(SingleLineTextInput()
                            .Title(ExtensionText.AzureTranslatorKey)
                            .Text(_settingsProvider.GetSetting(TranslatorKey))
                            .OnTextChanged(key => _settingsProvider.SetSetting(TranslatorKey, key)))
                        .WithRightPaneChild(SingleLineTextInput()
                            .Title(ExtensionText.AzureTranslatorRegion)
                            .Text(_settingsProvider.GetSetting(TranslatorRegion))
                            .OnTextChanged(region => _settingsProvider.SetSetting(TranslatorRegion, region)));

            IUISplitGrid horizontalSection = SplitGrid()
                .Vertical()
                .LeftPaneLength(new UIGridLength(2, UIGridUnitType.Fraction))
                .RightPaneLength(new UIGridLength(2, UIGridUnitType.Fraction))
                .WithLeftPaneChild(SingleLineTextInput()
                    .Title(ExtensionText.FromLanguage)
                    .Text("en")
                    .OnTextChanged(text => _fromLanguage = text))
                .WithRightPaneChild(SingleLineTextInput()
                    .Title(ExtensionText.ToLanguage)
                    .Text("es")
                    .OnTextChanged(text => _toLanguage = text));
            
            IUIStack verticalSection = Stack()
                .Vertical()
                .WithChildren(
                    FileSelector()
                        .CanSelectManyFiles()
                        .LimitFileTypesTo(".resx", ".resw")
                        .OnFilesSelected(file => _selectedFiles = file),
                    Button()
                        .Text(ExtensionText.TranslateFile)
                        .OnClick(OnButtonClickAsync),
                    ProgressRing(), _progressRing
                );

            if (_view.RootElement is null)
            {
                _view.WithRootElement(Stack()
                    .Vertical()
                    .WithChildren(azureKeySection, horizontalSection, verticalSection));
                
            }

            return _view;
        }
    }
    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
        // Handle Smart Detection.
    }

    private async ValueTask OnButtonClickAsync()
    {
        try
        {
            if (_selectedFiles is not null && _selectedFiles.Any())
            {
                _progressRing.StartIndeterminateProgress();
        
                var file = _selectedFiles.FirstOrDefault();
                if (file is not null)
                {
                    await using Stream stream = await file.GetNewAccessToFileContentAsync(CancellationToken.None);
                    StreamReader streamReader = new StreamReader(stream);
        
                    await using FileStream result = await _fileStorage.PickSaveFileAsync(".resx", ".resw");
                    StreamWriter writer = new StreamWriter(result);
        
        
                    _azureTranslatorService = new AzureTranslatorService(_settingsProvider.GetSetting(TranslatorKey), _settingsProvider.GetSetting(TranslatorRegion));
        
                    XDocument xmlDoc =
                        await XDocument.LoadAsync(streamReader, LoadOptions.None, CancellationToken.None);
        
                    await TranslateXmlValues(xmlDoc, _toLanguage, _fromLanguage);
        
                    await writer.WriteAsync(xmlDoc.ToString());
                    await writer.FlushAsync();
        
                    _progressRing.StopIndeterminateProgress();
                    await OpenEndDialogAsync(result.Name);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            _progressRing.StopIndeterminateProgress();
            await OpenErrorDialogAsync();
        }
    }

    private static async Task TranslateXmlValues(XDocument xmlDoc, string targetLanguage, string fromLanguage)
    {
        var valueElements = xmlDoc.Descendants("data").Elements("value").ToList();

        foreach (var valueElement in valueElements)
        {
            string originalContent = valueElement.Value;
            valueElement.Value =
                await _azureTranslatorService.Translator(fromLanguage, targetLanguage, originalContent);
        }
    }
    
    
    private async Task<UIDialog> OpenEndDialogAsync(string fileName)
    {
        UIDialog dialog
            = await _view.OpenDialogAsync(
                dialogContent:
                Stack()
                    .Vertical()
                    .WithChildren(
                        Label()
                            .Style(UILabelStyle.Subtitle)
                            .Text(ExtensionText.FileUpdated),
                        Label()
                            .Style(UILabelStyle.Body)
                            .Text(fileName)),
                footerContent:
                Button()
                    .AlignHorizontally(UIHorizontalAlignment.Right)
                    .Text(ExtensionText.Close)
                    .OnClick(OnCloseDialogButtonClick),
                isDismissible: true);
        
        return dialog;
    }
    private async Task<UIDialog> OpenErrorDialogAsync()
    {
        UIDialog dialog
            = await _view.OpenDialogAsync(
                dialogContent:
                Stack()
                    .Vertical()
                    .WithChildren(
                        Label()
                            .Style(UILabelStyle.Subtitle)
                            .Text(ExtensionText.Error),
                        Label()
                            .Style(UILabelStyle.Body)
                            .Text(ExtensionText.UnexpectedError)),
                footerContent:
                Button()
                    .AlignHorizontally(UIHorizontalAlignment.Right)
                    .Text(ExtensionText.Close)
                    .OnClick(OnCloseDialogButtonClick),
                isDismissible: true);
        
        return dialog;
    }
    void OnCloseDialogButtonClick()
    {
        _view.CurrentOpenedDialog?.Close();
    }
    
}