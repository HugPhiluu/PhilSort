


# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.1] - 2025-09-07
### Fixed
- Full localization/multi-language support for all UI and dialogs (English and Japanese included by default)
- Ensured consistency between editor window and context menu localization implementations

### Improved
- Better code consistency across all localization implementations
- Enhanced maintainability of localization system

### Changed
- All hardcoded UI strings replaced with localization lookups
- Improved maintainability and extensibility for future language support

## [1.0.1] - 2025-08-19
### Added
- History tab now logs all folder moves, merges, and when a folder is set as a target.
- Each history entry has a Jump button to quickly select the affected folder in the Project window.
- Display name of target folders can be edited directly in the UI.
- Multi-select support for adding multiple folders as targets at once.
- Added a "Do not Ask again" prompt for the confirm moving folder dialogue

### Changed
- Improved user feedback and error dialogs for invalid or duplicate folder actions.
- Tip updated to clarify multi-select support for adding target folders.

## [1.0.0] - 2025-08-18
### Added
- Initial implementation of Phil's Sorter with configurable target folders, categories, and move operations.
