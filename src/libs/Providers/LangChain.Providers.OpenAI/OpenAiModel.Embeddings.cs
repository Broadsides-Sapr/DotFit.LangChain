namespace LangChain.Providers;

public partial class OpenAiModel : IEmbeddingModel
{
    #region Properties

    /// <inheritdoc cref="OpenAiConfiguration.EmbeddingModelId"/>
    public string EmbeddingModelId { get; init; } = EmbeddingModelIds.Ada002;
    
    /// <inheritdoc/>
    public int MaximumInputLength => ApiHelpers.CalculateContextLength(EmbeddingModelId);

    #endregion

    #region Methods

    private Usage GetUsage(CreateEmbeddingResponse response)
    {
        var promptTokens = response.Usage.Prompt_tokens;
        var priceInUsd = CalculatePriceInUsd(
            completionTokens: 0,
            promptTokens: promptTokens);
        
        return new Usage(
            PromptTokens: promptTokens,
            CompletionTokens: 0,
            Messages: 0,
            PriceInUsd: priceInUsd);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<double>> EmbedQueryAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var response = await Api.CreateEmbeddingAsync(new CreateEmbeddingRequest
        {
            Input = text,
            Model = EmbeddingModelId,
            User = User,
        }, cancellationToken).ConfigureAwait(false);

        var usage = GetUsage(response);
        lock (_usageLock)
        {
            TotalUsage += usage;
        }

        return response.Data.First().Embedding.ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<IReadOnlyCollection<double>>> EmbedDocumentsAsync(
        string[] texts,
        CancellationToken cancellationToken = default)
    {
        return await Task.WhenAll(
            texts
                .Select(text => EmbedQueryAsync(text, cancellationToken))).ConfigureAwait(false);
    }
    
    #endregion
}