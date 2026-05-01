namespace CentralServer.Presentation.GraphQL.Security;

using HotChocolate.Language;

internal static class GraphQLDocumentAnalyzer
{
    public static bool TryAnalyze(string query, out GraphQLRequestMetrics metrics, out string? syntaxError)
    {
        try
        {
            var document = Utf8GraphQLParser.Parse(query);
            metrics = Analyze(document);
            syntaxError = null;
            return true;
        }
        catch (SyntaxException ex)
        {
            metrics = new GraphQLRequestMetrics();
            syntaxError = ex.Message;
            return false;
        }
    }

    private static GraphQLRequestMetrics Analyze(DocumentNode document)
    {
        var metrics = new GraphQLRequestMetrics();
        var fragments = document.Definitions
            .OfType<FragmentDefinitionNode>()
            .ToDictionary(fragment => fragment.Name.Value, StringComparer.Ordinal);

        foreach (var operation in document.Definitions.OfType<OperationDefinitionNode>())
        {
            AnalyzeSelectionSet(operation.SelectionSet, 1, metrics, fragments, new HashSet<string>(StringComparer.Ordinal));
        }

        return metrics;
    }

    private static void AnalyzeSelectionSet(
        SelectionSetNode selectionSet,
        int depth,
        GraphQLRequestMetrics metrics,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        HashSet<string> recursionGuard)
    {
        metrics.MaxDepth = Math.Max(metrics.MaxDepth, depth);

        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    metrics.FieldCount++;

                    if (field.Name.Value.StartsWith("__", StringComparison.Ordinal))
                    {
                        metrics.ContainsIntrospection = true;
                    }

                    if (field.SelectionSet != null)
                    {
                        AnalyzeSelectionSet(field.SelectionSet, depth + 1, metrics, fragments, recursionGuard);
                    }

                    break;

                case InlineFragmentNode inlineFragment:
                    AnalyzeSelectionSet(inlineFragment.SelectionSet, depth + 1, metrics, fragments, recursionGuard);
                    break;

                case FragmentSpreadNode fragmentSpread:
                    var fragmentName = fragmentSpread.Name.Value;
                    if (!recursionGuard.Add(fragmentName))
                    {
                        break;
                    }

                    if (fragments.TryGetValue(fragmentName, out var fragmentDefinition))
                    {
                        AnalyzeSelectionSet(fragmentDefinition.SelectionSet, depth + 1, metrics, fragments, recursionGuard);
                    }

                    recursionGuard.Remove(fragmentName);
                    break;
            }
        }
    }
}
