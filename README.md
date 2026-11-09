# RaceDay – Event and Race Management System
<img width="1920" height="1080" alt="MVC Event" src="https://github.com/user-attachments/assets/c4762797-69c0-446f-883c-a7ae10e6e0f0" />
<img width="1920" height="1080" alt="build-and-test-part3" src="https://github.com/user-attachments/assets/1161f9b2-04a3-461b-a6b0-9c9d66248591" />
<img width="1920" height="1080" alt="Docker Swagger" src="https://github.com/user-attachments/assets/20e0e4a8-4072-4335-9051-a1501950875a" />
<img width="1920" height="1080" alt="02_GitHub_CI_Automated_Tests_60_Passed" src="https://github.com/user-attachments/assets/38226065-4c88-41bd-a038-b40caee33d17" />
<img width="1920" height="1080" alt="raceday-api-1 and raceday-mvc-1" src="https://github.com/user-attachments/assets/7c295c3a-17c9-4776-9086-f0bf92372f41" />
Module: PROG6212 — Programming Portfolio of Evidence, Part 3
Technology: ASP.NET Core MVC, ASP.NET Core REST API, C#, .NET 10, SQL Server
Repository: Samukelisiwe2/RaceDay_POE-Part3-
Development branch: poe-part3-development
youtube link: https://youtu.be/dtgkpIjTQec
GitHub link:https://github.com/Samukelisiwe2/RaceDay_POE-Part3-.git

The video walks through the MVC website layout, Organiser and Participant functionality, API integration, Azure Blob Storage, Docker, and automated testing.
1. Introduction
RaceDay is a student-built event and race management application. Organisers create events and race categories, track registrations, and capture results. Participants browse events, enrol in races, and view their registrations and results. The project was developed through successive POE parts, building on the existing application rather than starting over.

For Part 3, the solution contains a separate ASP.NET Core MVC frontend that communicates with the existing REST API. The system also demonstrates Azure Blob Storage for event banners, Docker containerisation, SQL Server integration, automated testing, and GitHub Actions continuous integration.
2. Architecture
Browser
   |
   v
RaceDay.Part3 (ASP.NET Core MVC)
   | HTTP requests + authenticated API session
   v
RaceDay (Part 2 REST API)
   |                         |
   v                         v
SQL Server / EF Core     Azure Blob Storage
RaceDayDb                Private event-banners container
The frontend uses a named HttpClient to call the API. API endpoints enforce authentication, authorisation and event ownership checks. The backend uses Entity Framework Core and SQL Server for persistent event, user, enrolment and result data. Event banner files are stored privately in Azure Blob Storage; the associated blob name is recorded in the event database record.
3. Solution projects
Project	Responsibility
RaceDay	Part 2 backend, REST endpoints, Identity, SQL Server and Azure integration
RaceDay.Part3	MVC website, Razor views and API client services
RaceDay.Tests	xUnit tests and integration tests using an isolated test database


Core technologies include .NET 10, C#, Razor, Entity Framework Core, ASP.NET Core Identity, Swagger / OpenAPI, Azure Storage Blobs, Docker Desktop, Docker Compose, xUnit and GitHub Actions.
4. User roles and functionality
Administrator: reviews role requests and manages authorised access.

Organiser: maintains their profile, creates and edits events, manages event categories, views registrations, captures and edits results, and uploads banners to events they own. Restrictions prevent unauthorised changes to other organisers' events.

Participant: maintains their profile, browses events, enrols in eligible race categories, checks their enrolments and views results.

Event information includes event name, description, type, location, date, registration closing date and status. Categories and enrolments are validated for business rules such as duplicates, capacity and registration state. Payment-related records are part of the data model; a live payment gateway is not claimed.
5. Running locally in Visual Studio
Prerequisites
- Visual Studio with ASP.NET development workload, or a compatible editor
- .NET 10 SDK
- SQL Server / SQL Server Express and a configured development database
- Git
- Azure Blob Storage configuration if testing image features
Clone and build:
git clone https://github.com/Samukelisiwe2/RaceDay_POE-Part3-.git
cd RaceDay_POE-Part3-
dotnet restore RaceDay.slnx
dotnet build RaceDay.slnx
Configure ConnectionStrings:DefaultConnection using local development settings or ASP.NET Core User Secrets. Apply EF Core migrations with an account authorised to make schema changes. Do not commit passwords or Azure connection strings.

Configure multiple startup projects in Visual Studio for RaceDay and RaceDay.Part3. The local URLs used for development were:
- API Swagger: https://localhost:7278/swagger/index.html
- Part 3 MVC website: https://localhost:7074
Actual URLs may differ based on local launch profiles.
6. Docker Compose setup
The repository contains Dockerfile, RaceDay.Part3/Dockerfile and docker-compose.yml. Docker Compose runs the API and MVC website on an internal Docker network. The tested local development configuration connects to an existing Windows SQL Server Express instance rather than starting a database container.
Component	Development address
Docker API / Swagger	http://localhost:8082/swagger/index.html
Docker MVC website	http://localhost:8083
API-to-database	host.docker.internal:14330
MVC-to-API	http://api:8080/


