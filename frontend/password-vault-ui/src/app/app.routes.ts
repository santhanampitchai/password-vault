import { Routes } from "@angular/router";
import { authGuard, guestGuard } from "./core/guards/auth.guard";
import { ShellComponent } from "./shared/components/shell/shell.component";

export const routes: Routes = [
  {
    path: "auth",
    canActivate: [guestGuard],
    children: [
      {
        path: "login",
        loadComponent: () =>
          import("./features/auth/login/login.component").then(
            (m) => m.LoginComponent,
          ),
      },
      {
        path: "register",
        loadComponent: () =>
          import("./features/auth/register/register.component").then(
            (m) => m.RegisterComponent,
          ),
      },
      { path: "", redirectTo: "login", pathMatch: "full" },
    ],
  },
  {
    path: "",
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: "accounts",
        children: [
          {
            path: "",
            loadComponent: () =>
              import("./features/accounts/account-list/account-list.component").then(
                (m) => m.AccountListComponent,
              ),
          },
          {
            path: "new",
            loadComponent: () =>
              import("./features/accounts/account-form/account-form.component").then(
                (m) => m.AccountFormComponent,
              ),
          },
          {
            path: "edit/:id",
            loadComponent: () =>
              import("./features/accounts/account-form/account-form.component").then(
                (m) => m.AccountFormComponent,
              ),
          },
        ],
      },
      { path: "", redirectTo: "accounts", pathMatch: "full" },
    ],
  },
  { path: "**", redirectTo: "/accounts" },
];
