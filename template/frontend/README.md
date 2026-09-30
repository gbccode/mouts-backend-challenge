# Frontend

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 20.3.37.

## Development server

To start a local development server, run:

```bash
npm start
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

`npm start` enables `proxy.conf.json`. All services use relative `/api/...` URLs,
so the same code also works through the existing Docker Nginx proxy.

## Sales and authentication API layer

The `src/app/core` folder contains the sale/auth models, HTTP services, functional
authentication interceptor and route guard. The interceptor is registered in
`app.config.ts`.

The models follow the backend DTOs: dates are ISO strings, GUIDs are strings,
discount rates are fractions (0.20 means 20%), and money uses numbers for display.
The backend calculates sale numbers, discounts and totals. Create requests use
`SaleInput`; updates use `UpdateSaleInput`, whose existing items retain their IDs
and whose new items can omit `id` or set it to `null`.

| Method | Endpoint | Response |
| --- | --- | --- |
| `AuthService.login({ email, password })` | `POST /api/Auth` | `ApiResponse<AuthResult>` |
| `SalesService.getById(id)` | `GET /api/sales/{id}` | `ApiResponse<Sale>` |
| `create(input)` | `POST /api/sales` | `ApiResponse<Sale>` (201) |
| `update(id, input)` | `PUT /api/sales/{id}` | `ApiResponse<Sale>` |
| `delete(id)` | `DELETE /api/sales/{id}` | No content (204) |
| `cancel(id)` | `PATCH /api/sales/{id}/cancel` | `ApiResponse<Sale>` |
| `cancelItem(saleId, itemId)` | `PATCH /api/sales/{saleId}/items/{itemId}/cancel` | `ApiResponse<Sale>` |
| `list(page, size, order, filters)` | `GET /api/sales` | Prepared client only; backend route is pending |

Responses retain `success`, `message`, `errors` and nullable `data`. HTTP failures
remain Angular `HttpErrorResponse` objects; the current backend does not use one
consistent error shape for all endpoints (for example, invalid login input returns
validation errors directly).

### Login and protected routes

Subscribe to `AuthService.login(...)` from the future login component. A successful
response stores the JWT in `sessionStorage` for the current browser tab. The service
restores it on reload, rejects malformed/expired tokens and exposes `getToken()`,
`isAuthenticated()` and `logout()`. If storage is blocked, the session lasts in memory
until reload. No password is stored. Local expiry checks do not verify JWT signatures;
that remains the backend's responsibility.

The interceptor adds `Authorization: Bearer <token>` only to relative `/api/` requests,
excluding login. It does not attach the token to external URLs or asset requests.

The create, edit and details routes are available. A login page is still pending;
these routes are currently unguarded. When adding login:

1. Add an unguarded `/login` route.
2. Import `authGuard` from `./core/guards/auth.guard` and add
   `canActivate: [authGuard]` to the sales routes.
3. After login, navigate to an internal `returnUrl` or `/sales/new` by default. The guard
   includes the attempted URL in the login redirect's `returnUrl` query parameter.
4. Handle HTTP errors in the UI. If a token is rejected with 401, log out and return
   to login. There is no refresh-token endpoint in the current backend.

Add the guard after `/login` exists so redirects have a working destination.
Server authorization is separate: `SalesController` currently has no `[Authorize]` attribute.

## Create and edit sales

| Route | Screen |
| --- | --- |
| `/` or `/sales/new` | New sale with an empty header and one item |
| `/sales/:id/edit` | The same standalone `SaleFormComponent`, populated from the API |
| `/sales/:id` | Saved sale details, fetched from the API on navigation and refresh |

The Reactive Form contains the sale date, customer ID/name, branch ID/name and
a typed `FormArray` of item groups. Selects populate both the external ID and its
name. Validation requires at least one active item, distinct product IDs,
integer quantities from 1 through 20, and positive prices with at most two decimal
places. Invalid submission marks every control as touched. Save and the form are
disabled during requests; HTTP errors preserve your input and re-enable Save.

Dates use the browser's local timezone in `datetime-local` inputs. Saving converts
them to UTC with `new Date(value).toISOString()`. Editing reconstructs local calendar
fields from the API timestamp, preserving seconds and milliseconds. It never labels
a sliced UTC timestamp as local time.

Only active items are editable. Existing item IDs are sent on updates; new items use
`id: null`. Omitted existing rows are cancelled by the backend on save. Cancelled
items appear in a separate read-only section and are never sent as active inputs.
An existing row's product is fixed because the backend's `SaleItem.Update` does not
change `ProductId`; remove the row and add a new one to replace the product.
Cancelled sales cannot be edited. Backend DTOs and domain rules continue to enforce
integer quantities and duplicate-product restrictions independently of the UI.

The form sends only the agreed input fields. Sale numbers, discounts and totals
come from the backend. There is no client-side pricing estimate. After saving,
the details route reloads the sale and shows its saved discount rates, amounts
and total. Cancelled item amounts are historical and excluded from the sale total.

### Demo external references

`src/app/features/sales/sales.fixtures.ts` defines typed, stable demo references:

- Customers: Acme Market and Central Grocery (`10000000-...` IDs).
- Branches: Blumenau and Joinville (`20000000-...` IDs).
- Products: Beverage case (100), Sparkling water case (25), Juice case (75)
  (`30000000-...` IDs). These prices are editable defaults.

These lists stand in for external customer/product/branch services. They do not
mock, seed or store sales, and no separate customer/product CRUD is included.
Every save uses the real API. When editing an existing sale, references absent
from these lists retain their saved IDs and names.

### Persistence checkpoint with Docker on Windows

From `C:\dev\mouts-backend-challenge\template\backend` in PowerShell:

```powershell
docker compose -f .\compose.local.yml up -d --build frontend
```

1. Open `http://localhost:4200/sales/new`.
2. Enter a date/time; choose **Acme Market**, **Blumenau**, and **Beverage case**.
3. Set **Quantity = 10** and **Unit price = 100**, then click **Save sale**.
4. On `/sales/<id>`, confirm **Saved total = 800.00**, discount **20% / 200.00**.
5. Refresh that URL. The screen issues `GET /api/sales/<id>` and must still show
   **800.00**. This is the real database persistence checkpoint.
