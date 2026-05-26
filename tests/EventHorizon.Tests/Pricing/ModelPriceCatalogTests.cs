using EventHorizon.Pricing;
using Microsoft.Extensions.AI;

namespace EventHorizon.Tests.Pricing;

public class ModelPriceCatalogTests
{
    [Fact]
    public void EstimateCost_Computes_Input_And_Output_Costs()
    {
        const string json = """
        {
          "gpt-test": {
            "input_cost_per_token": 0.001,
            "output_cost_per_token": 0.002,
            "cache_read_input_token_cost_per_token": 0.0005,
            "max_tokens": 128000
          }
        }
        """;

        var catalog = ModelPriceCatalog.FromJson(json);
        UsageDetails usage = new()
        {
            InputTokenCount = 100,
            OutputTokenCount = 50,
            CachedInputTokenCount = 10,
        };

        var cost = catalog.EstimateCost("gpt-test", usage);

        Assert.True(cost.HasPrice);
        Assert.Equal(100, cost.InputTokens);
        Assert.Equal(50, cost.OutputTokens);
        Assert.Equal(10, cost.CachedInputTokens);
        Assert.Equal(0.205m, cost.TotalCost);
    }

    [Fact]
    public void FromJson_Ignores_Unused_String_MaxToken_Metadata()
    {
        const string json = """
        {
          "sample_spec": {
            "input_cost_per_token": 0.000001,
            "output_cost_per_token": 0.000002,
            "max_input_tokens": "200000",
            "max_output_tokens": "100000"
          }
        }
        """;

        var catalog = ModelPriceCatalog.FromJson(json);

        Assert.True(catalog.TryGetEntry("sample_spec", out _));
    }

    [Fact]
    public void EstimateCost_Returns_Unknown_When_Model_Is_Missing()
    {
        ModelPriceCatalog catalog = new([]);
        UsageDetails usage = new() { InputTokenCount = 11, OutputTokenCount = 7 };

        var cost = catalog.EstimateCost("missing-model", usage);

        Assert.False(cost.HasPrice);
        Assert.Equal(11, cost.InputTokens);
        Assert.Equal(7, cost.OutputTokens);
    }
}



