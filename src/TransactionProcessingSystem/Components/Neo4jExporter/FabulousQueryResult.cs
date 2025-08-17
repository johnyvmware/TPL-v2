using System;
using Neo4j.Driver;

namespace TransactionProcessingSystem.Components.Neo4jExporter;

public class FabulousQueryResult<T>
    {
        public IEnumerable<T> Data { get; init; } = new List<T>();
        public long ExecutionTimeMs { get; init; }
        public string QueryPlan { get; init; } = string.Empty;
        public IResultSummary? Summary { get; init; }
        public bool IsSuccess { get; init; } = true;
        public string ErrorMessage { get; init; } = string.Empty;
        
        public static FabulousQueryResult<T> Success(IEnumerable<T> data, long executionTime, IResultSummary? summary = null)
        {
            return new FabulousQueryResult<T>
            {
                Data = data,
                ExecutionTimeMs = executionTime,
                Summary = summary,
                IsSuccess = true
            };
        }
        
        public static FabulousQueryResult<T> Failure(string error, long executionTime)
        {
            return new FabulousQueryResult<T>
            {
                ExecutionTimeMs = executionTime,
                ErrorMessage = error,
                IsSuccess = false
            };
        }
    }
