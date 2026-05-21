import { Injectable } from "@angular/core";
import { HttpErrorResponse } from "@angular/common/http";
import { ToastService } from "./toast.service";

/** Centralised HTTP error handler used by components. */
@Injectable({ providedIn: "root" })
export class ErrorHandlerService {
  constructor(private toast: ToastService) {}

  handle(err: unknown, fallback = "An unexpected error occurred."): void {
    if (err instanceof HttpErrorResponse) {
      const msg = err.error?.message ?? err.error?.Message ?? fallback;
      const code = err.error?.code ?? "";
      this.toast.error(code ? `[${code}] ${msg}` : msg);
    } else if (err instanceof Error) {
      this.toast.error(err.message || fallback);
    } else {
      this.toast.error(fallback);
    }
  }
}
