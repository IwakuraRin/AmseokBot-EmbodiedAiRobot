# OpenAPI contract

The API will generate its OpenAPI document from ASP.NET Core. Generated TypeScript clients belong under `apps/web/src/app/api-client/generated` and must not be edited manually.

The browser communicates with `apps/api` only. It never calls the privileged Agent directly.
