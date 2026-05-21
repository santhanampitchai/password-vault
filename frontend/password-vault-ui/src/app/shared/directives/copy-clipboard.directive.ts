import { Directive, HostListener, Input, inject } from "@angular/core";
import { ToastService } from "../../core/services/toast.service";

/** Usage: <button appCopyClipboard="text to copy">Copy</button> */
@Directive({
  selector: "[appCopyClipboard]",
  standalone: true,
})
export class CopyClipboardDirective {
  @Input("appCopyClipboard") text = "";
  @Input() copyLabel = "Text";

  private toast = inject(ToastService);

  @HostListener("click")
  onClick(): void {
    if (!this.text) return;
    navigator.clipboard.writeText(this.text).then(() => {
      this.toast.success(`${this.copyLabel} copied!`);
    });
  }
}
