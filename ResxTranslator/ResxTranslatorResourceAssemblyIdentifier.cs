using DevToys.Api;
using System.ComponentModel.Composition;

namespace ResxTranslator
{
    [Export(typeof(IResourceAssemblyIdentifier))]
    [Name(nameof(ResxTranslatorResourceAssemblyIdentifier))]
    internal sealed class ResxTranslatorResourceAssemblyIdentifier : IResourceAssemblyIdentifier
    {
        public ValueTask<FontDefinition[]> GetFontDefinitionsAsync()
        {
            throw new NotImplementedException();
        }
    }
}
