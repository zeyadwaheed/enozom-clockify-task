# API layer

Controllers handle HTTP requests and responses; services handle use cases. Controllers do not use AppDbContext, repositories or HttpClient directly.

## Start

From the solution directory:

```powershell
dotnet run --project EnozomTask.API --launch-profile https
```

Open https://localhost:7057/swagger in Development. Import examples are included in Swagger and `EnozomTask.API.http`.

## Local configuration

Keep the existing MySQL connection string in your ignored `appsettings.json`. For a new checkout, copy `appsettings.example.json` to `appsettings.json` and supply your local values.

Add this top-level section to the local settings file, with your real values:

```json
"Clockify": {
  "ApiKey": "YOUR_PERSONAL_API_KEY",
  "WorkspaceId": "YOUR_WORKSPACE_ID"
}
```

Alternatively use `Clockify__ApiKey` and `Clockify__WorkspaceId` environment variables. The personal key belongs on the server, not in API request bodies or Swagger authorization fields.

The code-first schema is created with:

```powershell
dotnet ef database update --project EnozomTask.Data --startup-project EnozomTask.API
```

## Endpoints

| Method | Route | Behavior |
| --- | --- | --- |
| GET | `/api/clockify/users` | Reads real remote workspace users and their IDs for dataset mapping. |
| POST | `/api/clockify/import` | Accepts the dataset, creates/reuses Clockify records, and saves local counterparts. |
| POST | `/api/clockify/sync` | Pulls remote data into MySQL. Inspect `isComplete` and `issues` in the 200 response. |
| GET | `/api/reports/time` | Returns local completed-time totals and the number of excluded running timers. |
| GET | `/api/reports/time/csv` | Downloads `tracked-time.csv`; `X-Running-Entries-Excluded` gives the running-timer count. |

Use `/users` first, replace the placeholder user ID in the import example, then import, preview the report, and download the CSV. Calling import or sync has side effects; report endpoints only read the local database.

The example's 09:15–11:45 UTC session yields 2.50 hours. The CSV columns are exactly `User`, `Project`, `Task`, `Original Estimate(hrs)`, and `TimeSpent(hrs)`. An empty database returns an empty report and a CSV containing the header row.

## Error responses

Errors contain `error`, `traceId`, optional `remoteStatusCode`, and optional `validationErrors`.

- 400: invalid JSON, model binding, business validation or missing Clockify configuration.
- 404: a service raises NotFoundException.
- 502: Clockify rejects a request or is unavailable. Remote 401/403/429 codes are preserved in `remoteStatusCode`.
- 500: unexpected errors; details are logged server-side and not returned to the caller.

Import and synchronization are not distributed transactions. Earlier successful writes remain if a later operation fails; see the Service README for retry behavior and model limitations. This task API has no application authentication configured; the Clockify key authenticates outbound requests only.
