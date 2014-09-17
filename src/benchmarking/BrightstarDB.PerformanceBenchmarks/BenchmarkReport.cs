using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace BrightstarDB.PerformanceBenchmarks
{
    [XmlRoot("benchmark")]
    public class BenchmarkReport
    {
        public BenchmarkReport()
        {
            Start = DateTime.UtcNow;
            Operations = new List<OperationReport>();
        }

        [XmlAttribute]
        public DateTime Start { get; set; }

        [XmlArrayItem("operation")]
        public List<OperationReport> Operations { get; private set; }

        public void LogOperationCompleted(string name, string description, int cycles, double duration)
        {
            Operations.Add(new OperationReport(name, description, OperationStatus.Ok, null, cycles, duration));
        }

        public void LogOperationException(string name, string description, string exceptionDetail)
        {
            Operations.Add(new OperationReport(name, description, OperationStatus.Exception, exceptionDetail, 0, 0));
        }

        public void LogOperationSkipped(string name, string description)
        {
            Operations.Add(new OperationReport(name, description, OperationStatus.Skipped, null, 0, 0));
        }


    }

    public class OperationReport
    {
        // For serialization
        public OperationReport(){}
        public OperationReport(string name, string description, OperationStatus status, string exceptionDetail, int cycles, double duration)
        {
            Name = name;
            Description = description;
            Status = status;
            ExceptionDetail = exceptionDetail;
            Cycles = cycles;
            TotalDuration = duration;
        }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlAttribute("status")]
        public OperationStatus Status { get; set; }

        [XmlElement("exception")]
        public string ExceptionDetail { get; set; }

        [XmlElement("cycles")]
        public int Cycles { get; set; }

        [XmlElement("duration")]
        public double TotalDuration { get; set; }
    }

    public enum OperationStatus
    {
        Ok,
        Skipped,
        Exception
    }
}
