# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction

## Error Handling in Blazor
- In a Blazor solution, the ErrorBoundary behavior differs depending on prerendering:
  - Enabling prerendering causes exceptions to be handled by the server-side layout ErrorBoundary.
  - Disabling prerendering shows the client `blazor-error-ui` instead.

## Logging Configuration
- Do not use Grafana Tempo; ensure that traces are sent to Jaeger instead.
- Do not use Loki; logs should go to Seq.