# Service layer

The API calls service interfaces; services coordinate Clockify HTTP calls and repository operations. Services never access AppDbContext directly. The explicit project references remain API -> Service -> Repository -> Data.

## Entry points

- `IClockifyService.GetWorkspaceUsersAsync`: list real workspace users and their Clockify IDs for mapping the supplied dataset.
- `IClockifyService.ImportAsync`: validate a dataset, reuse or create its remote projects/tasks/time entries, and insert or update each corresponding local record.
- `IClockifyService.SynchronizeAsync`: read users, active/archived projects, active/inactive tasks and paginated time entries, then update local records by Clockify ID.
- `ITimeReportService.GetReportAsync`: group completed sessions by task ID, obtain the user through the task's assignee, sum elapsed ticks, convert to hours, and round the final total to two decimals.
- `ITimeReportService.ExportCsvAsync`: generate UTF-8 CSV with the five required columns, using CsvHelper for escaping and invariant numeric formatting.

## Import example

Replace `ACTUAL_CLOCKIFY_USER_ID` with a real member ID returned by GetWorkspaceUsersAsync. Names from the paper must be mapped explicitly; the application does not create fictitious users or send invitations.

```json
{
  "tasks": [
    {
      "projectName": "Website Redesign",
      "taskName": "Homepage Mockup",
      "assignedUserClockifyId": "ACTUAL_CLOCKIFY_USER_ID",
      "originalEstimateHours": 7
    }
  ],
  "timeEntries": [
    {
      "userClockifyId": "ACTUAL_CLOCKIFY_USER_ID",
      "projectName": "Website Redesign",
      "taskName": "Homepage Mockup",
      "start": "2025-07-20T09:15:00Z",
      "end": "2025-07-20T11:45:00Z"
    }
  ]
}
```

## Responsibilities and lifetimes

- Scoped `ClockifyService`: orchestration, reference resolution, import conflict checks and save boundaries.
- Scoped `TimeReportService`: report calculation and CSV generation.
- `ClockifyClient`: HttpClient integration, API-key header, pagination, ISO-8601 request values and typed remote failures. Registered through AddHttpClient.
- Singleton `ClockifyEntityFactory`: maps Clockify DTOs to local entities, including UTC and estimate conversion.
- Singleton task/time-entry validation strategies: stateless input validation, invoked before remote access.
- Plain `ClockifyRecordValidation` helper: checks remote relationships, identities, estimates and intervals before saving. It has no interface or DI registration.
- Scoped repositories and UnitOfWork: shared DbContext; repositories do not save independently.

## Configuration

Merge the Clockify section from `EnozomTask.API/appsettings.example.json` into your existing ignored local appsettings file, or supply `Clockify__ApiKey` and `Clockify__WorkspaceId` environment variables. Never commit real credentials. Controllers expose these services; API middleware maps validation errors to 400, missing records to 404, Clockify failures to 502, and unexpected failures to a generic 500.

## Explicit behavior and limits

- One configured workspace per local database. Do not change to a different workspace while reusing the same database.
- One assigned user per task and a required task per time entry, following the agreed model. Remote records that cannot fit it are listed in SynchronizationResult.Issues; IsComplete is false. They are not silently remapped.
- Import expects all referenced tasks in the request. Import and synchronization require each session's remote user to match its task's assignee. TimeEntries has no UserId or ProjectId; both are obtained through ProjectTask. If the remote logging user differs, synchronization reports an issue instead of attributing the time to someone else. Reassigning a task changes the user displayed for its historical local time entries.
- Projects are matched by exact trimmed name; tasks by project and exact trimmed name. Existing task assignee/estimate conflicts are rejected rather than overwritten remotely.
- Completed entries are matched by user/project/task/start/end. Matching entries with different descriptions or multiple matches are rejected. This is practical retry reconciliation, not a distributed exactly-once guarantee.
- Local records are saved after each confirmed remote operation. Earlier successes remain if a later request or database save fails. There is no transaction spanning MySQL and Clockify, and remote POSTs are not automatically retried. Retry a failed import to reconcile existing remote records.
- Imports are intended to be run sequentially for this assessment. There is no process-level lock. Repeated imports reuse existing records, but simultaneous requests are not serialized.
- Synchronization inserts/updates records; it does not delete local history when a remote record disappears or becomes incompatible with the model.
- Running timers are excluded from completed totals and counted in RunningEntriesExcluded. No estimate is multiplied by the number of sessions. Tasks without completed entries do not produce a CSV row.
- Clockify's role/subscription restrictions still apply, including recording time for another user. Live write testing requires the final dataset and approved user mapping.

API contract reference: https://docs.clockify.me/
CSV library: https://joshclose.github.io/CsvHelper/

## Explaining the flow

`ImportAsync`: validate the input, find the real users, and read existing Clockify records. Reuse matching projects, tasks and time entries; create only missing ones. For every record, the save helper finds its local Clockify ID, adds or updates the entity, and saves through UnitOfWork.

`SynchronizeAsync`: read Clockify users first, then projects and their tasks, then each user's time entries. Save them in that order so referenced records already exist. Invalid remote records appear in the result's issues list rather than being assigned to the wrong task or user.

The import strategies own input rules, the Factory owns mapping, repositories own local database access, and UnitOfWork owns saving. CSV reporting stays in TimeReportService.
