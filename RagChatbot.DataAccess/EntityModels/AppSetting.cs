namespace RagChatbot.DataAccess.EntityModels
{
    // Generic key-value store for runtime-editable settings (chunking config, USD/VND rate, ...).
    public class AppSetting
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
