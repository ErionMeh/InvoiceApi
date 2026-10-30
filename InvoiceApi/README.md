# Invoice API

A small ASP.NET Core Web API for an invoicing flow with three roles: finance, postman and client. Finance creates invoices, a postman delivers them, and the client pays them, fully or in parts.

## How it works

1. **Finance** creates an invoice for a client and assigns it to a postman.
2. The **postman** sees only his assigned invoices with the delivery address and marks them as delivered.
3. After delivery, the invoice appears in the **client's** pending list. A client only sees their own invoices.
4. The client pays the full amount or part of it. Finance can see every payment.
5. When an invoice is fully paid, it leaves the pending list.

Every invoice can also be downloaded as a PDF.

## Built with

C#, .NET 8, ASP.NET Core Web API, Entity Framework Core, SQL Server (LocalDB), QuestPDF, Swagger, and a simple page in plain HTML, CSS and JavaScript.

## Rules

- An invoice goes through these statuses: Created, Assigned, Delivered, PartiallyPaid, Paid.
- Only finance can create invoices and assign them, and only to a user with the Postman role.
- Only the assigned postman can mark an invoice as delivered, and only if it was assigned.
- Only the invoice's own client can pay, and only after delivery.
- A payment must be greater than zero and cannot be more than the remaining amount.
- Errors return a clear message: 400 for a broken rule, 403 if the user is not allowed, 404 if something doesn't exist.

## Main endpoints

The user who does the action is passed as `userId` in the URL.

- `POST /api/invoices?userId=1` creates an invoice
- `GET /api/invoices?userId=1` lists all invoices (finance)
- `GET /api/invoices/{id}?userId=...` gets one invoice
- `PUT /api/invoices/{id}/assign?userId=1` assigns a postman
- `PUT /api/invoices/{id}/deliver?userId=2` marks it as delivered
- `POST /api/invoices/{id}/payments?userId=3` records a payment
- `GET /api/invoices/{id}/pdf?userId=...` downloads the PDF
- `GET /api/postmen/{id}/invoices` shows a postman's invoices
- `GET /api/clients/{id}/invoices?pending=true` shows a client's invoices

## Test users

The database starts with four users:

- 1: Finance Admin (Finance)
- 2: Postman One (Postman)
- 3: Client One (Client)
- 4: Client Two (Client)

## How to run

Requirements: Visual Studio 2022 with the ASP.NET workload, the .NET 8 SDK and SQL Server LocalDB.

1. Open the solution in Visual Studio.
2. The connection string in `appsettings.json` uses `(localdb)\MSSQLLocalDB` with Windows authentication, so no password is needed.
3. Open **Tools → NuGet Package Manager → Package Manager Console** and run `Update-Database` to create the `InvoiceDb` database and the test users.
4. Press the green play button. Swagger opens in the browser. To use the web page, open the root address, for example `https://localhost:7017/`, and choose a user in the "Logged in as" dropdown.

## How the code is organized

- `Controllers/` handles the HTTP requests and returns the status codes.
- `Services/` has the business rules (assign, deliver, pay) and the PDF generator.
- `Dtos/` holds the shapes of the data going in and out.
- `Models/` holds the classes that become tables: User, Invoice, Payment.
- `Data/AppDbContext.cs` connects the code to SQL Server through EF Core.
- `Filters/` has one filter that turns my custom exceptions into the right HTTP responses.
- `wwwroot/` has the simple web page.

## Notes and limitations

- There is no real login. The user is chosen with `userId`, and the role is checked in the service. This keeps the project simple, but a real version would use authentication.
- The delivery address is the client's address.
- No unit tests yet.