using System;
using System.Configuration;
using Serilog;

namespace GridDomain.Logging
{
    public class DefaultLoggerConfiguration : LoggerConfiguration
    {
        public DefaultLoggerConfiguration()
        {
            var filePath = ConfigurationManager.AppSettings["logFilePath"] ?? @"C:\Logs";
            var machineName = ConfigurationManager.AppSettings["envName"] ?? Environment.MachineName;
            var elasticEndpoint = ConfigurationManager.AppSettings["logElasticEndpoint"] ?? "http://soloinfra.cloudapp.net:9222";
            var configuration = WriteTo.RollingFile(filePath + "\\logs-{Date}.txt")
                .WriteTo.Elasticsearch(elasticEndpoint)
                .WriteTo.Console()
                .Enrich.WithProperty("MachineName", machineName);

            foreach (var type in TypesForScalarDestructionHolder.Types)
            {
                configuration = configuration.Destructure.AsScalar(type);
            }
        }
    }
}