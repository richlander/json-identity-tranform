using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text.Json;

namespace JsonReaders;

public class JsonPipeReader(PipeReader reader, ReadResult result)
{
    private JsonReaderState _readerState = default;
    private SequencePosition _position = default;
    private int _depth = 0;
    private long _bytesConsumed = 0;
    private readonly PipeReader _pipeReader = reader;
    private ReadOnlySequence<byte> _text = result.Buffer;

    public void UpdateState(Utf8JsonReader reader)
    {
        _bytesConsumed = reader.BytesConsumed;
        _position = reader.Position;
        _readerState = reader.CurrentState;
        _depth = reader.CurrentDepth;
    }

    public Utf8JsonReader GetReader()
    {
        var slice = _bytesConsumed > 0 ? _text.Slice(_position) : _text;
        var reader = new Utf8JsonReader(slice, false, _readerState);
        return reader;
    }

    public int Depth => _depth;

    public async Task AdvanceAsync()
    {
        _pipeReader.AdvanceTo(_position);
        var result = await _pipeReader.ReadAsync();
        _text = result.Buffer;
        _bytesConsumed = 0;
    }
}
