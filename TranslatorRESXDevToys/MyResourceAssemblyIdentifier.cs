using DevToys.Api;
using System.ComponentModel.Composition;

namespace TranslatorRESXDevToys
{
    [Export(typeof(IResourceAssemblyIdentifier))]
    [Name(nameof(MyResourceAssemblyIdentifier))]
    internal sealed class MyResourceAssemblyIdentifier : IResourceAssemblyIdentifier
    {
        public ValueTask<FontDefinition[]> GetFontDefinitionsAsync()
        {
            throw new NotImplementedException();
        }
    }
}
