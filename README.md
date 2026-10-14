# Bazos Watchdog

Web-based Bazos.cz listing monitor built with .NET 10 and SQLite. Adding a query immediately imports the first three result pages as a notification-free baseline. It then checks each saved query daily at 08:00, 14:00, and 20:00 in the `Europe/Prague` timezone, notifying only listings discovered after that baseline.

## Run with Docker

1. Copy `.env.example` to `.env` and set `NTFY_URL` to the complete ntfy topic URL.
2. Run `docker compose up -d --build`.
3. Open `http://localhost:8080`, add a name and a complete Bazos.cz search URL, and leave the service running.

The SQLite database persists in `./data/bazos-watchdog.db`. Listings are unique per query by normalized title, are retained for 14 days, and the UI shows only listings posted during the last seven days.

ntfy notifications contain the listing title, price/location, and link. When `NTFY_URL` is empty, results are still collected and notifications stay pending until a URL is configured.
