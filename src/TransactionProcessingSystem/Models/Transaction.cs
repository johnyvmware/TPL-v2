using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TransactionProcessingSystem.Models;

public abstract record Transaction(DateTime Date, decimal Amount)
{
    public CategoryAssignment? CategoryAssignment { get; init; }

    protected abstract string DisplayName { get; }

    public string Describe()
    {
        string description = $"""
            Transaction type: {DisplayName}
            {DescribeProperties()}
            """;

        return description;
    }

    protected abstract string DescribeProperties();
}