6. Click **Edit sale**, change quantity to **4**, save, and refresh. The expected
   saved total is **360.00**, with a **10% / 40.00** discount.

For Angular running outside Docker, `npm start` uses the existing proxy target
`http://localhost:5119`. If the API runs only in Docker at port 8080, adjust that
local proxy target before starting Angular. Docker Nginx already forwards to `api:8080`.

Form and details tests mock HTTP to check request mapping, error handling and
refetching. They do not replace the real database checkpoint above. Date round-trip
tests should also run in a non-UTC timezone, such as `America/Sao_Paulo`.

### Pending collection endpoint

`SalesController` currently has no collection `GET` action. `list()` prepares `_page`,
`_size` and `_order` query parameters plus an optional trimmed `searchTerm`:

```typescript
salesService.list(1, 10, 'saleDate desc', { searchTerm: 'Main branch' });
```

`searchTerm` is the only filter currently implemented in `ISaleRepository.GetPageAsync`;
it matches sale number, customer name or branch name. It is not yet exposed over HTTP.
The repository currently uses fixed sale-date descending ordering, so `_order` also
needs backend implementation. No unsupported field/range filters are advertised here.

`PagedResponse<T>` matches the checked-in `WebApi/Common/PaginatedResponse<T>` class,
which uses `totalCount`. The challenge's generic API examples use `totalItems` instead.
Resolve this naming difference when implementing the list endpoint and update the
client type/test together if its public response uses `totalItems`.

To complete listing, add `GET /api/sales`, bind the query parameters, map repository
sales to `SaleResult` (including items), and return the agreed pagination envelope.
The frontend HTTP tests use mocked responses and do not claim this route exists.

### Verify the API layer

```bash
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

The tests cover sale payloads and server totals, cancellation and 204 deletion,
list parameter encoding, login persistence/expiry, token destination restrictions,
and guard redirects. Headless tests require Chrome (or `CHROME_BIN` set to Chromium).

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
