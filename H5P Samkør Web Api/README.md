# SamKør: Samkørselsplatform

Svendeprøveprojekt: en webbaseret samkørselsplatform, hvor brugere kan oprette og finde ture, booke ledige pladser, kommunikere med medrejsende og bedømme hinanden efter en gennemført tur.

## Teknologistak

| Lag | Teknologi |
|---|---|
| Frontend | Angular (standalone components, signals) |
| Backend | .NET Core Web API (C#) |
| Database | Microsoft SQL Server (LocalDB i udvikling) |
| ORM | Entity Framework Core |
| Autentificering | ASP.NET Core Identity + JWT |
| Realtid | SignalR (chat) |
| Automatiske tests | xUnit (backend), Vitest (frontend) |

## Status: projektet er funktionelt færdigt

Samtlige krav fra kravspecifikationen er implementeret i både backend og frontend. Nedenfor er et overblik over de enkelte dele.

### 1. Databaselag
- Datamodellen er defineret som EF Core-entiteter i `Models/`: `User` (arver fra `IdentityUser<Guid>`), `Vehicle`, `Trip`, `Booking`, `Message` og `Rating`
- `Trip.RowVersion` som concurrency-token, så to samtidige godkendelser af booking ikke begge kan nedjustere ledige pladser på samme tid
- `DeleteBehavior.Restrict` på de fleste relationer til `User`, da SQL Server ikke tillader flere cascade-veje ind til samme tabel. Konsekvensen er, at en bruger med tilknyttet data ikke kan slettes permanent, kun deaktiveres (se afsnit 10)
- Unikt indeks på `Rating (TripId, RaterId, RateeId)`, så en bruger kun kan bedømme den samme medrejsende én gang pr. tur
- Al sammenligning af tidspunkter mod "nu" sker via `TripTimeExtensions`, som konverterer til dansk tid (`Europe/Copenhagen`) med korrekt håndtering af sommer-/vintertid, i stedet for at sammenligne direkte mod UTC

### 2. Autentificering (ASP.NET Core Identity)
- `User` arver fra `IdentityUser<Guid>` og får dermed `Email`, `PasswordHash` og indbygget password-hashing fra Identity
- `AppDbContext` arver fra `IdentityDbContext<User, IdentityRole<Guid>, Guid>`
- Roller (`"User"` og `"Administrator"`) samt en standard-administrator seedes automatisk ved opstart (Krav 1 og 8), konfigureret via `SeedAdmin`-sektionen i `appsettings.json`
- `POST /api/Auth/register` og `POST /api/Auth/login` udsteder JWT-tokens via `TokenService`
- Login afvises med `401` for deaktiverede brugere (se admin-dashboardet, afsnit 10)
- Swagger er konfigureret med JWT Bearer-autorisation

### 3. Tur-CRUD (Krav 2 og 3)
- `TripsController`: søgning (`GET /api/Trips`, filtrerer på fra/til/tidligste afgang og ekskluderer fortidige ture), enkelt-visning, oprettelse, redigering og aflysning (kun ejer/admin, ikke ved fuldt booket tur)
- `VehiclesController` og tilknytning af flere køretøjer til en profil er bevidst nedprioriteret (se "Endnu ikke lavet")

### 4. Booking-flow (Krav 4 og 5)
- `BookingsController`: anmodning, godkendelse, afvisning, egne bookinger og modtagne anmodninger
- Godkendelse nedjusterer `Trip.AvailableSeats`; en samtidig, konkurrerende godkendelse af den sidste ledige plads afvises med `409 Conflict` via optimistic concurrency (`Trip.RowVersion`). Dette er bevist med en automatisk test, der sender to reelle, parallelle requests (se afsnit 8)

### 5. Oversigt over ture (Krav 7)
- `GET /api/Trips/mine` samler brugerens ture på tværs af begge roller, opdelt i `planned` og `completed`

### 6. Bedømmelse af medrejsende (Krav 7, tilføjet)
- `RatingsController`: afgivelse af bedømmelse (1-5 stjerner + valgfri kommentar), kun for gennemførte ture man selv deltog i
- En brugers gennemsnitlige bedømmelse opdateres inkrementelt og vises på tur-kort (chaufførens rating) og bookinganmodninger (passagerens rating), så modparten kan træffe et informeret valg, før de booker/godkender

### 7. Chat (Krav 6)
- `ChatHub` (SignalR): realtidsbeskeder tilknyttet en tur, adgang begrænset til chauffør, passagerer med godkendt booking, samt administratorer
- JWT sendes som query-parameter ved forbindelsesopstart, da browsere ikke kan sætte en Authorization-header på en WebSocket-forbindelse
- Beskedhistorik hentes via `GET /api/trips/{tripId}/messages`

### 8. Automatiske tests
- **Backend (xUnit):** 15 integrationstests via `WebApplicationFactory` mod en isoleret testdatabase (`SamKorTestDb`), fordelt på autentificering, tur-validering/autorisation, booking-concurrency/regler, bedømmelse og admin/adgang
- **Frontend (Vitest):** 14 tests af `AuthService`, login-formularens validering, og korrekt skjulning af bookingknappen på egne ture

### 9. Frontend (Angular)
- Standalone components med signals, JWT-interceptor, route guards (`authGuard`, `adminGuard`)
- Sider: login/registrering, tur-søgning (med chaufførens rating synlig), opret/aflys tur, "Mine ture" (planlagt/gennemført, farvekodede status-badges), bookinganmodninger (med passagerens rating synlig), chat, bedømmelse, admin-dashboard
- Genanvendelige komponenter: `ConfirmDialogComponent` (erstatter `confirm()`), `FlatpickrDirective` (stylet dato/dato+tid-vælger)
- Visuel designretning "Fællesskab" (Fraunces/Karla, varm grøn/gul palet), responsivt med mobil sidebar-navigation under 640px

### 10. Administrator-dashboard (Krav 8, udvidet)
- `AdminController`: overblik over alle brugere, ture og bookinger (også fortidige/gennemførte), med søgefelt pr. fane i frontend
- Deaktivering af en bruger via Identity's lockout-mekanisme (ikke permanent sletning, se afsnit 1), med tilhørende genaktivering
- Administrator kan redigere/slette enhver tur eller booking, uanset ejerskab eller fuld booking, og kan tilslutte sig enhver turs chat til moderation

## Endnu ikke lavet

- `VehiclesController` og tilknytning af flere køretøjer til en brugerprofil. Lavt prioriteret, ikke en del af kravspecifikationen
- Mulighed for at booke flere pladser i én booking (fx til en ven). Bevidst fravalgt for at holde datamodellen enkel, dokumenteret som en begrænsning
- Automatiske tests af `ChatHub` (SignalR). Testet manuelt gennem hele udviklingsforløbet, men kræver en mere avanceret testopsætning end de øvrige REST-baserede endpoints

## Kom i gang lokalt

### Backend
1. Klon repositoriet, og åbn `H5P_Samkør_Web_Api`-solutionen i Visual Studio 2026
2. Ret evt. connection string i `appsettings.json`, hvis du ikke bruger LocalDB
3. Åbn **Package Manager Console**, og kør:
   ```powershell
   Update-Database
   ```
4. Kør projektet (F5). Swagger UI åbner automatisk i development-miljø

### Frontend
1. Installér Node.js (LTS) og Angular CLI: `npm install -g @angular/cli`
2. Kør `npm install` i `samkor-web`-mappen
3. Kør `ng serve`, og åbn `http://localhost:4200`
4. Bekræft at CORS er aktiveret i API'et for `http://localhost:4200` (se `Program.cs`)

### Automatiske tests
- **Backend:** `dotnet test` fra solution-roden (se `SamKor.Api.Tests/SETUP.md` for opsætning)
- **Frontend:** `ng test` fra `samkor-web`-mappen

## Arbejdsproces

Projektet udvikles solo og agilt vha. et GitHub Projects-board (Backlog / I gang / Færdig), med opgaver grupperet i milestones pr. udviklingsuge. En opgave anses først for færdig, når den lever op til projektets **Definition of Done**:

- Koden er merget til `main` gennem en pull request
- Der er foretaget review af koden (selv-review, da projektet er solo)
- Testparametrene fra kravspecifikationen er kørt, og resultatet er noteret
- Automatiske tests er grønne
- README for komponenten er opdateret