The Docker environment expects RACEDAY_SQL_PASSWORD and, for event image operations, RACEDAY_AZURE_CONNECTION_STRING. Set these privately in the terminal running Compose; they should not appear in tracked files. The Docker API is configured with RaceDay__ApplyMigrations=false to avoid running database schema migrations through its restricted application login.
# After securely setting the required environment variables:
docker compose build
docker compose up -d
docker compose ps

# Inspect startup output when necessary:
docker compose logs --tail 50 api
docker compose logs --tail 50 mvc

# Stop the containers:
docker compose down
Development only: The HTTP endpoints and database certificate trust used in this local container setup are not production deployment settings. SQL Server port access should remain restricted; do not expose it publicly. Docker Data Protection key persistence is a future hardening item.

7. Azure Blob Storage and event banner evidence
The API supports protected event banner uploads and retrieval. Uploaded images are stored in a private event-banners container. The MVC Event Details page can display the associated image from the backend.
7.1 Part 3 event details page
The following screenshot demonstrates the Boxer Super Run Event Details page displaying the stored event banner, event information, and organiser actions.

7.2 Docker Swagger API screenshot
The screenshot below shows the Docker-hosted Swagger interface for POST /api/events/{eventId}/banner at port 8082. The visible response is HTTP 401 Unauthorized, so this particular image is evidence that the protected endpoint is accessible and enforces authentication; it is not evidence of a successful upload. Successful image uploads were separately tested during development.

For successful upload evidence, capture a separate authenticated 201 Created response without exposing credentials or authentication cookies.
8. Automated tests
RaceDay uses xUnit for unit and integration tests, including validation, authentication, enrolments, event management and role authorisation. Isolated SQLite-backed integration tests avoid changing the main SQL Server development database.

Run the tests:
dotnet test RaceDay.Tests/RaceDay.Tests.csproj --configuration Release
Verified on 9 November 2026: 60 tests passed, 0 failed and 0 skipped. Automated tests complement, but do not replace, manual workflow testing.
9. GitHub Actions CI/CD evidence
The RaceDay Build and Tests workflow checks out the repository, sets up .NET 10, restores dependencies, builds the solution and runs automated tests.

Successful workflow: GitHub Actions run #32
9.1 CI build-and-test job
The screenshot shows the successful build-and-test job with green checks for restore, build and automated testing.

9.2 Automated test result: 60 passed
The expanded test output confirms a successful test run of 60 tests and a successful build with no build warnings or errors in the displayed test step.

The successful run also displayed an unrelated GitHub Actions runtime deprecation warning and Ubuntu runner migration notice. These did not fail the CI job.
10. Security
The application uses ASP.NET Core Identity, authenticated API sessions, role-based permissions and event ownership restrictions. Azure event images are stored in a private container. Database and Azure credentials must be supplied through private developer configuration rather than committed files.

This is an academic development system, not a production-hardened deployment. Persisting Data Protection keys, production HTTPS/certificate configuration and further security reviews are recommended before real-world deployment.
11. POE evidence index
Evidence file	Demonstrates
POE_Evidence/CI_CD/build-and-test-part3.png	Successful GitHub Actions build-and-test job
POE_Evidence/CI_CD/02_GitHub_CI_Automated_Tests_60_Passed.png	60/60 automated tests passing in GitHub Actions
POE_Evidence/Docker/Docker Swagger.png	Docker-hosted protected API endpoint; 401 response shown
POE_Evidence/MVC/MVC Event.png	Working MVC Event Details page with banner


Additional evidence to collect or confirm before final submission includes the Docker Desktop containers list showing both containers running, fully authenticated Azure upload proof, final Organiser and Participant end-to-end checks, and any written report required by the rubric. The project demonstration video is linked above.
12. Challenges and lessons learned
Part 3 required connecting a separate MVC frontend to a protected REST API and coordinating authenticated user sessions. Azure Blob Storage required upload validation, private blob retrieval and event-to-image persistence. Docker introduced WSL 2, virtualization, container networking and SQL Server authentication challenges. GitHub Actions and xUnit provided repeatable evidence that the solution builds and its current automated tests pass.
13. Future improvements
- Persist Data Protection keys across container recreation.
- Add further end-to-end, accessibility and security regression tests.
- Use production-grade HTTPS and secret management for deployment.
- Extend operational monitoring and reporting.
- Introduce a live payment provider only after explicit implementation and verification.
14. Conclusion
RaceDay Part 3 extends the event and race management application with a separate MVC interface, API integration, cloud-based image storage, Docker containerisation and CI testing. The documented test evidence shows 60 passing tests locally and on GitHub Actions, and the development environment demonstrates functioning API and MVC workflows.
PROG6212 — Portfolio of Evidence, Part 3
Repository: https://github.com/Samukelisiwe2/RaceDay_POE-Part3-
