# Acceptance Criteria Baseline Review

- **Previous reviewed digest**: `fdd5825c138ae898b514206ec577b1b411ef226dd62a93ad219d1de88f06e452`
- **Refreshed digest**: `9a02495835bbe110242d9b8a9d404f649b07763bc363605a46aa63416c118679`
- **Change**: Added AC-027 and SC-021 for visible, correctly colored arrowhead tips across all supported node shapes.
- **Reason**: Visual QA found the rasterizer hid arrow tips under node borders; its triangle fill also omitted the tip pixel. The criterion makes this user-visible connector behavior testable.
- **Review boundary**: Existing AC-002/SC-007 human usability reviews remain unchanged and still require five real stakeholders.
