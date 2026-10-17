# RaceDay – Event and Race Management System
<img width="1920" height="1080" alt="RaceDay_GitHub_Actions_56_Passed" src="https://github.com/user-attachments/assets/f8ec630e-452b-4262-bd2e-858c22bf5ae6" />
<img width="1920" height="1080" alt="raceday_tests" src="https://github.com/user-attachments/assets/4eaa335a-f0e9-47c3-884f-070ca607c0b2" />
**Module:** PROG6212 – Portfolio of Evidence Part 2  
**Developed using:** ASP.NET Core MVC, C#, .NET 10 and SQL Server  
**GitHub Repository:** https://github.com/Samukelisiwe2/RaceDay_Part2
**YouTube**:

## 1. About the Project

RaceDay is a web application I developed to make organising and participating in sporting events easier.

The idea behind the project is to have one system where organisers can create events, manage race categories, keep track of participants and record race results. Participants can register for an account, browse available events, enter races and view their results.

I built the application using ASP.NET Core MVC and C#, with SQL Server storing the information. I also used ASP.NET Core Identity to handle user authentication and make sure people can only access the features allowed by their roles.

For Part 2 of the project, I expanded the application by adding API endpoints documented through Swagger, automated testing with xUnit and continuous integration using GitHub Actions.

## 2. Project Objectives

The main goals of RaceDay are to:

- Make race and event management easier for organisers.
- Allow participants to find and enter available events.
- Provide secure registration and login.
- Keep organiser and participant information organised.
- Prevent duplicate registrations and invalid entries.
- Record race times and finishing positions.
- Use role-based permissions to protect different parts of the system.
- Provide API documentation that can be tested through Swagger.
- Use automated tests to check important application behaviour.

## 3. Technologies Used

| Technology | How I Used It |
|---|---|
| C# | Writing the application logic |
| ASP.NET Core MVC | Building the web application |
| .NET 10 | Application framework |
| Razor Views | Creating the user interface |
| SQL Server | Storing application data |
| Entity Framework Core | Working with the database |
| ASP.NET Core Identity | Registration, login and user roles |
| Swagger / Swashbuckle | Documenting and testing API endpoints |
| xUnit | Writing automated tests |
| SQLite | Running isolated integration tests |
| Git and GitHub | Tracking changes and storing the project |
| GitHub Actions | Automatically building and testing the project |
| Visual Studio | Developing and debugging the application |

## 4. User Roles

RaceDay has three main user roles.

### Administrator

The Administrator manages user role requests. When someone registers and requests to become an Organiser or Participant, the request can be reviewed before the role is approved.

This helps prevent users from giving themselves permissions they shouldn't have.

### Organiser

Organisers are responsible for managing races.

They can create events, edit their own events, manage race categories, view event registrations and record race results.

An Organiser is not supposed to modify another Organiser's events.

### Participant

Participants use RaceDay to find and enter races.

They can maintain their profiles, view available events, select race categories, check their registrations and view their recorded race results.

## 5. Main Features

### 5.1 Registration and Login

I used ASP.NET Core Identity to manage user accounts.

The system supports registration, login, logout, password hashing and authenticated sessions.

When a user registers, they request either the Organiser or Participant role. That role is not automatically approved during registration.

I also added authorization rules to restrict access to protected pages and API operations.

### 5.2 User Profiles

Both Organisers and Participants can view and update their profiles.

A Participant profile stores their name, date of birth, gender, phone number and email address.

An Organiser profile stores the organiser's name, email address and contact number.

The system uses the logged-in account to identify which profile belongs to the user.

### 5.3 Event Management

Organisers can create and manage race events.

Each event contains information such as its name, description, date, location, event type, registration closing date and current status.

Supported event types are Run, Walk and Cycle.

Events can have statuses such as Planned, Open, Closed, Completed or Cancelled.

Ownership checks are used to prevent an Organiser from changing events belonging to someone else.

### 5.4 Race Categories

Each event can have different categories.

For example, an event could offer a 5 km Fun Run and a 10 km Run, each with its own entry fee and participant limit.

For every category, RaceDay stores the category name, distance, fee, maximum capacity and current participant count.

The application also checks for duplicate categories and restrictions around participant capacity.

### 5.5 Event Enrolments

