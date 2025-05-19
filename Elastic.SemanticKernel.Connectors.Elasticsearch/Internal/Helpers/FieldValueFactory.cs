using System;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch.Internal.Helpers;

internal static class FieldValueFactory
{
    public static Elastic.Clients.Elasticsearch.FieldValue FromValue(object? value)
    {
        return value switch
        {
            null => Clients.Elasticsearch.FieldValue.Null,
            bool and true => Clients.Elasticsearch.FieldValue.True,
            bool and false => Clients.Elasticsearch.FieldValue.False,
            float v => v,
            double v => v,
            sbyte v => v,
            short v => v,
            int v => v,
            long v => v,
            byte v => v,
            ushort v => v,
            uint v => v,
            ulong v => unchecked((long)v),
            string v => v,
            _ => throw new NotSupportedException($"Unsupported value type '{value!.GetType().Name}'.")
        };
    }
}
