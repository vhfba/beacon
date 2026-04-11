# Plugin Bundles

Place probe plugin bundle artifacts in this folder using the naming convention:

- `<plugin-id>-<plugin-version>.zip`

Examples:

- `plugin-http-v2-2.1.0.zip`
- `plugin-iperf-1.0.0.zip`

The central server exposes bundle download at:

- `/plugins/{pluginId}/{version}/bundle`

If running with Docker Compose, this folder is mounted read-only into the container at `/app/plugin-bundles`.
