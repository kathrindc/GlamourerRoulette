using System;

namespace GlamourerRoulette;

[Serializable]
public class DesignPreference
{
    public Guid DesignId { get; init; }
    public string Name { get; init; } = "";
    public bool Enabled { get; set; } = true;
}
