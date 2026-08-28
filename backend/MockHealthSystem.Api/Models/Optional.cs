using System.Text.Json;
using System.Text.Json.Serialization;

namespace MockHealthSystem.Api.Models;

/// <summary>
/// Wraps a PATCH request field so "omitted from the JSON body" (IsSet == false) can be told apart
/// from "present, possibly null" (IsSet == true, Value possibly null). Plain nullable properties
/// can't make that distinction, which silently drops requests that clear a field to null.
/// </summary>
/// <remarks>
/// Currently only <c>PatientPatchModel</c> uses this. <c>StudyPatchModel</c> and
/// <c>SubjectPatchModel</c> have the same gap and are candidates for adopting this type, but that
/// is intentionally out of scope here — deliberately not done yet, not missed.
/// </remarks>
[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly struct Optional<T>
{
    public bool IsSet { get; }
    public T? Value { get; }

    public Optional(T? value)
    {
        IsSet = true;
        Value = value;
    }

    public static implicit operator Optional<T>(T? value) => new(value);
}

public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var innerType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionalJsonConverter<>).MakeGenericType(innerType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
    // For a non-nullable value type (e.g. bool), default(T) is a real value (false), not an
    // absence of one — so an explicit JSON null can't be represented as "IsSet, Value=default"
    // without being indistinguishable from the client explicitly choosing that default. Reject
    // it instead of silently clobbering the field.
    private static readonly bool IsNonNullableValueType =
        typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) is null;

    // Without this, System.Text.Json throws on a JSON null instead of invoking Read, since
    // Optional<T> itself is a non-nullable struct.
    public override bool HandleNull => true;

    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            if (IsNonNullableValueType)
            {
                throw new JsonException(
                    $"'{typeof(T).Name}' fields cannot be explicitly set to null; omit the field to leave it unchanged.");
            }

            return new Optional<T>(default);
        }

        return new Optional<T>(JsonSerializer.Deserialize<T>(ref reader, options));
    }

    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
    {
        if (!value.IsSet || value.Value is null) writer.WriteNullValue();
        else JsonSerializer.Serialize(writer, value.Value, options);
    }
}