Participants can select an available race category and submit an enrolment.

The system records their selected category, registration date, enrolment status and a generated race number.

RaceDay includes checks for closed registrations, duplicate entries and full categories.

Enrolment statuses include Pending, Confirmed and Cancelled.

### 5.6 Race Results

Organisers can capture race results for registered participants.

Each result can include a start time, finish time, overall finishing position and result status.

Participants can then view their available race results through their accounts.

I tested saving and editing race results during development, including checking that the Participant's results page displays the recorded information.

### 5.7 Payment Records

The database design also includes payment records linked to race enrolments.

Payment statuses include Pending, Paid, Failed and Refunded.

Database constraints and validation rules help maintain payment consistency.

This is payment data management within the project, not a claim that a live payment gateway is connected.

## 6. Database Design

I used SQL Server with Entity Framework Core to manage the RaceDay database.

The main tables cover Organisers, Participants, Events, Race Categories, Enrolments, Payments, Race Results and Role Requests.

ASP.NET Core Identity manages the additional tables needed for user accounts and roles.

Some important relationships are:

- One Organiser can create multiple Events.
- One Event can contain multiple Race Categories.
- One Participant can have multiple Enrolments.
- One Race Category can have multiple Enrolments.
- Each Enrolment belongs to a Participant and a Race Category.
- An Enrolment can have related Payments and one Race Result.

I used foreign keys, unique indexes, check constraints and SQL Server triggers to help keep the database consistent.

Entity Framework Core migrations are used to manage database schema changes.

## 7. Swagger API Documentation

Alongside the MVC application, I added API controllers so the system's functionality can be accessed and tested through HTTP requests.

Swagger provides a way to view the endpoints, enter request information and check the responses without needing to build a separate client application.

The API covers several areas:

| Area | Examples of Functionality |
|---|---|
| Authentication | Registration, login and current user information |
| Profiles | Viewing and updating profiles |
| Events | Retrieving, creating, editing and deleting events |
| Race Categories | Managing categories |
| Enrolments | Viewing registrations and entering races |
| Results | Retrieving and managing race results |

### Example API Endpoints

```text
POST   /api/auth/register
POST   /api/auth/login
GET    /api/auth/me

GET    /api/events
GET    /api/events/{id}
POST   /api/events
PUT    /api/events/{id}
DELETE /api/events/{id}

GET    /api/events/{eventId}/categories
GET    /api/categories/{id}

GET    /api/enrolments/my
GET    /api/events/{eventId}/enrolments
POST   /api/events/{eventId}/enrolments

GET    /api/results/my
GET    /api/events/{eventId}/results

GET    /api/profiles/organiser
PUT    /api/profiles/organiser
GET    /api/profiles/participant
PUT    /api/profiles/participant
```

The API also uses role-based authorization where necessary.

### Opening Swagger

Start the RaceDay application in Development mode and open:

`https://localhost:7278/swagger`

The port may be different depending on the local development settings.

### Swagger Testing

During development, I used Swagger to test successful and unsuccessful requests.

Some of the responses I checked included:

- `200 OK` when retrieving existing records.
- `400 Bad Request` for invalid information.
- `401 Unauthorized` when login details were incorrect.
- `403 Forbidden` for restricted access.
- `404 Not Found` when a requested event did not exist.

These checks helped me confirm that the API was returning appropriate responses for the scenarios tested.

## 8. How to Run RaceDay

### Requirements

Before running the application, you need:

- .NET 10 SDK
- Visual Studio with ASP.NET development support
- SQL Server or SQL Server Express
- Git

### Clone the Repository

```bash
git clone https://github.com/Samukelisiwe2/RaceDay_Part2.git
```

Open `RaceDay.slnx` in Visual Studio.

### Restore Packages

```bash
dotnet restore RaceDay.slnx
```

### Configure the Database

Set up the `DefaultConnection` connection string for your local SQL Server database.

Make sure it points to the correct development database before running migrations.

### Build the Project

```bash
dotnet build RaceDay.slnx
```

### Run the Application

```bash
dotnet run --project RaceDay.csproj
```

Open the localhost address displayed when the application starts.

## 9. Automated Testing

One of the main improvements I made during Part 2 was adding automated tests.

I used xUnit to test important parts of the application, including input validation, authentication-related behaviour, event models, enrolment models and authorization rules.

