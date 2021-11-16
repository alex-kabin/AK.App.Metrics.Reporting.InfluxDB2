# App.Metrics.Reporting.InfluxDB2

[![](https://img.shields.io/nuget/dt/App.Metrics.Reporting.InfluxDB2?style=for-the-badge&label=nuget%20downloads)](https://www.nuget.org/packages/App.Metrics.Reporting.InfluxDB2)

Usage sample:

binding config in C#:
```
if (configuration.GetSection(nameof(InfluxDbOptions)).Exists()) {
    builder.Report.ToInfluxDb(
        options => {
            configuration.GetSection(nameof(InfluxDbOptions)).Bind(options.InfluxDb);
            options.FlushInterval = TimeSpan.FromSeconds(5);
        }
    );
}
```

configuration section:
```
"InfluxDbOptions": {
  "BaseUri": "http://localhost:8086/api/v2/",
  "Bucket": "my_metrics",
  "Token": "IORPUJjn_FeqkAwIiPuxzcRlnEF5COXo2rFtkgxEareZuvzBnLngpwVV6jNNAcq5285r1PUNqO7xh4s1hqlcAA==",
  "OrganizationId": "57c2f3218e7fc734",
  "CreateBucketIfNotExists": true,
  "BucketRetentionDuration": "06:00:00",
  "EnableGzip": true
}
```
