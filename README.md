# OneVault

A temporary encrypted secret storage service with a JSON API, gRPC, and an MCP endpoint at `/mcp`.

## Connect the plugin to Codex

You need Docker with Docker Compose and the Codex CLI installed. Run the commands from the repository root.

### 1. Start OneVault

```bash
cd src/OneVault
docker compose up -d
cd ../..
```

Compose builds the image on the first run. The MCP endpoint is available at `http://localhost:5000/mcp`.

Check the container status and view its logs:

```bash
docker compose -f src/OneVault/compose.yaml ps
docker compose -f src/OneVault/compose.yaml logs --tail=100 onevault
```

Rebuild the image after changing the code:

```bash
docker compose -f src/OneVault/compose.yaml up -d --build
```

### 2. Install the plugin

From the repository root, register the local marketplace and install OneVault:

```bash
codex plugin marketplace add .
codex plugin add onevault@onevault-local
```

The marketplace is defined in `.agents/plugins/marketplace.json`. It points to `plugins/onevault`, which contains the plugin manifest (`plugin.json`) and the MCP server connection settings (`mcp.json`).

### 3. Start a new Codex chat

After installation, start a new chat or CLI session so Codex loads the MCP tools. If the plugin does not appear in the desktop app, restart the app and check the Plugins section.

Example prompt:

> Use OneVault to store the text "Test secret" for 60 seconds and return the token.

Then:

> Read the secret from OneVault using the token TOKEN_FROM_RESPONSE.

The plugin provides two tools:

| Tool | Arguments | Result |
| --- | --- | --- |
| `create_secret` | `text`, `ttlSeconds` (1–86400) | `token`, `expiresAt` |
| `get_secret` | `token` | `text`, `expiresAt` |

The plugin connects to an existing server and does not start Docker automatically. The `localhost` address assumes Codex runs on the same computer as the container. For another host, update the URL in `plugins/onevault/mcp.json` to a server address reachable from that host and reinstall the plugin.

The token grants access to the secret. Data is stored in memory and lost when the container restarts. Client authentication is not configured in the service.

### Stop OneVault

From the repository root:

```bash
docker compose -f src/OneVault/compose.yaml down
```

For more information about plugin packaging and local marketplaces, see the [OpenAI documentation](https://developers.openai.com/plugins/build/plugins).
