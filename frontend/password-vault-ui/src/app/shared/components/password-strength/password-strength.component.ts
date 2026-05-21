import { Component, Input, OnChanges } from "@angular/core";
import { CommonModule } from "@angular/common";
import { MatProgressBarModule } from "@angular/material/progress-bar";

interface Strength {
  score: number;
  pct: number;
  label: string;
  color: string;
}

function evaluate(pwd: string): Strength {
  let s = 0;
  if (pwd.length >= 8) s++;
  if (pwd.length >= 12) s++;
  if (/[A-Z]/.test(pwd)) s++;
  if (/[0-9]/.test(pwd)) s++;
  if (/[^A-Za-z0-9]/.test(pwd)) s++;
  const map: [string, string][] = [
    ["Very Weak", "#f44336"],
    ["Weak", "#ff5722"],
    ["Fair", "#ff9800"],
    ["Good", "#8bc34a"],
    ["Strong", "#4caf50"],
    ["Very Strong", "#00bcd4"],
  ];
  return { score: s, pct: s * 20, label: map[s][0], color: map[s][1] };
}

@Component({
  selector: "app-password-strength",
  templateUrl: "./password-strength.component.html",
  styleUrls: ["./password-strength.component.css"],
  standalone: true,
  imports: [CommonModule, MatProgressBarModule],
})
export class PasswordStrengthComponent implements OnChanges {
  @Input() password = "";
  str: Strength = evaluate("");

  ngOnChanges(): void {
    this.str = evaluate(this.password);
  }

  hints(): string {
    const tips: string[] = [];
    if (this.password.length < 12) tips.push("length ≥12");
    if (!/[A-Z]/.test(this.password)) tips.push("uppercase");
    if (!/[0-9]/.test(this.password)) tips.push("number");
    if (!/[^A-Za-z0-9]/.test(this.password)) tips.push("symbol");
    return tips.slice(0, 2).join(", ");
  }
}
