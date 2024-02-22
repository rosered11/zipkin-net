using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using zipkin4net;
using zipkin4net.Middleware;
using zipkin4net.Tracers.Zipkin;

namespace Rosered.Zipkin.Net;

public static class ZipkinHelper
{
    public static void StartZipkin(ILogger logger, IZipkinSender zipkinSender)
    {
        TraceManager.Trace128Bits = true;
        TraceManager.SamplingRate = 1.0f;
        var tracer = new ZipkinTracer(zipkinSender, new JSONSpanSerializer());
        TraceManager.RegisterTracer(tracer);
        TraceManager.Start(logger);
    }

    public static void StopZipkin() => TraceManager.Stop();
    public static Trace GetTrace(IEnumerable<IHeader>? headers)
    {
        if (headers == null)
            return Trace.Create();
        string? traceHighId = null, traceId = null, parentSpanId = null, spanId = null;
        string data;
        foreach (var header in headers)
        {
            data = Encoding.UTF8.GetString(header.GetValueBytes());
            switch (header.Key)
            {
                case ZipkinHeader.ParentSpanId: parentSpanId = data; break;
                case ZipkinHeader.SpanId: spanId = data; break;
                case ZipkinHeader.TraceId: traceId = data; break;
                case ZipkinHeader.TraceHighId: traceHighId = data; break;
            }
        }
        return GetTrace(traceHighId, traceId, parentSpanId, spanId);
    }
    public static Trace GetTrace(string traceHighId, string traceId, string spanId) => GetTrace(traceHighId, traceId, null, spanId);
    public static Trace GetTrace(string? traceHighId, string? traceId, string? parentSpanId, string? spanId)
    {
        if (string.IsNullOrEmpty(traceHighId) || string.IsNullOrEmpty(traceId) || string.IsNullOrEmpty(spanId))
            return Trace.Create();
        long? parentIdNum = null;
        if (string.IsNullOrEmpty(parentSpanId))
        {
            Int64.TryParse(parentSpanId, out long number);
            parentIdNum = number;
        }
        if (!Int64.TryParse(traceHighId, out long traceHighIdNum))
            return Trace.Create();
        if (!Int64.TryParse(traceId, out long tracIdNum))
            return Trace.Create();
        if (!Int64.TryParse(spanId, out long spanIdNum))
            return Trace.Create();

        return GetTrace(traceHighIdNum, tracIdNum, parentIdNum, spanIdNum);
    }
    public static Trace GetTrace(long traceHighId, long traceId, long? parentSpanId, long spanId) => Trace.CreateFromId(new SpanState(traceHighId, traceId, parentSpanId, spanId, true, false));
    public static string CreateXB3(ITraceContext traceContext)
    {
        StringBuilder sb = new StringBuilder(2);
        sb.Append($"{traceContext.TraceIdHigh}|{traceContext.TraceId}|{traceContext.SpanId}");
        if (traceContext.ParentSpanId.HasValue)
            sb.Append($"|{traceContext.ParentSpanId.GetValueOrDefault()}");
        return sb.ToString();
    }
    public static Trace GetTrace(string traceXB3)
    {
        if (string.IsNullOrEmpty(traceXB3))
            return Trace.Create();
        string[] traceArray = traceXB3.Split("|");
        if (traceArray.Length < 3)
            return Trace.Create();
        if (traceArray.Length > 3)
            return GetTrace(traceArray[0], traceArray[1], traceArray[3], traceArray[2]);
        return GetTrace(traceArray[0], traceArray[1], default, traceArray[2]);
    }
    public static Dictionary<string, string> CreateHeaders(ITraceContext traceContext) =>
        new()
        {
            { ZipkinHeader.ParentSpanId, traceContext.ParentSpanId.HasValue? traceContext.ParentSpanId.Value.ToString() : string.Empty },
            { ZipkinHeader.SpanId, traceContext.SpanId.ToString() },
            { ZipkinHeader.TraceId, traceContext.TraceId.ToString() },
            { ZipkinHeader.TraceHighId, traceContext.TraceIdHigh.ToString() }
        };

    //public static IServiceCollection AddZipkin(this IServiceCollection services) => services.AddSingleton<IZipkinSender, KafkaZipkinSender>();
    public static IServiceProvider UseZipkin(this IServiceProvider serviceProvider) => UseZipkin(serviceProvider, new TracingLogger(serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>(), "zipkin4net"));
    public static IServiceProvider UseZipkin(this IServiceProvider serviceProvider, ILogger logger)
    {
        var hostApplicationLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
        var zipkin = serviceProvider.GetRequiredService<IZipkinSender>();
        var loggerFactory = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
        StartZipkin(logger, zipkin);
        hostApplicationLifetime.ApplicationStopped.Register(() => ZipkinHelper.StopZipkin());
        return serviceProvider;
    }
}
