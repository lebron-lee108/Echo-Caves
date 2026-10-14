// Kept as a diagnostic record; native async build is the supported long-running path.
throw new System.InvalidOperationException("Do not queue via delayCall. Stop Play, run SliceBuilder.Validate(), then unity command build --target StandaloneWindows64 --outputPath <versioned exe> --scenes <JSON scene array> --confirm true; poll build_status and retain its summary.");
