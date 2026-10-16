# Chart Usability Review

Use five independent reviewers. Let each person inspect the charts before explaining them. Record one response per reviewer; do not replace missing responses with implementer or AI ratings.

## Viewing setup

- **Compact chart:** [release check](flows/compact-release-check.png), 1188 × 1208 pixels, viewed at native size.
- **Large chart:** [release pipeline tile 1](flows/tiled-release-pipeline/tiled-release-pipeline-x0000000-y0000000.png), then continue in [manifest order](flows/tiled-release-pipeline/tiled-release-pipeline.tiles.json). Tiles are 1800 pixels with 128-pixel overlaps. The renderer reports two remaining connector crossings.
- Ask reviewers to inspect both examples without a verbal walkthrough and rate whether the main path is clear at native size. If they zoom for detail, record it.

## Record one row per reviewer and chart

| Reviewer ID / role | Chart | Simple 1–5 | Elegant 1–5 | Usable 1–5 | Useful 1–5 | Followed main path? | Purpose and takeaway in their words | Zoom or help needed? |
|---|---|---:|---:|---:|---:|---|---|---|
| R1 | Compact |  |  |  |  |  |  |  |
| R1 | Large |  |  |  |  |  |  |  |
| R2 | Compact |  |  |  |  |  |  |  |
| R2 | Large |  |  |  |  |  |  |  |
| R3 | Compact |  |  |  |  |  |  |  |
| R3 | Large |  |  |  |  |  |  |  |
| R4 | Compact |  |  |  |  |  |  |  |
| R4 | Large |  |  |  |  |  |  |  |
| R5 | Compact |  |  |  |  |  |  |  |
| R5 | Large |  |  |  |  |  |  |  |

Score each dimension from 1 (poor) to 5 (excellent): **simple** means the purpose and main flow are clear; **elegant** means spacing and visual conventions are consistent; **usable** means labels and relationships can be followed; **useful** means the chart answers a real team question.

## Acceptance check

- At least four of five reviewers score **every dimension** at least 4/5 for both charts.
- At least four of five can state the chart's purpose and primary takeaway within 30 seconds without a walkthrough.
- The intended stakeholder scores the published showcase at least 4/5 and follows its main path without a walkthrough.

Record the actual results and any layout issues in `specs/068-polished-local-flowcharts/verification.md`. Keep individual names private unless they agree to be identified.
