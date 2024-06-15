using DevToys.Api;
using TranslatorRESXDevToys;
using System.ComponentModel.Composition;
using static DevToys.Api.GUI;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Threading;

namespace TranslatorRESXDevToys;

[Export(typeof(IGuiTool))]
[Name("TranslatorRESXDevToys")]                                                         // A unique, internal name of the tool.
[ToolDisplayInformation(
    IconFontName = "FluentSystemIcons",                                       // This font is available by default in DevToys
    IconGlyph = '\uE670',                                                     // An icon that represents a pizza
    GroupName = PredefinedCommonToolGroupNames.Converters,                    // The group in which the tool will appear in the side bar.
    ResourceManagerAssemblyIdentifier = nameof(MyResourceAssemblyIdentifier), // The Resource Assembly Identifier to use
    ResourceManagerBaseName = "TranslatorRESXDevToys.TranslatorRESXDevToys",                      // The full name (including namespace) of the resource file containing our localized texts
    ShortDisplayTitleResourceName = nameof(ExtensionText.ShortDisplayTitle),    // The name of the resource to use for the short display title
    LongDisplayTitleResourceName = nameof(ExtensionText.LongDisplayTitle),
    DescriptionResourceName = nameof(ExtensionText.Description),
    AccessibleNameResourceName = nameof(ExtensionText.AccessibleName))]
internal sealed class MyExtensionGui : IGuiTool
{
    private SandboxedFileReader[]? selectedFiles;

    [Import] // Import the file storage service.
    private IFileStorage _fileStorage = null;
    

    public UIToolView View
        => new UIToolView(
            Stack()
                .Horizontal()
                .WithChildren(
                    FileSelector()
                        .CanSelectManyFiles()  // Allow multiple file selection
                        .LimitFileTypesTo(".resx")  // Limit file types to .resx
                        .OnFilesSelected(OnFilesSelected),  // Handle file selection event
                    Button()
                        .Text("Click me")  // Button text
                        .OnClick(OnButtonClickAsync)  // Handle button click event
                )
        );

    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
        // Handle Smart Detection.
    }

    private async ValueTask OnButtonClickAsync()
    {
        if (selectedFiles is not null && selectedFiles.Any())
        {
            var file = selectedFiles.FirstOrDefault();
            if (file is not null)
            {
                await using var stream = await file.GetNewAccessToFileContentAsync(CancellationToken.None);
                using var streamReader = new StreamReader(stream);

                string line;
                while ((line = await streamReader.ReadLineAsync()) != null)
                {

                    // Process the line
                    Console.WriteLine(line);
                }
            }
        }
    }

    private void OnFilesSelected(SandboxedFileReader[] files)
    {
        // Guardar los archivos seleccionados
        selectedFiles = files;
    }
}
