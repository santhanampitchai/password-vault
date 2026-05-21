import { Component } from "@angular/core";
import { CommonModule } from "@angular/common";
import {
  ReactiveFormsModule,
  FormBuilder,
  Validators,
  AbstractControl,
} from "@angular/forms";
import { RouterModule, Router } from "@angular/router";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { AuthService } from "../../../core/services/auth.service";
import { ToastService } from "../../../core/services/toast.service";

function passwordStrength(pwd: string): {
  score: number;
  label: string;
  color: string;
} {
  let score = 0;
  if (pwd.length >= 8) score++;
  if (pwd.length >= 12) score++;
  if (/[A-Z]/.test(pwd)) score++;
  if (/[0-9]/.test(pwd)) score++;
  if (/[^A-Za-z0-9]/.test(pwd)) score++;
  const map: Record<number, { label: string; color: string }> = {
    0: { label: "Very Weak", color: "#f44336" },
    1: { label: "Weak", color: "#ff5722" },
    2: { label: "Fair", color: "#ff9800" },
    3: { label: "Good", color: "#8bc34a" },
    4: { label: "Strong", color: "#4caf50" },
    5: { label: "Very Strong", color: "#00bcd4" },
  };
  return { score, ...map[score] };
}

@Component({
  selector: "app-register",
  standalone: true,
  templateUrl: "./register.component.html",
  styleUrls: ["./register.component.css"],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
  ],
})
export class RegisterComponent {
  showPwd = false;
  loading = false;
  strength = passwordStrength("");

  form = this.fb.group(
    {
      fullName: ["", Validators.required],
      email: ["", [Validators.required, Validators.email]],
      password: ["", [Validators.required, Validators.minLength(8)]],
      confirmPassword: ["", Validators.required],
    },
    { validators: this.matchPasswords },
  );

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private toast: ToastService,
    private router: Router,
  ) {}

  updateStrength(): void {
    this.strength = passwordStrength(this.form.get("password")?.value ?? "");
  }

  matchPasswords(g: AbstractControl) {
    return g.get("password")?.value === g.get("confirmPassword")?.value
      ? null
      : { mismatch: true };
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const { fullName, email, password } = this.form.value;
    this.auth.register(fullName!, email!, password!).subscribe({
      next: () => {
        this.toast.success("Account created! Welcome to Password Vault.");
        this.router.navigate(["/accounts"]);
      },
      error: (err) => {
        this.loading = false;
        this.toast.error(err.error?.message ?? "Registration failed.");
      },
    });
  }
}
