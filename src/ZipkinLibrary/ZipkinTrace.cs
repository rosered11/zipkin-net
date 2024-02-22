using zipkin4net;
using zipkin4net.Annotation;

namespace Rosered.Zipkin.Net;

public abstract class ZipkinBaseTrace
{
    private const string _tagResult = "result";
    private const string _successResult = "success";
    private const string _failResult = "fail";
    protected ZipkinBaseTrace(Trace trace)
    {
        Trace = trace;
    }

    public Trace Trace { get; protected set; }
    public void AddAnnotation(IAnnotation annotation)
    {
        Trace.Record(annotation);
    }
    public async Task TryProcessAsync(Func<Task> action)
    {
        try
        {
            await action();
            Trace.Record(Annotations.Tag(_tagResult, _successResult));
        }
        catch (Exception)
        {
            Trace.Record(Annotations.Tag(_tagResult, _failResult));
            throw;
        }
    }
    public void TryProcess(Action action)
    {
        try
        {
            action();
            Trace.Record(Annotations.Tag(_tagResult, _successResult));
        }
        catch (Exception)
        {
            Trace.Record(Annotations.Tag(_tagResult, _failResult));
            throw;
        }
    }
}
public sealed class ZipkinServerTrace : ZipkinBaseTrace, IDisposable
{

    public ZipkinServerTrace(Trace trace, string serviceName, string rpc) : base(trace)
    {
        Trace = trace;
        Trace.Record(Annotations.ServerRecv());
        Trace.Record(Annotations.ServiceName(serviceName));
        Trace.Record(Annotations.Rpc(rpc));
    }

    public void Dispose()
    {
        Trace.Record(Annotations.ServerSend());
    }
}

public sealed class ZipkinProducerTrace : ZipkinBaseTrace, IDisposable
{
    private const string eventTag = "EventName";
    private const string statusTag = "Status";
    public ZipkinProducerTrace(Trace trace, string serviceName, string rpc, string eventName, string status) : base(trace.Child())
    {
        Trace.Record(Annotations.ProducerStart());
        Trace.Record(Annotations.ServiceName(serviceName));
        Trace.Record(Annotations.Rpc(rpc));
        Trace.Record(Annotations.Tag(eventTag, eventName));
        Trace.Record(Annotations.Tag(statusTag, status));
    }

    public void Dispose()
    {
        Trace.Record(Annotations.ProducerStop());
    }
}

public sealed class ZipkinConsumerTrace : ZipkinBaseTrace, IDisposable
{
    private const string eventTag = "EventName";
    private const string statusTag = "Status";
    public ZipkinConsumerTrace(Trace trace, string serviceName, string rpc, string eventName, string status) : base(trace.Child())
    {
        Trace.Record(Annotations.ConsumerStart());
        Trace.Record(Annotations.ServiceName(serviceName));
        Trace.Record(Annotations.Rpc(rpc));
        Trace.Record(Annotations.Tag(eventTag, eventName));
        Trace.Record(Annotations.Tag(statusTag, status));
    }

    public void Dispose()
    {
        Trace.Record(Annotations.ConsumerStop());
    }
}
