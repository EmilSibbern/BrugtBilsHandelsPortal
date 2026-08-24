# Database

Databasen oprettes med SQL Server-scripts, så en lokal database kan genskabes
uden maskinspecifikke filstier eller data fra andre miljøer.

## Opsætning

Kør scripts fra `Scripts` i nummerorden med en SQL Server-bruger, der må oprette
databaser:

1. `001_CreateDatabase.sql` opretter databasen, hvis den ikke findes.
2. `002_CreateTables.sql` opretter tabeller, relationer og constraints.
3. `003_CreateIndexes.sql` opretter søge- og relationsindeks.
4. `004_CreateViews.sql` opretter læsemodellen til aktive annoncer.
5. `005_SeedData.sql` opretter brugergrupper og bilmærker.
6. `006_UseWholeNumberPrices.sql` migrerer ældre databaser til priser i hele kroner.
7. `007_RenameAnnouncementSchema.sql` retter det tidligere `Announce`-navn til `Announcement`.
8. `008_UpdateCommentTrigger.sql` opdaterer triggeren til redigering af kommentarer.

Eksempel med SQL Server Express og Windows Authentication:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -b -i ".\Scripts\001_CreateDatabase.sql"
sqlcmd -S ".\SQLEXPRESS" -E -b -i ".\Scripts\002_CreateTables.sql"
sqlcmd -S ".\SQLEXPRESS" -E -b -i ".\Scripts\003_CreateIndexes.sql"
sqlcmd -S ".\SQLEXPRESS" -E -b -i ".\Scripts\004_CreateViews.sql"
sqlcmd -S ".\SQLEXPRESS" -E -b -i ".\Scripts\005_SeedData.sql"
sqlcmd -S ".\SQLEXPRESS" -E -b -i ".\Scripts\006_UseWholeNumberPrices.sql"
sqlcmd -S ".\SQLEXPRESS" -E -b -i ".\Scripts\007_RenameAnnouncementSchema.sql"
sqlcmd -S ".\SQLEXPRESS" -E -b -i ".\Scripts\008_UpdateCommentTrigger.sql"
```

Flaget `-b` får `sqlcmd` til at stoppe med en fejlkode, hvis et script fejler.
Scripts kan køres igen uden at slette eksisterende data. De tilføjer ikke
demo-brugere, annoncer, beskeder eller kommentarer.

Applikationens connection string sættes i `appsettings.Development.json` eller
med .NET user-secrets. Adgangskoder og miljøspecifikke servernavne må ikke
committes.
