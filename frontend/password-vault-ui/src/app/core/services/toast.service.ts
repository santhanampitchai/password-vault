import { Injectable } from "@angular/core";
import { MatSnackBar } from "@angular/material/snack-bar";

@Injectable({ providedIn: "root" })
export class ToastService {
  constructor(private snack: MatSnackBar) {}

  success(msg: string): void {
    this.snack.open(msg, "Close", {
      duration: 3000,
      panelClass: ["toast-success"],
      horizontalPosition: "end",
      verticalPosition: "top",
    });
  }

  error(msg: string): void {
    this.snack.open(msg, "Close", {
      duration: 5000,
      panelClass: ["toast-error"],
      horizontalPosition: "end",
      verticalPosition: "top",
    });
  }

  info(msg: string): void {
    this.snack.open(msg, "Close", {
      duration: 3000,
      panelClass: ["toast-info"],
      horizontalPosition: "end",
      verticalPosition: "top",
    });
  }
}