I also added integration tests that send HTTP requests to selected API endpoints.

These integration tests use a temporary SQLite database, keeping the tests separate from the main SQL Server database.

### Test Breakdown

| Test Class | Number of Tests |
|---|---:|
| ApiIntegrationTests | 7 |
| AuthenticationTests | 7 |
| EnrollmentTests | 12 |
| EventManagementTests | 20 |
| RoleAuthorizationTests | 8 |
| UnitTest1 | 2 |
| **Total** | **56** |

### Running the Tests

From the solution directory, run:

```bash
dotnet test RaceDay.slnx
```

### Local Results

I ran the automated tests using Visual Studio Test Explorer.

**Final verified local result:**

- 56 tests passed
- 0 tests failed
- 0 tests skipped

The tests cover the implemented test cases, although further testing would still be useful for complete end-to-end workflows and authenticated user scenarios.

## 10. GitHub Actions

I configured GitHub Actions so that RaceDay can automatically build and run its tests after code changes are pushed to GitHub.

The workflow is stored in:

`.github/workflows/dotnet-tests.yml`

It checks out the repository, installs the .NET SDK, restores dependencies, builds the solution and executes the automated tests.

### GitHub Actions Results

The workflow completed successfully on the `master` branch.

**Verified results:**

- Build succeeded
- 56 tests passed
- 0 tests failed
- 0 build errors

This was useful because it confirmed that the project and tests could run in a GitHub-hosted environment, not only on my own computer.

**Successful workflow run:**  
https://github.com/Samukelisiwe2/RaceDay_Part2/actions/runs/39073074875

## 11. Security and Validation

Security was an important consideration while developing RaceDay.

I used ASP.NET Core Identity for managing accounts, and role-based authorization to separate Organiser and Participant functionality.

I also included input validation, ownership checks, unique database constraints and anti-forgery protection on relevant MVC forms.

These measures help prevent invalid data and unauthorised actions.

The project has not undergone a full production security audit, so further security testing would be needed before deploying it for real users.

## 12. Evidence for Part 2

The project evidence includes screenshots demonstrating:

- Registration and login functionality.
- Organiser and Participant profiles.
- Event creation and management.
- Race categories and enrolments.
- Capturing and viewing race results.
- Swagger API operations and responses.
- Role-based access restrictions.
- Visual Studio Test Explorer showing 56 passing tests.
- GitHub Actions showing 56 passing tests.

The screenshots provide supporting evidence for the features and tests completed during development.

## 13. Challenges and What I Learned

One of the challenges I faced was getting the integration tests to work with a separate database.

The main application uses SQL Server, while the integration tests use SQLite. Some SQL Server-specific database expressions initially caused errors in the SQLite test environment.

I had to adjust the Entity Framework Core configuration so that the correct SQL expressions were used for each database provider.

I also encountered issues when moving the test project into the GitHub repository. The project references and solution paths had to be corrected before both projects could build together.

Another important part was testing role-based access. Some requests returned `403 Forbidden`, which initially looked like errors, but helped demonstrate that restricted endpoints were enforcing permissions.

Working through these problems helped me understand more about database providers, automated testing, debugging, project references and continuous integration.

## 14. Future Improvements

Although the main coursework functionality has been implemented, there are still areas I would improve in future.

These include more complete end-to-end tests, additional authenticated API integration tests, stronger concurrency handling, better reporting features, interface improvements and integration with a real payment provider.

I would also like to improve how the system displays race statistics and participant progress.

## 15. Conclusion

Developing RaceDay helped me bring together several concepts I've learned in software development, including object-oriented programming, databases, MVC architecture, authentication and testing.

Part 2 gave me the opportunity to expand the working application with API documentation, stronger validation and automated testing.

One of the most useful outcomes was getting all 56 automated tests to pass both locally and through GitHub Actions. This gave me a better understanding of why testing and continuous integration are important when maintaining a software project.

RaceDay is still a student project with room for improvement, but it demonstrates the main features of an event and race management system and the development practices used to build and test it.

---

**Project:** RaceDay  
**Module:** PROG6212  
**Assessment:** Portfolio of Evidence – Part 2  
**Repository:** https://github.com/Samukelisiwe2/RaceDay_Part2
