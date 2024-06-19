using Newtonsoft.Json;

namespace ResxTranslator.Models
{

    class TranslationList
    {
        [JsonProperty("translations")]
        public List<Translation> Translations { get; set; }
    }
    class Translation
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }
    }

}
