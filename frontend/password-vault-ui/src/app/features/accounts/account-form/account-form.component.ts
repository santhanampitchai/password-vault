import { Component, OnInit } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ReactiveFormsModule, FormBuilder, Validators } from "@angular/forms";
import { RouterModule, Router, ActivatedRoute } from "@angular/router";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatSelectModule } from "@angular/material/select";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatCardModule } from "@angular/material/card";
import { MatTooltipModule } from "@angular/material/tooltip";
import { AccountService } from "../../../core/services/account.service";
import { ToastService } from "../../../core/services/toast.service";

function pwdStrength(p: string): { pct: number; color: string; label: string } {
  let s = 0;
  if (p.length >= 8) s++;
  if (p.length >= 12) s++;
  if (/[A-Z]/.test(p)) s++;
  if (/\d/.test(p)) s++;
  if (/\W/.test(p)) s++;
  const map: [string, string][] = [
    ["Very Weak", "#f44336"],
    ["Weak", "#ff5722"],
    ["Fair", "#ff9800"],
    ["Good", "#8bc34a"],
    ["Strong", "#4caf50"],
    ["Very Strong", "#00bcd4"],
  ];
  return { pct: s * 20, color: map[s][1], label: map[s][0] };
}

@Component({
  selector: "app-account-form",
  templateUrl: "./account-form.component.html",
  styleUrls: ["./account-form.component.css"],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatCardModule,
    MatTooltipModule,
  ],
})
export class AccountFormComponent implements OnInit {
  isEdit = false;
  saving = false;
  showPwd = false;
  strength = { pct: 0, color: "#888", label: "" };
  editId?: number;

  categories = [
    "Social",
    "Banking",
    "Work",
    "Email",
    "Shopping",
    "Entertainment",
    "Other",
  ];

  form = this.fb.group({
    accountName: ["", Validators.required],
    userName: ["", Validators.required],
    password: ["", Validators.required],
    otherInfo: [""],
    category: [""],
    websiteUrl: [""],
  });

  constructor(
    private fb: FormBuilder,
    private accountSvc: AccountService,
    private toast: ToastService,
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("id");
    if (id) {
      this.isEdit = true;
      this.editId = +id;
      this.loadAccount(this.editId);
    }
  }

  private loadAccount(id: number): void {
    this.accountSvc.revealPassword(id).subscribe({
      next: async (r) => {
        const pwd = await this.accountSvc.decryptPassword(r.data);
        this.form.patchValue({
          accountName: r.data.accountName,
          userName: r.data.userName,
          password: pwd,
          category: r.data.category ?? "",
          websiteUrl: r.data.websiteUrl ?? "",
          otherInfo: "", // Server returns encrypted; UX note
        });
        this.updateStrength();
      },
      error: () => this.toast.error("Could not load account."),
    });
  }

  updateStrength(): void {
    this.strength = pwdStrength(this.form.get("password")?.value ?? "");
  }

  generatePassword(): void {
    const chars =
      "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%^&*";
    const arr = crypto.getRandomValues(new Uint8Array(20));
    const pwd = Array.from(arr, (b) => chars[b % chars.length]).join("");
    this.form.get("password")!.setValue(pwd);
    this.showPwd = true;
    this.updateStrength();
  }

  copyField(field: string, label: string): void {
    const val = this.form.get(field)?.value ?? "";
    if (val) {
      navigator.clipboard.writeText(val);
      this.toast.success(`${label} copied!`);
    }
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving = true;
    const { accountName, userName, password, otherInfo, category, websiteUrl } =
      this.form.value;
    const obs = this.isEdit
      ? this.accountSvc.updateAccount(
          this.editId!,
          accountName!,
          userName!,
          password!,
          otherInfo || null,
          category || null,
          websiteUrl || null,
        )
      : this.accountSvc.createAccount(
          accountName!,
          userName!,
          password!,
          otherInfo || null,
          category || null,
          websiteUrl || null,
        );

    obs.subscribe({
      next: () => {
        this.toast.success(this.isEdit ? "Account updated." : "Account saved.");
        this.router.navigate(["/accounts"]);
      },
      error: (e) => {
        this.saving = false;
        this.toast.error(e.error?.message ?? "Save failed.");
      },
    });
  }
}
