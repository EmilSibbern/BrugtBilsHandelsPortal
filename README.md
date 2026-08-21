# Brugtbilshandelsportal

En Blazor-baseret handelsportal til brugte biler. Besøgende kan gennemse aktive
annoncer, mens registrerede brugere kan oprette og administrere annoncer,
kontakte sælgere og deltage i offentlige kommentarspor. Administratorer har
adgang til moderation og brugeradministration.

## Teknologi

- .NET 10 og Blazor
- Microsoft SQL Server
- `Microsoft.Data.SqlClient`
- xUnit-baseret testprojekt

## Forudsætninger

- .NET 10 SDK
- SQL Server eller SQL Server Express
- Git

## Lokal opsætning

1. Klon repositoryet.
2. Tilpas `ConnectionStrings:DefaultConnection` med lokal konfiguration eller
   .NET user-secrets. Commit aldrig adgangskoder eller andre hemmeligheder.
3. Kør databasescripts i nummerorden fra
   `BrugtBilsHandelsPortal/Database/Scripts`.
4. Gendan pakker, byg og start løsningen:

   ```powershell
   dotnet restore
   dotnet build
   dotnet run --project BrugtBilsHandelsPortal/BrugtBilsHandelsPortal.csproj
   ```

## Test

```powershell
dotnet test
```

## Projektstruktur

- `BrugtBilsHandelsPortal/Components` – sider, layouts og delte UI-komponenter
- `BrugtBilsHandelsPortal/Services` – forretningslogik
- `BrugtBilsHandelsPortal/Data` – forbindelser, mapping og repositories
- `BrugtBilsHandelsPortal/Database` – schema-, indeks-, view- og seed-scripts
- `tests/BrugtBilsHandelsPortal.Tests` – automatiserede tests

## Arbejdsgang på GitHub

Backloggen håndteres som GitHub Issues og samles i et GitHub Project. Brug
user-story-skabelonen ved nye stories, opret en feature-branch fra `main`, og
knyt pull requesten til det relevante issue med eksempelvis `Closes #12`.

Foreslåede labels er `epic:*`, `priority:critical`, `priority:high`,
`priority:medium`, `frontend`, `backend`, `security` og `mvp`.
