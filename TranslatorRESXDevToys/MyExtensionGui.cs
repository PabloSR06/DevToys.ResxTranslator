﻿using DevToys.Api;
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
        "TranslatorRESXDevToys.ExtensionText", // The full name (including namespace) of the resource file containing our localized texts
    ShortDisplayTitleResourceName =
        nameof(ExtensionText.ShortDisplayTitle), // The name of the resource to use for the short display title
    LongDisplayTitleResourceName = nameof(ExtensionText.LongDisplayTitle),
    DescriptionResourceName = nameof(ExtensionText.Description),
    AccessibleNameResourceName = nameof(ExtensionText.AccessibleName))]
internal sealed class MyExtensionGui : IGuiTool
{
    private SandboxedFileReader[]? _selectedFiles;

    [Import] private IFileStorage _fileStorage = null!;

    private string _fromLanguage = "en";
    private string _toLanguage = "es";


    private static AzureTranslatorService _azureTranslatorService;

    private readonly IUIProgressRing _progressRing = ProgressRing();

    [Import] private ISettingsProvider _settingsProvider = null!;

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
                        .LimitFileTypesTo(".resx")
                        .OnFilesSelected(file => _selectedFiles = file),
                    Button()
                        .Text(ExtensionText.TranslateFile)
                        .OnClick(OnButtonClickAsync),
                    ProgressRing(), _progressRing
                );

            IUIStack rootElement = Stack()
                .Vertical()
                .WithChildren(azureKeySection, horizontalSection, verticalSection);

            return new UIToolView(true, rootElement);
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

                    await using FileStream result = await _fileStorage.PickSaveFileAsync(".resx");
                    StreamWriter writer = new StreamWriter(result);


                    _azureTranslatorService = new AzureTranslatorService(_settingsProvider.GetSetting(TranslatorKey), _settingsProvider.GetSetting(TranslatorRegion));

                    XDocument xmlDoc =
                        await XDocument.LoadAsync(streamReader, LoadOptions.None, CancellationToken.None);

                    await TranslateXmlValues(xmlDoc, _toLanguage, _fromLanguage);

                    await writer.WriteAsync(xmlDoc.ToString());
                    await writer.FlushAsync();

                    _progressRing.StopIndeterminateProgress();
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
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
    
}