namespace QA.Backend.Options;

public sealed class KnowledgeBaseOptions
{
    public const string SectionName = "KnowledgeBase";

    public string FilePath { get; set; } = "knowledge_base.txt";
    public int ChunkSize { get; set; } = 800;
    public int MaxQuestionLength { get; set; } = 500;
}
