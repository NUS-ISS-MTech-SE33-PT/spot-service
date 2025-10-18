# Repository Guidelines

## Project Structure & Module Organization
- `spot-service/` holds the .NET 9 minimal API with `Program.cs` endpoints, DynamoDB models (`Spot.cs`, `CenterSummary.cs`) and data access (`SpotRepository.cs`).
- Environment config lives in `spot-service/appsettings*.json`; specify the `DynamoDb` table name before running locally.
- Infrastructure and delivery scripts sit in `terraform/` (Fargate, NLB, API Gateway) and `docker-compose.yml` builds the API container for local smoke tests.

## Build, Test, and Development Commands
- `dotnet restore spot-service/spot-service.csproj` installs project dependencies.
- `dotnet build spot-service/spot-service.csproj` validates the solution compiles with nullable reference types enabled.
- `dotnet run --project spot-service` starts the API on `https://localhost:5001`; use `dotnet watch run --project spot-service` while iterating.
- `docker-compose up --build makangoapi` mirrors the production container entrypoint.

## Coding Style & Naming Conventions
- Follow C# conventions: PascalCase for public types, camelCase for locals, async suffix on tasks (`GetAllSpotsAsync`).
- Keep indentation at 4 spaces, with `using` directives grouped at the top and implicit usings left enabled.
- Prefer dependency injection over static helpers; repository logic should stay in `SpotRepository` with DTOs in `GetSpotsResponse`.

## Testing Guidelines
- Target xUnit tests in a sibling `spot-service.Tests` project; name files `<TypeName>Tests.cs` and methods `MethodName_State_Expected`.
- Run tests with `dotnet test`; add `--collect:"XPlat Code Coverage"` when checking coverage.
- Mock DynamoDB via the AWS SDK interfaces; avoid hitting real tables in unit tests.

## Commit & Pull Request Guidelines
- Match the existing history's short, imperative messages (`add endpoint`, `fix config error`); keep the first line under 72 characters.
- One logical change per commit; include follow-up context in the body if you touch Terraform or infrastructure wiring.
- Pull requests must link to the tracking issue, outline testing performed, and attach API response samples or screenshots when endpoints change.

## Environment & Deployment Notes
- Local runs need AWS credentials with DynamoDB access; set `AWS_PROFILE` or `AWS_ACCESS_KEY_ID` + `AWS_SECRET_ACCESS_KEY` before launching.
- Terraform deployments expect remote state buckets pre-configured in `backend.tf`; run `terraform init` before `terraform plan -var='image_uri=...'`.
