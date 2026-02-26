# MAK-75 Implementation Note

## What Was Done
- Added centralized exception handling middleware with a standard JSON error schema.
- Masked sensitive request headers in request logging.
- Updated endpoint validation/auth/not-found responses to use the standard error shape.

## Implemented
- No stack traces returned in production (`500` returns generic message with `traceId`).
- No token exposure in request logs (`Authorization`, cookies, `x-user-sub` masked).
- Standard error response fields: `code`, `message`, `traceId`.

## Pending / Notes
- Error schema coverage now includes the explicit errors in `Program.cs`; deeper repository/service exceptions are handled by the global exception middleware.
