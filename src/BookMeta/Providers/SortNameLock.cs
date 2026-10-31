using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace BookMeta.Providers;

/// <summary>
/// Emby 4.10.2 : <c>BaseItem.AfterMetadataRefresh</c> appelle <c>ResetSortName</c>, qui remet le tri à « calculé depuis le nom »
/// sauf si <see cref="MetadataFields.SortName"/> figure dans <c>LockedFields</c>. Un <c>SortName</c> fourni par un lecteur local
/// est donc perdu après chaque actualisation, à moins de verrouiller le champ sur l'item réel (voir docs/SDK_NOTES.md §8).
/// </summary>
public static class SortNameLock
{
    /// <summary>Verrouille le champ SortName de <paramref name="item"/> puis y pose <paramref name="sortName"/>.</summary>
    public static void Apply(BaseItem item, string sortName)
    {
        if (string.IsNullOrWhiteSpace(sortName)) return;
        var fields = item.LockedFields ?? Array.Empty<MetadataFields>();
        if (!fields.Contains(MetadataFields.SortName))
            item.LockedFields = fields.Append(MetadataFields.SortName).ToArray();
        item.SortName = sortName;
    }
}
