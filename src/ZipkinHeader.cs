namespace Rosered.Zipkin.Net;

public static class ZipkinHeader
{
    public const string TraceHighId = "X-B3-TraceHighId";
    public const string TraceId = "X-B3-TraceId";
    public const string SpanId = "X-B3-SpanId";
    public const string ParentSpanId = "X-B3-ParentSpanId";
}
