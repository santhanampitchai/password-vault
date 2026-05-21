import { Component, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterModule, RouterOutlet } from "@angular/router";
import { MatSidenavModule } from "@angular/material/sidenav";
import { MatToolbarModule } from "@angular/material/toolbar";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatListModule } from "@angular/material/list";
import { MatTooltipModule } from "@angular/material/tooltip";
import { MatMenuModule } from "@angular/material/menu";
import { BreakpointObserver } from "@angular/cdk/layout";
import { AuthService } from "../../../core/services/auth.service";
import { ThemeService } from "../../../core/services/theme.service";

@Component({
  selector: "app-shell",
  templateUrl: "./shell.component.html",
  styleUrls: ["./shell.component.css"],
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    RouterOutlet,
    MatSidenavModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatTooltipModule,
    MatMenuModule,
  ],
})
export class ShellComponent {
  isMobile = signal(false);
  constructor(
    public auth: AuthService,
    public theme: ThemeService,
    bp: BreakpointObserver,
  ) {
    bp.observe(["(max-width: 768px)"]).subscribe((s) =>
      this.isMobile.set(s.matches),
    );
  }
  userInitial(): string {
    return (this.auth.currentUser()?.fullName ?? "U")[0].toUpperCase();
  }
}
