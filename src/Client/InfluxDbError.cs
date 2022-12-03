using System.Runtime.Serialization;

namespace App.Metrics.Reporting.InfluxDb2.Client
{
    [DataContract]
    public class InfluxDbError
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }
    }
}
