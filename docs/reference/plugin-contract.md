# Plugin Contract

Plugins are versioned extension packages executed by probe runtimes and managed by the control plane.

## Identity

Plugin identity is the combination of:

- `id`
- `version`

The same identity must be used consistently in registration, assignment, bundle filenames, manifests, probe downloads, and reported samples.

## Execution Modes

- `SCHEDULED`: executed by the probe runtime on an interval after assignment and scheduled test enablement.
- `ACTION`: executed only when the control plane queues an on-demand action for a probe.

## Bundle Contract

Each bundle must contain:

- `manifest.json`
- plugin implementation entrypoint

Current bundle archives are served from the control plane bundle directory using:

```text
<plugin-id>-<plugin-version>.zip
```

## Manifest Contract

The manifest should identify the plugin, version, execution mode, checksum context, and any operator-facing metadata needed by the control plane or probe runtime.

## Scheduled Result Shape

Scheduled plugins return normalized metrics and optional records:

```json
{
  "metrics": [
    { "name": "metric_name", "kind": "gauge", "value": 1.0, "labels": { "k": "v" } }
  ],
  "records": [
    {
      "category": "ping",
      "testType": "PING",
      "target": "8.8.8.8",
      "passed": true,
      "latencyMs": 12.3
    }
  ]
}
```

## Action Result Shape

Action plugins return status, metrics, and an action record:

```json
{
  "status": "SUCCEEDED",
  "metrics": [
    { "name": "metric_name", "kind": "gauge", "value": 1.0, "labels": { "k": "v" } }
  ],
  "record": {
    "category": "action",
    "pluginId": "WIFI_SCAN_ACTION",
    "passed": true
  }
}
```

## Dashboard Metadata

Plugins may provide dashboard metadata during registration. Dashboard import happens during plugin registration, not when an embed session is requested.

Sample dashboard definitions live in [plugin dashboard examples](../examples/plugin-dashboards/README.md).

## Change Rules

- Plugin IDs, versions, manifests, bundle filenames, and output shapes are cross-component contracts.
- Contract changes require updates to this reference, API docs, probe runtime behavior, central registration behavior, and tests.
- New plugin output metrics must also update [metrics.md](./metrics.md).
