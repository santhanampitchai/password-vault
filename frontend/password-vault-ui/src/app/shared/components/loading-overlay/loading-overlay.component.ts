import { Component, Input } from "@angular/core";
import { CommonModule } from "@angular/common";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";

@Component({
  selector: "app-loading-overlay",
  templateUrl: "./loading-overlay.component.html",
  styleUrls: ["./loading-overlay.component.css"],
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
})
export class LoadingOverlayComponent {
  @Input() visible = false;
  @Input() message = "";
}
