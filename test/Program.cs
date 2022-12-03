using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Logging;
using test;

LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());

var metrics = new MetricsBuilder()
              .Report.ToInfluxDb(
                  options => {
                      options.InfluxDb.BaseUri = new Uri("http://localhost:8086/api/v2/");
                      options.InfluxDb.Bucket = "my-metrics";
                      options.InfluxDb.Token = "m7fWrpKAbxGIalbqy4dCtCVysxEXRK9HjviJFHGMtLhFmrB04G0tqtHZ1w5v8ncki5cbfCteX-bnnRX3CzPQAA==";
                      options.InfluxDb.OrganizationId = "948fd3f515c08878";
                      options.InfluxDb.CreateBucketIfNotExists = true;
                      options.InfluxDb.BucketRetentionDuration = TimeSpan.FromHours(5);
                      options.InfluxDb.EnableGzip = true;
                      options.FlushInterval = TimeSpan.FromSeconds(5);
                  }
              )
              .Build();

var counterOptions = new CounterOptions {
    Name = "test-counter",
    MeasurementUnit = Unit.Calls,
    Context = "Demo",
    Tags = MetricTags.FromSetItemString("test")
};

Console.WriteLine("Press any key to increment counter, press Esc to exit");
while (Console.ReadKey(true).Key != ConsoleKey.Escape) {
    Console.WriteLine("Increment");
    metrics.Measure.Counter.Increment(counterOptions);
    await Task.WhenAll(metrics.ReportRunner.RunAllAsync());
}