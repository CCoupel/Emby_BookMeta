using Xunit;

namespace BookMeta.Tests.Support
{
    /// <summary>
    /// [Fact] des tests de spécification LOT-2 : ignoré tant que SpecApi n'est pas branché sur le code de production
    /// (dotnet test -p:Lot2Specs=true, voir BookMeta.Tests.csproj). Le test apparaît « ignoré », jamais caché.
    /// </summary>
    public sealed class SpecFactAttribute : FactAttribute
    {
        public SpecFactAttribute()
        {
            if (!SpecApi.Implemented)
            {
                Skip = "LOT-2 non branché (Lot2Specs=false) : Providers/Covers pas encore livrés";
            }
        }
    }

    /// <summary>[Theory] équivalent de SpecFact.</summary>
    public sealed class SpecTheoryAttribute : TheoryAttribute
    {
        public SpecTheoryAttribute()
        {
            if (!SpecApi.Implemented)
            {
                Skip = "LOT-2 non branché (Lot2Specs=false) : Providers/Covers pas encore livrés";
            }
        }
    }
}
