namespace Evo.Engine.Host.Clean.Discovery;

public static class SourceClassifier
{
    public static DiscoveredSource InspectDirectory(
        string id,
        string name,
        string category,
        string subcategory,
        string rawPath,
        string sourceType,
        string relatedComponent,
        string? ownerUser,
        string preliminaryRisk,
        string inclusionReason,
        DiscoveryOptions options,
        CancellationToken ct,
        bool metadataOnly = false)
    {
        var restrictions = new List<string>();
        var errors = new List<string>();
        string normalizedPath;
        try
        {
            normalizedPath = Path.GetFullPath(rawPath);
        }
        catch (Exception ex)
        {
            errors.Add($"Invalid path: {ex.Message}");
            return new DiscoveredSource(
                id, name, category, subcategory, rawPath, sourceType, relatedComponent, ownerUser,
                false, false, null, null, null,
                DiscoverySourceState.Inaccessible.ToString(),
                preliminaryRisk, inclusionReason, restrictions, errors);
        }

        if (!Directory.Exists(normalizedPath))
        {
            return new DiscoveredSource(
                id, name, category, subcategory, normalizedPath, sourceType, relatedComponent, ownerUser,
                false, false, null, null, null,
                DiscoverySourceState.Discovered.ToString(),
                preliminaryRisk, inclusionReason, restrictions, errors);
        }

        try
        {
            var dirInfo = new DirectoryInfo(normalizedPath);
            if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                restrictions.Add("reparse_point_skipped");
                return new DiscoveredSource(
                    id, name, category, subcategory, normalizedPath, sourceType, relatedComponent, ownerUser,
                    true, true, null, null, dirInfo.LastWriteTimeUtc.ToString("O"),
                    DiscoverySourceState.PartiallyInspected.ToString(),
                    preliminaryRisk, inclusionReason, restrictions, errors);
            }

            if (metadataOnly)
            {
                restrictions.Add("metadata_only_protected");
                return new DiscoveredSource(
                    id, name, category, subcategory, normalizedPath, sourceType, relatedComponent, ownerUser,
                    true, true, null, null, dirInfo.LastWriteTimeUtc.ToString("O"),
                    DiscoverySourceState.Accessible.ToString(),
                    preliminaryRisk, inclusionReason, restrictions, errors);
            }

            int fileCount = 0;
            long totalBytes = 0;
            bool partial = false;
            var enumOpts = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = options.MaxDepth > 1,
                MaxRecursionDepth = Math.Max(0, options.MaxDepth - 1),
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            foreach (string file in Directory.EnumerateFiles(normalizedPath, "*", enumOpts))
            {
                ct.ThrowIfCancellationRequested();
                if (fileCount >= options.MaxFilesPerSource)
                {
                    partial = true;
                    restrictions.Add($"max_files_limit_{options.MaxFilesPerSource}");
                    break;
                }
                try
                {
                    var fi = new FileInfo(file);
                    if ((fi.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                    fileCount++;
                    totalBytes += fi.Length;
                }
                catch (Exception ex)
                {
                    partial = true;
                    errors.Add(ex.Message);
                }
            }

            var state = partial ? DiscoverySourceState.PartiallyInspected : DiscoverySourceState.Inspected;
            return new DiscoveredSource(
                id, name, category, subcategory, normalizedPath, sourceType, relatedComponent, ownerUser,
                true, true, fileCount, totalBytes, dirInfo.LastWriteTimeUtc.ToString("O"),
                state.ToString(),
                preliminaryRisk, inclusionReason, restrictions, errors);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            errors.Add(ex.Message);
            restrictions.Add("access_denied");
            return new DiscoveredSource(
                id, name, category, subcategory, normalizedPath, sourceType, relatedComponent, ownerUser,
                true, false, null, null, null,
                DiscoverySourceState.Inaccessible.ToString(),
                preliminaryRisk, inclusionReason, restrictions, errors);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
            return new DiscoveredSource(
                id, name, category, subcategory, normalizedPath, sourceType, relatedComponent, ownerUser,
                true, false, null, null, null,
                DiscoverySourceState.Inaccessible.ToString(),
                preliminaryRisk, inclusionReason, restrictions, errors);
        }
    }
}
