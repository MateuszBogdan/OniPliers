using PeterHan.PLib.Options;

namespace Pliers;

public class PliersConfig
{
    [Option("Pliers.PliersStrings.STRING_PLIERS_OPTIONS_ERRANDS", "Pliers.PliersStrings.STRING_PLIERS_OPTIONS_ERRANDS_TOOLTIP")]
    public bool ErrandEnabled { get; set; }
}