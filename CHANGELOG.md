# Changelog

## [0.1.4-preview] - 2024-11-18

### Changed

- Bumped the .NET SDK to 9.0.100 and `TargetFramework` to `net9.0`.

## [0.1.3] - 2024-11-11

### Added

- Added (non-transitive) `SampleEquivalenceComparer` based on `MannWhitneyTest` with a threshold of 2% and a significance level of 10<sup>–5</sup>.
- Added `RankHelpers.GetRanksOrdered<T, TComparer>()` method for ranking the ordered entries, handling ties in a standard way ("1224" ranking).
- Added a warm-up phase to reduce outliers and heavy tails. 
- Added a table control for adding URLs directly from the UI, apart from loading the project.

### Changed

- Replaced the arbitrary debugging timeout of 1 second for request with the default timeout of 100 seconds.
- `HttpClient` instances are now created for each job instead of sharing the single instance.

### Removed

- Removed `MetricsHeuristicComparer` in favor of simple mean comparison, where distributions close in terms of `SampleEquivalenceComparer` are treated as equivalent.

### Fixed

- Reduced the likelihood of instances of the same URL being distributed across different ranks. 

[0.1.4-preview]: https://github.com/qbit86/responsive-flow/compare/responsive-flow-0.1.3...HEAD

[0.1.3]: https://github.com/qbit86/responsive-flow/compare/responsive-flow-0.1.2...responsive-flow-0.1.3

