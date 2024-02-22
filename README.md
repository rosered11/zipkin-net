# zipkin-net

## Intro

Startup.cs file

```c#
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddSingleton<IZipkinSender, CustomZipkinSender>();
    ...
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    ...
    app.ApplicationServices.UseZipkin();
    ...
}
```

## Zipkin Server Trace

```c#
// init trace
var trace = Trace.Create();
using var serverTrace = new ZipkinServerTrace(currentTrace, "portal", "create order sap");

// Use TryProcess for add tag result of process
serverTrace.TryProcess(() =>
{
    // Add custom tag
    serverTrace.AddAnnotation(Annotations.Tag("MyTag", "MyValue"));

    // Get header Zipkin XB3
    string zipkinXB3 = ZipkinHelper.CreateXB3(trace.CurrentSpan);

    // Get trace id
    string traceId = trace.CurrentSpan.SerializeTraceId();

    // Process
});
```

## Zipkin Producer Trace

```c#
string zipkinXB3 = ZipkinHelper.CreateXB3(trace.CurrentSpan);

Trace trace = ZipkinHelper.GetTrace(zipkinXB3);

using var producerTrace = new ZipkinProducerTrace(trace, "{service-name}.outbox", "Send event {event-name}", {event-name});

Dictionary<string, string> zipkinXB3Dic = ZipkinHelper.CreateHeaders(producerTrace.Trace.CurrentSpan);

// Process

```

## Zipkin Consumer Trace

```c#
Dictionary<string, string> zipkinXB3Dic = ZipkinHelper.CreateHeaders(producerTrace.Trace.CurrentSpan);

// Convert Dictionary to Kafka.IHeader
Kafka.IHeader zipkinHeadersKafka = zipkinXB3Dic;

// Get trace from 
Trace trace = ZipkinHelper.GetTrace(zipkinHeadersKafka);

using var consumerTrace = new ZipkinConsumerTrace(trace, "{service-name}.listener", "Process event {event-name}", {event-name});

// Process

```

## Command push nuget package

`dotnet nuget push bin/Release/Rosered.Zipkin.Net.0.0.1.nupkg --api-key {} --source https://api.nuget.org/v3/index.json`