# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Worker .NET now runs the ingestion orchestration loop and shares the same DI setup as the API.
- Processing orchestration settings for active/idle/fault delays.
- JWT settings validation via data annotations and validation-on-start.

### Changed
- Ingestion pipeline leasing is more resilient with atomic status updates.
- Upload validation now checks filename extension and content type consistency.
- Upload storage uses async streaming with sequential I/O hints.

### Fixed
- Guarded the pipeline against missing Python processor scripts.
- Worker no longer runs as a placeholder loop.
