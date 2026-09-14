# SamKør – Samkørselsplatform

Svendeprøveprojekt: en webbaseret samkørselsplatform, hvor brugere kan oprette og finde ture, booke ledige pladser, kommunikere med medrejsende og bedømme hinanden efter en gennemført tur.

## Teknologistak

| Lag | Teknologi |
|---|---|
| Frontend | Angular |
| Backend | .NET Core Web API (C#) |
| Database | Microsoft SQL Server (LocalDB i udvikling) |
| ORM | Entity Framework Core |
| Autentificering | ASP.NET Core Identity + JWT |
| Realtid | SignalR (chat) – endnu ikke implementeret |

## Status – hvad er lavet indtil videre

### 1. Databaselag
- Datamodellen er defineret som EF Core-entiteter i `Models/`:
  - `User` – arver fra `IdentityUser<Guid>` (se afsnittet om Identity nedenfor)
  - `Vehicle` – en bruger kan eje flere biler
  - `Trip` – en tur oprettet af en chauffør, med fra-/til-lokation, tidspunkt, ledige pladser og pris
  - `Booking` – en passagers anmodning om en plads på en tur, med status (`Pending`, `Accepted`, `Rejected`, `Cancelled`)
  - `Message` – beskeder i chatten tilknyttet en tur
  - `Rating` – bedømmelse af en medrejsende efter en gennemført tur (1-5 stjerner + valgfri kommentar)
- Relationerne og constraints er konfigureret i `Data/AppDbContext.cs`, bl.a.:
  - `Trip.RowVersion` som concurrency-token, så to samtidige godkendelser af booking ikke begge kan nedjustere ledige pladser på samme tid
  - `DeleteBehavior.Restrict` på de fleste relationer til `User`, da SQL Server ikke tillader flere cascade-veje ind til samme tabel
  - Unikt indeks på `Rating (TripId, RaterId, RateeId)`, så en bruger kun kan bedømme den samme medrejsende én gang pr. tur

### 2. Autentificering (ASP.NET Core Identity)
- `User` arver fra `IdentityUser<Guid>` og får dermed `Email`, `PasswordHash` og `PhoneNumber` samt indbygget password-hashing fra Identity
- `AppDbContext` arver fra `IdentityDbContext<User, IdentityRole<Guid>, Guid>`, hvilket automatisk tilføjer Identity-tabellerne (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` m.fl.)
- Roller ("User" og "Administrator") håndteres via Identity's eget rollesystem, i stedet for et felt på `User`
- JWT-bearer authentication er sat op i `Program.cs`, klar til at udstede og validere tokens, når login-endpointet bygges
- Konfiguration (connection string, JWT-nøgle, issuer/audience) ligger i `appsettings.json`

### 3. Database oprettet
- Første migration (`InitialCreate`) er kørt via Package Manager Console (`Add-Migration`, `Update-Database`)
- Databasen `SamKorDb` kører lokalt på `(localdb)\mssqllocaldb` og indeholder både Identity-tabellerne og projektets egne tabeller

## Endnu ikke lavet

- Registrering og login-endpoints (udsteder JWT)
- Seeding af standard-administrator og roller ved opstart
- CRUD for ture (oprette/redigere/aflyse)
- Søgning efter ture
- Booking-flow (anmode, godkende/afvise)
- Chat (SignalR)
- Bedømmelse af medrejsende (Krav 7)
- Angular-frontend

## Kom i gang lokalt

1. Klon repositoriet og åbn solutionen i Visual Studio 2026
2. Ret evt. connection string i `appsettings.json`, hvis du ikke bruger LocalDB
3. Åbn **Package Manager Console** og kør:
   ```powershell
   Update-Database
   ```
4. Kør projektet (F5) – Swagger UI åbner automatisk i development-miljø

## Arbejdsproces

Projektet udvikles solo og agilt vha. et GitHub Projects-board (Backlog / I gang / Færdig), med opgaver grupperet i milestones pr. udviklingsuge. En opgave anses først for færdig, når den lever op til projektets **Definition of Done**:

- Koden er merget til `main` gennem en pull request
- Der er foretaget review af koden (selv-review, da projektet er solo)
- Testparametrene fra kravspecifikationen er kørt, og resultatet er noteret
- Automatiske tests er grønne
- README for komponenten er opdateret
