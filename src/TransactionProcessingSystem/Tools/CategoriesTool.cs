using OpenAI.Responses;

namespace TransactionProcessingSystem.Tools;

public static class CategoriesTool
{
    private static readonly BinaryData _emptyFunctionParameters = BinaryData.FromString("{}");

    public static IEnumerable<ResponseTool> Get()
    {
        return
        [
            GetHomeCategoriesTool(),
            GetDailyCategoriesTool(),
            GetEducationCategoriesTool(),
            GetTransportCategoriesTool(),
            GetEntertainmentCategoriesTool(),
            GetHealthCategoriesTool(),
            GetPersonalCategoriesTool(),
            GetOtherCategoriesTool()
        ];
    }

    // here when function schema is strict (true), the schema must be supplied with the additonal properties set to false
    // so maybe better to 
    public static ResponseTool GetHomeCategoriesTool()
    {
/*         var functionDefinition = new
        {
            name = "getcategories_home",
            description = "Returns categories for the home page",
            parameters = new
            {
                type = "object",
                properties = new { },
                required = Array.Empty<string>(),
                additionalProperties = false
            }
        }; */

        return ResponseTool.CreateFunctionTool(
            functionName: "getcategories-home",
            functionDescription: "Returns a list of all subcategories under the \"Home\" category",
            functionParameters: _emptyFunctionParameters,
            functionSchemaIsStrict: false);
    }

    public static ResponseTool GetDailyCategoriesTool()
    {
        return ResponseTool.CreateFunctionTool(
            functionName: "getcategories-daily",
            functionDescription: "Returns a list of all subcategories under the \"Daily\" category",
            functionParameters: _emptyFunctionParameters,
            functionSchemaIsStrict: false);
    }

    public static ResponseTool GetEducationCategoriesTool()
    {
        return ResponseTool.CreateFunctionTool(
            functionName: "getcategories-education",
            functionDescription: "Returns a list of all subcategories under the \"Education\" category",
            functionParameters: _emptyFunctionParameters,
            functionSchemaIsStrict: false);
    }

    public static ResponseTool GetTransportCategoriesTool()
    {
        return ResponseTool.CreateFunctionTool(
            functionName: "getcategories-transport",
            functionDescription: "Returns a list of all subcategories under the \"Transport\" category",
            functionParameters: _emptyFunctionParameters,
            functionSchemaIsStrict: false);
    }

    public static ResponseTool GetEntertainmentCategoriesTool()
    {
        return ResponseTool.CreateFunctionTool(
            functionName: "getcategories-entertainment",
            functionDescription: "Returns a list of all subcategories under the \"Entertainment\" category",
            functionParameters: _emptyFunctionParameters,
            functionSchemaIsStrict: false);
    }

    public static ResponseTool GetHealthCategoriesTool()
    {
        return ResponseTool.CreateFunctionTool(
            functionName: "getcategories-health",
            functionDescription: "Returns a list of all subcategories under the \"Health\" category",
            functionParameters: _emptyFunctionParameters,
            functionSchemaIsStrict: false);
    }

    public static ResponseTool GetPersonalCategoriesTool()
    {
        return ResponseTool.CreateFunctionTool(
            functionName: "getcategories-personal",
            functionDescription: "Returns a list of all subcategories under the \"Personal\" category",
            functionParameters: _emptyFunctionParameters,
            functionSchemaIsStrict: false);
    }

    public static ResponseTool GetOtherCategoriesTool()
    {
        return ResponseTool.CreateFunctionTool(
            functionName: "getcategories-other",
            functionDescription: "Returns a list of all subcategories under the \"Other\" category",
            functionParameters: _emptyFunctionParameters,
            functionSchemaIsStrict: false);
    }
}